# GPU Image Statistics library for Unity

## Usage
```csharp
var tex = Resources.Load<Texture2D>("Some image in Resources folder");
var stat = new GPUStatistics();
var sum = stat.Sum(tex);
var average = stat.Average(tex);
var covariance = stat.Covariance(tex);
```

## Color space

`GPUStatistics.ColorSpace` selects how pixel values are interpreted before statistics:

| Mode | Behavior (Linear rendering project) |
|------|-------------------------------------|
| `StatisticsColorSpace.Linear` | Use texture values as-is (linear) |
| `StatisticsColorSpace.SRGB` | Convert linear → sRGB (IEC 61966-2-1 piecewise) before statistics |

Default is `SRGB` for backward compatibility. In Gamma rendering projects, no conversion is applied regardless of the setting.

```csharp
var stat = new GPUStatistics { ColorSpace = StatisticsColorSpace.Linear };
var average = stat.Average(tex);
```
