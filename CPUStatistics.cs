using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GPUImageStatisticsSystem {

    public static class CPUStatistics {

        public static Vector4 Total(this IEnumerable<Vector4> iter) {
            var total = Vector4.zero;
            foreach (var v in iter)
                total += v;
            return total;
        }
        public static Vector4 Average(this IEnumerable<Vector4> iter) {
            var count = 0;
            var total = Vector4.zero;
            foreach (var v in iter) {
                count++;
                total += v;
            }
            return total / count;
        }
        public static Matrix4x4 Covariance(this IEnumerable<Vector4> iter, Vector4 average) {
            var count = 0;
            var total = Matrix4x4.zero;
            foreach (var v in iter) {
                count++;
                var dv = v - average;
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < 4; x++)
                        total[x, y] += dv[x] * dv[y];
            }
            for (var i = 0; i < 16; i++)
                total[i] /= count;
            return total;
        }
        public static IEnumerable<Vector4> ColorToVector4(this IEnumerable<Color> iter) {
            return iter.Select(c => (Vector4)c);
        }
    }
}
