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
  mapfile -t files < <(git status --porcelain | awk '{print $2}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files to add"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git add -- "${selected[@]}"
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
alias gbv="git branch -vv" # List branches with verbose information
alias gba="git branch -a" # List all branches (local and remote)
alias gbr="git branch --remotes" # List remote branches
alias gbd="git branch -d" # Delete a local branch
alias gbD="git branch -D" # Force delete a local branch
alias gbm="git branch -m" # Rename the current branch
alias gco="git checkout" # Switch branches
alias gcot="git checkout --track" # Switch to a remote branch and track it
alias gcob="git checkout -b" # Create and switch to a new branch

# Interactively select a branch to check out (menu, current branch excluded)
function gcobm() {
  local current_branch
  current_branch=$(git symbolic-ref --short HEAD 2>/dev/null)

  local -a branches selected
  mapfile -t branches < <(git branch --list | sed 's/^[* ] //' | grep -v "^${current_branch}$")

  if [[ ${#branches[@]} -eq 0 ]]; then
    echo "No other branches to switch to"
    return 1
  fi

  mapfile -t selected < <(printf '%s\n' "${branches[@]}" | __git_select)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git checkout "${selected[0]}"
}

# ============================ Merge ============================
alias gm="git merge --no-edit" # Merge branches without opening an editor
alias gma="git merge --abort" # Abort a merge
alias gmc="git merge --continue" # Continue a merge after resolving conflicts
alias gms="git merge --squash" # Squash commits during a merge

# ============================ Fetch ============================
alias gf="git fetch" # Fetch changes from the remote
alias gfa="git fetch --all" # Fetch changes from all remotes
alias gfap="git fetch --all --prune" # Fetch changes from all remotes and prune deleted branches
alias gfs="git fetch && git status" # Fetch changes and show the status

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

# ============================ Stash ============================
alias gsu="git stash push -u" # Stash untracked changes
alias gsum="git stash push -u -m" # Stash untracked changes with a message
alias gsd="git stash drop" # Drop a stash
alias gsp="git stash pop" # Apply the most recent stash
alias gsl="git stash list" # List all stashes
alias gsc="git stash clear" # Clear all stashes
alias gsa="git stash apply" # Apply a stash
alias gsshno="git stash show --name-only" # Show names of files changed in a stash

# Show changes of a specific stash
function gssh() {
  if [ -z "$1" ]; then
    echo "Usage: gssh <stash-index>"
    return 1
  fi
  git stash show -w -p "stash@{$1}"
}

# Interactively select a modified/untracked file to stash (menu)
function gsufm() {
  local -a files selected
  mapfile -t files < <(git status --porcelain | awk '{print $2}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files to stash"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git stash push -u -- "${selected[@]}"
}

# ============================ Log ============================
alias glog="git log --graph --pretty=format:'%C(bold cyan)%h%Creset%C(auto)%d%Creset %C(white)%s %Cgreen(%cr) %C(bold cyan)<%an>%Creset' --abbrev-commit" # Show a graphical log with commit details
alias glh="glog HEAD.." # Show commits in other branches not yet merged into HEAD
alias gluh="glog @{u}..HEAD" # Show commits not pushed to the upstream branch

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
  local index line short_hash commit_message

  if [ -z "$1" ]; then
    echo "Usage: gcc <commit-position>"
    echo "Example: gcc 4"
    return 1
  fi

  if ! [[ "$1" =~ ^[1-9][0-9]*$ ]]; then
    echo "Error: commit-position must be a positive integer"
    return 1
  fi

  index="$1"
  line=$(git log -n "$index" --oneline 2>/dev/null | tail -n 1)

  if [ -z "$line" ]; then
    echo "Error: could not find commit at position $index"
    return 1
  fi

  short_hash=$(awk '{print $1}' <<< "$line")
  commit_message=$(awk '{$1=""; sub(/^ /, ""); print}' <<< "$line")

  if command -v clip.exe >/dev/null 2>&1; then
    if command -v iconv >/dev/null 2>&1; then
      printf '%s' "$short_hash" | iconv -f UTF-8 -t UTF-16LE | clip.exe
    else
      printf '%s' "$short_hash" | clip.exe
    fi
  elif command -v clip >/dev/null 2>&1; then
    printf '%s' "$short_hash" | clip
  else
    echo "Error: no clipboard command found (expected clip.exe or clip)"
    return 1
  fi

  echo "Copied commit #$index: $short_hash - $commit_message"
}

# ============================ Show ============================
alias gbl="git blame --color-by-age --color-lines" # Show blame information with color-by-age and color-lines
alias ggr="git grep --no-index -i -I --exclude-standard --heading --line-number" # Search for a string in the repository
alias gsh="git show -w" # Show details of a commit
alias gshno="git show --oneline --name-only" # Show names of files changed in a commit

# ============================ Reset ============================
alias grm="git reset" # Reset index but keep changes in the working directory (mixed mode)

# Interactively select staged files to unstage (menu)
function grmm() {
  local -a files selected
  mapfile -t files < <(git status --porcelain | awk 'substr($0,1,1) != " " && substr($0,1,1) != "?" {print $2}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No staged files to reset"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git reset -- "${selected[@]}"
}

alias grhh="git reset HEAD --hard" # Discards all uncommitted changes (hard reset).

# Reset the current branch to n commits before HEAD
function grh() {
  if [ -z "$1" ]; then
    echo "Usage: grh <number-of-commits>"
    return 1
  fi
  git reset "HEAD~$1" --soft
}

# Reset the current branch to the specified commit and apply --hard
function grch() {
  if [ -z "$1" ]; then
    echo "Usage: grch <commit-hash>"
    return 1
  fi
  git reset "$1" --hard
}

# ============================ Diff ============================
alias gd="git diff -w" # Show changes between commits, branches, or the working directory
alias gds="git diff -w --staged" # Show changes in the staging area
alias gdfu="git diff --name-only --diff-filter=U" # Show files with unmerged changes or conflicts

# Show the diff of a file interactively selected from modified files (menu)
function gdm() {
  local -a files selected
  mapfile -t files < <(git status --porcelain | awk '{print $2}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git diff -w -- "${selected[0]}"
}

# Show the diff of a staged file interactively selected from staged files (menu)
function gdsm() {
  local -a files selected
  mapfile -t files < <(git status --porcelain | awk 'substr($0,1,1) != " " && substr($0,1,1) != "?" {print $2}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No staged files"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git diff -w --staged -- "${selected[0]}"
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
  mapfile -t files < <(git status --porcelain | awk '{print $2}')
  if [[ ${#files[@]} -eq 0 ]]; then
    echo "No modified files"
    return 1
  fi
  mapfile -t selected < <(printf '%s\n' "${files[@]}" | __git_select --multi)
  [[ ${#selected[@]} -eq 0 ]] && return 0
  git checkout -- "${selected[@]}"
}

# ============================ Cherry-Pick ============================
alias gcp="git cherry-pick" # Apply the changes introduced by an existing commit
alias gcpa="git cherry-pick --abort" # Cancel the cherry-picking operation and return to the pre-sequence state.
alias gcpc="git cherry-pick --continue" # Continue the cherry-picking operation in progress.

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

  case "$remote_url" in
    *github.com*)
      printf '%s\n' "$(printf '%s' "$remote_url" | sed -Ee 's#git@github\.com:#https://github.com/#' -e 's%\.git$%%')"
      ;;
    git@ssh.dev.azure.com:*)
      local path org project repo
      path="${remote_url#git@ssh.dev.azure.com:v3/}"
      IFS='/' read -r org project repo <<< "$path"
      printf 'https://dev.azure.com/%s/%s/_git/%s\n' "$org" "$project" "$repo"
      ;;
    https://dev.azure.com/* | https://*@dev.azure.com/*)
      # Strip embedded credentials if present (e.g. https://Org@dev.azure.com/... → https://dev.azure.com/...)
      printf '%s\n' "$(printf '%s' "${remote_url%.git}" | sed 's#https://[^@/]*@#https://#')"
      ;;
    git@vs-ssh.visualstudio.com:*)
      local path org project repo
      path="${remote_url#git@vs-ssh.visualstudio.com:v3/}"
      IFS='/' read -r org project repo <<< "$path"
      printf 'https://dev.azure.com/%s/%s/_git/%s\n' "$org" "$project" "$repo"
      ;;
    https://*.visualstudio.com/*)
      printf '%s\n' "${remote_url%.git}"
      ;;
    *)
      printf 'Error: unsupported remote URL format: %s\n' "$remote_url" >&2
      return 1
      ;;
  esac
}

# Create a pull request and open it in the default browser
function gpr() {
  local base_url branch_name main_branch pr_url

  base_url=$(__git_web_url) || { echo "Error: no supported remote found (GitHub or Azure DevOps)"; return 1; }

  branch_name=$(git symbolic-ref --short HEAD 2>/dev/null)
  [ -n "$branch_name" ] || { echo "Error: not in a git repository or in detached HEAD state"; return 1; }

  main_branch=$(gbdefault)
  [ -n "$main_branch" ] || { echo "Error: could not determine default branch"; return 1; }

  if [[ "$base_url" == *dev.azure.com* || "$base_url" == *visualstudio.com* ]]; then
    pr_url="$base_url/pullrequestcreate?sourceRef=refs/heads/$branch_name&targetRef=refs/heads/$main_branch"
  else
    pr_url="$base_url/compare/$main_branch...$branch_name"
  fi

  explorer.exe "$pr_url"
}

# Open the current branch or the main branch in the repository
function grepo() {
  local base_url main_branch current_branch url

  git rev-parse --is-inside-work-tree >/dev/null 2>&1 || { echo "Error: not in a git repository"; return 1; }

  base_url=$(__git_web_url) || { echo "Error: no supported remote found (GitHub or Azure DevOps)"; return 1; }

  main_branch=$(gbdefault)
  [ -n "$main_branch" ] || { echo "Error: could not determine default branch"; return 1; }

  current_branch=$(gbcurrent 2>/dev/null)
  url="$base_url"
  if [[ -n "$current_branch" && "$main_branch" != "$current_branch" ]]; then
    if [[ "$base_url" == *dev.azure.com* || "$base_url" == *visualstudio.com* ]]; then
      url="$base_url?version=GB$current_branch"
    else
      url="$base_url/tree/$current_branch"
    fi
  fi

  explorer.exe "$url"
}

# ============================ Utils ============================
# Get the default branch name with fallbacks when origin/HEAD is not configured
function gbdefault() {
  local default_branch

  default_branch=$(git symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null | cut -d'/' -f2)

  if [ -z "$default_branch" ]; then
    for branch in main master; do
      if git show-ref --verify --quiet "refs/remotes/origin/$branch" 2>/dev/null; then
        default_branch="$branch"
        break
      fi
    done
  fi

  if [ -z "$default_branch" ]; then
    default_branch=$(git remote show origin 2>/dev/null | awk -F': ' '/HEAD branch/ {print $2; exit}')
  fi

  if [ -z "$default_branch" ]; then
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

# Interactive menu picker for selecting from a list of items.
# Usage: <list> | __git_select [--multi]
# - Reads candidates from stdin (one per line).
# - If exactly one candidate exists, auto-selects it without prompting.
# - Uses fzf when installed; otherwise falls back to a numbered list prompt.
# - --multi: allow selecting more than one item (Tab in fzf; space-separated numbers in fallback).
# Output: selected item(s) on stdout, one per line.
function __git_select() {
  local multi=0
  [[ "${1:-}" == "--multi" ]] && multi=1

  local -a items
  mapfile -t items

  if [[ ${#items[@]} -eq 0 ]]; then
    return 1
  fi

  if [[ ${#items[@]} -eq 1 ]]; then
    echo "→ Auto-selected: ${items[0]}" >&2
    printf '%s\n' "${items[0]}"
    return 0
  fi

  if command -v fzf >/dev/null 2>&1; then
    if [[ $multi -eq 1 ]]; then
      printf '%s\n' "${items[@]}" | fzf --multi --header "Tab: toggle selection | Enter: confirm"
    else
      printf '%s\n' "${items[@]}" | fzf
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
fi

