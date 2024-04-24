using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-1000)]
public class GlobalInitializationLogic : MonoBehaviour
{
    private void Awake()
    {
        XRSettings.enabled = true;
    }
}
