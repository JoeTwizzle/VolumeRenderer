using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelInteractionController : MonoBehaviour
{
    public void ToggleGameObjectState(GameObject go)
    {
        go.SetActive(!go.activeSelf);
    }
}
