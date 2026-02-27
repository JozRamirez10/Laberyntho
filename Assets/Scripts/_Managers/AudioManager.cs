using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Configuración de audios
public class AudioManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public MusicCollection musicCollection;
    public AudioMixer audioMixer;
    private string currentSceneName;

    public GameSettingsSO gameSettingsSO;

    public static AudioManager Instance;

    void Awake() 
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        PlayMusic(currentSceneName);
    }

    // Carga los volumenes guardados en el Scriptable Object
    public void LoadAudioSettings()
    {
        MasterVolume(gameSettingsSO.masterVolume);
        MusicVolume(gameSettingsSO.musicVolume);
        SFXVolume(gameSettingsSO.sfxVolume);
    }

    public void SFXVolume(float volume)
    {
        audioMixer.SetFloat(Settings.SFX_VOLUME, volume);
        gameSettingsSO.sfxVolume = volume;
    }

    public void MusicVolume(float volume)
    {
        audioMixer.SetFloat(Settings.MUSIC_VOLUME, volume);
        gameSettingsSO.musicVolume = volume;
    }

    public void MasterVolume(float volume)
    {
        audioMixer.SetFloat(Settings.MASTER_VOLUME, volume);
        gameSettingsSO.masterVolume = volume;
    }

    private void PlayMusic(string name)
    {
        AudioClip clipToPlay = null;

        switch (name)
        {
            case Scenes.MENU:
                clipToPlay = musicCollection.musicMenu;
                break;
            case Scenes.CLASSIC:
                clipToPlay = musicCollection.musicMenu;
                break;
            case Scenes.RANDOM:
                clipToPlay = musicCollection.musicMenu;
                break;
            default:
                clipToPlay = musicCollection.musicMenu;
                break;
            
        }
        
        if(clipToPlay != null)
        {
            musicSource.Stop();
            musicSource.clip = clipToPlay;
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void playToConfirm()
    {
        sfxSource.PlayOneShot(musicCollection.sfxConfirm);
    }

    public void playToBack()
    {
        sfxSource.PlayOneShot(musicCollection.sfxBack);
    }


    public void playToSelect()
    {
        sfxSource.PlayOneShot(musicCollection.sfxSelect);
    }

    public void playToError()
    {
        sfxSource.PlayOneShot(musicCollection.sfxError);
    }

    public void playStartTurn()
    {
        sfxSource.PlayOneShot(musicCollection.startTurn);
    }
    
    public void playWinKey()
    {
        sfxSource.PlayOneShot(musicCollection.winKey);
    }

    public void playSpendKey()
    {
        sfxSource.PlayOneShot(musicCollection.spendKey);
    }

    public void playOpenDoor()
    {
        sfxSource.PlayOneShot(musicCollection.openDoor);
    }

    public void playCreakingDoor()
    {
        sfxSource.PlayOneShot(musicCollection.creakingDoor);
    }

    public void playCloseDoor()
    {
        sfxSource.PlayOneShot(musicCollection.closeDoor);
    }

    public void playConfirmAttack()
    {
        sfxSource.PlayOneShot(musicCollection.confirmAttack);
    }

    public void playDiePlayer()
    {
        sfxSource.PlayOneShot(musicCollection.diePlayer);
    }

    public void playRebirthPlayer()
    {
        sfxSource.PlayOneShot(musicCollection.rebirthPlayer);
    }

    public void playRoarMinotaur()
    {
        sfxSource.PlayOneShot(musicCollection.roarMinotaur);
    }

    public void playSetWall()
    {
        sfxSource.PlayOneShot(musicCollection.setWall);
    }

    public void playRotateWall()
    {
        sfxSource.PlayOneShot(musicCollection.rotateWall);
    }
    
}
