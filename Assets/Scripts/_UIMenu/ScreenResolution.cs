using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ScreenResolution : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    private List<Resolution> resolutions;

    public GameSettingsSO gameSettingsSO;

    public static ScreenResolution Instance;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SetupResolutionDropdown();
    }

    private void SetupResolutionDropdown()
    {
        Resolution[] allResolutions = Screen.resolutions;
        resolutions = new List<Resolution>();

        HashSet<string> uniqueResolution = new HashSet<string>();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        bool foundSavedResolution = false;

        for(int i = 0; i < allResolutions.Length ; i++)
        {
            string resolutionString = allResolutions[i].width + "x" + allResolutions[i].height;
            if (!uniqueResolution.Contains(resolutionString))
            {
                uniqueResolution.Add(resolutionString);
                options.Add(resolutionString);
                resolutions.Add(allResolutions[i]);

                if(gameSettingsSO.resolutionWidth != 0 && 
                    allResolutions[i].width == gameSettingsSO.resolutionWidth &&
                    allResolutions[i].height == gameSettingsSO.resolutionHeight)
                {
                    currentResolutionIndex = resolutions.Count - 1;
                    foundSavedResolution = true;
                }

                if(!foundSavedResolution &&
                    allResolutions[i].width == Screen.currentResolution.width &&
                    allResolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = resolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution selectedResolution = resolutions[resolutionIndex];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
        
        gameSettingsSO.resolutionWidth = selectedResolution.width;
        gameSettingsSO.resolutionHeight = selectedResolution.height;
    }

    public void LoadResolution()
    {
        Screen.SetResolution(gameSettingsSO.resolutionWidth, gameSettingsSO.resolutionHeight, Screen.fullScreen);
    }
}
