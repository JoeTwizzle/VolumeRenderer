using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Assets.Code.Fits
{    
    //Originally from https://github.com/RononDex/FitsLibrary,
    //as such this file is available under MPL2.0 https://github.com/RononDex/FitsLibrary/blob/master/LICENSE
    //Code modified by JoeTwizzle
    public class Header
    {
        private readonly List<HeaderEntry> _entries;

        private DataContentType? _cachedDataContentType;

        private int? _cachedNumberOfAxisInMainContent;

        private int[]? _cachedAxisSizes;

        public DataContentType DataContentType
        {
            get
            {
                DataContentType valueOrDefault = _cachedDataContentType.GetValueOrDefault();
                if (!_cachedDataContentType.HasValue)
                {
                    valueOrDefault = (DataContentType)Convert.ToInt32(this["BITPIX"]);
                    _cachedDataContentType = valueOrDefault;
                    return valueOrDefault;
                }

                return valueOrDefault;
            }
        }

        public int NumberOfAxisInMainContent
        {
            get
            {
                int valueOrDefault = _cachedNumberOfAxisInMainContent.GetValueOrDefault();
                if (!_cachedNumberOfAxisInMainContent.HasValue)
                {
                    valueOrDefault = Convert.ToInt32(this["NAXIS"]);
                    _cachedNumberOfAxisInMainContent = valueOrDefault;
                    return valueOrDefault;
                }

                return valueOrDefault;
            }
        }

        public int[] AxisSizes => _cachedAxisSizes ??= Enumerable.Range(0, NumberOfAxisInMainContent).Select(delegate (int i)
        {
            return Convert.ToInt32(this[$"NAXIS{i + 1}"]);
        }).ToArray();

        public IList<HeaderEntry> Entries => _entries;

        public object? this[string key]
        {
            get
            {
                string key2 = key;
                return _entries.Find((HeaderEntry entry) => string.Equals(entry.Key, key2, StringComparison.Ordinal))?.Value;
            }
        }

        public Header(List<HeaderEntry> entries)
        {
            _entries = entries;
        }

        public Header()
        {
            _entries = new List<HeaderEntry>();
        }
    }
}
