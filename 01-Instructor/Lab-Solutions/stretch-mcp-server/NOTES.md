# Stretch: build-your-own MCP server (optional instructor demo)

**Not part of the student labs.** Lab 6 uses two ready-made servers (Microsoft Learn and
Playwright; see `../lab6/NOTES.md`). This folder is an optional extra: a live demo of what is inside
an MCP server, for a room that finishes Lab 6 early or asks "how would we build one for our own
API?", or a stretch for one fast attendee you hand the folder to. It needs nothing beyond the .NET
10 SDK and a NuGet restore.

`AssetDeskMcp/` (in this folder) is a stdio MCP server (C#, `ModelContextProtocol` 2.2.0,
net10.0) over the AssetDesk HTTP API in SPEC.md §4.7. Two tools, two planted flaws, each marked
`// LAB 6:` in `AssetDeskTools.cs` (the marker dates from when this was Lab 6). The answer key is `AssetDeskTools.fixed.cs` in this folder: a drop-in
replacement for that file. `Program.cs` and the `.csproj` do not change.

Everything below was run end to end against the reference build on .NET SDK 10.0.302 and Claude
Code 2.1.259, driving the server over stdio with raw JSON-RPC.

## Seed facts the demo uses (SPEC.md §7)

| Tag | Device | Status in `/api/state` | Use |
|---|---|---|---|
| AST-1003 | Lenovo ThinkPad X1 Carbon G11 | `InStock` | The only in-stock laptop. Answer to "which laptops are in stock?" |
| AST-1004 | Lenovo ThinkPad T14 G4 | `Repair` | The in-repair laptop. Assigning it must fail |

Employee ids are GUIDs generated at seed time, so they differ per machine. `list_assets` returns the
employee list (id, name, department) next to the assets, so Claude can resolve "Aisha Rahman" to an
id without a third tool. The status enum member is `Repair`, not `InRepair`; the UI label is
"In repair".

## Put it in a clone

The student repository no longer ships it. Copy it into the clone you demo from (any clone with a
working `AssetDesk/`, for example on `reference-build`), under `tools/` so the commands below work
unchanged. From the clone's root, with `<package>` the path to this workshop package:

```bash
mkdir -p tools && cp -r <package>/01-Instructor/Lab-Solutions/stretch-mcp-server/AssetDeskMcp tools/
dotnet build tools/AssetDeskMcp        # restores once, so the first connect is quick
```

```powershell
New-Item -ItemType Directory -Force tools | Out-Null
Copy-Item -Recurse <package>\01-Instructor\Lab-Solutions\stretch-mcp-server\AssetDeskMcp tools\
dotnet build tools/AssetDeskMcp
```

Do not commit `tools/` or the `.mcp.json` below to a branch attendees pull. Afterwards:
`claude mcp remove --scope project assetdesk` and delete `tools/`. `AssetDesk/` never references
it, and `dotnet build AssetDesk` ignores it.

## What you type

**T2**, from the repository root, on the branch that has a working `AssetDesk/`:

```bash
dotnet run --project AssetDesk --urls http://localhost:5198
```

**T1**, from the repository root, in a plain shell (`/exit` Claude first if it is running):

```bash
claude mcp add --transport stdio --scope project assetdesk --env 'ASSETDESK_URL=${ASSETDESK_URL:-http://localhost:5198}' -- dotnet run --project tools/AssetDeskMcp
```

The single quotes matter: they stop the shell expanding `${...}` so Claude Code expands it at
launch. The same line works in PowerShell. It writes this `.mcp.json` at the repository root, which
you can also create by hand (identical on macOS and Windows):

```json
{
  "mcpServers": {
    "assetdesk": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "tools/AssetDeskMcp"
      ],
      "env": {
        "ASSETDESK_URL": "${ASSETDESK_URL:-http://localhost:5198}"
      }
    }
  }
}
```

Then restart Claude and approve the server:

```bash
claude --permission-mode default     # or: claude --continue --permission-mode default
```

Claude Code asks once whether to use the project's MCP server `assetdesk`: approve it. Then:

```
/mcp
```

Check: `assetdesk` shows **connected** with 2 tools. From a shell, `claude mcp list` shows
`assetdesk: dotnet run --project tools/AssetDeskMcp - ✔ Connected` once approved (before approval
it shows `⏸ Pending approval (run claude to approve)`).

### Why this launch command

- `dotnet run --project tools/AssetDeskMcp` with **no** `--no-build`: every connect and reconnect
  rebuilds the server, so "edit the tool, reconnect" picks up the edit with no separate build step.
  Build output does not reach stdout (tested from a clean `bin/obj` and after an edit), so it does
  not corrupt the JSON-RPC stream. Logging is on stderr by design (`LogToStandardErrorThreshold`).
- Build it once after copying it in (above), so the first connect takes a few seconds, not a
  restore. From a clean copy the first connect took about 13 seconds including restore, inside
  Claude Code's startup timeout.
- The relative path resolves because Claude Code starts project-scope servers in the project
  directory. Start `claude` from the repository root.
- `dotnet` is a real executable on Windows, so no `cmd /c` wrapper is needed.

## The two prompts

**Prompt 1:** `Which laptops are in stock?`

The only tool description is "Gets data." Claude has to guess what `list_assets` returns and what
its `status` and `category` parameters accept. What to look for in the transcript: it calls
`list_assets` with no filters and filters the result itself, passes a status spelling the API does
not use, or goes around the tool entirely (`curl` is allowed in `.claude/settings.json`, and it can
read the code). It often still gets the right answer with only two tools on offer; the lesson is
that it got there by guessing. With 30 tools on a real server, the guess fails.

Correct answer: **AST-1003**, Lenovo ThinkPad X1 Carbon G11, HQ / Store Room.

Fix 1: ask Claude to rewrite the descriptions in `tools/AssetDeskMcp/AssetDeskTools.cs`: what the
tool returns, when to use it, and a `[Description]` on each parameter listing the accepted values.
Reconnect and ask again. Check: Claude calls `list_assets` with `status: "InStock"`,
`category: "Laptop"`.

**Prompt 2:** `Assign AST-1004 to Aisha Rahman.`

AST-1004 is in repair. The API answers 400 `{"error":"AST-1004 is in repair and cannot be assigned."}`.

| | What the tool returns to Claude |
|---|---|
| Planted | `Something went wrong.` (not even flagged as an error: `isError` is absent) |
| Fixed | `AST-1004 is in repair and cannot be assigned.` with `isError: true` |

With the planted version Claude cannot tell the user why, and tends to retry, guess ("the service may
be down"), or try another laptop. With the fixed version it reports the API's reason. That is
SPEC.md §4.6 ("the UI shows the message verbatim and never writes its own copy") carried across one
more boundary: the tool is a UI whose user is a model.

Fix 2: ask Claude to make `assign_asset` return the API's `error` message verbatim. Reconnect, retry.

Other cases, both verified:

| Call | Planted | Fixed |
|---|---|---|
| Unknown asset id (or a tag passed as the id) | `Something went wrong.` | `That asset no longer exists. Refresh to see current data.` (isError) |
| In-stock AST-1003, fabricated employee id | `Something went wrong.` | `That asset no longer exists. Refresh to see current data.` (isError, invariant 5) |
| AST-1003 to a real employee | the updated asset JSON | the updated asset JSON |

A successful assignment changes the data. Reset it afterwards (INSTRUCTIONS §5).

## Throw or return? What the SDK actually sends

Tested with `ModelContextProtocol` 2.2.0:

| Tool does | Claude receives |
|---|---|
| `throw new McpException("AST-1004 is in repair and cannot be assigned.")` | `An error occurred invoking 'assign_asset': AST-1004 is in repair and cannot be assigned.` (isError) |
| `throw` any other exception | `An error occurred invoking 'assign_asset'.` (isError, message hidden) |
| `return new CallToolResult { Content = [new TextContentBlock { Text = msg }], IsError = true }` | `AST-1004 is in repair and cannot be assigned.` (isError) |

The fixed file returns a `CallToolResult`, because it is the only option that passes the message
through byte for byte. A student fix that throws `McpException(message)` is acceptable: the message
arrives, with a prefix. A fix that throws anything else is not: the message is hidden again.

If the app is not running, any tool call answers `An error occurred invoking 'list_assets'.` That
is the generic path (an `HttpRequestException`); start the app in T2.

## Reconnecting after an edit

`/mcp` → `assetdesk` → **Reconnect**. Claude Code restarts the server and `dotnet run` rebuilds it.
A compile error shows as a failed connection: run `dotnet build tools/AssetDeskMcp` to see it.

**Windows:** do not run `dotnet build tools/AssetDeskMcp` while the server is connected. The running
server holds `bin\...\AssetDeskMcp.dll` and the build fails with `MSB3027`/`MSB3021` (file in use).
Edit, then reconnect, and let `dotnet run` build. If reconnect itself reports a file-in-use error,
`/exit` and `claude --continue`, which stops the old server before starting the new one. (Not
reproduced here: the test machine is a Mac, where the lock does not occur.)

## Line count

`AssetDeskTools.cs` 67 lines, `Program.cs` 21 lines. The fixed tools file is 92 lines.
