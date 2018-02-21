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
        public const string KERNEL_SUM = "Sum";
        public const string KERNEL_MULTIPLY4 = "Multiply4";
        public const string KERNEL_MULTIPLY4x4 = "Multiply4x4";
        public const string KERNEL_COVARIANCE = "Covariance";

        public const string PROP_INPUT_SIZE = "_InputSize";
        public const string PROP_INPUT4 = "_Input4";
        public const string PROP_INPUT4x4 = "_Input4x4";
        public const string PROP_INPUT_IMAGE = "_InputImage";

        public const string PROP_OUTPUT_SIZE = "_OutputSize";
        public const string PROP_OUTPUT4 = "_Output4";
        public const string PROP_OUTPUT4x4 = "_Output4x4";

        public const string PROP_PARAM0_4 = "_Param0_4";
        public const string PROP_PARAM0_4x4 = "_Param0_4x4";
        public const string PROP_PARAM_BUF0_4 = "_ParamBuf0_4";

        public const int NUM_THREADS = 16;
        public const int SCALE_TO_INT = 255;
        public const float SCALE_TO_FLOAT = 1f / SCALE_TO_INT;

        public static readonly int STRIDE_OF_VECTOR4 = Marshal.SizeOf(typeof(Vector4));
        public static readonly int STRIDE_OF_VECTOR4x4 = Marshal.SizeOf(typeof(Matrix4x4));

        protected readonly GPUReduction reduction;
        protected readonly ComputeShader cs;
        protected readonly int kernelSum;
        protected readonly int kernelMultiply4;
        protected readonly int kernelMultiply4x4;
        protected readonly int kernelCovariance;

        public GPUStatistics() {
            reduction = new GPUReduction();
            cs = Resources.Load<ComputeShader>(CS_NAME);
            kernelSum = cs.FindKernel(KERNEL_SUM);
            kernelMultiply4 = cs.FindKernel(KERNEL_MULTIPLY4);
            kernelMultiply4x4 = cs.FindKernel(KERNEL_MULTIPLY4x4);
            kernelCovariance = cs.FindKernel(KERNEL_COVARIANCE);
        }

        #region Sum
        public Vector4 Sum(Texture2D tex) {
            var total = new Vector4[1];
            using (var totalBuffer = new DisposableBuffer(total.Length, STRIDE_OF_VECTOR4)) {
                totalBuffer.Buffer.SetData(total);
                Sum(tex, totalBuffer);
                totalBuffer.Buffer.GetData(total);
                return total[0];
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
        #endregion

        #region Average
        public Vector4 Average(Texture2D tex) {
            using (var averageBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4)) {
                Average(tex, averageBuf);
                var average = new Vector4[1];
                averageBuf.Buffer.GetData(average);
                return average[0];
            }
        }
        public void Average(Texture2D tex, ComputeBuffer outputBuf) {
            using (var totalBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4)) {
                var pixelCount = tex.width * tex.height;
                Sum(tex, totalBuf);
                Multiply4(totalBuf, Vector4.one / pixelCount, outputBuf);
            }
        }
        #endregion

        #region Covariance
        public Matrix4x4 Covariance(Texture2D tex) {
            using (var covarianceBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4x4)) {
                Covariance(tex, covarianceBuf);
                var covariance = new Matrix4x4[1];
                covarianceBuf.Buffer.GetData(covariance);
                return covariance[0];
            }
        }
        public void Covariance(Texture2D tex, ComputeBuffer averageBuf, ComputeBuffer outputBuf) {
            var totalWidth = Mathf.CeilToInt(tex.width / (float)NUM_THREADS);
            var totalHeight = Mathf.CeilToInt(tex.height / (float)NUM_THREADS);

            using (var totalCovBuf = new DisposableBuffer(totalWidth * totalHeight, STRIDE_OF_VECTOR4x4))
            using (var totalBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4x4)) {
                cs.SetInts(PROP_INPUT_SIZE, tex.width, tex.height);
                cs.SetTexture(kernelCovariance, PROP_INPUT_IMAGE, tex);

                cs.SetInts(PROP_OUTPUT_SIZE, totalWidth, totalHeight);
                cs.SetBuffer(kernelCovariance, PROP_OUTPUT4x4, totalCovBuf);

                cs.SetBuffer(kernelCovariance, PROP_PARAM_BUF0_4, averageBuf);

                cs.Dispatch(kernelCovariance, totalWidth, totalHeight, 1);

                reduction.Accumulate4x4(totalCovBuf, totalWidth, totalHeight, outputBuf);

                var pixelCount = tex.width * tex.height;
                var v = Vector4.one / pixelCount;
                var m = new Matrix4x4(v, v, v, v);
                //Multiply4x4(totalBuf, m, outputBuf);
            }
        }
        public void Covariance(Texture2D tex, ComputeBuffer outputBuf) {
            using (var averageBuf = new DisposableBuffer(1, STRIDE_OF_VECTOR4)) {
                Average(tex, averageBuf);
                Covariance(tex, averageBuf, outputBuf);
            }
        }
        #endregion

        #region Multiply
        public void Multiply4(ComputeBuffer inputBuf, Vector4 multiplied, ComputeBuffer outputBuf) {
            var inputSize = inputBuf.count;
            cs.SetInts(PROP_INPUT_SIZE, inputSize, 1);
            cs.SetVector(PROP_PARAM0_4, multiplied);
            cs.SetBuffer(kernelMultiply4, PROP_INPUT4, inputBuf);
            cs.SetBuffer(kernelMultiply4, PROP_OUTPUT4, outputBuf);
            cs.Dispatch(kernelMultiply4, Mathf.CeilToInt(inputSize / (float)NUM_THREADS), 1, 1);
        }
        public void Multiply4x4(ComputeBuffer inputBuf, Matrix4x4 multiplied, ComputeBuffer outputBuf) {
            var inputSize = inputBuf.count;
            cs.SetInts(PROP_INPUT_SIZE, inputSize, 1);
            cs.SetMatrix(PROP_PARAM0_4x4, multiplied);
            cs.SetBuffer(kernelMultiply4x4, PROP_INPUT4x4, inputBuf);
            cs.SetBuffer(kernelMultiply4x4, PROP_OUTPUT4x4, outputBuf);
            cs.Dispatch(kernelMultiply4x4, Mathf.CeilToInt(inputSize / (float)NUM_THREADS), 1, 1);
        }
        #endregion
    }
}
