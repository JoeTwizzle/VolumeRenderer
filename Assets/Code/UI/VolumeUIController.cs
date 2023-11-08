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

public class VolumeUIController : MonoBehaviour
{
    public Image VolumeSliceImage;

    public TMP_Text VolumeInfoText;
    public TMP_InputField VolumePathField;
    public TMP_InputField SourcesPathField;

    public TMP_InputField[] CoordinateFields;

    public Button LoadVolumeInfoButton;
    public Button LoadSourcesInfoButton;
    public Button LoadVolumeSliceButton;

    public ScrollRect ScrollRect;

    public Toggle[] Toggles;

    public int SelectedAxis;

    GlobalVolumeInfo? GlobalVolumeInfo;
    MemoryMappedFile? VolumeFile;

    private void OnEnable()
    {
        LoadVolumeSliceButton.interactable = false;

        LoadVolumeInfoButton.onClick.AddListener(OnLoadVolumeClicked);
        LoadSourcesInfoButton.onClick.AddListener(OnLoadSourcesClicked);
        LoadVolumeSliceButton.onClick.AddListener(OnLoadVolumeSliceClicked);

        Debug.Assert(Toggles.Length == 3);
        SelectedAxis = 2;
        Toggles[0].SetIsOnWithoutNotify(false);
        Toggles[1].SetIsOnWithoutNotify(false);
        Toggles[2].SetIsOnWithoutNotify(true);
        Toggles[0].onValueChanged.AddListener(Toggle0Changed);
        Toggles[1].onValueChanged.AddListener(Toggle1Changed);
        Toggles[2].onValueChanged.AddListener(Toggle2Changed);

        Debug.Assert(CoordinateFields.Length == 3);

    }

    void Toggle0Changed(bool value)
    {
        ToggleChanged(0);
    }

    void Toggle1Changed(bool value)
    {
        ToggleChanged(1);
    }

    void Toggle2Changed(bool value)
    {
        ToggleChanged(2);
    }

    void ToggleChanged(int switched)
    {
        Debug.Assert(switched < 3 && switched >= 0);
        Debug.Log($"Toggled {switched}");
        for (int i = 0; i < 3; i++)
        {
            Toggles[i].SetIsOnWithoutNotify(false);
        }
        Toggles[switched].SetIsOnWithoutNotify(true);
        SelectedAxis = switched;
    }

    void OnLoadVolumeClicked()
    {
        string path = VolumePathField.text;
        bool exists = File.Exists(path);

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
                UpdateInfoText(info);
            }
            catch (Exception e)
            {
                Debug.LogError("Path exist, but is not a valid Source Volume file." + e.ToString());
            }
        }

        LoadVolumeSliceButton.interactable = exists;
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
                VolumeFileParser.ExtractSourceRegionsFromXML(path);
            }
            catch (Exception e)
            {
                Debug.LogError("Path exist, but is not a valid Source Regions file." + e.ToString());
            }
        }
    }

    private void OnDisable()
    {
        LoadVolumeInfoButton.onClick.RemoveListener(OnLoadVolumeClicked);
        LoadSourcesInfoButton.onClick.RemoveListener(OnLoadSourcesClicked);
        LoadVolumeSliceButton.onClick.RemoveListener(OnLoadVolumeSliceClicked);

        Toggles[0].onValueChanged.RemoveListener(Toggle0Changed);
        Toggles[1].onValueChanged.RemoveListener(Toggle1Changed);
        Toggles[2].onValueChanged.RemoveListener(Toggle2Changed);

        VolumeFile?.Dispose();
    }

    private void OnApplicationQuit()
    {
        VolumeFile?.Dispose();
        VolumeSliceImage?.material?.SetVector("_MinMaxVal", new Vector4(0, 1, 0, 0));
    }

    unsafe void OnLoadVolumeSliceClicked()
    {
        if (GlobalVolumeInfo.HasValue)
        {
            int x = int.Parse(CoordinateFields[0].text);
            int y = int.Parse(CoordinateFields[1].text);
            int z = int.Parse(CoordinateFields[2].text);

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
            Debug.Log(imageData.Length);
        }
    }
}
