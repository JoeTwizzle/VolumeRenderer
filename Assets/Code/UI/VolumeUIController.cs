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
using UnityEngine.UI.Extensions;
using MixedReality.Toolkit.UX;
using SimpleFileBrowser;

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
    public PressableButton XR_LoadVolumeButton;
    public PressableButton XR_LoadSourcesButton;
    public PressableButton XR_SelectVolumePathButton;
    public PressableButton XR_SelectSourcesPathButton;
    public PressableButton XR_SourcesOnlyToggle;


    public ScrollRect ScrollRect;

    public UnityEngine.UI.Slider IndexSlider;
    public MixedReality.Toolkit.UX.Slider XR_IndexSlider;

    public Toggle SourcesOnlyToggle;

    public int SelectedAxis;

    public MinMaxSlider Slider;

    SourceRegion[]? sourceRegions;
    ComputeBuffer sourceRegionsBuffer;
    ComputeBuffer visibleSourceRegionsBuffer;

    Vector2 valueRange = new Vector2(0, 1);

    GlobalVolumeInfo? globalVolumeInfo;
    MemoryMappedFile? volumeFile;

    List<int> visibleRegions;

    private void OnEnable()
    {
        visibleRegions = new List<int>();
        XR_SourcesOnlyToggle.OnClicked.AddListener(XR_SourcesOnlyToggleClicked);
        XR_LoadVolumeButton.OnClicked.AddListener(OnLoadVolumeClicked);
        XR_LoadSourcesButton.OnClicked.AddListener(OnLoadSourcesClicked);
        LoadVolumeInfoButton.onClick.AddListener(OnLoadVolumeClicked);
        LoadSourcesInfoButton.onClick.AddListener(OnLoadSourcesClicked);
        IndexSlider.onValueChanged.AddListener(OnLoadVolumeSliceChanged);
        SourcesOnlyToggle?.onValueChanged.AddListener(ToggleChanged);
        Slider.onValueChanged.AddListener(MinMaxSliderChanged);
        XR_IndexSlider.OnValueUpdated.AddListener(XR_SliderChanged);
        XR_SelectVolumePathButton.OnClicked.AddListener(XR_VolumePathClicked);
        XR_SelectSourcesPathButton.OnClicked.AddListener(XR_SourcePathClicked);
        SelectedAxis = 2;
        ToggleChanged(SourcesOnlyToggle?.isOn ?? false);
        SetShaderDefaults();
    }

    private void OnDisable()
    {
        LoadVolumeInfoButton.onClick.RemoveListener(OnLoadVolumeClicked);
        LoadSourcesInfoButton.onClick.RemoveListener(OnLoadSourcesClicked);
        XR_SourcesOnlyToggle.OnClicked.RemoveListener(XR_SourcesOnlyToggleClicked);
        XR_LoadVolumeButton.OnClicked.RemoveListener(OnLoadVolumeClicked);
        XR_LoadSourcesButton.OnClicked.RemoveListener(OnLoadSourcesClicked);
        IndexSlider.onValueChanged.RemoveListener(OnLoadVolumeSliceChanged);
        SourcesOnlyToggle?.onValueChanged.RemoveListener(ToggleChanged);
        Slider.onValueChanged.RemoveListener(MinMaxSliderChanged);
        XR_IndexSlider.OnValueUpdated.RemoveListener(XR_SliderChanged);
        XR_SelectVolumePathButton.OnClicked.RemoveListener(XR_VolumePathClicked);
        XR_SelectSourcesPathButton.OnClicked.RemoveListener(XR_SourcePathClicked);
        volumeFile?.Dispose();
    }

    private void OnApplicationQuit()
    {
        volumeFile?.Dispose();
        VolumeSliceImage?.material?.SetVector("_MinMaxVal", new Vector4(0, 1, 0, 0));
        ToggleChanged(false);
    }

    void XR_SourcesOnlyToggleClicked()
    {
        ToggleChanged(XR_SourcesOnlyToggle.IsToggled);
    }

    void XR_VolumePathClicked()
    {
        FileBrowser.ShowLoadDialog(VolumePathLoadSuccess, null, FileBrowser.PickMode.Files);
    }

    void VolumePathLoadSuccess(string[] path)
    {
        VolumePathField.text = path[0];
    }

    void XR_SourcePathClicked()
    {
        FileBrowser.ShowLoadDialog(SourcesPathLoadSuccess, null, FileBrowser.PickMode.Files);
    }

    void SourcesPathLoadSuccess(string[] path)
    {
        SourcesPathField.text = path[0];
    }

    void XR_SliderChanged(SliderEventData eventData)
    {
        OnLoadVolumeSliceChanged(eventData.NewValue);
    }

    void MinMaxSliderChanged(float min, float max)
    {
        valueRange = new Vector2(min / 100f, max / 100f);

        var info = globalVolumeInfo.Value;
        VolumeSliceImage.material.SetVector("_MinMaxVal", new Vector4(info.MinValue, info.MaxValue, valueRange.x, valueRange.y));
    }

    void ToggleChanged(bool value)
    {
        VolumeSliceImage.material.SetFloat("_FilterSourceRegions", value ? 1f : 0f);
        OnLoadVolumeSliceChanged(XR_IndexSlider.Value);
    }

    void SetShaderDefaults()
    {
        sourceRegionsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.Structured);
        sourceRegionsBuffer.SetData(new Vector4Int[] { new Vector4Int(0, 0, 0, 0) });
        visibleSourceRegionsBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
        visibleSourceRegionsBuffer.SetData(new int[] { 0 });

        VolumeSliceImage.material.SetBuffer("sourceRegionsBuffer", sourceRegionsBuffer);
        VolumeSliceImage.material.SetBuffer("visibleSourceRegionsBuffer", visibleSourceRegionsBuffer);
    }

    void OnLoadVolumeClicked()
    {
        string path = VolumePathField.text;
        if (File.Exists(path))
        {
            try
            {
                volumeFile?.Dispose();

                (var file, var info) = VolumeFileParser.LoadSourceVolume(path);
                volumeFile = file;
                if (info.MinValue > info.MaxValue)
                {
                    info = new GlobalVolumeInfo(info.Dimensions, info.MaxValue, info.MinValue);
                }

                globalVolumeInfo = info;
                IndexSlider.maxValue = info.Dimensions.Max.z - 1;
                IndexSlider.minValue = info.Dimensions.Min.z;
                IndexSlider.SetValueWithoutNotify(info.MinValue);

                XR_IndexSlider.MaxValue = info.Dimensions.Max.z - 1;
                XR_IndexSlider.MinValue = info.Dimensions.Min.z;
                XR_IndexSlider.Value = info.MinValue;

                UpdateInfoText(info);
                OnLoadVolumeSliceChanged(0);
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

            var sourceData = VolumeFileParser.ExtractSourceRegionsFromXML(path);
            sourceRegions = sourceData.Item2;
            if (sourceRegionsBuffer.count != sourceData.Item2.Length)
            {
                sourceRegionsBuffer.Dispose();
                sourceRegionsBuffer = new ComputeBuffer(sourceRegions.Length, sizeof(int) * 4, ComputeBufferType.Structured);
                Vector4Int[] data = new Vector4Int[sourceRegions.Length];
                for (int i = 0; i < sourceRegions.Length; i++)
                {
                    var min = sourceRegions[i].SourceDimensions.Min;
                    var max = sourceRegions[i].SourceDimensions.Max;
                    data[i] = new Vector4Int(min.x, min.y, max.x, max.y);
                }
                sourceRegionsBuffer.SetData(data);
                VolumeSliceImage.material.SetBuffer("sourceRegionsBuffer", sourceRegionsBuffer);
            }

            ShowSourceDataUIInfo(sourceData.Item1, sourceData.Item2);
        }
    }

    void ShowSourceDataUIInfo(long volume, SourceRegion[] regions)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine($"Total volume: {volume}");
        stringBuilder.AppendLine($"Total source regions: {regions.Length}");
        for (int i = 0; i < regions.Length; i++)
        {
            stringBuilder.AppendLine($"Source Region {i + 1}:{Environment.NewLine}{{{regions[i].SourceDimensions}}}");
        }
        SourceRegionsText.text = stringBuilder.ToString();
    }


    unsafe void OnLoadVolumeSliceChanged(float v)
    {
        if (globalVolumeInfo.HasValue)
        {
            int x = 0;
            int y = 0;
            int z = (int)v;
            VolumeSliceIndexText.text = z.ToString();
            Vector3Int pos = new Vector3Int(x, y, z);

            var info = globalVolumeInfo.Value;

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

            var srcView = info.GetDataView(volumeFile);
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
            VolumeSliceImage.material.SetVector("_MinMaxVal", new Vector4(info.MinValue, info.MaxValue, valueRange.x, valueRange.y));
            VolumeSliceImage.sprite = Sprite.Create(image, new Rect(0.0f, 0.0f, image.width, image.height), Vector2.zero);

            if (sourceRegions != null)
            {
                visibleRegions.Clear();
                for (int i = 0; i < sourceRegions.Length; i++)
                {
                    if (sourceRegions[i].SourceDimensions.Min.z <= z && sourceRegions[i].SourceDimensions.Max.z > z)
                    {
                        visibleRegions.Add(i);
                    }
                }
                if (visibleSourceRegionsBuffer.count != visibleRegions.Count)
                {
                    visibleSourceRegionsBuffer.Dispose();
                    visibleSourceRegionsBuffer = new ComputeBuffer(visibleRegions.Count > 0 ? visibleRegions.Count : 1, sizeof(int), ComputeBufferType.Structured);
                }
                visibleSourceRegionsBuffer.SetData(visibleRegions);
                VolumeSliceImage.material.SetInteger("_VisibleRegionsCount", visibleRegions.Count);
                VolumeSliceImage.material.SetBuffer("visibleSourceRegionsBuffer", visibleSourceRegionsBuffer);
            }
        }
    }
}
