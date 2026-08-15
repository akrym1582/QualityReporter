# QualityReporter

QualityReporter は、静的メトリクスと Git の変更履歴を組み合わせて、**レビューと保守の優先順位**を見つけるツールです。コードの良し悪しを断定するものではありません。無料でローカル実行でき、`quality-csharp` と `quality-ts` に分かれています。両実装で共有する契約はスキーマとリスク定義だけです。

## 使い方

### 前提

.NET 10 と Node.js 20 以上が必要です（サンプルの GitHub Actions は Node.js 24 を使用します）。Git の変更履歴を解析するため、リポジトリは必ず全履歴で取得してください（`actions/checkout` の `fetch-depth: 0`）。

### C#

```bash
dotnet run --project csharp/src/QualityReporter.CSharp -- analyze \
  --solution App.sln --since 180d --coverage coverage.cobertura.xml \
  --analyzer build.log --config quality.json --baseline previous.json \
  --output reports/csharp.json --markdown reports/csharp.md
```

`--solution`（ソリューションファイル）または `--project`（`.csproj` ファイル）のどちらか一方と、`--output` は必須です。プロジェクトだけを解析する場合は、たとえば `--project src/App/App.csproj` を指定します。`--coverage`、`--analyzer`、`--config`、`--baseline`、`--markdown` は任意です。

### TypeScript / TSX

```bash
cd typescript
npm ci
npm run build
node dist/cli.js analyze --root ../frontend --eslint ../reports/eslint.json \
  --coverage ../coverage/coverage-final.json --since 180d --config ../quality.json \
  --baseline ../previous.json --output ../reports/typescript.json \
  --markdown ../reports/typescript.md
```

`--root` と `--output` は必須です。TypeScript では TSX も解析対象になります。任意の入力がない場合は警告になりますが、解析は停止しません。

## 静的解析結果の取得方法

QualityReporter は解析そのものに加えて、既存ツールが出力したカバレッジ、アナライザー、リンターの結果を読み込みます。入力ファイルは解析対象と同じリポジトリを基準にしたファイルパスを含む必要があります。

### C# のカバレッジとアナライザー

Cobertura 形式のカバレッジを取得する例です。

```bash
dotnet test App.sln --collect:"XPlat Code Coverage" \
  --results-directory TestResults
```

生成された `coverage.cobertura.xml` を `--coverage` に渡してください。実際の生成場所はテストランナーや設定により異なるため、必要に応じて `find TestResults -name coverage.cobertura.xml` で確認します。

`dotnet build` の診断結果は標準出力と標準エラーをファイルに保存し、そのファイルを `--analyzer` に渡します。

```bash
dotnet build App.sln > build.log 2>&1
```

C# のアナライザー入力は、たとえば `File.cs(12,3): warning CAxxxx ...` の形式を対象にしています。Roslyn アナライザーやコンパイラー診断を有効にしたビルドを実行してください。

### TypeScript のカバレッジと ESLint / oxlint

テストランナーが Istanbul 形式を出力する設定でカバレッジを取得します。たとえば c8 では次のように実行できます。

```bash
cd typescript
npm run coverage
```

生成された `coverage/coverage-final.json` を `--coverage` に渡します。別のテストツールを使う場合も、Istanbul の `coverage-final.json` 形式で出力してください。

ESLint の結果は JSON 形式で出力します。

```bash
npx eslint ../frontend --format json > ../reports/eslint.json
```

プロジェクトで ESLint の設定やインストール方法が異なる場合は、そのプロジェクトの設定に合わせて実行してください。生成した JSON を `--eslint` に渡すと、エラー・警告・情報の件数がファイルごとに集計されます。

oxlint の結果も JSON 形式で出力して読み込めます。

```bash
npx oxlint ../frontend --format json > ../reports/oxlint.json
```

生成した JSON を `--oxlint` に渡してください。`--eslint` と `--oxlint` を両方指定した場合は、両方の問題件数をファイルごとに合算します。

### GitHub Actions で取得する場合

GitHub Actions では、チェックアウト時に全履歴を取得し、各ツールの出力をレポート生成より前に作成します。

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
- run: dotnet build App.sln > build.log 2>&1
- run: dotnet test App.sln --collect:"XPlat Code Coverage"
- run: npx eslint frontend --format json > reports/eslint.json
```

生成された JSON、XML、ログのパスを `--coverage`、`--analyzer`、`--eslint` または `--oxlint` に指定してください。入力が存在しない、または任意入力を省略した場合、そのメトリクスは `Not Available` として警告され、ゼロとして扱われません。

## メトリクス

* **コミット数**は期間内にファイルを変更した非マージコミット数、**変更量（churn）** は追加行数と削除行数の合計、**著者数（author count）** は大文字小文字を区別しない著者メールアドレス数です。
* **再作業率（rework rate）** は、過去の変更から設定した期間内に発生した変更数を全変更数で割った値です。
* **変更結合（change coupling）** は同じコミットで一緒に変更されたファイルを記録します。設定したファイル数上限を超えるコミットを除外し、最小件数と比率のフィルターを適用します。
* 現在のソースから、ファイルごとの LOC と循環的複雑度、および安定した `Symbol` を収集します。TS/TSX は TypeScript Compiler API、C# は Roslyn の構文・セマンティックモデルを使用します。シンボル ID は行番号ではなく論理的な識別情報の SHA-256 ハッシュです。ネストした関数の複雑度は親に重複計上しません。
* カバレッジとアナライザー／リンターの問題数は任意です。カバレッジが低いほどリスクが高くなるよう、カバレッジのリスクは反転します。

Activity は変更 45%、変更量 35%、著者 10%、新しさ 10% です。Risk は複雑度 25%、再作業 25%、カバレッジ不足 20%、変更結合 20%、問題数 10% です。利用できないメトリクスは除外し、残りの重みを再正規化します。レベルは Critical（80 以上）、High（60 以上）、Medium（40 以上）、Low（40 未満）です。ホットスポットの順位はリスクだけでなく優先度を使用します。ベースラインは Critical／hotspot の状態、リスク、優先度の変化を示します。

## 設定と出力

まず [`examples/quality.json`](examples/quality.json) を使用してください。機械可読な契約は [`schema/`](schema/) にあります。JSON の `schemaVersion` は 1 です。Markdown 出力は `$GITHUB_STEP_SUMMARY` に対応しています。終了コードは、成功が 0、解析エラーが 1、引数エラーが 2 です（3 は将来の quality gate 用に予約されています）。

## ユニットテストとカバレッジ

```bash
dotnet test csharp/QualityReporter.CSharp.slnx --collect:"XPlat Code Coverage"
cd typescript && npm run coverage
```

.NET のコマンドは `TestResults` 以下に Cobertura を出力します。TypeScript のコマンドは c8 の概要を表示し、`typescript/coverage/coverage-summary.json` を出力します。カバレッジは解析器、パーサー、リスク／ベースライン処理、レポーターなど、独立してテスト可能な部分を対象にします。プロセスを起動する CLI の配線はエンドツーエンドのスモークチェックで確認します。

## v1.2 のシンボル解析

`symbols` 配列は v1 のファイルモデルに追加されたもので、現在の HEAD にあるメソッド、コンストラクター、演算子、設定されたアクセサー／ローカル関数、TypeScript の関数とメソッド、変数に代入された関数、React コンポーネント、カスタムフックを記述します。抽出と複雑度のしきい値は `methodAnalysis` で設定します。既存の `functions` 出力は互換性のため引き続き利用できます。シンボル履歴、カバレッジ／問題の割り当て、coupling、スコア、推奨事項、ベースラインの傾向は契約に含まれていますが、履歴マッピングが実装されるまで利用できません。欠損値を品質の判定として解釈しないでください。デフォルトでは rename tracking が無効なため、メソッド名の変更で ID が分割されることがあります。

## 制限事項

v1 はバグ修正の意図を推測せず、GitHub API を呼び出さず、履歴を保存せず、メソッドの履歴／rename を追跡せず、クローンを検出せず、アーキテクチャスコアや UI を提供しません。rename の解析は Git の numstat 出力に従い、バイナリの numstat エントリは churn に加算しません。複雑度は制御フロー構文に基づく優先順位付けのシグナルであり、意味論的な品質判定ではありません。

## v1.1 の評価モデル

QualityReporter は**開発活動**と**品質リスク**を分離します。Activity（変更頻度、churn、著者、新しさ）だけで問題を判定することはありません。Quality risk は複雑度、rework、カバレッジ不足、coupling、アナライザー／リンターの問題を使い、任意メトリクスがない場合は重みを再正規化します。Hotspot priority はリスクと成熟度調整済み Activity（`risk × (0.60 + 0.40 × effectiveActivity/100)`）を組み合わせます。

ファイルは `ACTIVE`、`NEW_ACTIVE`、`COMPLEX`、`REWORK`、`UNTESTED`、`COUPLED`、`CRITICAL` に分類できます。推奨事項は決定的で、メトリクスの根拠を含み、確定的な品質判定ではなく、レビュー、テスト、リファクタリング、設計レビューの候補を示します。JSON は後方互換性のため `schemaVersion: 1` を維持し、`activity`、`hotspot`、`recommendations` オブジェクトを追加しています。

## v1.3: Untested Complexity と重複コード

v1.3 は複雑度と未テスト割合を組み合わせた `untestedComplexity` を追加します。カバレッジがない場合は `null`／`Not Available` とし、0% とみなしません。`coverageMode` は `line`（既定）、`branch`、`combined`（line 60% + branch 40%）から選択できます。複雑度自体も独立したレビューシグナルとして維持されます。

重複検出アルゴリズムは QualityReporter に実装しません。CI で jscpd を実行し、その JSON を `--duplication reports/jscpd.json` で渡します。QualityReporter は C#、TypeScript、TSX の fragment をファイルと symbol の行範囲へ割り当て、重複率と Activity を組み合わせてレビュー優先度を付けます。重複は共通化の確定判断ではなく、業務上同じ責務かを確認する候補です。

```yaml
- run: npx jscpd src --reporters json --output reports
- run: quality-ts analyze --root . --coverage coverage/coverage-final.json --duplication reports/jscpd-report.json --output reports/quality.json --markdown "$GITHUB_STEP_SUMMARY"
```

Maintainability Risk は Complexity 25%、Rework 25%、Duplication 20%、Coupling 15%、Analyzer Issues 15% です。Test Risk は Coverage Risk 40%、Untested Complexity 60% で、利用不能な項目を除いて再正規化します。Overall Risk は Maintainability 50%、Test 30%、Architecture 20% を基本とし、Hotspot Priority は従来どおり Activity factor を適用します。
