using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class UIPauseManager : UIBaseManager
{
    public static UIPauseManager Instance {get; private set;}

    public bool isPaused {get; private set;} = false;

    private float lastPauseToggleTime;
    private const float PAUSE_COOLDOWN = 0.25f;

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

    void Awake()
    {
        if(Instance == null) Instance = this;
        else { Destroy(gameObject); return;}

        if(principalPanel != null) principalPanel.SetActive(false);
        Time.timeScale = gameSettingsSO.speedGame;
    }

    protected override void Start()
    {
        base.Start();

        LoadAudioSliderSettings(); // Configura los sliders del volumen
    }

    protected override void Update()
    {
        // Si se presiona el botón [ESC] activa el menú de pausa
        if (InputManager.Instance != null && InputManager.Instance.IsPausePressed 
            && verifyScene() && verifyState())
        {
            if(Time.unscaledTime - lastPauseToggleTime >= PAUSE_COOLDOWN)
            {
                lastPauseToggleTime = Time.unscaledTime;
                TogglePause();
            }
        }

        if(isPaused) base.Update();
    }

    private bool verifyScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if(currentSceneName == Scenes.MENU) return false;
        return true;
    }

    private bool verifyState()
    {
        if(GameManager.Instance != null 
            && GameManager.Instance.currentState == GameState.GameOver) return false;
        return true;
    }

    protected override void OnSelectionChanged(GameObject selectedObject)
    {
        UpdateTextColors(selectedObject);
    }

    public void TogglePause()
    {
        if(isPaused) BackToGame(); // Si el juego esta pausado, vuelve al juego
        else Pause(); // Si no, pausa el juego
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if(EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = true;
        }

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

        ClearFocus();

        if(UIManager.Instance != null)
        {
            UIManager.Instance.RestoreLastFocus();
        }

        if(EventSystem.current != null && GameManager.Instance != null)
        {
            EventSystem.current.sendNavigationEvents = !GameManager.Instance.isTurnCPU;
        }

        if(AudioManager.Instance != null) AudioManager.Instance.playToBack();
    }

    // Quita toda la selección de colores de los botones
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

    // Actualiza la selección de botones de acuerdo al panel y selección
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
            if(selectedObj == confirmResetButton.gameObject) confirmResetText.color = activeSelectionColor;
            if(selectedObj == cancelResetButton.gameObject) cancelResetText.color = activeSelectionColor;
        }

        if (exitPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == confirmExitButton.gameObject) confirmExitText.color = activeSelectionColor;
            if(selectedObj == cancelExitButton.gameObject) cancelExitText.color = activeSelectionColor;
        }
    }

    // Volver al menú principal
    public void OnExitGameButtonPressed()
    {
        Time.timeScale = gameSettingsSO.speedGame;
        SceneManager.LoadScene(Scenes.MENU);
    }

    // Reinicia la escena
    public void OnResetGameButtonPressed()
    {
        Time.timeScale = gameSettingsSO.speedGame;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void DrawPrincipalPausePanel()
    {
        screenPanel.SetActive(false);
        audioPanel.SetActive(false);
        resetPanel.SetActive(false);
        exitPanel.SetActive(false);

        pausePanel.SetActive(true);
        ForceSelectButton(backtoGameButton);
    }

    public void DrawScreenPanel()
    {
        pausePanel.SetActive(false);
        screenPanel.SetActive(true);
        ForceSelectButton(resolutionDropDown);
    }

    public void DrawAudioPanel()
    {
        pausePanel.SetActive(false);
        audioPanel.SetActive(true);
        ForceSelectButton(musicSlider);
    }

    public void DrawResetPanel()
    {
        pausePanel.SetActive(false);
        resetPanel.SetActive(true);
        ForceSelectButton(cancelResetButton);
    }

    public void DrawExitPanel()
    {
        pausePanel.SetActive(false);
        exitPanel.SetActive(true);
        ForceSelectButton(cancelExitButton);
    }

    // Configura los sliders de volumen
    public void LoadAudioSliderSettings()
    {
        if(masterSlider != null) masterSlider.SetValueWithoutNotify(gameSettingsSO.masterVolume);
        if(musicSlider!= null) musicSlider.SetValueWithoutNotify(gameSettingsSO.musicVolume);
        if(sfxSlider != null) sfxSlider.SetValueWithoutNotify(gameSettingsSO.sfxVolume);
    }
}
