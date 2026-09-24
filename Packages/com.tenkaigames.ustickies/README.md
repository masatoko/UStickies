# 概要

UStickiesは、UnityのScene内にノートを配置し、Scene Viewと専用Windowから管理するEditor拡張パッケージ。

ノートはScene上の任意位置に固定するか、GameObjectに紐づけて配置できる。作業メモ、未完了タスク、不具合箇所などをScene内の位置と結び付けて残せる。

## 主な機能

- Scene View上へのノート表示
- 位置固定ノートとGameObject紐づけノートの作成
- 本文、カテゴリ、Done状態、Pin状態の編集
- 本文検索、カテゴリ・Done状態による絞り込み
- 作成日時、Scene内の登録順、カテゴリによる並べ替え
- プロジェクト共通カテゴリの追加、編集、並べ替え、削除
- Unity Undoによる操作の取り消し

## ノートが保存される単位

ノートはScene単位で保存され、別のSceneとは共有されない。

Sceneに初めてノートを追加すると、Scene直下に非表示の管理用GameObject `[UStickies]` が作成される。ノートデータはこのGameObjectに保持され、Sceneとともに保存される。

# 動作環境

- Unity Editor専用

| Gitタグ  | パッケージバージョン | Unityバージョン | 対応状況     |
| -------- | -------------------- | --------------- | ------------ |
| `v0.1.2` | `0.1.2`              | 6000.3          | 対応         |
| `v0.1.2` | `0.1.2`              | 6000.6          | 動作確認済み |

Unity 6000.3および6000.6では `v0.1.2` の利用を推奨する。

Runtimeでの利用は想定していない。ノートデータとEditor機能は、ビルド後のプレイヤーには含まれない。

# インストール方法

このリポジトリでは、パッケージが `Packages/com.tenkaigames.ustickies` に格納されている。Git URLから追加する場合は、URLに `?path=/Packages/com.tenkaigames.ustickies` を指定する。

## Package Managerから追加

1. Unityの `Window > Package Manager` を開く
2. 左上の `+` を選択する
3. `Install package from git URL...` を選択する
4. 次のURLを入力する

```text
https://github.com/masatoko/UStickies.git?path=/Packages/com.tenkaigames.ustickies
```

## manifest.jsonへ直接追加

導入先Unityプロジェクトの `Packages/manifest.json` に依存関係を追加する。

```json
{
  "dependencies": {
    "com.tenkaigames.ustickies": "https://github.com/masatoko/UStickies.git?path=/Packages/com.tenkaigames.ustickies"
  }
}
```

既存の `dependencies` がある場合は、その中に `com.tenkaigames.ustickies` の行だけを追加する。

## バージョンを指定

特定バージョンへ固定する場合は、URL末尾にGitタグまたはコミットハッシュを指定する。

```text
https://github.com/masatoko/UStickies.git?path=/Packages/com.tenkaigames.ustickies#v0.1.2
```

更新による意図しない挙動変更を避けるため、共同開発や継続運用ではバージョンの固定を推奨する。

# ノートの種類

## 位置固定ノート

保存したワールド座標に表示するノート。Scene内の空間、地形、構図など、特定のGameObjectに属さない内容を残す用途に向く。

Scene View上で `Shift + 左ドラッグ` すると位置を変更できる。

## GameObject紐づけノート

対象GameObjectのTransform位置に表示するノート。対象が移動するとノートも追従する。

ノートは対象GameObjectにコンポーネントとしてアタッチされるのではなく、UStickiesの管理データからGameObjectを参照する。GameObject紐づけノートはScene View上でドラッグ移動できない。

## 2種類の違い

| 項目                 | 位置固定ノート       | GameObject紐づけノート    |
| -------------------- | -------------------- | ------------------------- |
| 表示位置             | 保存したワールド座標 | GameObjectのTransform位置 |
| GameObjectへの追従   | なし                 | あり                      |
| `Shift + 左ドラッグ` | 移動可能             | 移動不可                  |
| 主な用途             | 空間や場所へのメモ   | 特定GameObjectへのメモ    |

## 紐づけ先GameObjectを削除した場合

紐づけ先を削除しても、ノート自体は削除されない。削除前に記録された最後のTransform位置へ残り、位置固定ノートとして扱われる。

紐づけが切れたことは一度だけ通知される。Unity UndoでGameObjectを復元した場合は、ノートとの紐づけも復元できる。

# 使い方

## ノートの作成

- **Scene上の任意位置に作成**
  Scene Viewの作成位置で右クリックし、`UStickies > Add Note` を選択する。Colliderに当たった場合はヒット位置、当たらなかった場合はScene Viewのpivotと同じ奥行きに作成される。

- **Scene Viewの中心位置に作成**
  `Window > UStickies > Notes` を開き、対象Sceneを選択して `Add New Note` を押す。アクティブなScene Viewのpivot位置に作成される。

- **GameObjectに紐づけて作成**
  対象GameObjectを選択し、Scene Viewの右クリックメニューから `UStickies > Add Note to Selected GameObject` を選択する。HierarchyでGameObjectを右クリックし、`UStickies > Add Note` を選択する方法もある。

新規作成後は編集Windowが開く。本文とカテゴリを入力して `Save` を押すと作成が確定する。`Cancel`、または保存せずにWindowを閉じた場合、作成途中のノートは削除される。

## ノートの確認と編集

- **ノートを選択**
  Scene Viewのノートアイコン、またはNotes Windowの行を左クリックする。Scene ViewとNotes Windowの選択状態は共有される。`Escape` またはノート以外の場所を左クリックすると選択を解除できる。

- **本文とカテゴリを編集**
  Scene ViewのノートアイコンまたはNotes Windowの行をダブルクリックする。Notes Windowで選択中の行にある `Edit` からも編集できる。

- **ノートの位置へ移動**
  Notes Windowでノートを選択し、`Move` を押す。Scene Viewがノート位置へ移動する。

- **GameObjectとの紐づけを変更**
  既存ノートの編集Windowを開き、`GameObject` を変更して保存する。同じSceneに属するGameObjectだけを指定できる。指定を解除すると位置固定ノートになる。

## Scene View上の操作

- **位置固定ノートを移動**
  Scene View上のアイコンを `Shift + 左ドラッグ` する。GameObject紐づけノートは移動できない。

- **本文を常時表示**
  Notes Windowでノートを選択し、`Pin` を押す。選択を解除してもScene Viewへ本文が表示される。解除する場合は `Unpin` を押す。

- **すべてのノートを非表示**
  Scene ViewのUStickiesオーバーレイ、またはNotes Windowの `Visible` を切り替える。この設定はユーザーごとに保存される。

- **絞り込み結果をScene Viewへ反映**
  Notes Windowで `Apply to Scene` をONにする。検索、カテゴリ、Done状態による絞り込みがScene Viewにも適用される。

## ノートの整理

- **Done状態を変更**
  Notes Windowでノートを選択し、`Done` または `Undone` を押す。編集Windowや行の右クリックメニューからも変更できる。

- **本文を検索**
  Notes Windowの検索欄へキーワードを入力する。複数語は空白で区切り、すべての語を含むノートを検索する。大文字と小文字は区別されない。

- **カテゴリとDone状態で絞り込み**
  Notes Windowのカテゴリフィルタと状態フィルタを使用する。カテゴリは複数選択でき、状態は `All states`、`Undone`、`Done` から選択できる。検索条件との併用も可能。

- **ノートを並べ替え**
  Notes Windowで `Newest`、`Oldest`、`Scene order`、`Category` のいずれかを選択する。Done状態は並び順に影響しない。

- **ノートを削除**
  Notes Windowでノートを選択して `Delete` を押す。選択中の行の右クリックメニューからも削除できる。

## 付箋の外観設定

`Edit > Project Settings > UStickies` の `Note Card Appearance` から設定する。

- `Use Custom Sticky Color` がOFFの場合は各ノートのカテゴリ色、ONの場合はプロジェクト共通の `Sticky Color` を背景に使用する。
- `Opacity` は背景色とは独立して設定でき、プロジェクト全体で共有される。
- 最大サイズと文字色は同じ画面から設定するが、ユーザーごとに保存される。

## カテゴリの管理

カテゴリはプロジェクト全体で共有される。`Edit > Project Settings > UStickies` から設定画面を開く。

- **カテゴリを追加**
  `Categories` の追加ボタンを押す。

- **カテゴリの名前と色を変更**
  `Categories` で対象カテゴリのラベルまたは色を編集する。変更後もカテゴリIDは維持される。

- **カテゴリを並べ替え**
  `Categories` でカテゴリを並べ替える。Notes Windowの `Category` 順もこの表示順に従う。

- **使用中のカテゴリを削除**
  追加したカテゴリの `Delete` を押し、そのカテゴリを使用中のノートに適用する置き換え先を選択する。組み込みカテゴリの `Note`、`Todo`、`Bug` は削除できない。

## Undo

- **変更を元に戻す**
  Unity標準のUndoを使用する。ノートの追加・削除・移動・編集、GameObjectとの紐づけ、カテゴリ操作などを取り消せる。

# データの保存

## Sceneごとのノートデータ

ノートデータは各Scene内の非表示GameObject `[UStickies]` に保存される。ノートを変更したあとは、通常のScene編集と同様にSceneを保存する必要がある。

ノートはScene間で共有されない。複数のSceneを開いている場合、Notes Window上部から操作対象のSceneを選択する。

## ユーザーごとの表示・検索設定

以下の設定は `UserSettings/UStickiesUserSettings.asset` に保存され、Gitで共有されない。

- 検索文字列
- カテゴリフィルタ
- Done状態フィルタ
- `Apply to Scene`
- 並び順
- ノート表示全体のON/OFF

## ビルドへの影響

管理用GameObjectには `EditorOnly` タグとビルドへ保存しない設定が適用される。ノートデータとUStickiesのEditor機能は、ビルド後のプレイヤーには含まれない。
