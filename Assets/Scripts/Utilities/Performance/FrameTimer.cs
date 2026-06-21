using System;
using UnityEngine;

public class FrameTimer : MonoBehaviour
{
    public GameObject holder;
    public TMPro.TextMeshProUGUI fpsDisplay;
    public TMPro.TextMeshProUGUI fpsTarget;
    public TMPro.TextMeshProUGUI vsyncCount;
    public TMPro.TextMeshProUGUI screenmode;
    public TMPro.TextMeshProUGUI xFrameText;

    public bool uncapped = false;
    public int xFrame = 0;
    public int maxStep = 200;
    
    
    private void Awake()
    {
        // for (int i = 0; i < Screen.resolutions.Length; i++)
        // {
            // Debug.Log("Resolution: " + Screen.resolutions[i].width + "x" + Screen.resolutions[i].height + ", "  + Screen.resolutions[i].refreshRate);
        // }
        
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow, Screen.currentResolution.refreshRateRatio);
        
        vsyncCount.SetText($"VSync: {QualitySettings.vSyncCount}");
        fpsTarget.SetText($"FPS Target: {Application.targetFrameRate}");
        screenmode.SetText($"Screen Mode: {Screen.fullScreenMode}");
        xFrameText.SetText($"Frame Capture Interval: {xFrame}/{maxStep}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tilde))
        {
            holder.SetActive(!holder.activeSelf);    
        }
        
        if (!holder.activeSelf)
            return;
        
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            screenmode.SetText($"Screen Mode: {Screen.fullScreenMode}");
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            screenmode.SetText($"Screen Mode: {Screen.fullScreenMode}");

        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
            screenmode.SetText($"Screen Mode: {Screen.fullScreenMode}");

        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            screenmode.SetText($"Screen Mode: {Screen.fullScreenMode}");
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            uncapped = !uncapped;
            QualitySettings.vSyncCount = uncapped ? 0 : 1;
            vsyncCount.SetText($"VSync: {QualitySettings.vSyncCount}");
            fpsTarget.SetText($"FPS Target: {Application.targetFrameRate}");
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            xFrame = Mathf.Clamp(xFrame - 1, 0, maxStep);
            xFrameText.SetText($"Frame Capture Interval: {xFrame}/{maxStep}");
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            xFrame = Mathf.Clamp(xFrame + 1, 0, maxStep);
            xFrameText.SetText($"Frame Capture Interval: {xFrame}/{maxStep}");
        }
        
        if (xFrame == 0 || Time.frameCount % xFrame == 0)
        {
            fpsDisplay.SetText("{0:2}ms | {1:1}fps", Time.deltaTime * 1000f, 1F / Time.deltaTime);
        }
    }
}