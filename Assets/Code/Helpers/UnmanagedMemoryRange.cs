using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.Helpers
{
    unsafe struct UnmanagedMemoryRange
    {
        public void* First;
        public ulong Length;

        public UnmanagedMemoryRange(void* first, ulong length)
        {
            First = first;
            Length = length;
        }
    }
}
