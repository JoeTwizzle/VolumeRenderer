﻿using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Code.Helpers;

namespace Assets.Code.Fits
{    
    //Originally from https://github.com/RononDex/FitsLibrary,
    //as such this file is available under MPL2.0 https://github.com/RononDex/FitsLibrary/blob/master/LICENSE
    //Code modified by JoeTwizzle
    public sealed class FitsHeaderDeserializer
    {
        /// <summary>
        /// Length of a header entry chunk, containing a single header entry
        /// </summary>
        public const int HeaderEntryChunkSize = 80;
        public const int HeaderBlockSize = 2880;
        public const int LogicalValuePosition = 20;
        public const char ContinuedStringMarker = '&';
        private const string ContinueKeyWord = "CONTINUE";

        /// <summary>
        /// Representation of the Headers END marker
        /// "END" + 77 spaces in ASCII
        /// </summary>
        public static readonly byte[] END_MARKER =
            new List<byte> { 0x45, 0x4e, 0x44 }
                .Concat(Enumerable.Repeat(element: (byte)0x20, count: 77))
                .ToArray();

        /// <summary>
        /// Deserializes the header part of the fits document
        /// </summary>
        /// <param name="dataStream">the stream from which to read the data from (should be at position 0)</param>
        /// <exception cref="InvalidDataException"></exception>
        public (bool endOfStreamReached, Header parsedHeader, ulong dataStart) Deserialize(MemoryMappedFile dataStream)
        {
            PreValidateStream(dataStream);

            var endOfHeaderReached = false;
            var headerEntries = new List<HeaderEntry>();
            var endOfStreamReached = false;
            int i = 0;

            while (!endOfHeaderReached)
            {
                using var block = dataStream.CreateViewAccessor(i * HeaderBlockSize, HeaderBlockSize);
                headerEntries.AddRange(ParseHeaderBlock(block, out endOfHeaderReached));
                i++;
            }

            return (endOfStreamReached, new Header(headerEntries), (ulong)(i * HeaderBlockSize));
        }

        private static List<HeaderEntry> ParseHeaderBlock(MemoryMappedViewAccessor headerBlock, out bool endOfHeaderReached)
        {
            endOfHeaderReached = false;
            var currentIndex = 0;
            Span<byte> headerBlockSpan = stackalloc byte[HeaderBlockSize];
            headerBlock.SafeMemoryMappedViewHandle.ReadSpan((ulong)headerBlock.PointerOffset, headerBlockSpan);
            var headerEntries = new List<HeaderEntry>();
            var isContinued = false;

            while (currentIndex < HeaderBlockSize)
            {
                var headerEntryChunk = headerBlockSpan.Slice(currentIndex, HeaderEntryChunkSize);
                currentIndex += HeaderEntryChunkSize;

                if (headerEntryChunk.SequenceEqual(END_MARKER))
                {
                    endOfHeaderReached = true;
                    break;
                }

                var parsedHeaderEntry = ParseHeaderEntryChunk(headerEntryChunk);
                if (!isContinued)
                {
                    if (ValueIsStringAndHasContinueMarker(parsedHeaderEntry.Value))
                    {
                        isContinued = true;
                        parsedHeaderEntry.Value = (parsedHeaderEntry.Value as string)!.Trim()[..^1];
                    }

                    headerEntries.Add(parsedHeaderEntry);
                }
                else
                {
                    if (!string.Equals(parsedHeaderEntry.Key, ContinueKeyWord, StringComparison.Ordinal))
                    {
                        throw new InvalidDataException("Unfinished continued value found");
                    }
                    var valueToAppend = parsedHeaderEntry.Value as string;
                    if (ValueIsStringAndHasContinueMarker(parsedHeaderEntry.Value))
                    {
                        valueToAppend = valueToAppend!.Trim()[..^1];
                        isContinued = true;
                    }
                    else
                    {
                        isContinued = false;
                    }
                    headerEntries[^1].Value = $"{headerEntries[^1].Value as string}{valueToAppend}";
                    if (parsedHeaderEntry.Comment != null)
                    {
                        headerEntries[^1].Comment += $" {parsedHeaderEntry.Comment}";
                    }
                }

            }

            return headerEntries;
        }

        private static bool ValueIsStringAndHasContinueMarker(object? value)
        {
            return value is string parsedString && parsedString.Trim().EndsWith(ContinuedStringMarker);
        }

        private static HeaderEntry ParseHeaderEntryChunk(ReadOnlySpan<byte> headerEntryChunk)
        {
            var key = Encoding.ASCII.GetString(headerEntryChunk.Slice(0, 8)).Trim();
            if (HeaderEntryChunkHasValueMarker(headerEntryChunk)
                    || HeaderEntryEntryChunkHasContinueMarker(key))
            {
                ReadOnlySpan<char> value = Encoding.ASCII.GetString(headerEntryChunk.Slice(10, 70)).Trim();
                if (value.IndexOf('/') != -1)
                {
                    var comment = value[(value.IndexOf('/') + 1)..].Trim().Trim('\0');
                    value = value[0..value.IndexOf('/')].Trim();
                    var parsedValue = ParseValue(value);
                    return new HeaderEntry(key, parsedValue, new string(comment));
                }
                else
                {
                    var parsedValue = ParseValue(value);
                    return new HeaderEntry(
                        key: key,
                        value: parsedValue,
                        comment: null);
                }
            }

            return new HeaderEntry(
                key: key,
                value: null,
                comment: null);
        }

        private static bool HeaderEntryEntryChunkHasContinueMarker(string key)
        {
            return string.Equals(key, ContinueKeyWord, StringComparison.Ordinal);
        }

        private static object? ParseValue(ReadOnlySpan<char> value)
        {
            value = value.Trim('\0').Trim(' ');
            if (value.Length == 0)
            {
                return null;
            }

            if (value[0] == '\'')
            {
                return new String(value[1..^1]);
            }

            if (value.IndexOf('.') != -1)
            {
                return double.Parse(value);
            }

            if (value.Length == 1 && (value[0] == 'T' || value[0] == 'F'))
            {
                return value[0] == 'T';
            }

            return long.Parse(value);
        }

        private static bool HeaderEntryChunkHasValueMarker(ReadOnlySpan<byte> headerEntryChunk)
        {
            return headerEntryChunk[8] == 0x3D && headerEntryChunk[9] == 0x20;
        }

        private static void PreValidateStream(MemoryMappedFile dataStream)
        {
            if (dataStream == null)
                throw new ArgumentNullException(nameof(dataStream), "The Stream from which to read from can not be NULL");
        }
    }
}
