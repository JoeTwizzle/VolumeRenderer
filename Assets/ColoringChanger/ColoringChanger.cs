using MixedReality.Toolkit.UX;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ColoringChanger : MonoBehaviour
{
    public bool useColormap = false;

    public Color colorToUse = new(0, 1, 0);

    public Material coloringMaterial;

    public Image uiImage;

    public Texture colormapsTexture;
    public Texture transferFunctiontexture;

    public float colorMapsTeyxturePixelHeight = 410;
    public float colorMapPixelHeight = 5;
    public GameObject buttonPrefab;
    public Transform buttonsList;
    public Material buttonsMaterial;

    RenderTexture transferRenderTex;
    int colorMapCount;

    [Min(0)]
    public int selectedColorMap = 0;

    float colorMapUvHeight;

    public ComputeShader transferFunctionComputeShader;

    public ImageManipulationPositionProvider positionProvider;

    Vector2 oldPos = new Vector2(1.1f, 0);
    bool wasValidBefore = false;

    private void Start()
    {

        transferRenderTex = new(transferFunctiontexture.width, transferFunctiontexture.height, 1, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
        };
        transferRenderTex.Create();
        Graphics.Blit(transferFunctiontexture, transferRenderTex);
        if (uiImage)
        {
            uiImage.material = new(coloringMaterial);
            uiImage.material.mainTexture = colormapsTexture;
            uiImage.material.SetTexture("_TransferFunctionTexture", transferRenderTex);
        }

        colorMapUvHeight = (1.0f / colorMapsTeyxturePixelHeight) * colorMapPixelHeight;
        colorMapCount = (int)(colorMapsTeyxturePixelHeight / colorMapPixelHeight);

        for (int i = 0; i < colorMapCount; i++)
        {
            var buttonGO = Instantiate(buttonPrefab, buttonsList);
            var button = buttonGO.GetComponent<PressableButton>();
            var index = i;
            button.OnClicked.AddListener(() => { selectedColorMap = index; });
            var backplate = buttonGO.GetNamedChild("Backplate");
            var img = backplate.GetComponent<RawImage>();
            img.material = new Material(buttonsMaterial);
            img.color = Color.white;
            img.material.mainTexture = transferFunctiontexture;
            img.material.SetVector("_colorMapParams", new Vector4(colorMapPixelHeight, i, 1f / colorMapsTeyxturePixelHeight));
        }

        transferFunctionComputeShader.SetTexture(transferFunctionComputeShader.FindKernel("CSMain"), "Result", transferRenderTex, 0);
        transferFunctionComputeShader.SetVector("startPos", new Vector2(0, 0));
        transferFunctionComputeShader.SetVector("endPos", new Vector2(0, 0));
        transferFunctionComputeShader.SetInt("textureWidth", transferFunctiontexture.width);
        transferFunctionComputeShader.Dispatch(transferFunctionComputeShader.FindKernel("CSMain"), transferFunctiontexture.width / 8, Mathf.Max(1, transferFunctiontexture.height / 8), 1);

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 localPos;

        selectedColorMap = Mathf.Min(selectedColorMap, colorMapCount);
        if (uiImage)
        {
            uiImage.material.SetFloat("_UseColorMap", useColormap ? 1 : 0);
            uiImage.material.SetColor("_SingleColor", colorToUse);
            uiImage.material.SetFloat("_ColorMapsTextureSelectionOffset", colorMapUvHeight * selectedColorMap);
            uiImage.material.SetFloat("_ColorMapsTextureHeight", colorMapUvHeight);
        }

        if (positionProvider.GetPosition(out localPos))
        {

            if (!wasValidBefore)
            {
                oldPos = localPos;
                wasValidBefore = true;
            }
            Debug.Log($"old: {oldPos}, new: {localPos}");
            transferFunctionComputeShader.SetVector("startPos", oldPos);
            transferFunctionComputeShader.SetVector("endPos", localPos);

            oldPos = localPos;

            transferFunctionComputeShader.Dispatch(transferFunctionComputeShader.FindKernel("CSMain"), transferFunctiontexture.width / 8, Mathf.Max(1, transferFunctiontexture.height / 8), 1);
        }
        else
        {
            wasValidBefore = false;
            oldPos = new(1.1f, 0);
            transferFunctionComputeShader.SetVector("startPos", oldPos);
            transferFunctionComputeShader.SetVector("endPos", oldPos);
        }

    }
}
