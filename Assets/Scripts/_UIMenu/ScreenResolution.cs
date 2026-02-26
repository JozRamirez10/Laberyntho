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

    public void LoadResolution()
    {
        // 1. Verificamos si el jugador ya había guardado una resolución antes
        if (PlayerPrefs.HasKey("SavedResWidth") && PlayerPrefs.HasKey("SavedResHeight"))
        {
            int savedWidth = PlayerPrefs.GetInt("SavedResWidth");
            int savedHeight = PlayerPrefs.GetInt("SavedResHeight");

            // 2. Medida de seguridad: ¿El monitor actual soporta esa resolución guardada?
            // (Útil si el jugador cambió de monitor desde la última vez que jugó)
            if (IsResolutionSupported(savedWidth, savedHeight))
            {
                gameSettingsSO.resolutionWidth = savedWidth;
                gameSettingsSO.resolutionHeight = savedHeight;
            }
            else
            {
                // Si no es soportada, usamos la resolución nativa de su pantalla actual
                SetNativeResolution();
            }
        }
        else
        {
            // 3. ES LA PRIMERA VEZ QUE JUEGA: Usamos la resolución nativa de su monitor
            SetNativeResolution();
        }

        // 4. Aplicamos la resolución segura
        Screen.SetResolution(gameSettingsSO.resolutionWidth, gameSettingsSO.resolutionHeight, Screen.fullScreen);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution selectedResolution = resolutions[resolutionIndex];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);
        
        // Actualizamos el ScriptableObject para uso en tiempo de ejecución
        gameSettingsSO.resolutionWidth = selectedResolution.width;
        gameSettingsSO.resolutionHeight = selectedResolution.height;

        // Guardamos la decisión del jugador en la memoria de la PC
        PlayerPrefs.SetInt("SavedResWidth", selectedResolution.width);
        PlayerPrefs.SetInt("SavedResHeight", selectedResolution.height);
        PlayerPrefs.Save();
    }

    // --- MÉTODOS AUXILIARES DE SEGURIDAD ---

    private void SetNativeResolution()
    {
        // Screen.currentResolution obtiene la resolución real del monitor de Windows
        Resolution nativeRes = Screen.currentResolution;
        gameSettingsSO.resolutionWidth = nativeRes.width;
        gameSettingsSO.resolutionHeight = nativeRes.height;

        // La guardamos para el futuro
        PlayerPrefs.SetInt("SavedResWidth", nativeRes.width);
        PlayerPrefs.SetInt("SavedResHeight", nativeRes.height);
        PlayerPrefs.Save();
    }

    private bool IsResolutionSupported(int width, int height)
    {
        // Comprueba si la resolución que intentamos poner existe en la lista de resoluciones del monitor
        foreach (Resolution res in Screen.resolutions)
        {
            if (res.width == width && res.height == height)
                return true;
        }
        return false;
    }
}
