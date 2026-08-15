#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
cd "$repo_root"

rm -rf TestResults typescript/coverage

dotnet test csharp/QualityReporter.CSharp.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults

(
  cd typescript
  npm ci
  npm run coverage
  npm run build
)

python3 -m json.tool schema/quality-report.schema.json >/dev/null
python3 -m json.tool schema/quality-config.schema.json >/dev/null
git diff --check

printf '\nQualityReporter validation completed successfully.\n'
