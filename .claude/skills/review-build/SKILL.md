---
name: review-build
description: Use when reviewing an AssetDesk build on this branch - dispatches a cold-context reviewer that scores the code against SPEC.md and this branch's skills and prints comparable finding counts.
---

# Review this build

Dispatch the `assetdesk-reviewer` agent. Do not review the code yourself.

## Why you must not review it yourself

If you wrote this code earlier in the conversation, reviewing it means reviewing your own reasoning,
and you will under-report. The point of the checkpoint exercise is that the finding counts are
comparable between branches — a biased reviewer destroys the comparison and the lesson with it. A
fresh agent reads the code cold.

## How

Dispatch one `assetdesk-reviewer` agent with this prompt, substituting the directory:

    Review the AssetDesk build in <directory>. Read SPEC.md and every SKILL.md under
    .claude/skills/ except review-build, then report findings in the required format.

Default `<directory>` to `AssetDesk/` when the user names none.

If the user asks for more than one directory — on checkpoint 3, `AssetDesk/` and
`reference/AssetDesk-noskills/` — dispatch **one agent per directory** so neither review contaminates
the other's context. Then print both count blocks side by side.

## Then

Print the agent's report verbatim, count block included. Do not summarise it away, do not soften a
finding, and do not offer to fix anything unless the user asks.
