# <Feature name>

**Version** 0.1 · **Status** Draft · **Amends** SPEC.md v3.0 §<n>, §<n>

<!--
A delta spec. It describes one change to an app that SPEC.md already defines.
Everything SPEC.md says still holds unless this file names the section it amends.
Save it as specs/<feature-name>.md. Delete these comments when the spec is done.
-->

## 1. Context

<!--
Two or three sentences. Who needs this, and what can they not do today?
Name the SPEC.md sections this touches. No solution here.
-->

## 2. The change

<!--
What the app does after this change that it did not do before. Plain sentences, one behaviour each.
Use exact names from SPEC.md (types, columns, endpoints, labels). No adjectives without a number
or an example: "fast" is not a requirement, "newest first" is.
-->

## 3. Non-goals

<!--
What a reasonable engineer might add and must not. One bullet each.
SPEC.md §2 still applies. List only what this feature makes tempting.
-->

- No ...

## 4. Contract

<!--
The exact surface. An agent copies these names verbatim, so write them once, here.
Delete a subsection only if the feature genuinely does not touch it.
-->

### 4.1 Schema

<!-- New tables or columns as SQL, in the style of SPEC.md §4.5. IF NOT EXISTS throughout. -->

```sql
```

### 4.2 Models

<!-- New records or enums as C#, in the style of SPEC.md §4.1. -->

```csharp
```

### 4.3 Repository surface

<!-- New or changed AssetRepository methods as signatures, in the style of SPEC.md §4.6. -->

```csharp
```

### 4.4 API

<!-- New endpoints in the table format of SPEC.md §4.7. Show one example response body. -->

| Method | Path | Body | Returns |
|---|---|---|---|
| | | | |

### 4.5 UI

<!-- Where it appears, what it shows, in what order. Reuse the components named in SPEC.md §3.2. -->

## 5. Errors

<!--
Every failure case and the exact message. Reuse the SPEC.md §4.6 messages verbatim where a case
already exists (for example an unknown id is "That asset no longer exists. Refresh to see current
data."). Write a new message only for a genuinely new case, in the same voice: what happened, and
what to do. The API returns 400 {"error": "<message>"}; the UI shows the message verbatim.
-->

| Case | Message |
|---|---|
| | |

## 6. Acceptance criteria

<!--
Each criterion is a test someone could write without asking you a question.
Given <starting state>, when <one action>, then <observable result>. Number them: AC-1, AC-2, ...
Use the SPEC.md §7 seed data for concrete values (AST-1003 is in stock, AST-1004 is in repair).
-->

- **AC-1** Given ..., when ..., then ...
- **AC-2** Given ..., when ..., then ...

## 7. Tests

<!--
Which acceptance criteria become automated tests, where the test project lives, and what it may
reference. This section amends SPEC.md §2 ("No tests") for this feature only; say so explicitly.
Name the framework and the test naming convention.
-->

## 8. Done when

<!-- A checklist a reviewer can tick without judgement. Include the SPEC.md §9 items that still apply. -->

- [ ] `dotnet build` succeeds with zero warnings
- [ ] Every acceptance criterion in §6 has a passing test, or a curl check written next to it
- [ ] Every error in §5 appears verbatim in the API response and in the UI
- [ ] SPEC.md is unchanged
- [ ] Nothing in §3 has been built

## 9. Working agreement for the agent

<!-- Short. SPEC.md §10 still applies; add only what this feature needs. -->

1. Read SPEC.md and this file before writing code.
2. Write the failing tests from §6 first, then implement until they pass.
3. Do not edit SPEC.md. If this spec contradicts it, stop and say which clause.
4. If something here is ambiguous, pick the simplest reading, state the assumption in one line, and keep going.
5. Commit once the tests pass, with the feature name in the message.
