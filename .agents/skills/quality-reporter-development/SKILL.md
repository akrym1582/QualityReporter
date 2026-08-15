---
name: quality-reporter-development
description: Develop, review, debug, or extend the QualityReporter repository's C# and TypeScript/React CLIs, including Git history metrics, Roslyn or TypeScript complexity analysis, coverage and lint parsers, percentile risk scoring, baselines, JSON schemas, Markdown reports, tests, and GitHub Actions. Use for any implementation, schema, testing, packaging, or CI change in this repository.
---

# QualityReporter development

## Workflow

1. Read the root `AGENTS.md`, then inspect the relevant implementation, schema, tests, and README before editing.
2. Decide whether the change is language-specific or affects the shared contract. For shared behavior, update both CLIs and add equivalent tests.
3. Preserve these invariants:
   - Treat risk as prioritization, not a quality verdict.
   - Keep C# and TypeScript runtime implementations separate.
   - Use one or a few repository-wide Git commands, abstracted behind the Git runner interface.
   - Omit unavailable metrics and re-normalize available risk weights; never convert missing data to zero.
   - Keep nonfatal missing coverage/analyzer/linter input as a report warning.
4. Use Roslyn for `.cs` and the TypeScript Compiler API for `.ts`/`.tsx`; do not write custom parsers.
5. Add focused unit tests. Mirror tests across languages when shared risk, baseline, Git, or reporting semantics change.
6. Run `scripts/validate.sh`. Inspect coverage output, not just the test exit status.
7. Run an end-to-end CLI smoke test when changing CLI wiring, discovery, configuration, or report generation.
8. Check `git diff --check` and ensure generated outputs remain untracked before committing.

## Review checklist

- Verify JSON remains compatible with `schema/quality-report.schema.json` and config with `schema/quality-config.schema.json`.
- Verify risk boundaries are Low `<40`, Medium `40–59`, High `60–79`, Critical `80–100`; hotspots start at 60.
- Verify coverage risk direction is reversed.
- Verify merge and bulk-commit filtering, rename/delete/binary handling, rework window, and directional coupling ratio when Git logic changes.
- Verify Markdown contains textual risk levels and `Not Available` for missing optional metrics.
- Keep exit codes: 0 success, 1 analysis error, 2 invalid arguments, 3 reserved for quality-gate failure.
