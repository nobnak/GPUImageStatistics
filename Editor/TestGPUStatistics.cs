using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace GPUStatisticsSystem {

    public class TestGPUStatistics {
        public const string IMAGE_NAME = "SampleImage";

        public const string CS_NAME = "Statistics";
        public const string KERN_SUM = "Sum";

        public const string PROP_IMAGE = "Image";
        public const string PROP_SIZE = "Size";
        public const string PROP_TOTAL = "Total";

        public const int NUM_THREADS = 16;

        [Test]
        public void TestGPUStatisticsSimplePasses() {
            var tex = Resources.Load<Texture2D>(IMAGE_NAME);
            var expectedSum = new Vector4(4, 16, 64, 1024);

            var stat = new GPUStatistics();
            var sum = stat.Sum(tex);
            AreEqual(sum, expectedSum);

            var average = stat.Average(tex);
            AreEqual(average, expectedSum / 1024);

            var redwidth = 4;
            var redheight = 4;
            var redinput = new Vector4[redwidth * redheight];
            var redbuf = new ComputeBuffer(redinput.Length, Marshal.SizeOf(redinput[0]));
            try {
                for (var x = 0; x < redwidth; x++) {
                    for (var y = 0; y < redheight; y++) {
                        var v = Vector4.zero;
                        v[x] = (y <= x ? 1f : 0f);
                        redinput[x + y * redwidth] = v;
                    }
                }
                redbuf.SetData(redinput);

                var red = new GPUReduction();
                var accum = red.Accumulate4(redbuf, redwidth, redheight);
                Debug.LogFormat("Reduction : {0}", accum);
            } finally {
                redbuf.Dispose();
            }
        }

        private string Format(Vector4 v, string accuracy = "e2") {
            return string.Format(
                string.Format("({{0:{0}}},{{1:{0}}},{{2:{0}}},{{3:{0}}}", accuracy),
                v.x, v.y, v.z, v.w);
        }
        private void AreEqual(Vector4 a, Vector4 b, float delta = 1e-3f) {
            for (var i = 0; i < 4; i++) {
                Assert.AreEqual(a[i], b[i], delta,
                    string.Format("Vector not equal at i={0} : a={1} b={2}", i, a[i], b[i]));
            }
        }
    }

}
