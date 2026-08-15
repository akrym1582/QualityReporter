# QualityReporter を GitHub Enterprise 内で共通利用する方法

## 1. 目的

社内専用の QualityReporter を、GitHub Enterprise 内の複数リポジトリから GitHub Actions で共通利用します。

各業務リポジトリへ QualityReporter 本体をコピーせず、中央管理された Internal Repository から呼び出します。

基本構成は次のとおりです。

```text
Internal Repository
+ Reusable Workflow
+ Composite Actions
```

## 2. 推奨構成

QualityReporter 専用リポジトリを 1 つ作成します。

```text
Enterprise
│
├── platform/quality-reporter        # Internal Repository
│   │
│   ├── csharp/
│   │   └── QualityReporter.CSharp
│   │
│   ├── typescript/
│   │   └── QualityReporter.TypeScript
│   │
│   ├── actions/
│   │   ├── csharp/
│   │   │   └── action.yml
│   │   └── typescript/
│   │       └── action.yml
│   │
│   └── .github/
│       └── workflows/
│           └── quality.yml
│
├── system-a/
│   └── .github/workflows/quality.yml
│
├── system-b/
│   └── .github/workflows/quality.yml
│
└── system-c/
    └── .github/workflows/quality.yml
```

`quality-reporter` リポジトリは `Internal` に設定し、Enterprise 内からのみ利用できるようにします。

## 3. 責務分担

### QualityReporter リポジトリで中央管理するもの

- Git 履歴解析
- Complexity 解析
- Coverage 解析
- Rework 解析
- Change Coupling
- Untested Complexity
- Duplicate Code 解析
- Risk Score
- Hotspot Priority
- Recommendation
- Markdown Report 生成
- JSON Report 生成

### 各業務リポジトリで保持するもの

```text
.github/workflows/quality.yml
.qualityreporter.json
```

業務リポジトリ側では QualityReporter の内部実装を意識しない構成にします。

## 4. Reusable Workflow

QualityReporter 側に共通 Workflow を作成します。

ファイル: `.github/workflows/quality.yml`

```yaml
name: Quality Reporter

on:
  workflow_call:
    inputs:
      solution:
        type: string
        required: false

      typescript-root:
        type: string
        required: false

      config:
        type: string
        required: false
        default: ".qualityreporter.json"

jobs:
  quality:
    runs-on: ubuntu-latest

    permissions:
      contents: read

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.x"

      - uses: actions/setup-node@v4
        with:
          node-version: "24"

      - name: Analyze C#
        if: inputs.solution != ''
        uses: company/quality-reporter/actions/csharp@v1
        with:
          solution: ${{ inputs.solution }}
          config: ${{ inputs.config }}

      - name: Analyze TypeScript
        if: inputs.typescript-root != ''
        uses: company/quality-reporter/actions/typescript@v1
        with:
          root: ${{ inputs.typescript-root }}
          config: ${{ inputs.config }}

      - name: Upload report
        uses: actions/upload-artifact@v4
        with:
          name: quality-report
          path: reports/
```

## 5. 各リポジトリからの呼び出し

各システムでは、次のような小さな Workflow だけを作成します。

`.github/workflows/quality.yml`

```yaml
name: Quality Report

on:
  pull_request:

  workflow_dispatch:

  schedule:
    - cron: "0 20 * * 5"

jobs:
  quality:
    uses: company/quality-reporter/.github/workflows/quality.yml@v1
    with:
      solution: MySystem.sln
      typescript-root: frontend
      config: .qualityreporter.json
```

C# のみの場合:

```yaml
jobs:
  quality:
    uses: company/quality-reporter/.github/workflows/quality.yml@v1
    with:
      solution: MySystem.sln
```

TypeScript / React のみの場合:

```yaml
jobs:
  quality:
    uses: company/quality-reporter/.github/workflows/quality.yml@v1
    with:
      typescript-root: frontend
```

## 6. Composite Action

Reusable Workflow の中に解析処理を大量に直接記述せず、C# と TypeScript ごとに Composite Action を作成します。

```text
actions/
├── csharp/
│   └── action.yml
│
└── typescript/
    └── action.yml
```

### C# Action

処理の流れ:

```text
dotnet restore
    ↓
dotnet build / Analyzer
    ↓
dotnet test
    ↓
Coverage 生成
    ↓
Duplicate Code 検出
    ↓
quality-csharp 実行
    ↓
JSON / Markdown 生成
```

呼び出し例:

```yaml
- uses: company/quality-reporter/actions/csharp@v1
  with:
    solution: MySystem.sln
```

### TypeScript / React Action

処理の流れ:

```text
npm ci
    ↓
ESLint
    ↓
Vitest
    ↓
Coverage 生成
    ↓
Duplicate Code 検出
    ↓
quality-ts 実行
    ↓
JSON / Markdown 生成
```

呼び出し例:

```yaml
- uses: company/quality-reporter/actions/typescript@v1
  with:
    root: frontend
```

## 7. リポジトリ固有設定

各業務リポジトリには `.qualityreporter.json` を配置します。

```json
{
  "historyDays": 180,
  "exclude": [
    "**/generated/**",
    "**/migrations/**",
    "**/bin/**",
    "**/obj/**",
    "**/node_modules/**"
  ],
  "methodAnalysis": {
    "enabled": true,
    "historyEnabled": true
  },
  "untestedComplexity": {
    "enabled": true
  },
  "duplication": {
    "enabled": true
  },
  "riskLevels": {
    "critical": 80,
    "high": 60,
    "medium": 40
  }
}
```

## 8. 中央管理するもの

QualityReporter 側で次の項目を管理します。

- 評価ロジック
- Risk Score 計算
- Hotspot 判定
- Classification
- Recommendation
- JSON Schema
- Git 解析仕様
- Markdown フォーマット
- 標準閾値

## 9. リポジトリ側で変更可能なもの

各システム固有の事情は設定ファイルで調整します。

- 解析除外ディレクトリ
- Coverage ファイル位置
- Solution ファイル
- Frontend ルート
- 評価期間
- 一部閾値

評価ロジックそのものを各リポジトリへコピーしないでください。

## 10. GitHub Actions の Git 履歴取得

QualityReporter は Git 履歴を使用するため、必ず完全履歴を取得します。

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

完全履歴がない場合、次の指標を正しく評価できません。

- Commit Count
- Churn
- Rework
- File Age
- Method History
- Change Coupling

## 11. レポート

最低限、次のファイルを生成します。

```text
reports/
├── csharp.json
├── csharp.md
├── typescript.json
└── typescript.md
```

Markdown は GitHub Actions Job Summary に追加します。

```bash
cat reports/csharp.md > "$GITHUB_STEP_SUMMARY"
cat reports/typescript.md > "$GITHUB_STEP_SUMMARY"
```

詳細な JSON は Artifact として保存します。

## 12. GitHub Enterprise での共有設定

QualityReporter リポジトリを `Internal` に設定します。そのうえで、次を確認します。

```text
Repository
→ Settings
→ Actions
→ General
```

Enterprise 内の他リポジトリから利用できる設定にし、Enterprise / Organization 側の GitHub Actions Policy も確認してください。

## 13. 推奨 GitHub Actions Policy

社内標準として、可能であれば次の Action のみを許可します。

```text
社内 Action
    company/**

GitHub 公式 Action
    actions/checkout
    actions/setup-dotnet
    actions/setup-node
    actions/upload-artifact
```

必要以上の外部 Action は利用しません。

## 14. 認証

QualityReporter の基本解析では、次の認証情報を要求しない設計とします。

- PAT
- GitHub App Token
- 独自 Secret

基本権限は次のとおりです。

```yaml
permissions:
  contents: read
```

将来、PR へのコメント、Issue 自動作成、Commit Status 更新などを行う場合のみ、追加権限を検討します。

## 15. QualityReporter の配布方法

初期版では GitHub Packages を使用しません。次のものを 1 つの Internal Repository に配置します。

```text
Internal Repository
├── QualityReporter Source
├── Composite Actions
└── Reusable Workflow
```

Composite Action が同じリポジトリ内の QualityReporter を実行します。

## 16. 将来的な Packages 化

利用リポジトリが増えた場合は、ツール本体を Package 化できます。

```text
quality-csharp
    ↓
GitHub Packages / NuGet

quality-ts
    ↓
GitHub Packages / npm
```

ただし初期構築では不要です。まずは Internal Repository から直接実行する構成を優先します。

## 17. バージョン管理

Reusable Workflow / Action はバージョンを指定して利用します。

```yaml
uses: company/quality-reporter/.github/workflows/quality.yml@v1
```

内部バージョンの例:

```text
v1.0
v1.1
v1.2
v1.3
```

評価基準を大幅に変更する場合は、Major Version を変更します。

```text
v1
↓
v2
```

## 18. 厳密なバージョン固定

品質評価基準を厳密に固定したいリポジトリでは、完全なバージョンを指定します。

```yaml
uses: company/quality-reporter/.github/workflows/quality.yml@v1.3.2
```

通常運用では `@v1` とし、同一 Major 内の改善を中央反映する方法でも構いません。

## 19. 運用イメージ

```text
System A ─┐
System B ─┤
System C ─┤
System D ─┤
          │
          ▼
company/quality-reporter
          │
          ├── Git History
          ├── Complexity
          ├── Coverage
          ├── Rework
          ├── Change Coupling
          ├── Method Hotspot
          ├── Untested Complexity
          ├── Duplicate Code
          ├── Risk
          └── Report
```

## 20. 新規リポジトリへの導入

新しいシステムでは、次だけ実施します。

1. `.qualityreporter.json` を作成する
2. `.github/workflows/quality.yml` を作成する
3. QualityReporter Reusable Workflow を呼び出す
4. 初回 `workflow_dispatch` を実行する
5. レポート結果を確認する

QualityReporter 本体のコピーは行いません。

## 21. 将来的な Starter Workflow

利用リポジトリが増えたら、Organization の `.github` リポジトリへ Starter Workflow を登録します。

新しいリポジトリから、次のように簡単に導入できる形を目指します。

```text
Actions
→ New workflow
→ Quality Reporter
```

## 22. 推奨方針

初期構成として、次を採用します。

```text
QualityReporter 専用 Internal Repository
        +
Reusable Workflow
        +
C# Composite Action
TypeScript Composite Action
        +
各業務 Repository の .qualityreporter.json
```

GitHub Packages は初期段階では使用しません。社内利用に限定し、QualityReporter の実装、評価基準、Workflow を中央管理します。

## 23. この構成のメリット

- QualityReporter を外部公開しなくてよい
- 各リポジトリへツールをコピーしなくてよい
- QualityReporter のバージョン管理を一元化できる
- 品質評価基準を全社で統一できる
- 各システム固有の設定は残せる
- GitHub Actions だけで完結できる
- QualityReporter 更新時の展開が容易
- 将来 Packages 化しても構成を移行しやすい

QualityReporter を社内標準の品質評価基盤として展開する場合は、**Internal Repository + Reusable Workflow + Composite Actions** を基本構成とします。
