# Adding a Config Field

Read this before adding, renaming, or removing a `config.jsonc` field. The schema is defined in four separate places that all have to agree — missing one doesn't fail loudly, it just silently breaks that field.

1. **`Configuration/ConfigDto.cs`** — add a nullable property (with `[JsonInclude]`/`[JsonPropertyName]`) to the relevant nested record (or top-level), plus a `internal const ... Default...` if it needs a fallback value.
2. **`Resources/default-config.jsonc`** — this is an embedded **template**, not literal defaults (see [architecture.md](architecture.md)'s framing of what's generated vs. hand-written). Add a `{fieldName}` placeholder in the right spot, with its inline `//` comment, matching `ConfigDto`'s shape/order.
3. **`ConfigInitializer.BuildConfigContent`** — add a `.Replace("{fieldName}", ...)` line converting the merged `Config` value back to a JSON literal, using the existing `JsonBool`/`JsonInt`/`JsonDouble`/`JsonNullableDouble`/`JsonValue` helpers for the field's shape.
4. **`ConfigInitializer.MergeWithDefaults`** — add the `userConfig.X?.Field ?? <default>` line that produces the final `Config` object prompt-building code actually reads.

## What breaks if you skip one

- Skip step 2: `BuildConfigContent`'s `.Replace` call has nothing to match, so the literal string `{fieldName}` gets written into the user's `config.jsonc` on disk.
- Skip step 3: `ConfigInitializer.MigrateConfigIfNeeded`'s regenerated default content silently omits the field forever — `HasMissingKeys` can never detect it as missing from an old user file, so existing installs never get it added.
- Skip step 4: the field parses fine from JSON, but nothing outside `MergeWithDefaults` reads `userConfig.X` directly — it silently never affects prompt output.

## Migration isn't backwards compatibility

`MigrateConfigIfNeeded` upgrades an old `config.jsonc` in place to add newly-introduced keys, preserving the user's existing values for keys that already existed. It does **not** preserve multiple schema versions — there is exactly one current schema at any time. See the "No backwards compatibility" principle in [AGENTS.md](../AGENTS.md#principles).

## The triple-parse in MigrateConfigIfNeeded is deliberate, not an oversight

`MigrateConfigIfNeeded` parses the config file up to three times: once as a raw `JsonDocument` (diffed against a freshly-built default to detect missing keys via `HasMissingKeys`), once via `JsonSerializer.Deserialize` into `ConfigDto` (to actually read the user's values), and once more for the freshly rebuilt default template it diffs against. This looks like an easy win to collapse into fewer parses — resist that unless you add tests for it first. It's not on the hot path (never runs during prompt render, only on `init`/`update`), and collapsing it risks conflating "does this file need rewriting" with "what are the values," a distinction no existing test isolates.

## Don't forget the human doc

`docs/configuration.md` is the canonical human-facing reference for the schema (see [AGENTS.md](../AGENTS.md#human-docs-canonical-for-the-user-facing-surface)) and covers none of the internal wiring above — update it too when adding a field.
