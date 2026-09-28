# Claude Code Day 2 — Quiz, Form A

**When:** 10:00, before the workshop starts · **Time:** 10 minutes · **Questions:** 12

Circle one letter per question. Guessing is fine; this measures where the room starts, not you. We report the room average only.

Name or seat number (optional): ________________

---

**1. You have steered Claude through one feature in a single chat for two hours. It now contradicts a decision you agreed in the first ten minutes. What is the most likely cause?**

- A) Claude Code deletes messages older than one hour
- B) The early decision is buried under two hours of tool output in a filling context window; a decision written in a file is re-read at full strength
- C) You need a larger model
- D) Claude only reads your most recent message

**2. Which line belongs in a spec's non-goals section?**

- A) "The UI should feel fast and clean"
- B) "Build the data layer first"
- C) "`dotnet build` succeeds with zero warnings"
- D) "No Entity Framework Core and no migrations: hand-written SQL through Dapper only"

**3. Which "done when" can an agent check on its own, without asking you?**

- A) "`curl localhost:5198/api/state` returns 5 employees and 12 assets"
- B) "The data layer works well"
- C) "The API is robust"
- D) "The dashboard looks right"

**4. The spec does not say whether the search box trims spaces. The agent is halfway through a long build and you are in a meeting. What should a good working agreement tell it to do?**

- A) Stop and wait until you come back to answer
- B) Add a settings screen so the user can choose
- C) Pick the simplest reading, state the assumption in one line, and keep going
- D) Skip the search box entirely

**5. "Never write an empty `catch` block" should apply to every C# change in this repo, in every feature, and a reviewer should be able to count violations of it. Where does it belong?**

- A) In SPEC.md, next to the product's screens
- B) In a skill under `.claude/skills/`, which sessions load when relevant and the reviewer reads as a rulebook
- C) In the prompt you type each morning
- D) In a comment at the top of `Program.cs`

**6. AssetDesk already exists and runs. You want to add an asset history feature. What do you write first?**

- A) A short feature spec in `specs/` that states only what changes: what it adds, which SPEC.md sections it amends, its non-goals, API, errors, acceptance criteria, and done-when
- B) A complete copy of SPEC.md with the change edited in
- C) Nothing; describe it in chat as you go
- D) A new SPEC.md, rewritten from scratch

**7. Claude built a feature over a long session. In the same session you type `Now review your code against the spec.` What is the problem?**

- A) Claude cannot read files it wrote itself
- B) Reviews only work in CI
- C) There is none; the author knows the code best
- D) It reviews its own reasoning with the same context and will under-report; a reviewer should start in a fresh context

**8. A cold reviewer reports `QUALITY: 0` on your build. The branch has no quality skills. What does the zero tell you?**

- A) The code has no quality problems
- B) The reviewer is broken
- C) Only that the build breaks none of the quality rules the reviewer was given, and it was given none
- D) The spec is wrong

**9. A GitHub Actions step must run Claude once over a pull request and exit so the job can finish. Which flag makes that possible?**

- A) `-p` (print mode): run once, print the result, exit
- B) `--continue`
- C) `--permission-mode plan`
- D) `--resume`

**10. A teammate wants pull requests from forks reviewed too, and proposes this change to the Claude review workflow. Should you accept it?**

```diff
 on:
-  pull_request:
+  pull_request_target:
     types: [opened, synchronize]
 ...
       - uses: actions/checkout@v4
+        with:
+          ref: ${{ github.event.pull_request.head.sha }}
```

- A) Yes. More pull requests get reviewed
- B) No. `pull_request_target` runs with the repository's secrets and write token, and this checks out untrusted fork code into that job
- C) Yes. `pull_request_target` is just the newer name for the same trigger
- D) No. It should pin `actions/checkout@v3`

**11. You have added two MCP servers, and Claude now sees tools from both. You ask "How do I set the render mode for a whole Blazor Web App in .NET 10?" What decides which tool Claude calls?**

- A) The order in which you added the servers
- B) Whichever tool has the shortest name
- C) Claude calls every tool it has and merges the answers
- D) Each tool's description: Claude reads what each tool says it does and when to use it, so a vague description such as "Gets data." leads to the wrong tool, or none

**12. Every teammate who clones the repo should get the Playwright MCP server. Claude is open in T1. In T2 you run `claude mcp add --scope project playwright -- npx @playwright/mcp@latest`. What happens next?**

- A) Every open session, including T1, can use it immediately, and nobody is asked anything
- B) It is saved for you only, in every project on your machine
- C) It is written to `.mcp.json` at the repo root for you to commit; T1 does not see it until you start a new session, and each teammate approves the project server the first time
- D) Playwright is installed on each teammate's machine the next time they run `git pull`
