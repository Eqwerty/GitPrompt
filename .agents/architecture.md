# Cache & Invalidation Design

Read this before touching `src/GitPrompt/Git/GitRepositorySharedCache.cs`, `GitStatusSharedCache.cs`, `SharedCacheUtilities.cs`, or `GitRepositoryLocator.cs`.

## Why a disk cache, not an in-memory one

`gitprompt` is a Native AOT binary invoked as a brand-new process on every single prompt render (see [shell-contract.md](shell-contract.md)). There is no long-lived process to hold an in-memory cache between renders, so both caches below live on disk under the XDG cache directory (`XdgPaths.GetCacheDirectory()`), keyed by an FNV-1a64 hash of the relevant path (`SharedCacheUtilities.HashPath`).

Both caches are strictly best-effort: every read/write path is wrapped in `try/catch` and treats any failure (permission error, corrupt file, race with another shell) as a cache miss or a no-op write. The cache must never break or slow down the prompt — a slow/broken cache should degrade to "call git directly," never to an error.

## Repository-location cache (`GitRepositorySharedCache`)

Avoids re-walking parent directories looking for `.git` on every prompt render (expensive in deep directory trees, e.g. monorepos).

- One file per hashed *start directory* (not per repo) in `repository-cache/`, so every directory visited during a walk gets its own cache entry pointing at the same resolved repo.
- `GitRepositoryLocator.FindRepositoryContext` walks upward from cwd; on each level it first checks the shared cache, and only falls back to a real `.git` existence check on a miss. Whichever way it resolves — a fresh `.git` discovery, or a cache hit found a few levels up — it retroactively writes a cache entry for *every* directory scanned during that walk (`scannedPaths`), not just the final hit — so the next prompt render from any of those directories is an immediate hit.
- TTL comes from `Cache.RepositoryTtl` (default 60s, see `docs/configuration.md`). `0` disables the cache entirely.
- A cache hit is **not** trusted blindly: `IsRepositoryContextValid` re-verifies the cached worktree/git-dir still resolves the same way before using it. This is what makes worktree removal, branch-switch-via-worktree, and repo deletion self-heal instead of serving stale data — see the "worktree" bug-fix commits in git history for the motivating cases.
- Stale entries (untouched for 7 days) are swept opportunistically, at most once per 5 minutes process-wide, via a static "next cleanup due" timestamp (`TryCleanupStaleEntries`) — there is no background timer; cleanup only runs as a side effect of a `Set` call.

## Git status cache (`GitStatusSharedCache`)

Avoids re-running and re-parsing `git status` (and related plumbing) on every prompt render when nothing in the repo has actually changed.

- One file per hashed *repository root* in `git-status-cache/`.
- TTL comes from `Cache.GitStatusTtl` (default 5s). `0` disables the cache.
- A cached entry is only served if **all** of these hold:
  1. TTL not expired.
  2. The stored invalidation-token value matches the current one on disk (see below and [shell-contract.md](shell-contract.md)).
  3. The stored fingerprint matches a freshly computed one.
- **Fingerprint** (`BuildFingerprint`) is a single FNV-1a64 hash of the resolved common git directory path (so a linked worktree and its main repo don't collide), folded together with the size+mtime of: `HEAD`, `index`, `packed-refs`, `refs/stash`, `FETCH_HEAD`, `MERGE_HEAD`, `REBASE_HEAD`, `CHERRY_PICK_HEAD`, `REVERT_HEAD`, `BISECT_LOG`, the resolved current-branch ref file, and the resolved upstream ref file. The upstream ref path is found by manually parsing `.git/config` for `branch.<name>.remote`/`.merge` — deliberately *not* shelling out to `git rev-parse` or similar, to keep this check cheap. This is why the cache can detect "a commit landed" or "the branch moved" without invoking git at all.
- **Invalidation token** (`status-invalidation.token`, a random GUID) is a second, independent signal from the fingerprint. It exists because the fingerprint is mtime-based and has real blind spots: same-second writes where filesystem mtime granularity can't distinguish before/after, or operations that change status output without touching any watched file. `gitprompt --invalidate-status-cache` rewrites this token; any *different* token value (not "newer" — just different) invalidates every repo's cached entry. See [shell-contract.md](shell-contract.md) for who calls this and when.

## Shared plumbing (`SharedCacheUtilities`)

- `WriteAtomically`: write to a random temp file, then `File.Move(overwrite: true)`. Prevents another concurrently-running shell from reading a half-written cache file.
- `HashPath`: FNV-1a64 hex digest used as the cache filename, so arbitrary working-directory paths don't hit filesystem path-length/character limits and don't collide across repos.
- `CleanupStaleEntries`: deletes `*.cache` files whose last-write time is older than the stale threshold. Called from both caches' own opportunistic cleanup gate, not a shared timer.
- `FingerprintHasher`: a small streaming FNV-1a64 accumulator (`AppendString`/`AppendByte`/`AppendInt64`) used to build the status fingerprint above.

Both cache classes expose `OverrideTimeProviderForTesting`, `OverrideCacheDirectoryForTesting`, and `ResetCleanupScheduleForTesting` — internal-only static seams for deterministic unit tests. Use these instead of reflection or wall-clock sleeps when writing new cache tests; see existing tests in `tests/GitPrompt.Tests.Unit/Git/` for the pattern (and note the `CacheIsolationCollection`/`ConfigIsolationCollection` markers that disable parallelization for tests that touch this shared static state).
