# Workspace Main Branch Merge Verification Plan

## Objective and scope

Verify every repository in the current multi-root workspace is on `main`, determine whether any local or remote side branches contain commits that have not been merged into `main`, merge any pending branch work locally when required, validate the affected projects, and push production-ready updates to each repository's `main` branch.

This task is workspace-maintenance oriented. It does not intentionally change application behavior unless unmerged branch work is discovered and merged.

## Affected repositories/projects

- `d:\repos\TripleG3\TripleG3.Windows.Shell`
- `d:\repos\TripleG3\TripleG3.Windows.Shell.UI`
- `d:\repos\TripleG3\TripleG3.Windows.Shell.Azure`
- `d:\repos\TripleG3\TripleG3.Windows.Shell.Web`
- `d:\repos\TripleG3\TripleG3.Windows.Shell.Alice`
- `d:\repos\TripleG3\TripleG3.Windows.Shell.Mobile`

## Current-state findings

Initial branch/status audit after `git fetch --prune origin` found:

| Repository | Current branch | Upstream | Tracked status | Local/remote branches not merged into `main` |
| --- | --- | --- | --- | --- |
| `TripleG3.Windows.Shell` | `main` | `origin/main` | clean and aligned | none |
| `TripleG3.Windows.Shell.UI` | `main` | `origin/main` | clean and aligned | none |
| `TripleG3.Windows.Shell.Azure` | `main` | `origin/main` | clean and aligned | none |
| `TripleG3.Windows.Shell.Web` | `main` | `origin/main` | clean and aligned | none |
| `TripleG3.Windows.Shell.Alice` | `main` | `origin/main` | clean and aligned | none |
| `TripleG3.Windows.Shell.Mobile` | `main` | `origin/main` | clean and aligned | none |

Additional observations:

- Local merged side branches still exist in some repositories, but `git branch --no-merged main` reported no unmerged local branch work.
- Remote tracking refs for deleted side branches were pruned in `TripleG3.Windows.Shell`, `TripleG3.Windows.Shell.UI`, and `TripleG3.Windows.Shell.Mobile`.
- No source-code merge is currently indicated by the audit.
- The first pushed audit commit triggered `TripleG3.Windows.Shell`'s NuGet publish workflow. Build, test, pack, and the first package/symbol publish succeeded, but the workflow failed by attempting to push the same `.snupkg` a second time.
- The workflow remediation removes the explicit second symbol push and keeps `--skip-duplicate` on the package push. The .NET CLI already discovers and pushes the adjacent `.snupkg` when pushing the `.nupkg`.
- Plan review confirmed that deleting already-merged local branches is unnecessary and intentionally omitted; the user asked to preserve `main` as the active working branch and merge unpublished work, not to prune local branch names.

## Implementation checklist

- [x] Inspect all repository branches, upstreams, HEAD commits, and tracked working-tree status.
- [x] Fetch and prune remotes in every repository.
- [x] Identify local branches not merged into `main`.
- [x] Identify remote branches not merged into `origin/main`.
- [x] Re-review and refine this plan before taking any merge or commit action.
- [x] Merge pending branch work into `main` if discovered during the final audit. No merge was required; final audit found zero unmerged local or remote branches.
- [ ] Validate every affected repository according to the validation matrix.
- [ ] Record validation evidence in this plan.
- [ ] Commit and push task-related changes to `main`.

## Acceptance criteria

- Every repository remains on `main`.
- Every repository has `main` tracking `origin/main`.
- No local or remote side branch has commits missing from `main`, or any such commits are merged, validated, and pushed.
- No unrelated local work or ignored secret files are committed.
- Any repository with merged code changes passes the relevant build/test checks before push.
- This plan records final validation evidence and any deviations from the original plan.

## Validation matrix

| Repository | Required validation | Result/evidence |
| --- | --- | --- |
| `TripleG3.Windows.Shell` | `git status --short --branch`; branch merge audit; publish workflow validation because this repo changed | In progress: branch audit passed with local unmerged count `0` and remote unmerged count `0`; first publish run built/tested/packed and published `1.0.4`, then failed on duplicate `.snupkg` push; workflow fix pending validation. |
| `TripleG3.Windows.Shell.UI` | `git status --short --branch`; branch merge audit; build/test only if code merged | Passed: on `main`, tracking `origin/main`; local unmerged count `0`; remote unmerged count `0`; no tracked changes. |
| `TripleG3.Windows.Shell.Azure` | `git status --short --branch`; branch merge audit; build/test only if code merged | Passed: on `main`, tracking `origin/main`; local unmerged count `0`; remote unmerged count `0`; no tracked changes. |
| `TripleG3.Windows.Shell.Web` | `git status --short --branch`; branch merge audit; build/test only if code merged | Passed: on `main`, tracking `origin/main`; local unmerged count `0`; remote unmerged count `0`; no tracked changes. |
| `TripleG3.Windows.Shell.Alice` | `git status --short --branch`; branch merge audit; build/test only if code merged | Passed: on `main`, tracking `origin/main`; local unmerged count `0`; remote unmerged count `0`; no tracked changes. |
| `TripleG3.Windows.Shell.Mobile` | `git status --short --branch`; branch merge audit; build/test only if code merged | Passed: on `main`, tracking `origin/main`; local unmerged count `0`; remote unmerged count `0`; no tracked changes. |

Because the final audit shows no branch-code merges are needed, application source builds are only required through workflows triggered by changed repositories. The `TripleG3.Windows.Shell` publish workflow is now in scope because this repo contains the audit plan and workflow remediation.

## Risks, rollback notes, and open questions

### Risks

- A side branch may be merged already but still present locally; deleting such branches is intentionally out of scope to avoid removing potentially useful local references.
- A remote branch could be created while this task is in progress; final validation should fetch/prune and re-check immediately before commit/push.
- Plan-only changes still produce a repository commit; this is intentional for `/Large-Change-Set` traceability.

### Rollback notes

- If no code merge is performed, rollback is limited to reverting the plan-file commit.
- If a merge becomes necessary and later needs rollback, revert the merge commit on `main` rather than rewriting history.

### Open questions

- None at this time. The user explicitly requested local merge, validation, and push to `main` when unmerged branch work exists.

## Commit and push strategy

- Keep every repository on `main`; do not force-push or rewrite history.
- Commit only files clearly related to this audit/merge task.
- If code merges are necessary, commit them in the repository where they occur after validation.
- If no code merges are necessary, commit this plan file in `TripleG3.Windows.Shell` as the audit artifact.
- Push completed commits to each repository's tracked `origin/main`.
