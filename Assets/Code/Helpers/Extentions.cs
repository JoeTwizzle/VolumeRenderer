using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace Assets.Code.Helpers
{
    internal static class Extentions
    {
        internal static unsafe uint AlignedSizeOf<T>() where T : unmanaged
        {
            uint size = (uint)sizeof(T);
            if (size == 1 || size == 2)
            {
                return size;
            }

            return (uint)((size + 3) & (~3));
        }
        /// <summary>
        /// Reads value types from memory starting at the offset, and writes them into a span. The number of value types that will be read is determined by the length of the span.</summary>
        /// <typeparam name="T">The value type to read.</typeparam>
        /// <param name="byteOffset">The location from which to start reading.</param>
        /// <param name="buffer">The output span to write to.</param>
        public static unsafe void ReadSpan<T>(this SafeBuffer sb, ulong byteOffset, Span<T> buffer)
            where T : unmanaged
        {

            uint alignedSizeofT = AlignedSizeOf<T>();
            byte* ptr = (byte*)sb.DangerousGetHandle() + byteOffset;

            bool mustCallRelease = false;
            try
            {
                sb.DangerousAddRef(ref mustCallRelease);
                fixed (T* dest = buffer)
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        Buffer.MemoryCopy(ptr + i * alignedSizeofT, dest + i, alignedSizeofT, alignedSizeofT);
                    }
                }
            }
            finally
            {
                if (mustCallRelease)
                    sb.DangerousRelease();
            }
        }
    }
}
