using UnityEngine;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance;
    
    public GameSettingsSO gameSettingsSO;


    void Awake()
    {
        if(Instance == null)
        {
          Instance = this;
          DontDestroyOnLoad(gameObject);
        } 
        else Destroy(gameObject); 
    }

    void Start()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        LoadResolution();
        LoadVolumes();
    }

    private void LoadResolution()
    {
        gameSettingsSO.resolutionWidth = PlayerPrefs.GetInt(Settings.RES_WIDTH);
        gameSettingsSO.resolutionHeight = PlayerPrefs.GetInt(Settings.RES_HEIGHT);

        if(ScreenResolution.Instance != null) ScreenResolution.Instance.LoadResolution();
    }

    private void LoadVolumes()
    {
        gameSettingsSO.masterVolume = PlayerPrefs.GetFloat(Settings.MASTER_VOLUME);
        gameSettingsSO.musicVolume = PlayerPrefs.GetFloat(Settings.MUSIC_VOLUME);
        gameSettingsSO.sfxVolume = PlayerPrefs.GetFloat(Settings.SFX_VOLUME);

        if(AudioManager.Instance != null) AudioManager.Instance.LoadAudioSettings();
        if(UIMenuManager.Instance != null) UIMenuManager.Instance.LoadAudioSliderSettings();
    }

    public void SaveResolution()
    {
        PlayerPrefs.SetInt(Settings.RES_WIDTH, gameSettingsSO.resolutionWidth);
        PlayerPrefs.SetInt(Settings.RES_HEIGHT, gameSettingsSO.resolutionHeight);
    }

    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat(Settings.MASTER_VOLUME, gameSettingsSO.masterVolume);
        PlayerPrefs.SetFloat(Settings.MUSIC_VOLUME, gameSettingsSO.musicVolume);
        PlayerPrefs.SetFloat(Settings.SFX_VOLUME, gameSettingsSO.sfxVolume);
    }
}
