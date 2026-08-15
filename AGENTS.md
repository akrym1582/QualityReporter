# QualityReporter contributor guidance

## Scope and intent

These instructions apply to the whole repository. QualityReporter identifies review and maintenance priorities; never describe its risk score as a definitive quality verdict.

Before changing implementation code, read `.agents/skills/quality-reporter-development/SKILL.md` and follow its workflow.

## Architecture

- Keep `quality-csharp` and `quality-ts` as independent implementations. Do not introduce a shared runtime library between them.
- Keep shared behavior limited to `schema/`, configuration semantics, metric names, risk calculation, and the Markdown report structure.
- Preserve `schemaVersion` and backwards compatibility. Update schemas, both implementations, tests, examples, and README together when a shared contract changes.
- Keep Git execution behind `IGitCommandRunner`/`GitRunner`. Analyze history with one or a small number of repository-wide commands; never run `git log` once per source file.
- Use Roslyn for C# syntax and the TypeScript Compiler API for TS/TSX syntax. Do not add handwritten language parsers or method-level Git history in v1.
- Missing optional inputs must create warnings and must not become zero-valued metrics. Re-normalize risk weights across available metrics.

## Code and tests

- Target Linux GitHub Actions, .NET 10, and the Node version declared by the workflow.
- Keep analyzer, parser, risk, baseline, and reporting logic independently unit-testable.
- Add regression tests for every bug fix. For behavior shared by both CLIs, add equivalent C# and TypeScript tests.
- Git tests must consider merge commits, rename/delete, binary numstat, bulk commits, exclusions, rework, and coupling thresholds when relevant.
- Parser tests should use representative fixture content and cover missing/partial optional data.
- Do not commit `bin`, `obj`, `dist`, `dist-tests`, `coverage`, `TestResults`, or generated reports.
- Do not wrap imports in try/catch blocks.

## Required validation

Run the focused tests while iterating. Before committing, run:

```bash
bash .agents/skills/quality-reporter-development/scripts/validate.sh
```

If a command cannot run because of an environment limitation, report it explicitly rather than silently skipping it. Commit all requested changes on the current branch.
