# AGENTS.md

## Purpose

This file provides instructions for AI agents and automated coding assistants working in this repository.

The goal is to preserve the current architecture, make safe incremental changes, and leave the codebase easier to understand than before.

---

## Core expectations

When working in this project, always:

- follow the existing architecture and folder structure
- prefer small, focused changes over broad rewrites
- preserve existing behavior unless the task explicitly requires a behavior change
- comment your work where clarification is useful
- explain architectural decisions in code comments or commit notes when the reason is not obvious
- avoid introducing new patterns when an existing project pattern already solves the problem
- keep code readable, maintainable, and consistent with surrounding files

If you are unsure how something should be implemented, first inspect nearby files and mirror the established project conventions.

---

## Architecture rules

### 1) Respect the current architecture
Do not reorganize major layers, rename core directories, or introduce new architectural patterns unless explicitly requested.

Examples:
- if the project already separates controllers, services, repositories, models, and data access, keep that separation
- if dependency injection is already being used, continue using it
- if there is an existing DTO / domain / persistence split, do not collapse those concerns together

### 2) Keep responsibilities separated
Do not mix unrelated responsibilities in one class or file.

General expectations:
- controllers/endpoints should handle request/response flow, not business logic
- services should contain business rules and orchestration
- repositories/data access should handle persistence concerns
- models/entities should represent data, not workflow logic
- UI components should not absorb backend business rules that belong in the server

### 3) Prefer extending existing patterns
Before creating a new abstraction, check whether the repo already has:
- a service pattern
- a repository pattern
- a validation pattern
- a logging pattern
- a configuration pattern
- an error-handling pattern

Prefer consistency over novelty.

---

## Commenting requirements

### 4) Comment all non-obvious work
All changes should include comments where they help future maintainers understand:
- why the change was made
- architectural constraints being preserved
- assumptions
- tradeoffs
- temporary limitations
- edge-case handling

Do **not** add useless comments that merely restate the code.

Good:
- explain why a validation rule exists
- explain why a query must stay server-side
- explain why a call must remain transactional
- explain why a null case is intentionally handled a certain way

Bad:
- `// set variable`
- `// loop through items`

### 5) Preserve and improve existing comments
Do not delete useful comments unless they are outdated and replaced with a better explanation.

When touching a confusing area, improve nearby comments if doing so reduces future ambiguity.

### 6) Mark temporary work clearly
Any temporary workaround, hack, or follow-up item must be labeled clearly with a consistent marker such as:

`TODO:`
`FIXME:`
`NOTE:`

Include:
- what remains to be done
- why it is temporary
- what risk or limitation remains

Example:
```csharp
// TODO: Replace this fallback query once the unified search service is available.
// Current approach is duplicated to avoid changing the request contract in this task.