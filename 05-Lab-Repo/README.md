# 05-Lab-Repo — the Day 2 training repository

`AssetDesk-Lab/` holds the student repository as plain files. The script turns it into a git
repository with the branches and tag the labs expect. Then you push that to GitHub.

| Branch / tag | Contents |
|---|---|
| `main`, tag `start` | `AssetDesk-Lab/` exactly: spec, tooling, workflow, no app |
| `reference-build` | `start` + the finished app from `01-Instructor/Lab-Solutions/reference-build/AssetDesk/` as `AssetDesk/` |

A finished app is never on `main`. An agent that finds one copies it instead of building from the spec.

## Build the repository

From the package root:

```bash
bash 05-Lab-Repo/make-lab-repo.sh ../assetdesk-lab
```

```powershell
powershell -ExecutionPolicy Bypass -File 05-Lab-Repo/make-lab-repo.ps1 ..\assetdesk-lab
```

The target folder must not exist. Without an argument it is `./assetdesk-lab-out`. The script copies
the files without `bin/`, `obj/` or databases, commits "Initial lab state" on `main`, tags `start`,
creates `reference-build` with the finished app, switches back to `main`, and prints the push
commands.

Check:

```bash
cd ../assetdesk-lab
git log --oneline --all --decorate   # two commits: start on main, the reference build on reference-build
git status                           # clean, on main
```

## Push it

Create an **empty private** repository in the client's GitHub organization, then:

```bash
git remote add origin <url>
git push -u origin main reference-build --tags
```

Then follow `01-Instructor/Lab-Solutions/lab5/SETUP.md` to install the Claude GitHub App, add the
`ANTHROPIC_API_KEY` secret, and give attendees write access. Lab 5 does not work without it.

## Changing the lab

Edit the files in `AssetDesk-Lab/`, never the generated repository, then rebuild it. Keep `SPEC.md`
byte-identical: every answer key and the reference build are checked against it.

To reset the GitHub repository after a workshop, delete it and push a freshly generated one.
