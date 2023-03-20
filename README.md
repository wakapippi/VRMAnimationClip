# VRMAnimationClip

Unityエディタ上でVRM0系のBlendShapeをAnimation Clipとして操作できるようにしたUnityアセットです。

## はじめに
UnityではHumanoid Rigのおかげでモーションの使い回しが自由度高くできますが、表情のデータはモデルのShape Keyに依存するため、これのアニメーションクリップを使い回すことは基本できません。

一方で、UniVRMには表情の操作を抽象化し、さまざまな構造のモデルの表情を共通化した方法で操作する仕組み（VRM BlendShape Proxy）があります。
この仕組みは表情をランタイムでコードから操作すること想定しているため、例えば「VRアプリでコントローラのキーを押したときに特定の表情をする」などの用途で活用できますが、映像制作には不向きです。

そこで、本Unityアセットでは、VRM BlendShape Proxyの操作をアニメーションクリップに記録したり、記録したアニメーションクリップを再生したりする仕組みを提供し、VRMの表情操作をAnimator ControllerやTimelineで行うことを可能にしています。

表情のデータやリップシンクのデータなどをいろんなモデルで使いまわしたいという方に役立つアセットになっています。

## 使い方
### 収録

本アセットのモーション収録部分は、EasyMotionCapture( https://github.com/neon-izm/EasyMotionRecorder ) に依存しています。

1.wakapippi/Prefab/VRMAnimationClipRecorderというPrefabをシーン上に設置します。

2.Motion Data RecorderのAnimatorと、VRM Animation Clip RecorderのBlend Shape Proxyにアバターをアタッチします。

3.Play Modeに移行します。

4.Motion Data Recorderで設定されている録画開始キー（デフォルトはR）を確認し、開始のタイミングで押します。

5.Motion Data Recorderで設定されている録画終了キー（デフォルトはX）を確認し、終了のタイミングで押します。

6.録画されたデータは、Resourcesフォルダ直下に配置され、身体のモーションとセットで、VRM用のAnimation Clipが生成されます。

表情を収録したい場面は様々ありますが、一例として、VSeeFaceとVMC4Uを使って受け取ったデータを収録する動画を添付しますので参考までに。

https://user-images.githubusercontent.com/7871221/226483463-cf10c5b4-0573-4d3b-9ecf-80e849527eee.mp4

### 再生
1.再生したいアバターに、「VRM Animation Clip Player」をアタッチします。

![スクリーンショット 2023-03-21 8 06 59](https://user-images.githubusercontent.com/7871221/226484816-624598bc-d431-4c9e-8dcf-c126fc8239b6.png)

2.あとはAnimator Controllerに収録したクリップを入れるか、Timelineで管理するか、従来のモーションのクリップと同様に扱えばOKです。


https://user-images.githubusercontent.com/7871221/226485276-e01c7a07-ebb6-4d4e-a7ba-0f299f795ac9.mp4


次の動画のように、収録したクリップを他のVRMアバターにも使用できます。


https://user-images.githubusercontent.com/7871221/226485884-e671ef7a-c157-4de6-8fc3-7c9e35a97e73.mp4

## 動作環境
Unity 2021.3.0f1で開発、動作確認しております。

## 不具合やお問い合わせなど
不具合がございましたら、こちらのIssueに投稿していただくか（形式は問いません）、Twitter（ https://twitter.com/wakapippi )までお問い合わせください。
また、Pull Requestを受け付けておりますので、修正や要望がございましたらお気軽にReqestをご送付ください。

## 参考
### EasyMotionCapture
https://github.com/neon-izm/EasyMotionRecorder
### UniVRM
https://github.com/vrm-c/UniVRM

## ライセンス
本ソフトウェアはMITライセンスです。
内包される各ライブラリのライセンスもご覧ください。
