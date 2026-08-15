# QualityReporter

QualityReporter combines static metrics with Git change history to identify **review and maintenance priorities**, not to declare code “good” or “bad”. It is free, local, and split into `quality-csharp` and `quality-ts`; their only shared contract is the schemas and risk definition.

## Quick start

Requires .NET 10 and Node.js 20+ (the sample Action uses Node 24). Always use a full clone (`actions/checkout` with `fetch-depth: 0`).

```bash
dotnet run --project csharp/src/QualityReporter.CSharp -- analyze \
  --solution App.sln --since 180d --coverage coverage.cobertura.xml \
  --analyzer build.log --config quality.json --baseline previous.json \
  --output reports/csharp.json --markdown reports/csharp.md

cd typescript && npm ci && npm run build
node dist/cli.js analyze --root ../frontend --eslint ../reports/eslint.json \
  --coverage ../coverage/coverage-final.json --since 180d --config ../quality.json \
  --baseline ../previous.json --output ../reports/typescript.json --markdown ../reports/typescript.md
```

C# coverage accepts Cobertura. Analyzer diagnostics can be captured from `dotnet build` output. TypeScript coverage accepts Istanbul `coverage-final.json`, and lint input is ESLint JSON. Missing optional inputs produce warnings and do not stop analysis.

## Metrics

* **Commit count** is the number of non-merge commits touching a file in the period; **churn** is added plus deleted lines; **author count** uses case-insensitive author email.
* **Rework rate** is the number of changes occurring within the configured window after a prior change divided by all changes.
* **Change coupling** records files changed together, excluding commits over the configured file limit and applying minimum count/ratio filters.
* Current-source **LOC and cyclomatic complexity** are collected per file and function/method. TS/TSX uses the TypeScript Compiler API; TSX is treated as TypeScript. C# uses Roslyn syntax trees.
* Coverage and analyzer/lint issue counts are optional. Coverage risk reverses line coverage so low coverage is risky.

Risk uses repository percentiles: change 25%, churn 15%, rework 20%, complexity 20%, coverage deficiency 15%, issues 5%. Missing metrics are omitted and remaining weights are re-normalized. Levels are Critical ≥80, High ≥60, Medium ≥40, and Low <40; files scoring ≥60 are hotspots. Baselines identify new/resolved hotspots and increased/decreased file risk.

## Configuration and outputs

Start with [`examples/quality.json`](examples/quality.json). Machine-readable contracts are in [`schema/`](schema/). JSON contains `schemaVersion: 1`; Markdown is designed for `$GITHUB_STEP_SUMMARY`. Exit codes are 0 success, 1 analysis error, and 2 invalid arguments (3 is reserved for future quality gates).

## Unit tests and coverage

```bash
dotnet test csharp/QualityReporter.CSharp.slnx --collect:"XPlat Code Coverage"
cd typescript && npm run coverage
```

The .NET command writes Cobertura under `TestResults`; the TypeScript command prints a c8 summary and writes `typescript/coverage/coverage-summary.json`. Coverage focuses on independently testable analyzers, parsers, risk/baseline logic, and reporters. Process-launching CLI wiring remains covered by end-to-end smoke checks.

## Limits

v1 does not infer bug-fix intent, call GitHub APIs, store history, track method history/renames, detect clones, or provide an architecture score/UI. Rename parsing follows Git numstat output; binary numstat entries do not contribute churn. Complexity is control-flow syntax based and is a prioritization signal, not a semantic quality verdict.
