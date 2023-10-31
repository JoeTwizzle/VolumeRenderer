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
    public class HeaderEntry
    {
        public string Key { get; set; }

        public object? Value { get; set; }

        public string? Comment { get; set; }

        public HeaderEntry(string key, object? value, string? comment)
        {
            Key = key;
            Value = value;
            Comment = comment;
        }
    }
}
