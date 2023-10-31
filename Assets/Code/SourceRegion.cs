using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code
{
    [StructLayout(LayoutKind.Sequential, Size = sizeof(int) * 8)]
    struct SourceRegion
    {
        public Box3i SourceDimensions;
        public long BufferOffset;

        public SourceRegion(Box3i sourceBox, long bufferOffset)
        {
            SourceDimensions = sourceBox;
            BufferOffset = bufferOffset;
        }
    }
}
