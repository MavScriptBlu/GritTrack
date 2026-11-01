## 2024-11-01 - Temporary File Auto-Staging

**Learning:** When creating a temporary benchmark project folder (e.g., `benchmark/`), any newly created files can be automatically staged by Git within the agent environment even if we `rm -rf benchmark/`.
**Action:** Always verify with `git diff --staged --name-only` to ensure only the intended modified files are staged. If the temporary files are staged but already deleted from disk, `git reset HEAD benchmark/` may fail. In such cases, explicitly `git rm --cached <files>` or just run `git reset HEAD <files>`.

## 2024-11-01 - Temporary File Auto-Staging

**Learning:** When creating a temporary benchmark project folder (e.g., `benchmark/`), any newly created files can be automatically staged by Git within the agent environment even if we `rm -rf benchmark/`.
**Action:** Always verify with `git diff --staged --name-only` to ensure only the intended modified files are staged.
