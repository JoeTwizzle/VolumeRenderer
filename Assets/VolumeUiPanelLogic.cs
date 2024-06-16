using Assets.Code;
using MixedReality.Toolkit.UX;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class VolumeUiPanelLogic : MonoBehaviour
{
    public PressableButton LoadFileButton;
    public GameObject ListOrigin;
    public TMP_Text SelectedVolumeText;
    public TMP_Text DimensionsText;
    public TMP_Text DateText;
    public TMP_Text BeamText;
    public TMP_Text SpectralText;
    public TMP_Text SpatialText;
    public TMP_Text FluxText;

    private void Awake()
    {
        LoadFileButton.OnClicked.AddListener(LoadClicked);
    }

    void LoadClicked()
    {
        FileBrowser.ShowLoadDialog(FileChosen, OnCancel, FileBrowser.PickMode.Files);
    }

    void OnCancel()
    {

    }

    void FileChosen(string[] files)
    {
        try
        {
            var header = VolumeFileParser.ReadHeader(files[0]);
            SelectedVolumeText.text = (string)header["OBJECT"];
            SelectedVolumeText.gameObject.SetActive(false);
            SelectedVolumeText.gameObject.SetActive(true);
            int dimCount = header.NumberOfAxisInMainContent;
            string text = "Axes: " + dimCount + " | " + header.AxisSizes[0];
            for (int i = 1; i < dimCount; i++)
            {
                text += ", " + header.AxisSizes[i];
            }
            DimensionsText.text = text;
            DimensionsText.gameObject.SetActive(false);
            DimensionsText.gameObject.SetActive(true);

            DateText.text = (string)header["DATE-OBS"];
            DateText.gameObject.SetActive(false);
            DateText.gameObject.SetActive(true);

            BeamText.text = "Major Axis: " + (double)header["BMAJ"] + Environment.NewLine +
                "Minor Axis: " + (double)header["BMIN"] + Environment.NewLine +
                "Phase Angle: " + (double)header["BPA"];
            BeamText.gameObject.SetActive(false);
            BeamText.gameObject.SetActive(true);

            string channelWidthKey = "CDELT" + (3);
            string pixelSizeKey1 = "CDELT" + (1);
            string pixelSizeKey2 = "CDELT" + (2);

            SpectralText.text = "" + (double)header[channelWidthKey];
            SpectralText.gameObject.SetActive(false);
            SpectralText.gameObject.SetActive(true);

            SpatialText.text = "X:" + (double)header[pixelSizeKey1] + Environment.NewLine +
                "Y: " + (double)header[pixelSizeKey2] + Environment.NewLine +
                "Beam size in pixels: " + Environment.NewLine + Math.Abs((double)header[pixelSizeKey1] / (double)header["BMAJ"]);
            SpatialText.gameObject.SetActive(false);
            SpatialText.gameObject.SetActive(true);

            FluxText.text = (string)header["BUNIT"];
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing fits file! Error: " + e.ToString());
        }
    }
}
