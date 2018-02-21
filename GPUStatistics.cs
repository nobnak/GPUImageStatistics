using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

namespace GPUImageStatisticsSystem {

    public class GPUStatistics {
        public const string CS_NAME = "Statistics";
        public const string KERN_SUM = "Sum";
        public const string KERN_AVERAGE = "Average";

        public const string PROP_IMAGE = "_Image";
        public const string PROP_SIZE = "_Size";
        public const string PROP_TOTAL = "_Total";
        public const string PROP_AVERAGE = "_Average";

        public const int NUM_THREADS = 16;
        public const int SCALE_TO_INT = 255;
        public const float SCALE_TO_FLOAT = 1f / SCALE_TO_INT;

        protected ComputeShader cs;
        protected int kernelSum;
        protected int kernelAverage;

        public GPUStatistics() {
            cs = Resources.Load<ComputeShader>(CS_NAME);
            kernelSum = cs.FindKernel(KERN_SUM);
            kernelAverage = cs.FindKernel(KERN_AVERAGE);
        }

        public Vector4 Sum(Texture2D tex) {
            var total = new uint[4];
            var totalBuffer = new ComputeBuffer(total.Length, Marshal.SizeOf(total[0]));
            try {
                totalBuffer.SetData(total);
                ComputeSum(tex, totalBuffer);
                totalBuffer.GetData(total);

                return new Vector4(
                    total[0] * SCALE_TO_FLOAT,
                    total[1] * SCALE_TO_FLOAT,
                    total[2] * SCALE_TO_FLOAT,
                    total[3] * SCALE_TO_FLOAT);
            } finally {
                totalBuffer.Dispose();
            }
        }
        public Vector4 Average(Texture2D tex) {
            var total = new uint[4];
            var average = new float[4];
            var totalBuffer = new ComputeBuffer(total.Length, Marshal.SizeOf(total[0]));
            var averageBuffer = new ComputeBuffer(average.Length, Marshal.SizeOf(average[0]));
            try {
                totalBuffer.SetData(total);
                ComputeSum(tex, totalBuffer);
                ComputeAverage(tex, average, totalBuffer, averageBuffer);
                averageBuffer.GetData(average);
                return new Vector4(average[0], average[1], average[2], average[3]);
            } finally {
                totalBuffer.Dispose();
                averageBuffer.Dispose();
            }
        }

        private void ComputeAverage(Texture2D tex, float[] average, ComputeBuffer totalBuffer, ComputeBuffer averageBuffer) {
            averageBuffer.SetData(average);
            cs.SetInts(PROP_SIZE, tex.width, tex.height);
            cs.SetBuffer(kernelAverage, PROP_TOTAL, totalBuffer);
            cs.SetBuffer(kernelAverage, PROP_AVERAGE, averageBuffer);
            cs.Dispatch(kernelAverage, 4, 1, 1);
        }

        private void ComputeSum(Texture2D tex, ComputeBuffer totalBuffer) {
            cs.SetBuffer(kernelSum, PROP_TOTAL, totalBuffer);
            cs.SetTexture(kernelSum, PROP_IMAGE, tex);
            cs.SetInts(PROP_SIZE, tex.width, tex.height);
            cs.Dispatch(kernelSum,
                Mathf.CeilToInt(tex.width / (float)NUM_THREADS),
                Mathf.CeilToInt(tex.width / (float)NUM_THREADS), 1);
        }
    }
}
