# UStickies 仕様書

## 基本情報

- ツール名：`UStickies`
- パッケージ名：`com.tenkaigames.ustickies`
- ルート名前空間：`Tenkai.UStickies`
- 対象Unityバージョン：Unity 6000.3以降

## 目的

Unity Editor上で、Scene内の任意位置またはGameObjectにノートを配置し、Scene Viewと専用Windowから管理できるようにする。

## ユーザー向け用語

- 完了状態は `Done` / `Undone` と表記する。
- Scene Viewへ本文を常時表示する状態は `Pin` / `Unpin` と表記する。

## ノートデータ

ノートは以下の情報を持つ。

- 一意なID
- 本文
- カテゴリID
- Done状態
- Pin状態
- ワールド座標
- 紐づけ先GameObject
- GameObjectとの紐づけ状態
- 作成日時
- Scene内の登録順

ノートは次のどちらかの位置を使用する。

- 位置固定ノート：保存したワールド座標
- GameObject紐づけノート：対象GameObjectのTransform位置

初期バージョンではGameObjectからの表示位置オフセットを持たない。

### GameObject削除時

- 紐づけ先GameObjectが削除されてもノート自体は削除しない。
- 削除前に記録した最後のTransform位置を保持する。
- 紐づけ切れを一度通知し、ノートは位置固定ノートとして扱う。
- UndoでGameObjectが復元された場合は紐づけも復元できる。

## ノートデータ管理

- ノートデータはScene単位で管理し、Scene間で共有しない。
- ノートを初めて追加したとき、Scene直下に非表示の `[UStickies]` GameObjectとデータベースを作成する。
- 管理GameObjectには `EditorOnly` タグとビルドへ保存しないHideFlagsを設定する。
- EditorWindow自体にはノートの永続データを保持しない。
- ノートデータはSceneとともに保存する。

## ノート作成

以下の経路から作成できる。

- Scene Viewの右クリックメニュー `UStickies > Add Note`
- Scene Viewの右クリックメニュー `UStickies > Add Note to Selected GameObject`
- HierarchyのGameObject右クリックメニュー `UStickies > Add Note`
- Scene Notes Windowの `Add New Note`

### Scene Viewからの位置固定ノート

- 右クリック位置からSceneへレイを飛ばす。
- Colliderにヒットした場合はヒット位置へ作成する。
- ヒットしない場合は、右クリック位置とScene Viewのpivotが同じ奥行きになる位置へ作成する。

### Scene Notes Windowからの位置固定ノート

- アクティブなScene Viewのpivot位置へ作成する。
- Windowで選択中のSceneを作成先とする。

### GameObject紐づけノート

- 選択中GameObjectと同じSceneへ作成する。
- GameObjectのTransform位置をノート位置とする。

### 作成直後

- 新規ノートを選択状態にして編集Windowを開く。
- 本文とカテゴリを編集できる。
- 初期カテゴリはプロジェクト設定のデフォルトカテゴリとする。
- Saveで作成を確定する。
- Cancelまたは未保存のままWindowを閉じた場合は、作成途中のノートを削除する。

## ノート編集Window

新規作成時は以下を編集できる。

- Body
- Category

既存ノートの編集時は以下も編集できる。

- Done
- Pin
- GameObjectの紐づけ先

別SceneのGameObjectは紐づけ先に指定できない。

## カテゴリ

カテゴリ定義はプロジェクト共通で管理する。

各カテゴリは以下を持つ。

- ID
- ラベル
- 表示色
- 組み込みカテゴリかどうか

初期カテゴリと初期色：

- `Note`：グレー
- `Todo`：黄
- `Bug`：赤

- カテゴリは追加、名称変更、色変更、並べ替えができる。
- 組み込みカテゴリは削除できない。
- 追加カテゴリは削除できる。
- 使用中カテゴリを削除するときは、参照中ノートの置き換え先を指定する。
- 名称や色を変更してもカテゴリIDは維持する。
- ノートはカテゴリ名ではなくカテゴリIDを保持する。

## Scene View

### アイコン

- ノートのワールド位置を画面座標へ変換し、一定サイズのアイコンを表示する。
- カテゴリ色をアイコンへ反映する。
- 通常ノートとDoneノートで別のアイコンを使用する。
- 未選択の通常ノートは不透明度0.5、未選択のDoneノートは不透明度0.35で表示する。
- 選択中アイコンは不透明で最前面へ表示する。
- オブジェクトに遮蔽されている場合は、通常の不透明度へ0.48を乗算して手前へ表示する。
- カメラ後方にあるノートは表示しない。

### 本文表示

- 選択中またはPin状態のノートは本文を表示する。
- 本文の左上をアイコン右端から14px、アイコン上端と同じY座標に固定する。
- Scene View外へはみ出しても位置を補正しない。
- 背景、枠線、カテゴリ名、Done状態の文字、カテゴリ色の帯、接続線は表示しない。
- 本文には最大高を設け、長文は表示領域内でスクロールできる。
- 複数の本文表示が重なっても自動整列しない。

### 操作

- アイコンの左クリックでノートを選択する。
- アイコンのダブルクリックで編集Windowを開く。
- Escapeまたはノート以外の左クリックで選択を解除する。
- 位置固定ノートは `Shift + 左ドラッグ` で移動できる。
- GameObject紐づけノートはドラッグ移動できない。
- Scene ViewのオーバーレイまたはNotes Windowの `Visible` で、ノート表示全体を切り替えられる。

## Scene Notes Window

`Window > UStickies > Notes` から開く。

### Scene選択

- 開いているSceneから一覧の対象Sceneを選択できる。
- 選択中Sceneが閉じられた場合は、利用可能なSceneへ切り替える。

### 一覧表示

- 本文の先頭行、カテゴリ、Done状態を表示する。
- 行の左クリックで選択し、選択中の行には本文全文と操作を表示する。
- 行のダブルクリックで編集Windowを開く。
- Scene Viewと選択状態を共有する。

選択中の行から以下を実行できる。

- `Move`：Scene Viewをノート位置へ移動する。
- `Edit`：編集Windowを開く。
- `Done` / `Undone`：完了状態を切り替える。
- `Pin` / `Unpin`：Pin状態を切り替える。
- `Delete`：ノートを削除する。

同じ操作を行の右クリックメニューからも実行できる。

### 検索とフィルタ

- 検索は本文を対象とする。
- 空白で区切った複数語を、大文字小文字を区別しないAND条件で検索する。
- カテゴリは複数選択でき、選択カテゴリのいずれかに一致するノートを表示する。
- 完了状態は `All states`、`Undone`、`Done` から選択する。
- 検索、カテゴリ、完了状態の条件は併用する。
- `Apply to Scene` がONの場合、同じ絞り込みをScene Viewにも適用する。

### 並び順

以下から選択する。

1. `Newest`：作成日時が新しい順
2. `Oldest`：作成日時が古い順
3. `Scene order`：Scene内の登録順
4. `Category`：カテゴリ表示順。同一カテゴリ内は登録順

Done状態は並び順に影響しない。

### ユーザー設定

以下は `UserSettings/UStickiesUserSettings.asset` へユーザーごとに保存する。

- 検索文字列
- カテゴリフィルタ
- 完了状態フィルタ
- `Apply to Scene`
- 並び順
- ノート表示全体のON/OFF
- Scene View上の内容カードの最大横幅、最大縦幅、文字色

## Project Settings

`Edit > Project Settings > UStickies` から開く。

- Scene View上の内容カードの最大横幅、最大縦幅、文字色を設定する。値はユーザーごとに保存する。
- カテゴリの追加、名称変更、色変更、並べ替え、削除を行う。
- カテゴリ設定はプロジェクト全体で共有する。
- 使用中カテゴリの削除時は置き換え先カテゴリを選択する。

## Undo

以下の操作はUnity Undoに対応する。

- ノート追加
- ノート削除
- 位置固定ノートの移動
- 本文変更
- カテゴリ変更
- Done状態変更
- Pin状態変更
- GameObjectへの紐づけ・解除
- カテゴリ追加、編集、並べ替え、削除

## パッケージ構成

```text
Packages/com.tenkaigames.ustickies
├─ Runtime/Data    Sceneへ保存するノートデータ型
├─ Editor/Data     データの検索、変更、選択、絞り込み
├─ Editor/SceneView
├─ Editor/Settings
├─ Editor/UI
├─ Editor/Window
└─ Tests/Editor
```

## 対象

- Unity Editor専用機能とする。
- Runtimeでの利用は想定しない。
- ビルド後のプレイヤーへノートデータと機能を含めない。
