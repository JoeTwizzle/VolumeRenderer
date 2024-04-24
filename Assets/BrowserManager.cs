using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser;

public class BrowserManager : MonoBehaviour
{
    public WebBrowserUIBasic Browser;

    // Start is called before the first frame update
    void Start()
    {
        
        Browser = GetComponent<WebBrowserUIBasic>();
    }

    // Update is called once per frame
    void Update()
    {
        Browser.browserClient.UpdateFps();
        Debug.Log(Browser.browserClient.FPS.ToString());
    }
}
