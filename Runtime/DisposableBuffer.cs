using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUImageStatisticsSystem {

    public class DisposableBuffer : System.IDisposable {
        protected ComputeBuffer buf;

        public DisposableBuffer(int count, int stride, ComputeBufferType type = ComputeBufferType.Default) {
            buf = new ComputeBuffer(count, stride, type);
        }

        public ComputeBuffer Buffer { get { return buf; } }

        public static implicit operator ComputeBuffer (DisposableBuffer cbd) {
            return cbd.buf;
        }

        public void Dispose() {
            if (buf != null) {
                buf.Dispose();
                buf = null;
            }
        }
    }
}
