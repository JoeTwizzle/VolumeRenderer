using Assets.Code.Helpers;
using System;
using System.Collections.Generic;

namespace Assets.Code.Fits
{    
    //Originally from https://github.com/RononDex/FitsLibrary,
    //as such this file is available under MPL2.0 https://github.com/RononDex/FitsLibrary/blob/master/LICENSE
    //Code modified by JoeTwizzle
    sealed class FitsFloatDocument
    {
        private Memory<int> AxisIndexFactors;

        public UnmanagedMemoryRange? RawData { get; }

        public Header Header { get; }

        public FitsFloatDocument(Header header, UnmanagedMemoryRange? content)
        {
            Header = header;
            RawData = content;
            InitHelperData();
        }

        private void InitHelperData()
        {
            if (RawData.HasValue)
            {
                AxisIndexFactors = new int[Header.NumberOfAxisInMainContent];
                Span<int> span = AxisIndexFactors.Span;
                span[0] = 1;
                for (int i = 1; i < AxisIndexFactors.Length; i++)
                {
                    ref int reference = ref span[i];
                    int num = span[i - 1];
                    Header header = Header;
                    reference = num * Convert.ToInt32(header[$"NAXIS{i}"]);
                }
            }
        }
    }
}
