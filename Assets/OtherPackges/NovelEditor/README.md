# novel-editor

<br>

## 概要
---
Unityで会話パートのデータをグラフで簡単に作って再生できるエディタ拡張のリポジトリです。

ノードを使って簡単に会話ごとの立ち絵や音楽の設定，選択肢による分岐・繰り返しを設定することができます。

また、立ち絵やBGMをオブジェクトフィールドから設定できるため，会話データの設定をUnity上で完結でき、ハードコーディングやcsvファイルによって設定する場合に起こるパス指定ミスを防止することができます

<br>

## 機能
---
こちらに詳しく記載しています。

https://usagi-meteor.com/noveleditor-feature/



<br>

## 使い方・導入方法など
----
こちらに記載しています

https://usagi-meteor.com/noveleditor/


```C#
[SerializeField]NovelPlayer player;
[SerializeField]NovelData data;

void Start(){
  player.play(data,false);
}
```
AudioSource的な感じで簡単に再生できます。

<br>

## スクリプトリファレンス
----

https://usagi-meteor.com/noveleditor-reference/

<br>

## 利用規約
----
・商用利用可能です。

・ゲームに使用した場合のクレジット表記は不要ですが、Twitterで #NovelEditor のハッシュタグをつけてくださるとリツイートします

・使用報告は不要ですが、報告してくださるとプレイします。

・改変の有無に関わらずソフトウェアそのものの再配布は禁止です。

・このツールで生じたいかなる不利益に対しても責任を取りません。(バグ取りきれてない可能性が高いのでその辺は特に注意、こまめなセーブ推奨。)



<br>

## バグ報告・要望など
----
こちらにお願いします。

https://forms.gle/f7wHe7zLATTZPGYt8
