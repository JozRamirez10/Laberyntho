using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class UIPauseManager : MonoBehaviour
{
    public static UIPauseManager Instace {get; private set;}

    public bool isPaused {get; private set;} = false;

    [Header("Panels")]
    public GameObject principalPanel;
    public GameObject pausePanel;
    public GameObject screenPanel;
    public GameObject audioPanel;
    public GameObject resetPanel;
    public GameObject exitPanel;

    [Header("Selection Colors")]
    public Color activeSelectionColor;
    public Color disableSelectionColor;

    [Header("Pause Buttons")]
    public Button backtoGameButton;
    public Button screenButton;
    public Button audioButton;
    public Button resetButton;
    public Button exitButton;

    [Header("Pause Principal Texts")]
    public TextMeshProUGUI backToGameText;
    public TextMeshProUGUI screenText;
    public TextMeshProUGUI audioText;
    public TextMeshProUGUI resetText;
    public TextMeshProUGUI exitText;

    [Header("Screen panel")]
    public TMP_Dropdown resolutionDropDown;
    public Button returnScreenButton;
    public TextMeshProUGUI returnScreenText;

    [Header("Audio panel")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider masterSlider;
    public Button returnAudioButton;
    public TextMeshProUGUI returnAudioText;

    [Header("Reset Panel")]
    public Button confirmResetButton;
    public Button cancelResetButton;
    public TextMeshProUGUI confirmResetText;
    public TextMeshProUGUI cancelResetText;

    [Header("Exit Panel")]
    public Button confirmExitButton;
    public Button cancelExitButton;
    public TextMeshProUGUI confirmExitText;
    public TextMeshProUGUI cancelExitText;

    [Header("GameSettingSO")]
    public GameSettingsSO gameSettingsSO;

    private GameObject lastSelectedObject;

    void Awake()
    {
        if(Instace == null) Instace = this;
        else { Destroy(gameObject); return;}

        if(principalPanel != null) principalPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Start()
    {
        LoadAudioSliderSettings();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();

        if(isPaused && EventSystem.current.currentSelectedGameObject != lastSelectedObject)
        {
            if(EventSystem.current.currentSelectedGameObject != null)
            {
                if(AudioManager.Instance != null) AudioManager.Instance.playToSelect();
                
                lastSelectedObject = EventSystem.current.currentSelectedGameObject;

                UpdateTextColors(lastSelectedObject); 
            }
        }
    }

    public void TogglePause()
    {
        if(isPaused) BackToGame();
        else Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if(principalPanel != null) principalPanel.SetActive(true);
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();

        DrawPrincipalPausePanel();
    }

    public void BackToGame()
    {
        isPaused = false;

        if(principalPanel != null) principalPanel.SetActive(false);
        if(screenPanel != null) screenPanel.SetActive(false);
        if(audioPanel != null) audioPanel.SetActive(false);

        Time.timeScale = gameSettingsSO.speedGame;

        EventSystem.current.SetSelectedGameObject(null);

        if(AudioManager.Instance != null) AudioManager.Instance.playToBack();
    }

    private void ForceSelectionButton(Selectable btnToSelect)
    {
        if(EventSystem.current != null && btnToSelect != null && btnToSelect.gameObject.activeInHierarchy && btnToSelect.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btnToSelect.gameObject);
            UpdateTextColors(btnToSelect.gameObject);
            lastSelectedObject = btnToSelect.gameObject;
        }
    }

    private void ResetAllTextColors()
    {
        if (pausePanel.gameObject.activeInHierarchy)
        {
            if(backToGameText) backToGameText.color = disableSelectionColor;
            if(screenText) screenText.color = disableSelectionColor;
            if(audioText) audioText.color = disableSelectionColor;
            if(resetText) resetText.color = disableSelectionColor;
            if(exitText) exitText.color = disableSelectionColor;
        }

        if (screenPanel.gameObject.activeInHierarchy)
        {
            if(returnScreenText) returnScreenText.color = disableSelectionColor;
        }

        if (audioPanel.gameObject.activeInHierarchy)
        {
            if(returnAudioText) returnAudioText.color = disableSelectionColor;
        }

        if (resetPanel.gameObject.activeInHierarchy)
        {
            if(confirmResetText) confirmResetText.color = disableSelectionColor;
            if(cancelResetText) cancelResetText.color = disableSelectionColor;
        }

        if (exitPanel.gameObject.activeInHierarchy)
        {
            if(confirmExitText) confirmExitText.color = disableSelectionColor;
            if(cancelExitText) cancelExitText.color = disableSelectionColor;
        }
    }

    private void UpdateTextColors(GameObject selectedObj)
    {
        ResetAllTextColors();

        if (pausePanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == backtoGameButton.gameObject) backToGameText.color = activeSelectionColor;
            if(selectedObj == screenButton.gameObject) screenText.color = activeSelectionColor;
            if(selectedObj == audioButton.gameObject) audioText.color = activeSelectionColor;
            if(selectedObj == resetButton.gameObject) resetText.color = activeSelectionColor;
            if(selectedObj == exitButton.gameObject) exitText.color = activeSelectionColor;
        }

        if (audioPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == returnScreenButton.gameObject) returnScreenText.color = activeSelectionColor;
        }

        if (audioPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == returnAudioButton.gameObject) returnAudioText.color = activeSelectionColor;
        }

        if (resetPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == confirmResetButton.gameObject) confirmResetText.color = disableSelectionColor;
            if(selectedObj == cancelResetButton) cancelResetText.color = disableSelectionColor;
        }

        if (exitPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == confirmExitButton) confirmExitText.color = disableSelectionColor;
            if(selectedObj == cancelExitButton) cancelExitText.color = disableSelectionColor;
        }
    }

    public void OnExitGameButtonPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(Scenes.MENU);
    }

    public void OnResetGameButtonPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void DrawPrincipalPausePanel()
    {
        screenPanel.SetActive(false);
        audioPanel.SetActive(false);
        resetPanel.SetActive(false);
        exitPanel.SetActive(false);

        pausePanel.SetActive(true);
        ForceSelectionButton(backtoGameButton);
    }

    public void DrawScreenPanel()
    {
        pausePanel.SetActive(false);
        screenPanel.SetActive(true);
        ForceSelectionButton(resolutionDropDown);
    }

    public void DrawAudioPanel()
    {
        pausePanel.SetActive(false);
        audioPanel.SetActive(true);
        ForceSelectionButton(musicSlider);
    }

    public void DrawResetPanel()
    {
        pausePanel.SetActive(false);
        resetPanel.SetActive(true);
        ForceSelectionButton(cancelResetButton);
    }

    public void DrawExitPanel()
    {
        pausePanel.SetActive(false);
        exitPanel.SetActive(true);
        ForceSelectionButton(cancelExitButton);
    }

    public void LoadAudioSliderSettings()
    {
        if(masterSlider != null) masterSlider.SetValueWithoutNotify(gameSettingsSO.masterVolume);
        if(musicSlider!= null) musicSlider.SetValueWithoutNotify(gameSettingsSO.musicVolume);
        if(sfxSlider != null) sfxSlider.SetValueWithoutNotify(gameSettingsSO.sfxVolume);
    }
}
