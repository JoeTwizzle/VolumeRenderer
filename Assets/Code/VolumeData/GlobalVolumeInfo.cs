using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.VolumeData
{
    readonly struct GlobalVolumeInfo
    {
        public readonly Box3i Dimensions;
        public readonly float MinValue;
        public readonly float MaxValue;

        public GlobalVolumeInfo(Box3i dimensions, float minValue, float maxValue)
        {
            Dimensions = dimensions;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}
