using Assets.Code;
using Assets.Code.VolumeData;
using System;
using Assets.Code.Helpers;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using System.Text;

public class VolumeUIController : MonoBehaviour
{
    public Image VolumeSliceImage;

    public TMP_Text VolumeInfoText;
    public TMP_Text VolumeSliceIndexText;
    public TMP_Text SourceRegionsText;
    public TMP_InputField VolumePathField;
    public TMP_InputField SourcesPathField;

    public Button LoadVolumeInfoButton;
    public Button LoadSourcesInfoButton;

    public ScrollRect ScrollRect;

    public Slider IndexSlider;

    public int SelectedAxis;

    GlobalVolumeInfo? GlobalVolumeInfo;
    MemoryMappedFile? VolumeFile;

    private void OnEnable()
    {
        LoadVolumeInfoButton.onClick.AddListener(OnLoadVolumeClicked);
        LoadSourcesInfoButton.onClick.AddListener(OnLoadSourcesClicked);
        IndexSlider.onValueChanged.AddListener(OnLoadVolumeSliceChanged);

        SelectedAxis = 2;
    }

    void OnLoadVolumeClicked()
    {
        string path = VolumePathField.text;
        if (File.Exists(path))
        {
            try
            {
                VolumeFile?.Dispose();
                (var file, var info) = VolumeFileParser.LoadSourceVolume(path);
                VolumeFile = file;
                if (info.MinValue > info.MaxValue)
                {
                    info = new GlobalVolumeInfo(info.Dimensions, info.MaxValue, info.MinValue);
                }

                GlobalVolumeInfo = info;
                IndexSlider.maxValue = info.Dimensions.Max.z - 1;
                IndexSlider.minValue = info.Dimensions.Min.z;
                IndexSlider.SetValueWithoutNotify(info.MinValue);

                UpdateInfoText(info);
            }
            catch (Exception e)
            {
                Debug.LogError("Path exist, but is not a valid Source Volume file." + e.ToString());
            }
        }
    }

    void UpdateInfoText(GlobalVolumeInfo info)
    {
        VolumeInfoText.text = $"Volume Info: \n\r" +
            $"Dimensions: \n\r" +
            $"Min:{info.Dimensions.Min} Max:{info.Dimensions.Max}\n\r" +
            $"Minimum Voxel Value: {info.MinValue}\n\r" +
            $"Maximum Voxel Value: {info.MaxValue}";
    }

    void OnLoadSourcesClicked()
    {
        string path = SourcesPathField.text;

        if (File.Exists(path))
        {
            try
            {
                var sourceData = VolumeFileParser.ExtractSourceRegionsFromXML(path);

                ShowSourceDataUIInfo(sourceData.Item1, sourceData.Item2);

            }
            catch (Exception e)
            {
                Debug.LogError("Path exist, but is not a valid Source Regions file." + e.ToString());
            }
        }
    }

    void ShowSourceDataUIInfo(long volume, SourceRegion[] regions)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Total volume: {volume}");
        stringBuilder.AppendLine($"Total source regions: {regions.Length}");
        for (int i = 0; i < regions.Length; i++)
        {
            stringBuilder.AppendLine($"Source Region {i + 1}:{Environment.NewLine}{{{regions[i].SourceDimensions.ToString()}}}");
        }
        SourceRegionsText.text = stringBuilder.ToString();
    }

    private void OnDisable()
    {
        LoadVolumeInfoButton.onClick.RemoveListener(OnLoadVolumeClicked);
        LoadSourcesInfoButton.onClick.RemoveListener(OnLoadSourcesClicked);
        IndexSlider.onValueChanged.RemoveListener(OnLoadVolumeSliceChanged);
        VolumeFile?.Dispose();
    }

    private void OnApplicationQuit()
    {
        VolumeFile?.Dispose();
        VolumeSliceImage?.material?.SetVector("_MinMaxVal", new Vector4(0, 1, 0, 0));
    }

    unsafe void OnLoadVolumeSliceChanged(float v)
    {
        Debug.Log("Called");
        if (GlobalVolumeInfo.HasValue)
        {
            Debug.Log("Changed");
            int x = 0;
            int y = 0;
            int z = (int)v;
            VolumeSliceIndexText.text = z.ToString();
            Vector3Int pos = new Vector3Int(x, y, z);

            var info = GlobalVolumeInfo.Value;

            bool isValidPos = info.Dimensions.Contains(pos);
            if (!isValidPos)
            {
                Debug.LogError("Position outside of volume bounds");
                return;
            }

            int xIndex = SelectedAxis == 0 ? 1 : 0;
            int yIndex = SelectedAxis == 1 ? 2 : 1;
            int xSize = info.Dimensions.Size[xIndex];
            int ySize = info.Dimensions.Size[yIndex];

            if (xSize <= 0 || ySize <= 0)
            {
                Debug.LogError("Position has zero volume in this axis");
                return;
            }

            var image = new Texture2D(xSize, ySize, TextureFormat.RFloat, false);
            var imageData = image.GetRawTextureData<float>();

            Vector3Int min = info.Dimensions.Min;
            min[SelectedAxis] = pos[SelectedAxis];
            Vector3Int max = info.Dimensions.Max;
            max[SelectedAxis] = pos[SelectedAxis] + 1;

            var srcView = info.GetDataView(VolumeFile);
            byte* srcData = null;
            srcView.SafeMemoryMappedViewHandle.AcquirePointer(ref srcData);
            srcData += srcView.PointerOffset;

            var srcRange = new UnmanagedMemoryRange(srcData, ((ulong)info.Dimensions.LongVolume) * sizeof(float));
            //This is safe because the memory of image.GetRawTextureData() is natively allocated
            var destSpan = new UnmanagedMemoryRange(Unsafe.AsPointer(ref imageData.AsSpan()[0]), (uint)(xSize * ySize) * sizeof(float));

            VolumeFileParser.Copy3d(srcRange, destSpan, min, max, 0, info.Dimensions.Size.x, info.Dimensions.Size.y);

            srcView.SafeMemoryMappedViewHandle.ReleasePointer();
            image.LoadRawTextureData(imageData);
            image.Apply();
            VolumeSliceImage.material.SetVector("_MinMaxVal", new Vector4(info.MinValue, info.MaxValue, 0, 0));
            VolumeSliceImage.sprite = Sprite.Create(image, new Rect(0.0f, 0.0f, image.width, image.height), Vector2.zero);
            Debug.Log("Applied texture");
        }
    }
}
