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
        public const string KERNEL_MULTIPLY4 = "Multiply4";

        public const string PROP_INPUT_SIZE = "_InputSize";
        public const string PROP_INPUT4 = "_Input4";
        public const string PROP_INPUT_IMAGE = "_InputImage";

        public const string PROP_OUTPUT_SIZE = "_OutputSize";
        public const string PROP_OUTPUT4 = "_Output4";

        public const string PROP_MULTIPLIED4 = "_Multiplied4";

        public const int NUM_THREADS = 16;
        public const int SCALE_TO_INT = 255;
        public const float SCALE_TO_FLOAT = 1f / SCALE_TO_INT;

        public static readonly int STRIDE_OF_VECTOR4 = Marshal.SizeOf(typeof(Vector4));

        protected GPUReduction reduction;
        protected ComputeShader cs;
        protected int kernelSum;
        protected int kernelMultiply4;

        public GPUStatistics() {
            reduction = new GPUReduction();
            cs = Resources.Load<ComputeShader>(CS_NAME);
            kernelSum = cs.FindKernel(KERN_SUM);
            kernelMultiply4 = cs.FindKernel(KERNEL_MULTIPLY4);
        }

        public Vector4 Sum(Texture2D tex) {
            var total = new Vector4[1];
            using (var totalBuffer = new DisposableBuffer(total.Length, STRIDE_OF_VECTOR4)) {
                totalBuffer.Buffer.SetData(total);
                Sum(tex, totalBuffer);
                totalBuffer.Buffer.GetData(total);
                return total[0];
            }
        }
        public Vector4 Average(Texture2D tex) {
            using (var totalBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4))
            using (var averageBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4)) {
                var pixelCount = tex.width * tex.height;
                Sum(tex, totalBuf);
                Multiply(totalBuf, Vector4.one / pixelCount, averageBuf);
                var average = new Vector4[1];
                averageBuf.Buffer.GetData(average);
                return average[0];
            }
        }

        public void Sum(Texture2D tex, ComputeBuffer outputBuf) {
            var totalWidth = Mathf.CeilToInt(tex.width / (float)NUM_THREADS);
            var totalHeight = Mathf.CeilToInt(tex.width / (float)NUM_THREADS);

            using (var totalBuf = new DisposableBuffer(totalWidth * totalHeight, STRIDE_OF_VECTOR4)) { 
                cs.SetInts(PROP_INPUT_SIZE, tex.width, tex.height);
                cs.SetTexture(kernelSum, PROP_INPUT_IMAGE, tex);

                cs.SetInts(PROP_OUTPUT_SIZE, totalWidth, totalHeight);
                cs.SetBuffer(kernelSum, PROP_OUTPUT4, totalBuf);

                cs.Dispatch(kernelSum, totalWidth, totalHeight, 1);

                reduction.Accumulate4(totalBuf, totalWidth, totalHeight, outputBuf);
            }
        }
        public void Multiply(ComputeBuffer inputBuf, Vector4 multiplied, ComputeBuffer outputBuf) {
            var inputSize = inputBuf.count;
            cs.SetInts(PROP_INPUT_SIZE, inputSize, 1);
            cs.SetVector(PROP_MULTIPLIED4, multiplied);
            cs.SetBuffer(kernelMultiply4, PROP_INPUT4, inputBuf);
            cs.SetBuffer(kernelMultiply4, PROP_OUTPUT4, outputBuf);
            cs.Dispatch(kernelMultiply4, Mathf.CeilToInt(inputSize / (float)NUM_THREADS), 1, 1);
        }
    }
}
