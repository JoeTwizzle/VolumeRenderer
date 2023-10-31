using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.Fits
{    
    //Originally from https://github.com/RononDex/FitsLibrary,
    //as such this file is available under MPL2.0 https://github.com/RononDex/FitsLibrary/blob/master/LICENSE
    //Code modified by JoeTwizzle
    public enum DataContentType
    {
        DOUBLE = -64,
        FLOAT = -32,
        BYTE = 8,
        SHORT = 0x10,
        INTEGER = 0x20,
        LONG = 0x40
    }
}
