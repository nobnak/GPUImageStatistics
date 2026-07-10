# GPU Image Statistics library for Unity

## Usage
```csharp
var tex = Resources.Load<Texture2D>("Some image in Resources folder");
var stat = new GPUStatistics();
var sum = stat.Sum(tex);
var average = stat.Average(tex);
var covariance = stat.Covariance(tex);
```
