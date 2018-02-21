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

        public const string PROP_INPUT_SIZE = "_InputSize";
        public const string PROP_INPUT4 = "_Input4";
        public const string PROP_OUTPUT4 = "_Output4";

        protected ComputeShader cs;
        protected int kernelAccumX4;
        protected int kernelAccumY4;

        public GPUReduction() {
            cs = Resources.Load<ComputeShader>(CS_NAME);
            kernelAccumX4 = cs.FindKernel(KERN_ACCUM_X4);
            kernelAccumY4 = cs.FindKernel(KERN_ACCUM_Y4);
        }

        public Vector4 Accumulate4(ComputeBuffer grid4, int width, int height) {
            var reduction = new Vector4[1];
            var reductionBuf = new ComputeBuffer(reduction.Length, Marshal.SizeOf(reduction[0]));
            var lineoutBuf = new ComputeBuffer(height, reductionBuf.stride);
            try {
                cs.SetInts(PROP_INPUT_SIZE, width, height);
                cs.SetBuffer(kernelAccumX4, PROP_INPUT4, grid4);
                cs.SetBuffer(kernelAccumX4, PROP_OUTPUT4, lineoutBuf);
                cs.Dispatch(kernelAccumX4, 1, Mathf.CeilToInt(height / (float)NUM_THREADS), 1);

                cs.SetInts(PROP_INPUT_SIZE, 1, height);
                cs.SetBuffer(kernelAccumY4, PROP_INPUT4, lineoutBuf);
                cs.SetBuffer(kernelAccumY4, PROP_OUTPUT4, reductionBuf);
                cs.Dispatch(kernelAccumY4, 1, 1, 1);

                reductionBuf.GetData(reduction);
                return reduction[0];
            } finally {
                lineoutBuf.Dispose();
                reductionBuf.Dispose();
            }
        }

        #region IDisposable
        public void Dispose() {
        }
        #endregion
    }
}
