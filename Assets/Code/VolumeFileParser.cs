using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Assets.Code;
using CommunityToolkit.HighPerformance;
using Assets.Code.VolumeData;
using Assets.Code.Helpers;
using Assets.Code.Fits;

namespace Assets.Code
{
    static class VolumeFileParser
    {
        public const long HeaderDataLength = sizeof(int) * 6 + sizeof(long) + sizeof(float) * 2;

        /**
         * \brief 
         * \param filePath Path to VOTable xml file
         * \param sourceBoxes reference to existing vector of sourceRegions
         * \return Number of voxels for all sourceRegions
         */
        public static (long, SourceRegion[]) ExtractSourceRegionsFromXML(string filePath)
        {
            string cacheFile = filePath + ".cache";
            //if (File.Exists(cacheFile))
            //{
            //    using var cachedFile = new BinaryReader(File.OpenRead(cacheFile));
            //    long voxelsCached = cachedFile.ReadInt64();
            //    int sourceBoxesCount = cachedFile.ReadInt32();
            //    sourceBoxes = new SourceRegion[sourceBoxesCount];
            //    var bytes = sourceBoxes.AsSpan().AsBytes();
            //    cachedFile.Read(bytes);
            //    Thread.Sleep(1000);
            //    return voxelsCached;
            //}

            long voxels = 0;

            XDocument doc = XDocument.Load(filePath);
            XElement tableElement = doc.Element("{http://www.ivoa.net/xml/VOTable/v1.3}VOTABLE")!.Element("{http://www.ivoa.net/xml/VOTable/v1.3}RESOURCE")!.Element("{http://www.ivoa.net/xml/VOTable/v1.3}TABLE")!;
            XNode tableChild = tableElement.FirstNode!;
            int tableElementsIdx = -1;
            for (var currTableChild = tableChild; currTableChild != null; currTableChild = currTableChild.NextNode)
            {
                tableElementsIdx++;
                if (currTableChild is not XElement tableChildElement)
                {
                    break;
                }
                var nameAttribute = tableChildElement.Attribute("name");
                if (nameAttribute != null)
                {
                    string? match = null;
                    foreach (var mapEle in BoxCoordinateIndices.idxMap.Keys)
                    {
                        if (mapEle == nameAttribute.Value)
                        {
                            match = mapEle;
                            break;
                        }
                    }
                    if (match != null)
                    {
                        BoxCoordinateIndices.idxMap[match] = tableElementsIdx;
                    }
                }
            }

            var dataEntry = tableElement.Descendants("{http://www.ivoa.net/xml/VOTable/v1.3}DATA").Last().Descendants("{http://www.ivoa.net/xml/VOTable/v1.3}TABLEDATA").First().DescendantNodes().First();

            // Foreach TR-element (source)
            var sb = new List<SourceRegion>();
            for (; dataEntry != null; dataEntry = dataEntry.NextNode)
            {
                SourceBox b = new();
                int idx = 0;
                var idxToDo = BoxCoordinateIndices.idxMap.Count;
                if (dataEntry is XElement de)
                {
                    for (var tdEntry = de.DescendantNodes().First(); tdEntry != null && idxToDo > 0; tdEntry = tdEntry.NextNode)
                    {
                        foreach (var mapEle in BoxCoordinateIndices.idxMap)
                        {
                            if (mapEle.Value == idx)
                            {
                                b.idxMap[mapEle.Key] = int.Parse(((XElement)tdEntry).Value);
                                idxToDo--;
                            }
                        }
                        idx++;
                    }
                }
                SourceRegion sr = new();
                sr.SourceDimensions = b.ToBox();
                voxels += sr.SourceDimensions.LongVolume;
                sb.Add(sr);
            }

            var sourceBoxes = sb.ToArray();

            using var file = File.Create(cacheFile);
            file.Write(voxels);
            file.Write(sourceBoxes.Length);
            file.Write(sourceBoxes.AsSpan().AsBytes());

            return (voxels, sourceBoxes);
        }

        public static unsafe (MemoryMappedFile data, GlobalVolumeInfo info) LoadSourceVolume(string fitsFilePath)
        {
            string cacheFileName = fitsFilePath + ".raw.cache";
            if (File.Exists(cacheFileName))
            {
                var file = MemoryMappedFile.CreateFromFile(cacheFileName, FileMode.Open);
                byte* ptr = null;
                //header
                using var metaDataAccessor = file.CreateViewAccessor(0, HeaderDataLength);
                metaDataAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                ptr += metaDataAccessor.PointerOffset;
                Box3i box;
                float min;
                float max;
                Buffer.MemoryCopy(ptr, &box, sizeof(Box3i), sizeof(Box3i));
                //Buffer.MemoryCopy(ptr + sizeof(Box3i), &box, sizeof(Box3i), sizeof(Box3i));
                Buffer.MemoryCopy(ptr + sizeof(Box3i) + sizeof(long), &min, sizeof(float), sizeof(float));
                Buffer.MemoryCopy(ptr + sizeof(Box3i) + sizeof(long) + sizeof(float), &max, sizeof(float), sizeof(float));
                metaDataAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                return (file, new(box, min, max));
            }
            else
            {
                var headerDeserializer = new FitsHeaderDeserializer();
                var contentDeserializer = new FitsFloatContentDeserializer();
                using var srcFile = MemoryMappedFile.CreateFromFile(fitsFilePath, FileMode.Open);
                (bool endOfStreamReached, Header header, ulong dataStart) = headerDeserializer.Deserialize(srcFile);

                int axisCount = header.NumberOfAxisInMainContent;
                var imgType = header.DataContentType;
                int[] axis = header.AxisSizes;
                Box3i gridBox = new(
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(axis[0], axis[1], axis[2])
                );

                Debug.Assert(axisCount == 3);
                Debug.Assert(imgType == DataContentType.FLOAT);

                long destDataLength = gridBox.LongVolume;
                var resultFile = MemoryMappedFile.CreateFromFile(cacheFileName, FileMode.Create, null, destDataLength * sizeof(float) + HeaderDataLength);
                byte* ptr = null;

                //data
                using var destDataAccessor = resultFile.CreateViewAccessor(HeaderDataLength, destDataLength * sizeof(float));
                destDataAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                (float min, float max) = contentDeserializer.Deserialize(srcFile, dataStart, header, new UnmanagedMemoryRange(ptr + destDataAccessor.PointerOffset, (ulong)destDataLength));
                destDataAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                //header
                using var metaDataAccessor = resultFile.CreateViewAccessor(0, HeaderDataLength);
                metaDataAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                ptr += metaDataAccessor.PointerOffset;
                Buffer.MemoryCopy(&gridBox, ptr, sizeof(Box3i), sizeof(Box3i));
                Buffer.MemoryCopy(&destDataLength, ptr + sizeof(Box3i), sizeof(long), sizeof(long));
                Buffer.MemoryCopy(&min, ptr + sizeof(Box3i) + sizeof(long), sizeof(float), sizeof(float));
                Buffer.MemoryCopy(&max, ptr + sizeof(Box3i) + sizeof(long) + sizeof(float), sizeof(float), sizeof(float));
                metaDataAccessor.SafeMemoryMappedViewHandle.ReleasePointer();


                return (resultFile, new(gridBox, min, max));
            }
        }

        public static unsafe (MemoryMappedFile, FilteredVolumeInfo) ExtractDataFromVolume(string fitsFilePath, string catalogueFilePath)
        {
            (var sourceFile, var info) = LoadSourceVolume(fitsFilePath);
            string cacheFileName = fitsFilePath + "_" + Path.GetFileName(catalogueFilePath) + ".cache";
            if (File.Exists(cacheFileName))
            {
                sourceFile.Dispose();
                var file = MemoryMappedFile.CreateFromFile(cacheFileName, FileMode.Open);
                byte* ptr = null;
                //header
                using var metaDataAccessor = file.CreateViewAccessor();
                metaDataAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                Box3i filteredDims;
                Buffer.MemoryCopy(ptr, &filteredDims, sizeof(Box3i), sizeof(Box3i));
                ptr += sizeof(Box3i);
                long totalVoxelCount;
                Buffer.MemoryCopy(ptr, &totalVoxelCount, sizeof(long), sizeof(long));
                ptr += sizeof(long);
                float min;
                Buffer.MemoryCopy(ptr, &min, sizeof(float), sizeof(float));
                ptr += sizeof(float);
                float max;
                Buffer.MemoryCopy(ptr, &max, sizeof(float), sizeof(float));
                ptr += sizeof(float);
                //source regions
                int sCount;
                Buffer.MemoryCopy(ptr, &sCount, sizeof(int), sizeof(int));
                ptr += sizeof(int);
                var sourceRegions = new SourceRegion[sCount];
                for (int i = 0; i < sourceRegions.Length; i++)
                {
                    fixed (SourceRegion* sourceBox = &sourceRegions[i])
                    {
                        Buffer.MemoryCopy(ptr, &sourceBox->SourceDimensions, sizeof(Box3i), sizeof(Box3i));
                        ptr += sizeof(Box3i);
                        Buffer.MemoryCopy(ptr, &sourceBox->BufferOffset, sizeof(long), sizeof(long));
                        ptr += sizeof(long);
                    }
                }
                metaDataAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                return (file, new FilteredVolumeInfo(info, filteredDims, totalVoxelCount, min, max, sourceRegions));
            }
            else
            {
                (long voxelsToExtract, var sourceRegions) = ExtractSourceRegionsFromXML(catalogueFilePath);
                long totalDataBytes = 0;
                for (int i = 0; i < sourceRegions.Length; i++)
                {
                    totalDataBytes += sourceRegions[i].SourceDimensions.LongVolume * sizeof(float);
                }

                //resultHeaderSize = totalSize + totalVoxels + min + max + totalSourceRegions + totalSourceRegions*(sourceregionSize + sourceRegionDataPtr)
                long resultHeaderSize = HeaderDataLength + sizeof(int) + (sizeof(Box3i) + sizeof(long)) * sourceRegions.Length;
                var resultFile = MemoryMappedFile.CreateFromFile(cacheFileName, FileMode.Create, null, resultHeaderSize + totalDataBytes);
                using var destDataAccessor = resultFile.CreateViewAccessor(resultHeaderSize, 0);
                byte* destPtr = null;
                destDataAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref destPtr);
                destPtr += resultHeaderSize;
                Vector3Int minPos = info.Dimensions.Max;
                Vector3Int maxPos = info.Dimensions.Min;
                long nextDataIdx = 0;
                using var srcDataAccessor = sourceFile.CreateViewAccessor(HeaderDataLength, 0, MemoryMappedFileAccess.Read);
                byte* srcPtr = null;
                srcDataAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref srcPtr);
                srcPtr += srcDataAccessor.PointerOffset;
                for (int i = 0; i < sourceRegions.Length; i++)
                {
                    SourceRegion sourceBox = sourceRegions[i];
                    sourceBox.BufferOffset = nextDataIdx;
                    minPos = Vector3Int.Min(minPos, sourceBox.SourceDimensions.Min);
                    maxPos = Vector3Int.Max(maxPos, sourceBox.SourceDimensions.Max);
                    sourceRegions[i] = sourceBox;
                    Copy3d(new UnmanagedMemoryRange(srcPtr, 0), new UnmanagedMemoryRange(destPtr, (ulong)totalDataBytes), sourceBox.SourceDimensions.Min, sourceBox.SourceDimensions.Max, nextDataIdx, info.Dimensions.Size.x, info.Dimensions.Size.y);
                    nextDataIdx += sourceBox.SourceDimensions.LongVolume;
                }
                srcDataAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                destDataAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                //Write header
                using var destHeaderAccessor = resultFile.CreateViewAccessor(0, resultHeaderSize);
                destHeaderAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref destPtr);
                Box3i filteredDims = new Box3i(minPos, maxPos);
                Buffer.MemoryCopy(&filteredDims, destPtr, sizeof(Box3i), sizeof(Box3i));
                destPtr += sizeof(Box3i);
                Buffer.MemoryCopy(&nextDataIdx, destPtr, sizeof(long), sizeof(long));
                destPtr += sizeof(long);
                Buffer.MemoryCopy(&info.MinValue, destPtr, sizeof(float), sizeof(float));
                destPtr += sizeof(float);
                Buffer.MemoryCopy(&info.MaxValue, destPtr, sizeof(float), sizeof(float));
                destPtr += sizeof(float);
                //source regions
                int sCount = sourceRegions.Length;
                Buffer.MemoryCopy(&sCount, destPtr, sizeof(int), sizeof(int));
                destPtr += sizeof(int);
                for (int i = 0; i < sourceRegions.Length; i++)
                {
                    SourceRegion sourceBox = sourceRegions[i];
                    Buffer.MemoryCopy(&sourceBox.SourceDimensions, destPtr, sizeof(Box3i), sizeof(Box3i));
                    destPtr += sizeof(Box3i);
                    Buffer.MemoryCopy(&sourceBox.BufferOffset, destPtr, sizeof(long), sizeof(long));
                    destPtr += sizeof(long);
                }
                destHeaderAccessor.SafeMemoryMappedViewHandle.ReleasePointer();

                sourceFile.Dispose();
                return (resultFile, new FilteredVolumeInfo(info, filteredDims, nextDataIdx, info.MinValue, info.MaxValue, sourceRegions));
            }
        }

        static unsafe void Copy3d(UnmanagedMemoryRange src, UnmanagedMemoryRange dest, Vector3Int minPos, Vector3Int maxPos, long idx, long xDims, long yDims)
        {
            long spanX = maxPos.x - minPos.x;
            long spanY = maxPos.y - minPos.y;
            long spanZ = maxPos.z - minPos.z;
            long offset = 0;
            for (int z = 0; z < spanZ; z++)
            {
                for (int y = 0; y < spanY; y++)
                {
                    checked
                    {
                        //Copy row of values 
                        float* srcAddress = (float*)src.First + minPos.x + (minPos.y + y) * xDims + (minPos.z + z) * xDims * yDims;
                        float* destAddress = (float*)dest.First + idx + offset;
                        long destAvailableBytes = (long)(dest.Length - (ulong)(idx + offset)) * sizeof(float);
                        long size = spanX * sizeof(float);
                        Buffer.MemoryCopy(srcAddress, destAddress, destAvailableBytes, size);
                        offset += spanX;
                    }
                }
            }
        }

        struct BoxCoordinateIndices
        {
            public static readonly Dictionary<string, int> idxMap = new(){
                {"x_min", -1},
                {"x_max", -1},
                {"y_min", -1},
                {"y_max", -1},
                {"z_min", -1},
                {"z_max", -1}
            };
        };

        struct SourceBox
        {
            public Dictionary<string, int> idxMap;

            public Box3i ToBox()
            {
                return new Box3i(
                    new Vector3Int(idxMap["x_min"], idxMap["y_min"], idxMap["z_min"]),
                    new Vector3Int(idxMap["x_max"] + 1, idxMap["y_max"] + 1, idxMap["z_max"] + 1));
            }

            public static SourceBox Create()
            {
                SourceBox box = new();
                box.idxMap = new() {
                    {"x_min", -1},
                    {"x_max", -1},
                    {"y_min", -1},
                    {"y_max", -1},
                    {"z_min", -1},
                    {"z_max", -1}
                };
                return box;
            }
        }
    }
}