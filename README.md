YMF825 Dumper
=============

MIDI ファイルを YMF825 ダンプファイルに変換します。

このリポジトリは [YMF825 Jukebox](https://github.com/nanase/ymf825jukebox) のために制作されました。[YMF825 MidiDriver](https://github.com/nanase/ymf825MidiDriver) でデバイスへ転送されているデータをファイルに書き出すものです。書き出しにデバイスは必要ありません。

## 引数

|引数| |説明|
|---|---|---|
|`input`|必須|入力となる MIDI ファイルへのパス|
|`-p` または `--project`|オプション|入力となるプロジェクト Json ファイルへのパス。省略された場合は `input` と同じファイル名のプロジェクトファイルが指定されたものとする|
|`-o` または `--output`|オプション|出力先の 825 ファイルパス。省略された場合は `input` と同じファイル名が指定されたものとする|

## YMF825 ダンプファイル仕様

- 構成ファイルとダンプファイルを *ZIP* で固めたもの
- 拡張子は `.825`

### 構成ファイル

- ファイル名は `config.json`
- ファイル形式は JSON

#### version

- ダンプファイルのバージョン

#### resolution

- コンテントパートの時間分解能、1 tickの時間
  - 値は `1.0 / resolution`、単位は 秒
  - 最小値は 1、最大値は 65536 (およそ 15.2 μS)

#### meta

- ダンプファイルの付加情報 (任意)
  - `title`: タイトル
  - `artist`: アーティスト
  - その他拡張可能

### ダンプファイル

- ファイル名は `dump`
- Write、BurstWrite、ウェイト（待ち時間）のみ記録する
- 16ビット以上の整数値は全て _リトルエンディアン_ で記録
- 注記がない限り、全ての整数値は符号なし

<table>
  <tr>
    <th>bits</th>
    <th>size</th>
    <th>+0 (command)</th>
    <th>+1</th>
    <th>+2</th>
    <th>+3</th>
    <th>+4</th>
  </tr>
  <tr>
    <td>Noop</td>
    <td>1</td>
    <td>0x00</td>
  </tr>
  <tr>
    <td>Write</td>
    <td>3</td>
    <td>0x10</td>
    <td>address</td>
    <td>value</td>
  </tr>
  <tr>
    <td>Write short</td>
    <td>2+</td>
    <td>0x12</td>
    <td>count</td>
    <td>address...</td>
    <td>value...</td>
  </tr>
  <tr>
    <td>Write long</td>
    <td>3+</td>
    <td>0x13</td>
    <td colspan="2">count (2 bytes)</td>
    <td>address...</td>
    <td>value...</td>
  </tr>
  <tr>
    <td>Write and flush short</td>
    <td>2+</td>
    <td>0x14</td>
    <td>count</td>
    <td>address...</td>
    <td>value...</td>
  </tr>
  <tr>
    <td>Write and flush long</td>
    <td>3+</td>
    <td>0x15</td>
    <td colspan="2">count (2 bytes)</td>
    <td>address...</td>
    <td>value...</td>
  </tr>
  <tr>
    <td>BurstWrite short</td>
    <td>3+</td>
    <td>0x20</td>
    <td>address</td>
    <td>length</td>
    <td>values ...</td>
  </tr>
  <tr>
    <td>BurstWrite long</td>
    <td>4+</td>
    <td>0x21</td>
    <td>address</td>
    <td colspan="2">length (2 bytes)</td>
    <td>values ...</td>
  </tr>
  <tr>
    <td>Flush</td>
    <td>1</td>
    <td>0x80</td>
  </tr>
  <tr>
    <td>ChangeTarget</td>
    <td>2</td>
    <td>0x90</td>
    <td>target</td>
  </tr>
  <tr>
    <td>ResetHardware</td>
    <td>1</td>
    <td>0xe0</td>
  </tr>
    <tr>
    <td>Realtime Wait short</td>
    <td>2</td>
    <td>0xfc</td>
    <td>tick</td>
  </tr>
  <tr>
    <td>Realtime Wait long</td>
    <td>3</td>
    <td>0xfd</td>
    <td colspan="2">tick (2 bytes)</td>
  </tr>
  <tr>
    <td>Wait short</td>
    <td>2</td>
    <td>0xfe</td>
    <td>tick</td>
  </tr>
  <tr>
    <td>Wait long</td>
    <td>3</td>
    <td>0xff</td>
    <td colspan="2">tick (2 bytes)</td>
  </tr>
</table>

#### Write

YMF825 に Write 命令で address と value を書き込む。
- address は 0x7f 以下である必要がある。

#### Write short / long

YMF825 に Write 命令で address と value を count 回書き込む。
- address は 0x7f 以下である必要がある。
- バイトの並びは address 1, value 1, address 2, value 2, ...

#### Write and Flush short / long

YMF825 に Write 命令で address と value を count 回書き込み、さらに最後に Flush を行う。
- address は 0x7f 以下である必要がある。
- バイトの並びは address 1, value 1, address 2, value 2, ...

#### BurstWrite

YMF825 に BurstWrite 命令で address と values を書き込む。
- address は 0x7f 以下である必要がある。
- length は values の長さ

#### Flush

YMF825へ送信するコマンドのバッファを即時に実行させる。

#### ChangeTarget

YMF825 が複数ある場合に、ターゲットとなる YMF825 のチップを変える。
- 最大で 8 個までサポート
- フラグ形式になっているので複数のターゲットを指定可能

<table>
  <tr>
    <th>bits</th>
    <th>+0</th>
    <th>+1</th>
    <th>+2</th>
    <th>+3</th>
    <th>+4</th>
    <th>+5</th>
    <th>+6</th>
    <th>+7</th>
  </tr>
  <tr>
    <td>0x00</td>
    <th>#8</th>
    <th>#7</th>
    <th>#6</th>
    <th>#5</th>
    <th>#4</th>
    <th>#3</th>
    <th>#2</th>
    <th>#1</th>
  </tr>
</table>

#### ResetHardware

YMF825 にハードウェアリセットの信号を送る。
- 読み込み側で適切なウェイトを入れる
  - その間は処理がブロックされる

#### RealtimeWait short / long

YMF825 の動作を待つためのウェイト。単位は tick。
- ウェイト時間は `tick + 1`
- Wait と異なり、RealtimeWait は必ずこの時間だけウェイトを入れる必要がある。

#### Wait short / long

次の演奏レジスタ書き込みまでのウェイト時間。単位は tick。
- ウェイト時間は `tick + 1`
- 演奏テンポの調整などでこのウェイト時間は調整可能。
  - ただし RealtimeWait と併用されていた場合は RealtimeWait が優先される。

#### コマンドの対応

- デバイスがフラッシュに対応していない場合は Flush コマンドは無視してよい。
- Wait コマンドは読み込み側で実数倍してもよい。しかし RealtimeWait は加工してはならない。
- デバイスが複数の YMF825 を搭載していない場合でも ChangeTarget は適切に処理しなければならない。 

## ライセンス

**MIT License**

