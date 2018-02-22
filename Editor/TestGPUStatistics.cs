using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace GPUImageStatisticsSystem {

    public class TestGPUStatistics {
        public const string IMAGE_NAME = "SampleImage";

        public const string CS_NAME = "Statistics";
        public const string KERN_SUM = "Sum";

        public const string PROP_IMAGE = "Image";
        public const string PROP_SIZE = "Size";
        public const string PROP_TOTAL = "Total";

        public const int NUM_THREADS = 16;
        public const float ACCURACY_RELATIVE = 1e-2f;
        public const float ACCURACY_ABSOLUTE = 1e-6f;

        [Test]
        public void GPUStatistics() {
            var tex = Resources.Load<Texture2D>(IMAGE_NAME);
            var pixels = (PlayerSettings.colorSpace == ColorSpace.Linear)
                ? tex.GetPixels().Select(c => c.linear).ColorToVector4()
                : tex.GetPixels().ColorToVector4();
            var expectedSum = pixels.Total();
            var expectedAverage = pixels.Average();
            var expectedCovariance = pixels.Covariance(expectedAverage);

            var stat = new GPUStatistics();
            var sum = stat.Sum(tex);
            AreApproximatelyEqual(expectedSum, sum);

            var average = stat.Average(tex);
            AreApproximatelyEqual(expectedAverage, average);

            var covariance = stat.Covariance(tex);
            AreApproximatelyEqual(expectedCovariance, covariance);
            Debug.LogFormat("Covariance :\nGPU\n{0}\nCPU\n{1}", covariance, expectedCovariance);

        }

        [Test]
        public void GPUReduction() { 
            var redwidth = 4;
            var redheight = 4;
            var redinput = new Vector4[redwidth * redheight];
            using (var redbuf = new DisposableBuffer(redinput.Length, Marshal.SizeOf(redinput[0]))) {
                for (var x = 0; x < redwidth; x++) {
                    for (var y = 0; y < redheight; y++) {
                        var v = Vector4.zero;
                        v[x] = (y <= x ? 1f : 0f);
                        redinput[x + y * redwidth] = v;
                    }
                }
                redbuf.Buffer.SetData(redinput);

                var red = new GPUReduction();
                var accum = red.Accumulate4(redbuf, redwidth, redheight);
                AreApproximatelyEqual(new Vector4(1, 2, 3, 4), accum);
            }
        }

        private string Format(Vector4 v, string accuracy = "e2") {
            return string.Format(
                string.Format("({{0:{0}}},{{1:{0}}},{{2:{0}}},{{3:{0}}}", accuracy),
                v.x, v.y, v.z, v.w);
        }
        private void AreApproximatelyEqual(Vector4 expected, Vector4 actual, float delta = ACCURACY_ABSOLUTE) {
            for (var i = 0; i < 4; i++) {
                Assert.AreEqual(expected[i], actual[i], Accuracy(expected[i], delta),
                    string.Format("Vector not equal at i={0} : a={1} b={2}", i, expected[i], actual[i]));
            }
        }
        private void AreApproximatelyEqual(Matrix4x4 expected, Matrix4x4 actual, float delta = ACCURACY_ABSOLUTE) {
            for (var x = 0; x < 4; x++) {
                for (var y = 0; y < 4; y++) {
                    var va = expected[x, y];
                    var vb = actual[x, y];
                    Assert.AreEqual(va, vb, Accuracy(va, delta),
                        string.Format("Matrix not equal at ({0},{1}) : a={2} b={3}",
                        x, y, va, vb));
                }
            }
        }
        private float Accuracy(float value, float delta) {
            return Mathf.Max(value * ACCURACY_RELATIVE, ACCURACY_ABSOLUTE);
        }
    }

}
