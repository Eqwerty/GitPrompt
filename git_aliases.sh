# ============================ Clone ============================
alias gcl="git clone" # Clone a repository

# ============================ Add ============================
alias ga="git add" # Add files to the staging area
alias gaa="git add -A" # Add all changes to the staging area
alias gas="git add -A && git status -s" # Add all changes to the staging area and show a short status
alias gap="git add --patch" # Interactively stage changes in the working directory

# Interactively select modified/untracked files to add (menu)
function gam() {
  local -a files selected
  mapfile -t files < <({ git diff --name-only; git ls-files --others --exclude-standard; } | sort -u)
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files to add"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi --preview "$__GIT_FILE_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git add -- "${selected[@]/#/:(top)}"  # :(top) = git pathspec: resolve path from repo root
}

# ============================ Commit ============================
alias gc="git commit -m" # Commit with a message
alias gca="git commit --amend --no-edit" # Amend the last commit without changing the message
alias gcae="git commit --amend" # Amend the last commit and open the editor to change the message
alias gcam="git commit --amend -m" # Amend the last commit and edit the message
alias gcf="git commit --fixup" # Create a fixup commit targeting a given commit
alias gcs="git commit --squash" # Create a squash commit targeting a given commit

# ============================ Branch ============================
alias gb="git branch" # List branches
alias gba="git branch -a" # List all branches (local and remote)
alias gbr="git branch --remotes" # List remote branches
alias gbd="git branch -d" # Delete a local branch
alias gbD="git branch -D" # Force delete a local branch
alias gbm="git branch -m" # Rename the current branch
alias gco="git checkout" # Switch branches
alias gcot="git checkout --track" # Switch to a remote branch and track it
alias gcob="git checkout -b" # Create and switch to a new branch

# List branches with verbose information and colors
function gbv() {
  local _red=$'\e[31m' _magenta=$'\e[95m' _boldcyan=$'\e[1;36m'
  git branch --color=always --format='%(color:bold cyan)%(HEAD)%(color:reset) %(color:bold cyan)%(objectname:short)%(color:reset) %(if)%(HEAD)%(then)%(color:bold green)%(else)%(color:brightblack)%(end)%(refname:short)%(color:reset)%(if)%(upstream)%(then) %(color:brightmagenta)[%(upstream:short)%(if)%(upstream:track,nobracket)%(then): %(upstream:track,nobracket)%(end)]%(color:reset)%(end) %(color:white)%(subject)%(color:reset)' \
    | sed -e "s/: gone\]/: ${_red}gone${_magenta}]/g" \
          -e "s/ahead \([0-9]*\)/${_boldcyan}ahead \1${_magenta}/g" \
          -e "s/behind \([0-9]*\)/${_boldcyan}behind \1${_magenta}/g"
}

# Interactively select local branches to delete (safe, menu, no auto-select)
function gbdm() {
  __git_branch_delete -d
}

# Interactively select local branches to force-delete (menu, no auto-select)
function gbDm() {
  __git_branch_delete -D
}

# Shared helper for gbdm / gbDm — $1 is the delete flag (-d or -D)
function __git_branch_delete() {
  local flag="$1"
  local current_branch
  current_branch=$(git symbolic-ref --short HEAD 2>/dev/null)

  local -a branches selected
  mapfile -t branches < <(git branch --list --sort=-committerdate | sed 's/^[* ] //' | grep -v "^${current_branch}$")

  if [[ ${#branches[@]} -eq 0 ]]; then
    echo "No branches to delete"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${branches[@]}" | __git_select --multi --preview "$__GIT_BRANCH_UNMERGED_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git branch "$flag" "${selected[@]}"
}

# Interactively select a branch to check out (menu, current branch excluded)
function gcobm() {
  local current_branch
  current_branch=$(git symbolic-ref --short HEAD 2>/dev/null)

  local -a branches selected
  mapfile -t branches < <(git branch --list --sort=-committerdate | sed 's/^[* ] //' | grep -v "^${current_branch}$")

  if [[ ${#branches[@]} -eq 0 ]]; then
    echo "No other branches to switch to"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${branches[@]}" | __git_select --preview "$__GIT_BRANCH_UNMERGED_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git checkout "${selected[0]}"
}

# Interactively select a remote branch to check out and track (menu)
function gcotm() {
  local current_upstream
  current_upstream=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null)

  local -a branches selected
  mapfile -t branches < <(git branch --remotes --sort=-committerdate | sed 's/^  //' | grep -v ' -> ' | grep -v "^${current_upstream}$")

  if [[ ${#branches[@]} -eq 0 ]]; then
    echo "No remote branches to track"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${branches[@]}" | __git_select --preview "$__GIT_BRANCH_UNMERGED_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git checkout --track "${selected[0]}"
}

# Interactively select a remote branch to check out in detached HEAD mode (menu)
function gcorbm() {
  local -a branches selected
  mapfile -t branches < <(git branch --remotes --sort=-committerdate | sed 's/^  //' | grep -v ' -> ')

  if [[ ${#branches[@]} -eq 0 ]]; then
    echo "No remote branches to check out"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${branches[@]}" | __git_select --preview "$__GIT_BRANCH_UNMERGED_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git checkout "${selected[0]}"
}

# ============================ Worktree ============================
alias gw="git worktree" # Manage multiple working trees
alias gwa="git worktree add" # Add a new worktree for an existing branch/commit
alias gwab="git worktree add -b" # Create a new branch and add a worktree for it
alias gwr="git worktree remove" # Remove a worktree
alias gwrf="git worktree remove --force" # Force-remove a worktree (even if dirty/locked)
alias gwp="git worktree prune" # Clean up stale worktree administrative data
alias gwm="git worktree move" # Move a worktree to a new path

# List worktrees with colors: current worktree marked, path/hash/branch highlighted, locked/prunable flagged
function gwl() {
  local current_path
  current_path=$(git rev-parse --show-toplevel 2>/dev/null)

  local _boldcyan=$'\e[1;36m' _boldgreen=$'\e[1;32m' _brightyellow=$'\e[93m' _brightblack=$'\e[90m' _red=$'\e[31m' _reset=$'\e[0m'
  local fs=$'\x1f' # unit separator: unlike tab, bash's `read` never collapses consecutive occurrences into one delimiter

  local -a records
  mapfile -t records < <(git worktree list --porcelain | awk -v current="$current_path" -v fs="$fs" '
    /^worktree /  { path = substr($0, 10); if (length(path) > maxlen) maxlen = length(path) }
    /^HEAD /      { fullhash = substr($0, 6) }
    /^branch /    { b = substr($0, 8); sub("refs/heads/", "", b); branch = b }
    /^bare$/      { bare = 1 }
    /^detached$/  { detached = 1 }
    /^locked/     { locked = 1 }
    /^prunable/   { prunable = 1 }
    /^$/          {
      is_current = (path == current) ? 1 : 0
      printf "%s%s%s%s%s%s%d%s%d%s%d%s%d%s%d\n", path, fs, fullhash, fs, branch, fs, bare?1:0, fs, detached?1:0, fs, is_current, fs, locked?1:0, fs, prunable?1:0
      path=""; fullhash=""; branch=""; bare=0; detached=0; locked=0; prunable=0
    }
    END { print "MAXLEN" fs maxlen }
  ')

  local maxlen=0
  local -a lines
  local record
  for record in "${records[@]}"; do
    if [[ "$record" == MAXLEN"$fs"* ]]; then
      maxlen="${record#MAXLEN"$fs"}"
    else
      lines+=("$record")
    fi
  done

  local path fullhash branch bare detached is_current locked prunable shorthash marker state extra
  for record in "${lines[@]}"; do
    IFS="$fs" read -r path fullhash branch bare detached is_current locked prunable <<< "$record"
    shorthash=$(git rev-parse --short "$fullhash" 2>/dev/null)
    marker=" "
    [[ "$is_current" == "1" ]] && marker="*"
    if [[ "$bare" == "1" ]]; then
      state="${_brightblack}(bare)${_reset}"
    elif [[ "$detached" == "1" ]]; then
      state="${_brightblack}(detached HEAD)${_reset}"
    elif [[ "$is_current" == "1" ]]; then
      state="${_boldgreen}[${branch}]${_reset}"
    else
      state="${_brightblack}[${branch}]${_reset}"
    fi
    extra=""
    [[ "$locked" == "1" ]] && extra="${extra} ${_brightyellow}[locked]${_reset}"
    [[ "$prunable" == "1" ]] && extra="${extra} ${_red}[prunable]${_reset}"
    printf '%s %-*s  %s%s%s  %s%s\n' "$marker" "$maxlen" "$path" "$_boldcyan" "$shorthash" "$_reset" "$state" "$extra"
  done
}

# List other worktrees as "path [branch]", one per line, excluding the current one.
# Detached worktrees are resolved to a matching remote-tracking ref when possible (e.g. "origin/master"),
# falling back to "detached" when the commit isn't pointed at by any remote-tracking branch.
function __git_worktree_others() {
  local current_path
  current_path=$(git rev-parse --show-toplevel 2>/dev/null)

  local sep=$'\x01'
  local -a records
  mapfile -t records < <(git worktree list --porcelain | awk -v current="$current_path" -v sep="$sep" '
    /^worktree /  { path = substr($0, 10) }
    /^HEAD /      { head = substr($0, 6) }
    /^branch /    { branch = substr($0, 8); sub("refs/heads/", "", branch); if (path != current) print path "\t" branch; path=""; head=""; branch="" }
    /^bare$/      { if (path != current) print path "\tbare"; path=""; head="" }
    /^detached$/  { if (path != current) print path "\t" sep head; path=""; head="" }
  ')

  local record path label head remote_ref
  for record in "${records[@]}"; do
    path="${record%%$'\t'*}"
    label="${record#*$'\t'}"
    if [[ "$label" == "$sep"* ]]; then
      head="${label#"$sep"}"
      remote_ref=$(git for-each-ref --points-at="$head" --format='%(refname)' refs/remotes 2>/dev/null | grep -v '/HEAD$' | head -n1)
      label="${remote_ref#refs/remotes/}"
      label="${label:-detached}"
    fi
    printf '%s [%s]\n' "$path" "$label"
  done
}

# Interactively select worktree(s) to remove (menu, current worktree excluded)
function gwrm() {
  local -a entries selected paths
  mapfile -t entries < <(__git_worktree_others)

  if [[ ${#entries[@]} -eq 0 ]]; then
    echo "No other worktrees to remove"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${entries[@]}" | __git_select --multi)
  [[ ${#selected[@]} -eq 0 ]] && return 0

  mapfile -t paths < <(printf '%s\n' "${selected[@]}" | sed -E 's/ \[[^]]*\]$//')
  git worktree remove "${paths[@]}"
}

# Interactively select a worktree to switch into (menu, current worktree excluded)
function gwcm() {
  local -a entries selected
  mapfile -t entries < <(__git_worktree_others)

  if [[ ${#entries[@]} -eq 0 ]]; then
    echo "No other worktrees to switch to"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${entries[@]}" | __git_select)
  [[ ${#selected[@]} -eq 0 ]] && return 0

  local path="${selected[0]% \[*\]}"
  cd "$path" || return
}

# ============================ Merge ============================
alias gm="git merge --no-edit" # Merge branches without opening an editor
alias gma="git merge --abort" # Abort a merge
alias gmc="git merge --continue" # Continue a merge after resolving conflicts
alias gms="git merge --squash" # Squash commits during a merge

# ============================ Fetch ============================
alias gf="git fetch --prune" # Fetch changes from the remote and prune deleted branches
alias gfa="git fetch --all --prune" # Fetch changes from all remotes and prune deleted branches

# ============================ Pull ============================
alias gpl="git pull" # Pull changes from the remote
alias gplr="git pull -r" # Pull changes and rebase

# ============================ Push ============================
alias gpo="git push -u origin HEAD" # Push the current branch to the remote and set upstream
alias gpof="git push -u origin HEAD --force-with-lease" # Force push the current branch to the remote

# ============================ Rebase ============================
alias gr="git rebase" # Rebase the current branch
alias gri="git rebase -i" # Start an interactive rebase
alias grias="git rebase -i --autosquash" # Interactive rebase with autosquash enabled
alias gra="git rebase --abort" # Abort a rebase
alias grc="git rebase --continue" # Continue a rebase after resolving conflicts
alias grs="git rebase --skip" # Skip the current commit during a rebase

# ============================ Stash ============================
alias gsu="git stash push -u" # Stash untracked changes
alias gsum="git stash push -u -m" # Stash untracked changes with a message
alias gsd="git stash drop" # Drop a stash
alias gsp="git stash pop" # Apply the most recent stash and remove it from the stash list
alias gsl="git stash list" # List all stashes
alias gsc="git stash clear" # Clear all stashes
alias gsa="git stash apply" # Apply a stash
alias gsshno="git stash show --name-only" # Show names of files changed in a stash

# Show changes of a specific stash
function gssh() {
  if [[ -z "$1" ]]; then
    echo "Usage: gssh <stash-index>"
    return 1
  fi
  git stash show -p "stash@{$1}"
}

# Interactively select a modified/untracked file to stash (menu)
function gsufm() {
  local -a files selected
  mapfile -t files < <(git status --porcelain | awk '{print substr($0,4)}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files to stash"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi --preview "$__GIT_HEAD_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git stash push -u -- "${selected[@]/#/:(top)}"  # :(top) = git pathspec: resolve path from repo root
}

# Interactively select a stash to pop (menu)
function gspm() {
  local -a stashes selected
  mapfile -t stashes < <(git stash list)
  if [[ ${#stashes[@]} -eq 0 ]]; then
    echo "No stashes to pop"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${stashes[@]}" | __git_select --preview "$__GIT_STASH_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  local ref
  ref=$(echo "${selected[0]}" | cut -d: -f1)
  git stash pop "$ref"
}

# Interactively select a stash to apply (menu)
function gsam() {
  local -a stashes selected
  mapfile -t stashes < <(git stash list)
  if [[ ${#stashes[@]} -eq 0 ]]; then
    echo "No stashes to apply"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${stashes[@]}" | __git_select --preview "$__GIT_STASH_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  local ref
  ref=$(echo "${selected[0]}" | cut -d: -f1)
  git stash apply "$ref"
}

# Interactively select a stash to drop (menu, no auto-select)
function gsdm() {
  local -a stashes selected
  mapfile -t stashes < <(git stash list)
  if [[ ${#stashes[@]} -eq 0 ]]; then
    echo "No stashes to drop"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${stashes[@]}" | __git_select --preview "$__GIT_STASH_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  local ref
  ref=$(echo "${selected[0]}" | cut -d: -f1)
  git stash drop "$ref"
}

# Interactively select a stash to show its diff (menu)
function gsshm() {
  local -a stashes selected
  mapfile -t stashes < <(git stash list)
  if [[ ${#stashes[@]} -eq 0 ]]; then
    echo "No stashes to show"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${stashes[@]}" | __git_select --preview "$__GIT_STASH_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  local ref
  ref=$(echo "${selected[0]}" | cut -d: -f1)
  git stash show -p "$ref"
}

# ============================ Log ============================
alias glog="git log --graph --pretty=format:'%C(bold cyan)%h%Creset%C(auto)%d%Creset %C(white)%s %Cgreen(%cr) %C(bold cyan)<%an>%Creset' --abbrev-commit" # Show a graphical log with commit details
alias glh="glog HEAD.." # Show commits in <branch> not yet merged into HEAD (usage: glh <branch>)
alias gluh="glog @{u}..HEAD" # Show commits not pushed to the upstream branch
alias glhu="glog HEAD..@{u}" # Show commits not pulled from the upstream branch (behind)

# Show a graphical log filtered to commits by the current git user
function glogm() {
  glog --author="$(git config --get user.email)" "$@"
}

# Display a limited number of recent Git log entries (default: all)
function gl() {
  local count=${1:--1}
  glog -n "$count"
}

# Display a limited number of recent Git log entries (default: all) by the author logged in
function glm() {
  local count=${1:--1}
  glogm -n "$count"
}

# Copy the short hash of the Nth most recent commit to the clipboard
function gcc() {
  local index short_hash commit_message

  if [[ -z "$1" ]]; then
    echo "Usage: gcc <commit-position>"
    echo "Example: gcc 4"
    return 1
  fi

  if ! [[ "$1" =~ ^[1-9][0-9]*$ ]]; then
    echo "Error: commit-position must be a positive integer"
    return 1
  fi

  index="$1"
  read -r short_hash commit_message < <(git log --skip=$((index-1)) -1 --format="%h %s" 2>/dev/null)

  if [[ -z "$short_hash" ]]; then
    echo "Error: could not find commit at position $index"
    return 1
  fi

  if command -v clip.exe >/dev/null 2>&1; then
    if command -v iconv >/dev/null 2>&1; then
      printf '%s' "$short_hash" | iconv -f UTF-8 -t UTF-16LE | clip.exe
    else
      printf '%s' "$short_hash" | clip.exe
    fi
  elif command -v pbcopy >/dev/null 2>&1; then
    printf '%s' "$short_hash" | pbcopy
  elif command -v wl-copy >/dev/null 2>&1; then
    printf '%s' "$short_hash" | wl-copy
  elif command -v xclip >/dev/null 2>&1; then
    printf '%s' "$short_hash" | xclip -selection clipboard
  elif command -v xsel >/dev/null 2>&1; then
    printf '%s' "$short_hash" | xsel --clipboard --input
  elif command -v clip >/dev/null 2>&1; then
    printf '%s' "$short_hash" | clip
  else
    echo "Error: no clipboard command found (tried clip.exe, pbcopy, wl-copy, xclip, xsel, clip)"
    return 1
  fi

  echo "Copied commit #$index: $short_hash - $commit_message"
}

# ============================ Show ============================
alias gbl="git blame --color-by-age --color-lines" # Show blame information with color-by-age and color-lines
alias ggr="git grep --no-index -i -I --exclude-standard --heading --line-number" # Search for a string in the repository
alias gsh="git show" # Show details of a commit
alias gshno="git show --name-only --format=" # Show names of files changed in a commit

# Interactively select a file changed in a commit to show its diff (menu, defaults to HEAD)
function gshm() {
  local commit="${1:-HEAD}"
  local -a files selected
  mapfile -t files < <(git show --name-only --format= "$commit" 2>/dev/null)
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No files changed in commit $commit"
    return 1
  fi
  local preview="if command -v delta >/dev/null 2>&1; then if git show --diff-filter=AD --name-only --format= ${commit} -- {} | grep -q .; then git show --format= --color=never ${commit} -- {} | delta --config \"\$__DELTA_LINEAR_CONFIG\" --paging=never --width \${FZF_PREVIEW_COLUMNS}; else git show --format= --color=never ${commit} -- {} | delta --paging=never --width \${FZF_PREVIEW_COLUMNS}; fi; else git show --format= --color=always ${commit} -- {}; fi"
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --preview "$preview")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git show "$commit" -- ":(top)${selected[0]}"
}

# ============================ Reset ============================
alias grm="git reset" # Reset index but keep changes in the working directory (mixed mode)
alias grhh="git reset --hard HEAD" # Discard all uncommitted changes (hard reset)

# Interactively select staged files to unstage (menu)
function grmm() {
  local -a files selected
  mapfile -t files < <(git diff --name-only --cached)
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No staged files to reset"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi --preview "$__GIT_STAGED_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git reset -- "${selected[@]/#/:(top)}"  # :(top) = git pathspec: resolve path from repo root
}

# Reset the current branch to n commits before HEAD
function grh() {
  if [[ -z "$1" ]]; then
    echo "Usage: grh <number-of-commits>"
    return 1
  fi
  git reset --soft "HEAD~$1"
}

# Reset the current branch to the specified commit and apply --hard
function grch() {
  if [[ -z "$1" ]]; then
    echo "Usage: grch <commit-hash>"
    return 1
  fi
  git reset --hard "$1"
}

# ============================ Diff ============================
alias gd="git diff" # Show changes between commits, branches, or the working directory
alias gdno="git diff --name-only" # Show names of changed files
alias gds="git diff --staged" # Show changes in the staging area
alias gdsno="git diff --staged --name-only" # Show names of staged files
alias gdfu="git diff --name-only --diff-filter=U" # Show files with unmerged changes or conflicts

# Show the diff of a file interactively selected from modified files (menu)
function gdm() {
  local -a files selected
  mapfile -t files < <(git diff --name-only)
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --preview "$__GIT_DIFF_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git diff -- ":(top)${selected[0]}"  # :(top) = git pathspec: resolve path from repo root
}

# Show the diff of a staged file interactively selected from staged files (menu)
function gdsm() {
  local -a files selected
  mapfile -t files < <(git diff --name-only --cached)
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No staged files"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --preview "$__GIT_STAGED_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git diff --staged -- ":(top)${selected[0]}"  # :(top) = git pathspec: resolve path from repo root
}

# ============================ Status ============================
alias gs="git status" # Show the status of the working directory
alias gss="git status -s" # Show a short status of the working directory

# ============================ Reflog ============================
alias gref="git reflog" # Show the reflog

# ============================ File Checkout ============================
# Interactively select modified/untracked files to check out (menu)
function gcofm() {
  local -a files selected
  mapfile -t files < <(git diff --name-only)
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No files with unstaged changes"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi --preview "$__GIT_FILE_PREVIEW")
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git checkout -- "${selected[@]/#/:(top)}"  # :(top) = git pathspec: resolve path from repo root
}

# ============================ Cherry-Pick ============================
alias gcp="git cherry-pick" # Apply the changes introduced by an existing commit
alias gcpa="git cherry-pick --abort" # Cancel the cherry-picking operation and return to the pre-sequence state
alias gcpc="git cherry-pick --continue" # Continue the cherry-picking operation in progress

# ============================ Tags ============================
alias gt="git tag" # List or create tags
alias gta="git tag -a" # Create an annotated tag
alias gtd="git tag -d" # Delete a local tag
alias gtl="git tag --list --sort=-version:refname" # List tags sorted newest-first

# ============================ Links ============================
# Resolve the base web URL for the origin remote (GitHub or Azure DevOps).
# Supported inputs: GitHub HTTPS/SSH, AzDO modern HTTPS/SSH, AzDO legacy HTTPS/SSH.
function __git_web_url() {
  local remote_url
  remote_url=$(git remote get-url origin 2>/dev/null) || { echo "Error: no remote 'origin' found" >&2; return 1; }

  local url org project repo path
  case "$remote_url" in
    *github.com*)
      url=$(printf '%s' "$remote_url" | sed -Ee 's#git@github\.com:#https://github.com/#' -e 's#\.git$##')
      echo "$url"
      ;;
    git@ssh.dev.azure.com:*)
      path="${remote_url#git@ssh.dev.azure.com:v3/}"
      org="${path%%/*}"; path="${path#*/}"; project="${path%%/*}"; repo="${path#*/}"
      url="https://dev.azure.com/$org/$project/_git/$repo"
      echo "$url"
      ;;
    https://dev.azure.com/* | https://*@dev.azure.com/*)
      # Strip embedded credentials if present (e.g. https://Org@dev.azure.com/... → https://dev.azure.com/...)
      url=$(printf '%s' "${remote_url%.git}" | sed 's#https://[^@/]*@#https://#')
      echo "$url"
      ;;
    git@vs-ssh.visualstudio.com:*)
      path="${remote_url#git@vs-ssh.visualstudio.com:v3/}"
      org="${path%%/*}"; path="${path#*/}"; project="${path%%/*}"; repo="${path#*/}"
      url="https://dev.azure.com/$org/$project/_git/$repo"
      echo "$url"
      ;;
    https://*.visualstudio.com/*)
      echo "${remote_url%.git}"
      ;;
    *)
      echo "Error: unsupported remote URL format: $remote_url" >&2
      return 1
      ;;
  esac
}

# Open a URL with the OS-appropriate default browser handler
function __git_open_url() {
  local url="$1"

  if [[ "$(uname -s)" == "Darwin" ]]; then
    open "$url"
    return
  fi

  if grep -qi microsoft /proc/version 2>/dev/null || [[ -n "$WSL_DISTRO_NAME" ]] || [[ "$(uname -s)" =~ ^(MINGW|MSYS|CYGWIN) ]]; then
    local pwsh=""
    if command -v powershell.exe >/dev/null 2>&1; then
      pwsh="powershell.exe"
    elif [[ -x /mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe ]]; then
      pwsh="/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe"
    fi

    if [[ -n "$pwsh" ]]; then
      "$pwsh" -NoProfile -Command "Start-Process '$url'" >/dev/null 2>&1
      return
    fi
  fi

  if command -v xdg-open >/dev/null 2>&1; then
    xdg-open "$url" >/dev/null 2>&1 &
    return
  fi

  echo "Error: could not determine how to open a URL on this platform." >&2
  echo "Please open this URL manually: $url"
  return 1
}

# Create a pull request and open it in the default browser
function gpr() {
  local base_url branch_name main_branch pr_url

  base_url=$(__git_web_url) || { echo "Error: no supported remote found (GitHub or Azure DevOps)"; return 1; }

  branch_name=$(git symbolic-ref --short HEAD 2>/dev/null)
  [[ -n "$branch_name" ]] || { echo "Error: not in a git repository or in detached HEAD state"; return 1; }

  main_branch=$(gbdefault)
  [[ -n "$main_branch" ]] || { echo "Error: could not determine default branch"; return 1; }

  if [[ "$base_url" == *dev.azure.com* || "$base_url" == *visualstudio.com* ]]; then
    pr_url="$base_url/pullrequestcreate?sourceRef=$branch_name&targetRef=$main_branch"
  else
    pr_url="$base_url/compare/$main_branch...$branch_name"
  fi

  __git_open_url "$pr_url"
}

# Open the current branch or the main branch in the repository
function grepo() {
  local base_url main_branch current_branch url

  git rev-parse --is-inside-work-tree >/dev/null 2>&1 || { echo "Error: not in a git repository"; return 1; }

  base_url=$(__git_web_url) || { echo "Error: no supported remote found (GitHub or Azure DevOps)"; return 1; }

  main_branch=$(gbdefault)
  [[ -n "$main_branch" ]] || { echo "Error: could not determine default branch"; return 1; }

  current_branch=$(gbcurrent 2>/dev/null)
  url="$base_url"
  if [[ -n "$current_branch" && "$main_branch" != "$current_branch" ]]; then
    if [[ "$base_url" == *dev.azure.com* || "$base_url" == *visualstudio.com* ]]; then
      url="$base_url?version=GB$current_branch"
    else
      url="$base_url/tree/$current_branch"
    fi
  fi

  __git_open_url "$url"
}

# ============================ Utils ============================
# Get the default branch name with fallbacks when origin/HEAD is not configured
function gbdefault() {
  local default_branch

  default_branch=$(git symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null | cut -d'/' -f2)

  if [[ -z "$default_branch" ]]; then
    for branch in main master; do
      if git show-ref --verify --quiet "refs/remotes/origin/$branch" 2>/dev/null; then
        default_branch="$branch"
        break
      fi
    done
  fi

  if [[ -z "$default_branch" ]]; then
    default_branch=$(git remote show origin 2>/dev/null | awk -F': ' '/HEAD branch/ {print $2; exit}')
  fi

  if [[ -z "$default_branch" ]]; then
    default_branch=$(git symbolic-ref --short HEAD 2>/dev/null)
  fi

  printf '%s\n' "$default_branch"
}

alias gbcurrent="git symbolic-ref --short HEAD" # Get the current branch name
alias gcgl="git config --global --list" # List the current global Git configuration
alias gcge="git config --global --edit" # Opens the global Git configuration file
alias gcfd="git clean -fd" # Remove untracked files and directories
alias gcfdn="git clean -fdn" # Show which untracked files and directories would be removed

function gcdroot() {
  local root
  root=$(git rev-parse --show-toplevel 2>/dev/null)
  if [[ -n "$root" ]]; then
    cd "$root" || return
  else
    echo "Not inside a Git repository"
    return 1
  fi
}

# Branch preview command for fzf — shows commits ahead of the default branch (origin/HEAD).
# When the selected branch IS the default branch, falls back to showing its most recent commits.
# fzf runs previews in a subshell where aliases are unavailable, so the full git command is stored here.
__GIT_BRANCH_UNMERGED_PREVIEW='
  _default=$(git symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null || echo origin/main)
  _default_short=${_default#*/}
  _fmt="%C(bold cyan)%h%Creset %C(white)%s %Cgreen(%cr) %C(bold cyan)<%an>%Creset"
  if [ {} = "$_default_short" ] || [ {} = "$_default" ]; then
    git log --graph --color=always --pretty=format:"$_fmt" --abbrev-commit -20 {}
  else
    git log --graph --color=always --pretty=format:"$_fmt" --abbrev-commit "$_default"..{}
  fi'

# Linear (non-side-by-side) delta config for pure-addition or pure-deletion diffs.
# Created once; includes ~/.gitconfig so all user delta styles are preserved.
__DELTA_LINEAR_CONFIG="$HOME/.cache/gitprompt/delta-linear.gitconfig"
if [[ ! -f "$__DELTA_LINEAR_CONFIG" ]]; then
  mkdir -p "$(dirname "$__DELTA_LINEAR_CONFIG")"
  printf '[include]\n\tpath = %s\n[delta]\n\tside-by-side = false\n' "$HOME/.gitconfig" > "$__DELTA_LINEAR_CONFIG"
fi
export __DELTA_LINEAR_CONFIG

# Diff preview constants — pipe through delta when available. Fallback to plain --color=always output when delta is not installed.
__GIT_FILE_PREVIEW='if git diff --quiet -- {} 2>/dev/null; then cmd=$(command -v batcat || command -v bat); [ -n "$cmd" ] && "$cmd" --paging=never --color=always --style=plain {} || cat {}; elif command -v delta >/dev/null 2>&1; then if git diff --diff-filter=D --name-only -- {} | grep -q .; then git diff --color=never -- {} | delta --config "$__DELTA_LINEAR_CONFIG" --paging=never --width ${FZF_PREVIEW_COLUMNS}; else git diff --color=never -- {} | delta --paging=never --width ${FZF_PREVIEW_COLUMNS}; fi; else git diff --color=always -- {}; fi'
__GIT_HEAD_PREVIEW='if git diff --quiet HEAD -- {} 2>/dev/null; then cmd=$(command -v batcat || command -v bat); [ -n "$cmd" ] && "$cmd" --paging=never --color=always --style=plain {} || cat {}; elif command -v delta >/dev/null 2>&1; then if git diff HEAD --diff-filter=AD --name-only -- {} | grep -q .; then git diff --color=never HEAD -- {} | delta --config "$__DELTA_LINEAR_CONFIG" --paging=never --width ${FZF_PREVIEW_COLUMNS}; else git diff --color=never HEAD -- {} | delta --paging=never --width ${FZF_PREVIEW_COLUMNS}; fi; else git diff --color=always HEAD -- {}; fi'
__GIT_DIFF_PREVIEW='if command -v delta >/dev/null 2>&1; then if git diff --diff-filter=D --name-only -- {} | grep -q .; then git diff --color=never -- {} | delta --config "$__DELTA_LINEAR_CONFIG" --paging=never --width ${FZF_PREVIEW_COLUMNS}; else git diff --color=never -- {} | delta --paging=never --width ${FZF_PREVIEW_COLUMNS}; fi; else git diff --color=always -- {}; fi'
__GIT_STAGED_PREVIEW='if command -v delta >/dev/null 2>&1; then if git diff --staged --diff-filter=AD --name-only -- {} | grep -q .; then git diff --staged --color=never -- {} | delta --config "$__DELTA_LINEAR_CONFIG" --paging=never --width ${FZF_PREVIEW_COLUMNS}; else git diff --staged --color=never -- {} | delta --paging=never --width ${FZF_PREVIEW_COLUMNS}; fi; else git diff --staged --color=always -- {}; fi'
__GIT_STASH_PREVIEW='ref=$(echo {} | cut -d: -f1); if command -v delta >/dev/null 2>&1; then _has_added=false; git stash show --diff-filter=AD --name-only "$ref" 2>/dev/null | grep -q . && _has_added=true; git cat-file -e "${ref}^3" 2>/dev/null && _has_added=true; if $_has_added; then { git stash show -p --color=never "$ref" 2>/dev/null; git show "${ref}^3" --color=never 2>/dev/null; } | delta --config "$__DELTA_LINEAR_CONFIG" --paging=never --width ${FZF_PREVIEW_COLUMNS}; else { git stash show -p --color=never "$ref" 2>/dev/null; git show "${ref}^3" --color=never 2>/dev/null; } | delta --paging=never --width ${FZF_PREVIEW_COLUMNS}; fi; else git stash show -p --color=always "$ref" 2>/dev/null; git show "${ref}^3" --color=always 2>/dev/null; fi'

# Interactive menu picker for selecting from a list of items.
# Usage: <list> | __git_select [--multi]
# - Reads candidates from stdin (one per line).
# - Always shows the interactive menu, even for a single item.
# - Uses fzf when installed; otherwise falls back to a numbered list prompt.
# - --multi: allow selecting more than one item (Tab in fzf; space-separated numbers in fallback).
# Output: selected item(s) on stdout, one per line.
function __git_select() {
  local multi=0 preview_cmd=""
  while [[ "${1:-}" == --* ]]; do
    case "$1" in
      --multi)   multi=1 ;;
      --preview) preview_cmd="$2"; shift ;;
    esac
    shift
  done

  local -a items
  mapfile -t items

  if [[ ${#items[@]} -eq 0 ]]; then
    return 1
  fi

  if command -v fzf >/dev/null 2>&1; then
    local -a fzf_opts=(--height 100% --layout=reverse)
    if [[ -n "$preview_cmd" ]]; then
      fzf_opts+=(--preview "$preview_cmd" --preview-window "bottom:80%:wrap" --bind "ctrl-/:toggle-preview")
    fi
    local header_preview=""
    [[ -n "$preview_cmd" ]] && header_preview=" | Ctrl-/: toggle preview"
    if [[ $multi -eq 1 ]]; then
      printf '%s\n' "${items[@]}" | fzf --multi --header "Tab: toggle selection | Enter: confirm${header_preview}" "${fzf_opts[@]}"
    else
      printf '%s\n' "${items[@]}" | fzf --header "Enter: confirm${header_preview}" "${fzf_opts[@]}"
    fi
    return $?
  fi

  echo "Tip: Install fzf for a better interactive selection experience." >&2
  local i
  for i in "${!items[@]}"; do
    printf '  %d) %s\n' "$((i+1))" "${items[$i]}" >&2
  done

  while true; do
    if [[ $multi -eq 1 ]]; then
      printf 'Pick numbers (space-separated, or q to quit): ' >&2
    else
      printf 'Pick a number (or q to quit): ' >&2
    fi
    read -r input </dev/tty
    [[ "$input" == "q" ]] && return 1

    if [[ $multi -eq 1 ]]; then
      local -a selected=()
      local valid=1 num
      for num in $input; do
        if ! [[ "$num" =~ ^[1-9][0-9]*$ ]] || [[ "$num" -gt "${#items[@]}" ]]; then
          echo "Invalid selection: $num" >&2
          valid=0
          break
        fi
        selected+=("${items[$((num-1))]}")
      done
      if [[ $valid -eq 1 && ${#selected[@]} -gt 0 ]]; then
        printf '%s\n' "${selected[@]}"
        return 0
      fi
    else
      if [[ "$input" =~ ^[1-9][0-9]*$ ]] && [[ "$input" -le "${#items[@]}" ]]; then
        printf '%s\n' "${items[$((input-1))]}"
        return 0
      else
        echo "Invalid selection: '$input'" >&2
      fi
    fi
  done
}

# Enable autocomplete for aliases
if type __git_complete >/dev/null 2>&1; then
  __git_complete ga _git_add
  __git_complete gb _git_branch
  __git_complete gbd _git_branch
  __git_complete gbD _git_branch
  __git_complete gco _git_checkout
  __git_complete gcot _git_checkout
  __git_complete gd _git_diff
  __git_complete gds _git_diff
  __git_complete ggr _git_grep
  __git_complete glh _git_log
  __git_complete gm _git_merge
  __git_complete gms _git_merge
  __git_complete gr _git_rebase
  __git_complete gri _git_rebase
  __git_complete gsh _git_show
  __git_complete gtd _git_tag
  __git_complete gw _git_worktree
  __git_complete gwa _git_worktree
  __git_complete gwab _git_worktree
  __git_complete gwr _git_worktree
  __git_complete gwrf _git_worktree
  __git_complete gwm _git_worktree
fi

