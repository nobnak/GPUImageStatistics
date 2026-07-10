# GPUImageStatistics

Unity 上でテクスチャ画像の統計量（合計・平均・共分散）を GPU（Compute Shader）で高速に計算するライブラリです。  
ライブラリ本体はローカルパッケージ `jp.nobnak.gpu_image_statistics` として同梱されています。

## 機能

### GPUStatistics

`Texture` を入力として、Compute Shader による並列リダクションで統計量を求めます。

| メソッド | 戻り値 | 説明 |
|----------|--------|------|
| `Sum(Texture)` | `Vector4` | 全ピクセルの RGBA 合計 |
| `Average(Texture)` | `Vector4` | 全ピクセルの RGBA 平均 |
| `Covariance(Texture, out Vector4)` | `Matrix4x4` | RGBA 4 成分の共分散行列（平均も同時に取得） |

各メソッドには `ComputeBuffer` に結果を書き込むオーバーロードもあり、パイプライン内で GPU 上の処理を続ける場合に使えます。

### カラースペース

`GPUStatistics.ColorSpace` で、統計計算前のピクセル値の解釈を指定できます。

| モード | 動作（Linear レンダリングプロジェクト） |
|--------|----------------------------------------|
| `StatisticsColorSpace.Linear` | テクスチャ値をそのまま使用 |
| `StatisticsColorSpace.SRGB` | Linear → sRGB（IEC 61966-2-1 区分関数）に変換してから統計 |

デフォルトは後方互換のため `SRGB` です。Gamma レンダリングプロジェクトでは、設定に関わらず変換は行われません。

### 内部コンポーネント

- **GPUReduction** — `Vector4` / `Matrix4x4` グリッドの 2 段階 GPU リダクション（X 方向 → Y 方向）
- **CPUStatistics** — テスト・検証用の CPU 参照実装（`Total` / `Average` / `Covariance` 拡張メソッド）
- **DisposableBuffer** — `ComputeBuffer` の `using` 対応ラッパー

## 使い方

```csharp
using GPUImageStatisticsSystem;
using UnityEngine;

var tex = Resources.Load<Texture2D>("SomeImage");
var stat = new GPUStatistics();

var sum = stat.Sum(tex);
var average = stat.Average(tex);
var covariance = stat.Covariance(tex, out average);

// Linear 空間で統計を取る場合
var statLinear = new GPUStatistics { ColorSpace = StatisticsColorSpace.Linear };
var avgLinear = statLinear.Average(tex);
```

## 開き方

Unity Hub からこのフォルダをプロジェクトとして開いてください（Unity 6000.3.19f1 推奨）。

## パッケージ

- パス: `Packages/jp.nobnak.gpu_image_statistics`
- 名前空間: `GPUImageStatisticsSystem`
- 詳細: [Packages/jp.nobnak.gpu_image_statistics/README.md](Packages/jp.nobnak.gpu_image_statistics/README.md)

## テスト

- コード: `Assets/Tests/Editor/`
- テスト用画像: `Assets/Tests/Resources/SampleImage.png`（64×64 の手続き生成グラデーション）
- Unity Test Runner（Edit Mode）から実行
- GPU 結果を CPU 参照実装と比較し、Linear / SRGB 両カラースペースを検証
