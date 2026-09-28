**Subject:** Claude Code Day 2 — your GitHub username, and 20 minutes of setup

Hi all,

Day 2 of the Claude Code workshop is on **<DATE>**, 10:00 to 17:00, in **<ROOM>**. Day 1 was the tool; Day 2 is the spec writing the app: you build a working .NET app from a spec, get it reviewed by Claude on every pull request, and give Claude a browser (over MCP) to check it.

Two things before the day. Setup takes about 20 minutes.

### 1. Send your GitHub username by <DATE − 3 days>

Reply to this email with your **GitHub username**. We'll give you write access to the training repository, where you push branches and open pull requests. GitHub then sends you an invitation email: **accept it before the day.** No GitHub account yet? Create one at https://github.com/signup; a personal account is fine.

### 2. Get your laptop ready by <DATE − 2 days>

**What you need**
- A Windows or Mac laptop you can install software on
- Your Claude account. The build runs unattended for 60–80 minutes, and in one lab two Claude sessions run in parallel. **Max, Team, or Enterprise seats are strongly recommended**; on Pro you may hit your usage limit mid-afternoon. The free plan does not include Claude Code.

**Steps (Windows PowerShell)**

If you did the Day 1 setup, steps 1–3 and 6 are already done. Do steps 4 and 5 (Node.js is new for Day 2), then continue from step 7.

1. Install **Git for Windows**: https://git-scm.com/downloads/win
2. Install the **.NET 10 SDK**: https://dotnet.microsoft.com/download/dotnet/10.0
3. Install **Claude Code**. In PowerShell:
   `irm https://claude.ai/install.ps1 | iex`
   (Mac: `curl -fsSL https://claude.ai/install.sh | bash`)
4. Install **Node.js LTS** (the last lab uses it to give Claude a browser). In PowerShell:
   `winget install --id OpenJS.NodeJS.LTS -e`
   (Mac: `brew install node`, or the LTS installer from https://nodejs.org)
5. **Close PowerShell and open a new window.**
6. Set your Git name and email if you have never done so:
   `git config --global user.name "Your Name"`
   `git config --global user.email "you@indusmotor.com"`
7. After you accept the GitHub invitation, get the training repository. On Windows, clone somewhere short that OneDrive does not sync, such as `C:\src`:
   `git clone <REPO-URL>`
   `cd assetdesk-lab`
   (The folder takes the repository's name. If your clone made a different folder, `cd` into that one.)
8. Sign in to Claude Code: type `claude`, complete the browser sign-in if asked, then type `/exit`.
9. Run the check:
   `powershell -ExecutionPolicy Bypass -File scripts/check-setup.ps1`
   (Mac: `bash scripts/check-setup.sh`)
   It checks six things: git, the .NET 10 SDK, Claude Code, that you are signed in, Node.js 18 or later with `npx`, and that your network reaches the NuGet and npm package servers.
10. Check that you can push:
    `git push --dry-run origin main`
    It should say `Everything up-to-date`. Windows may open a browser to sign in to GitHub the first time.

**You are done when all six lines say `[ OK ]`, the last line says `All checks passed. You are ready for Day 2.`, and step 10 says `Everything up-to-date`.** Reply with a screenshot of that output.

If any line says `[FAIL]`, follow the fix printed under it. If you're still stuck, reply with the screenshot anyway and we'll sort it out before the day.

**Optional:** install the GitHub CLI (https://cli.github.com) and run `gh auth login`. It lets you open pull requests from the terminal; the GitHub website works just as well. On a Mac, if step 10 asks for a password, `gh auth login` fixes it: answer **Yes** to "Authenticate Git with your GitHub credentials".

### On the day
- Bring your laptop and charger.
- You'll use three terminal windows: one for the build, one plain shell, and one for a second Claude session. A second monitor helps but isn't required.
- If your office network needs a proxy for npm or NuGet, set it up before the day: the last line of the check tells you whether it works.
- Keep your Day 1 `OrderService` clone; one of the stretch exercises uses it.
- The first ten minutes are a short, anonymous quiz so we can measure what the day adds.

See you there,
<INSTRUCTOR NAME>
10Pearls
