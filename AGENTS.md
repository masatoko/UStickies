# Design Judgement

- Prioritize alignment with requirements, design consistency, separation of concerns, and maintainability when making decisions.
- Do not adopt the user's proposal as-is; always examine its drawbacks, failure modes, and long-term costs.
- If a proposal has problems, object clearly and present a more appropriate alternative.

---

# Workflow

Before making any code changes, always:
1. Analyze the request and identify affected files
2. Present a detailed implementation plan
3. Wait for explicit approval before proceeding

Do NOT write or edit any code until the plan is approved.

- Do not run a build at the end of a task by default.
- Run builds only when the user explicitly requests verification by build.

## UnityMCP

- 本プロジェクトでは UnityMCP を利用できる。

## 原因調査と改修判断

- 不具合対応では、原因が未特定のまま構造変更、責務移動、新 API 追加などの大きな改修案を出さない。
- まず観測可能な事実を確認すること。位置、回転、状態フラグ、参照先、入力値、呼び出し順、イベント発火有無など、症状に直結する値をログや既存コードから確認する。
- 仮説は必ず検証手段とセットで扱う。ログ追加、差分確認、最小再現、既存 API の挙動確認などで、仮説が原因と一致するかを確かめてから修正案を出す。
- 「設計上きれいそう」「責務として自然そう」という理由だけで対処しない。観測された原因に直接対応する最小修正を優先し、設計整理は原因解決後に必要性を判断する。
- 症状が座標、Transform、物理、入力、所有関係に関わる場合は、改修前に変更前後の値と参照先を確認する。特に Unity の Rigidbody / Transform / CharacterController は同フレーム反映や内部状態の差を疑い、実測ログで確認する。

---

# 口調
- 「です」「ます」調は禁止。

---


# プロジェクト構成

## ドキュメント

- `./docs` にゲームコンセプトなどの計画のための文書を格納する。
- 仕様書は `./docs/spec.md` とする。

## ディレクトリ構造

- コアとなるパッケージは `./Packages/` 内に作成。
- デモシーンやデモで使うスクリプトやアセットは `./Assets/UStickies` 内に作成。

## コーディング規約

- 詳細は `docs/agents/coding-rules.md` を参照する。

## git commit 規約

- 言語は英語とする

## パッケージ運用

- 初期バージョンが完成するまでは `Packages/com.tenkaigames.ustickies/CHANGELOG.md` を空に保つ。
