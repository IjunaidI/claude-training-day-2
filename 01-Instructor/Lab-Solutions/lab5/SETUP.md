# Lab 5 setup: Claude reviews every PR

Do this at least two days before the workshop. It takes about 20 minutes, plus one dry-run PR.

The workflow file is already in the student repo at `.github/workflows/claude-review.yml`
(identical copy here: `claude-review.yml`). You provide the repository, the GitHub App, and the key.

## 1. Create the training repository

1. In the client's GitHub organization, create a **private** repository, for example
   `indusmotor/assetdesk-lab`. No README, no licence, no .gitignore: it must start empty.
2. Build the local repo and push it (see `05-Lab-Repo/README.md`):

   ```bash
   bash 05-Lab-Repo/make-lab-repo.sh ../assetdesk-lab
   cd ../assetdesk-lab
   git remote add origin https://github.com/<org>/assetdesk-lab.git
   git push -u origin main reference-build --tags
   ```

Check: the repo shows branches `main` and `reference-build`, tag `start`, and the Actions tab lists
"Claude review".

## 2. Install the Claude GitHub App

Open https://github.com/apps/claude, choose **Install**, pick the organization, and select **Only
select repositories** → the training repository. An organization owner may need to approve it.

The app is what posts the comments. The workflow's `id-token: write` permission lets the action
exchange a GitHub OIDC token for a short-lived app token.

## 3. Add the API key secret

1. In the Claude Console (https://platform.claude.com), create a **separate workspace** for the
   workshop and set a **monthly spend limit** on it. Create an API key in that workspace.
2. In the repository: **Settings → Secrets and variables → Actions → New repository secret**.
   Name `ANTHROPIC_API_KEY`, value the key.

Use a Console key, not a personal subscription token. A `CLAUDE_CODE_OAUTH_TOKEN` from
`claude setup-token` also works (the workflow shows the one-line change), but it bills one person's
subscription and is tied to that person.

After the workshop, delete the secret **and** revoke the key in the Console. Deleting the secret
alone leaves the key valid.

## 4. Give attendees access

**Settings → Collaborators and teams** → add a team with the attendees, role **Write**. They need
write access to push branches and open PRs, and the action only runs for users with write access.

Branch protection is optional. If you want it, protect `main` only (require a PR, no direct pushes).
Do not protect `build/*`: attendees push there all afternoon.

## Shortcut: `/install-github-app`

From a clone of the training repo, run `claude`, then `/install-github-app`. It installs the app,
stores the secret, and opens a PR that adds its own workflow files. You still want **our**
`claude-review.yml`: close or edit that PR so the repository keeps one review workflow, or the room
gets two reviews per PR.

## 5. Dry-run it with one PR

From your clone of the training repo:

```bash
git switch -c build/instructor reference-build
git push -u origin build/instructor
git switch -c feature/instructor-dryrun
```

Make one deliberate violation. In `AssetDesk/Components/Pages/Home.razor`, in `HandleReturn`,
change `_errorMessage = ex.Message;` to `_errorMessage = "Something went wrong.";`. Then:

```bash
git commit -am "Dry run: swallowed error message"
git push -u origin feature/instructor-dryrun
gh pr create --base build/instructor --title "Dry run" --body "Checking the review workflow."
```

Check, within about five minutes:
- The Actions tab shows **Claude review** running, then green.
- One inline comment on the changed line cites `SPEC.md §4.6` (or `csharp-quality §1` if the
  base branch has the Lab 3 skills).
- One summary comment ends with the `SPEC / QUALITY / OTHER / TOTAL` block.
- Push one more commit: the review runs again, and a run still in progress is cancelled.

If the run fails: a 401 or "credit balance" error is the secret or the Console workspace. "Resource
not accessible by integration" is the app not installed on this repository. No comments but a green
run: read the run log. The review is there, which means the prompt's comment step failed; check that
`claude_args` still lists the four tools.

Close the dry-run PR and delete both branches before the workshop.

## Cost

Each review is one Claude Code run: it reads SPEC.md (about 30 KB), the feature spec, the skills,
and the diff, then writes comments. Expect a small fraction of a dollar to about a dollar per run at
the default model, depending on diff size and model. Check current rates at
https://claude.com/pricing before quoting a number. Every push to an open PR is another run.

For a room of 20: roughly 20 PRs times 2 to 3 pushes, so 40 to 60 runs in Lab 5. Set the workspace
spend limit to a comfortable multiple of that, and check actual spend in the Console after the
dry run. GitHub Actions minutes are separate: each run takes a few minutes of an Ubuntu runner.

The workflow caps each run with `--max-turns 40` and `timeout-minutes: 15`, skips draft PRs, and
cancels a superseded run on a new push.
