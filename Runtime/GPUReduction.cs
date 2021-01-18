using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GPUImageStatisticsSystem {

    public class GPUReduction : System.IDisposable {
        public const string CS_NAME = "Reduction";
        public const int NUM_THREADS = 16;

        public const string KERN_ACCUM_X4 = "AccumulateX4";
        public const string KERN_ACCUM_Y4 = "AccumulateY4";
        public const string KERN_ACCUM_X4x4 = "AccumulateX4x4";
        public const string KERN_ACCUM_Y4x4 = "AccumulateY4x4";

        public const string PROP_INPUT_SIZE = "_InputSize";
        public const string PROP_INPUT4 = "_Input4";
        public const string PROP_INPUT4x4 = "_Input4x4";
        public const string PROP_OUTPUT4 = "_Output4";
        public const string PROP_OUTPUT4x4 = "_Output4x4";

        public static readonly int STRIDE_OF_VECTOR4 = Marshal.SizeOf(typeof(Vector4));
        public static readonly int STRIDE_OF_VECTOR4x4 = Marshal.SizeOf(typeof(Matrix4x4));

        protected readonly ComputeShader cs;
        protected readonly int kernelAccumX4;
        protected readonly int kernelAccumY4;
        protected readonly int kernelAccumX4x4;
        protected readonly int kernelAccumY4x4;

        public GPUReduction() {
            cs = Resources.Load<ComputeShader>(CS_NAME);
            kernelAccumX4 = cs.FindKernel(KERN_ACCUM_X4);
            kernelAccumY4 = cs.FindKernel(KERN_ACCUM_Y4);
            kernelAccumX4x4 = cs.FindKernel(KERN_ACCUM_X4x4);
            kernelAccumY4x4 = cs.FindKernel(KERN_ACCUM_Y4x4);
        }

        public Vector4 Accumulate4(ComputeBuffer grid4, int width, int height) {
            var reduction = new Vector4[1];
            using (var reductionBuf = new DisposableBuffer(reduction.Length, STRIDE_OF_VECTOR4)) {
                Accumulate4(grid4, width, height, reductionBuf);
                reductionBuf.Buffer.GetData(reduction);
                return reduction[0];
            }
        }
        public Matrix4x4 Accumulate4x4(ComputeBuffer grid4x4, int width, int height) {
            var reduction = new Matrix4x4[1];
            using (var reductionBuf = new DisposableBuffer(reduction.Length, STRIDE_OF_VECTOR4x4)) {
                Accumulate4x4(grid4x4, width, height, reductionBuf);
                reductionBuf.Buffer.GetData(reduction);
                return reduction[0];
            }
        }

        public void Accumulate4(ComputeBuffer grid4, int width, int height, ComputeBuffer outputBuf) {
            using (var lineoutBuf = new DisposableBuffer(height, STRIDE_OF_VECTOR4)) {
                cs.SetInts(PROP_INPUT_SIZE, width, height);
                cs.SetBuffer(kernelAccumX4, PROP_INPUT4, grid4);
                cs.SetBuffer(kernelAccumX4, PROP_OUTPUT4, lineoutBuf);
                cs.Dispatch(kernelAccumX4, 1, Mathf.CeilToInt(height / (float)NUM_THREADS), 1);

                cs.SetInts(PROP_INPUT_SIZE, 1, height);
                cs.SetBuffer(kernelAccumY4, PROP_INPUT4, lineoutBuf);
                cs.SetBuffer(kernelAccumY4, PROP_OUTPUT4, outputBuf);
                cs.Dispatch(kernelAccumY4, 1, 1, 1);
            }
        }
        public void Accumulate4x4(ComputeBuffer grid4x4, int width, int height, ComputeBuffer outputBuf) {
            using (var lineoutBuf = new DisposableBuffer(height, STRIDE_OF_VECTOR4x4)) {
                cs.SetInts(PROP_INPUT_SIZE, width, height);
                cs.SetBuffer(kernelAccumX4x4, PROP_INPUT4x4, grid4x4);
                cs.SetBuffer(kernelAccumX4x4, PROP_OUTPUT4x4, lineoutBuf);
                cs.Dispatch(kernelAccumX4x4, 1, Mathf.CeilToInt(height / (float)NUM_THREADS), 1);

                cs.SetInts(PROP_INPUT_SIZE, 1, height);
                cs.SetBuffer(kernelAccumY4x4, PROP_INPUT4x4, lineoutBuf);
                cs.SetBuffer(kernelAccumY4x4, PROP_OUTPUT4x4, outputBuf);
                cs.Dispatch(kernelAccumY4x4, 1, 1, 1);
            }
        }

        #region IDisposable
        public void Dispose() {
        }
        #endregion
    }
}
