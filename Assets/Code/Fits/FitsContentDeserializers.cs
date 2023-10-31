using Assets.Code.Helpers;
using System;
using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.Fits
{
    //Originally from https://github.com/RononDex/FitsLibrary,
    //as such this file is available under MPL2.0 https://github.com/RononDex/FitsLibrary/blob/master/LICENSE
    //Code modified for faster float parsing by JoeTwizzle
    sealed class FitsFloatContentDeserializer
    {
        private const int ChunkSize = 2880;
        private const int numberOfBytesPerValue = sizeof(float);

        public unsafe (float minValue, float maxValue) Deserialize(MemoryMappedFile dataStream, ulong start, Header header, UnmanagedMemoryRange dest)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            if (header.NumberOfAxisInMainContent < 1)
            {
                // Return endOfStreamReached false, since this method is only called if endOfStreamReached was false
                // before calling this method, so since we did not read anything, it should still be false
                return (min, max);
            }

            var numberOfAxis = header.NumberOfAxisInMainContent;

            var axisSizes = Enumerable.Range(1, numberOfAxis).Select(axisIndex => Convert.ToUInt64(header[$"NAXIS{axisIndex}"])).ToArray();

            var axisSizesSpan = new ReadOnlySpan<ulong>(axisSizes);

            var totalNumberOfValues = axisSizes.Aggregate((ulong)1, (x, y) => x * y);

            ulong contentSizeInBytes = numberOfBytesPerValue * totalNumberOfValues;

            UnmanagedMemoryRange dataPointsMemory = dest;
            var dataPointer = (float*)dataPointsMemory.First;

            var q = (ulong)Math.DivRem((long)contentSizeInBytes, ChunkSize, out var r);
            if (r > 0)
            {
                q++;
            }

            ulong totalContentSizeInBytes = q * ChunkSize;

            if (header.DataContentType != DataContentType.FLOAT)
            {
                throw new ArgumentException("Content must be float");
            }

            ulong bytesRead = 0;
            ulong currentValueIndex = 0;
            Span<byte> currentValueBuffer = stackalloc byte[numberOfBytesPerValue];
            for (ulong index = 0; index < q; index++)
            {
                var blockSize = Math.Min(ChunkSize, contentSizeInBytes - bytesRead);
                bytesRead += blockSize;
                using var chunk = dataStream.CreateViewAccessor((long)(start + ChunkSize * index), (long)blockSize, MemoryMappedFileAccess.Read);
                byte* pointer = null;
                try
                {
                    chunk.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
                    pointer += chunk.PointerOffset;
                    for (ulong i = 0; i < blockSize; i += numberOfBytesPerValue)
                    {
                        float val = ReadSingleBigEndian(new Span<byte>(pointer + i, numberOfBytesPerValue));
                        min = Math.Min(min, val);
                        max = Math.Max(max, val);
                        dataPointer[currentValueIndex++] = val;
                    }
                }
                finally
                {
                    if (pointer != null)
                    {
                        chunk.SafeMemoryMappedViewHandle.ReleasePointer();
                    }
                }
            }

            return (min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadSingleBigEndian(ReadOnlySpan<byte> source)
        {
            return BitConverter.IsLittleEndian ?
                BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(source))) :
                MemoryMarshal.Read<float>(source);
        }
    }
}
