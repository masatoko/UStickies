# コーディング規約

この文書は、Unity/C# プロジェクトで守るコーディング規約をまとめる。

## 基本方針

- 最低でも概要や流れが把握できるように日本語のコメントを書くこと。
- 自明でない関数には XML documentation の `summary` コメントを書くこと。
  - 状態遷移、座標変換、複数条件の判定、外部から呼ばれる公開APIなど、呼び出し側が意図を読み取りにくい関数を対象とする。
- コメント内では括弧は全角ではなく半角を使うこと。
  - 良い例: `// 初期化する (未設定時のみ)`
  - 悪い例: `// 初期化する（未設定時のみ）`
- パブリックフィールドは小文字始まりとする。
  - 良い例: `public float speed;`
  - 悪い例: `public float Speed;`
- private フィールドは `_` 始まりの camelCase とする。
  - 良い例: `private int _currentHealth;`
  - 悪い例: `private int currentHealth;`
- ユーザーが Inspector で調整する設定値には `[Tooltip("...")]` を付けること。
  - `SerializeField` や public フィールドのうち、ゲームデザイン調整用の値、挙動の切り替え、参照先の指定などを対象とする。
  - 内部キャッシュ、実行時に自動設定される参照、自明な一時値は対象外としてよい。

## Inspector 属性

- Inspector の操作性・可読性を上げる目的では、NaughtyAttributes を前提として使ってよい。
- エディタ上で手動実行する補助処理は、`ContextMenu` より `[Button]` を優先する。
- 条件付きの表示・非表示や有効・無効の切り替えには、`[ShowIf]`, `[HideIf]`, `[EnableIf]`, `[DisableIf]` を使う。
- 読み取り専用で値を確認したいフィールドには `[ReadOnly]` を使う。
- Unity 標準属性だけで十分なものは標準属性を使ってよい。
  - 例: `[SerializeField]`, `[Tooltip]`, `[Header]`, `[Min]`, `[Range]`
- Inspector 属性は入力補助と表示整理のためのものとし、実行時の安全性が必要な値はコード側でも検証・補正する。

## using の並べ方

`using` は依存の種類ごとに以下の順で並べる。

グループ間は1行空け、同一グループ内はアルファベット順とする。

1. `System.*`
2. `UnityEngine.*`
3. `Unity.*`
4. サードパーティ Package (`Cysharp.*`, `DG.Tweening`, `NaughtyAttributes`, `R3.*` など)
5. `TenkaiKit.*`
6. `Kenkai.*`, `Stagehand.*`
7. `MyGame.Shared.*`
8. `MyGame.*`

## 式形式メンバー

- 1行で完結する単純な関数・プロパティは、式形式メンバー (`=>`) を使う。
- 複数行の処理、複雑な分岐、副作用が主目的の処理、途中にログやデバッグポイントを置きたい処理はブロック形式を使う。

```csharp
bool IsValid() =>
    target != null;
```

## ローカル関数

- 1つの関数内でしか使わない補助処理は、クラスの private メソッドではなくローカル関数を優先する。
  - 呼び出し可能な範囲を狭め、他の関数から使う想定があるように見えることを避ける。
- ローカル関数は、呼び出し元の処理に強く従属する待機処理、条件判定、短い変換処理などに使う。
- ローカル関数が長くなり、メイン処理と見分けにくい場合は、関数末尾にまとめて配置する。
- 関数末尾にローカル関数をまとめる場合は、区切りコメントを置く。
  - 区切りコメントの直前には3行空ける。
  - 区切りコメントは `// ===== FunctionName Local Helpers =====` の形式を基本とする。

```csharp
IEnumerator LoadIslandMap() {
    yield return WaitForIslandTerrainCollider();

    StartPlayer();



    // ===== LoadIslandMap Local Helpers =====
    IEnumerator WaitForIslandTerrainCollider() {
        ...
    }
}
```

## region と区切りコメント

大きめのクラスでは、役割ごとに `#region` を使って関数を整理する。

`#region` の直前には、視認性を高めるための区切りコメントを置く。

```csharp



// ===== Unity Lifecycle ===== ===== ===== ===== ===== ===== ===== ===== ===== =====
#region Unity Lifecycle

void Awake() {
    ...
}

#endregion
```

規則:

- `#region` の直前には3行空ける。
- 区切りコメントは `// ===== Section Name ===== ===== ===== ===== ===== ===== ===== ===== ===== =====` の形式にする。
- `#region` 名と区切りコメント名は一致させる。
- `#endregion` 側には原則としてコメントを付けない。
- 状態機械のクラスでは、状態単位で region を作る。

状態機械の例:

```csharp



// ===== Spawning [State] =====
#region Spawning [State]

void EnterSpawning(Vector3 spawnPosition) {
    ...
}

void TickSpawning(float deltaTime) {
    ...
}

void CompleteSpawning() {
    ...
}

#endregion
```

## 推奨する region 構成

Unityコンポーネントでは、以下のように外部から見える入口、状態、補助処理の順に並べる。

```text
Unity Lifecycle / Public API
State: Spawning または Spawning [State]
State: Chasing Player または Chasing Player [State]
State: Retreating または Retreating [State]
Appearance / Target Helpers
State Machine Helpers
Setup / Validation
```

物理挙動や移動処理を持つクラスでは、以下のように責務単位で分ける。

```text
Unity Lifecycle
Public State
Public Animation API
Public Movement API
Ground Sampling
Movement Tick
Ground Following
Slope Speed Reduction
Obstacle Steering
Setup / Utility
```

## 関数の並べ方

- 公開APIとUnityイベントは上部に置く。
- 状態機械では、`EnterXxx`, `TickXxx`, `CompleteXxx` を同じ状態regionにまとめる。
- イベントハンドラが特定状態の遷移トリガーである場合、その状態regionに置く。
- 共通ヘルパーは状態regionの後ろにまとめる。
- 参照解決、Validate、停止処理などのセットアップ系は末尾に置く。
