using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class UIMenuManager : MonoBehaviour
{
    public static UIMenuManager Instance {get; private set;}

    [Header("Panels")]
    public GameObject principalPanel;
    public GameObject gamemodePanel;
    public GameObject playersPanel;
    public GameObject turnPlayersPanel;
    public GameObject optionsPanel;
    public GameObject screenPanel;
    public GameObject audioPanel;
    public GameObject creditsPanel;

    [Header("Selection colors")]
    public Color activeSelectionColor;
    public Color disableSelectionColor;

    [Header("Menu Principal Buttons")]
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button exitButton;

    [Header("Menu Principal Texts")]
    public TextMeshProUGUI playText;
    public TextMeshProUGUI optionsText;
    public TextMeshProUGUI creditsText;
    public TextMeshProUGUI exitText;

    [Header("Players panel")]
    public Button startGameButton;
    public Button returnPlayersButton;
    public TextMeshProUGUI startGameText;
    public TextMeshProUGUI returnPlayersText;
    public TMP_InputField player1InputText;

    [Header("GameMode Panel")]
    public Button classicGameButton;
    public TextMeshProUGUI classicGameText;
    public Button randomGameButton;
    public TextMeshProUGUI randomGameText;
    public Button returnGameModeButton;
    public TextMeshProUGUI returnGameModeText;

    [Header("Turn players panel")]
    public Toggle randomTurnToggle;
    public TMP_Dropdown firstPlayerDropdown;
    public TMP_Dropdown fourthPlayerDropdown;

    [Header("Options panel")]
    public Button screenButton;
    public Button audioButton;
    public Button returnOptionsButton;
    public TextMeshProUGUI screenText;
    public TextMeshProUGUI audioText;
    public TextMeshProUGUI returnOptionsText;

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

    [Header("Credits")]
    public Button returnCreditsButton;
    public TextMeshProUGUI returnCreditsText;

    private GameObject lastSelectObject;

    [Header("GameSettingsSO")]
    public GameSettingsSO gameSettingsSO;

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Forza el botón principal del menú
        if(playButton != null) ForceSelectionButton(playButton);
    }

    void Update()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if(currentSelected == null || currentSelected == lastSelectObject) return;
        
        if(!Input.GetButtonDown("Submit") && AudioManager.Instance != null && lastSelectObject != null)
        {
            AudioManager.Instance.playToSelect();    
        }

        lastSelectObject = currentSelected;
        UpdateTextColors(currentSelected);
    }

    // Quita toda la selección de colores de los botones
    private void ResetAllTextColors()
    {
        if (principalPanel.gameObject.activeInHierarchy)
        {
            if(playText) playText.color = disableSelectionColor;
            if(optionsText) optionsText.color = disableSelectionColor;
            if(creditsText) creditsText.color = disableSelectionColor;
            if(exitText) exitText.color = disableSelectionColor;
        }

        if (playersPanel.gameObject.activeInHierarchy)
        {
            if(startGameText) startGameText.color = disableSelectionColor;
            if(returnPlayersText) returnPlayersText.color = disableSelectionColor;
        }

        if (gamemodePanel.gameObject.activeInHierarchy)
        {
            if(classicGameText) classicGameText.color = disableSelectionColor;
            if(randomGameText) randomGameText.color = disableSelectionColor;
            if(returnGameModeText) returnGameModeText.color = disableSelectionColor;
        }

        if (optionsPanel.gameObject.activeInHierarchy)
        {
            if(screenText) screenText.color = disableSelectionColor;
            if(audioText) audioText.color = disableSelectionColor;
            if(returnOptionsText) returnOptionsText.color = disableSelectionColor;
        }

        if (screenPanel.gameObject.activeInHierarchy)
        {
            if(returnScreenText) returnScreenText.color = disableSelectionColor;
        }

        if (audioPanel.gameObject.activeInHierarchy)
        {
            if(returnAudioText) returnAudioText.color = disableSelectionColor;
        }

        if (creditsPanel.gameObject.activeInHierarchy)
        {
            if(returnCreditsText) returnCreditsText.color = disableSelectionColor;
        }
    }

    // Actualiza la selección de botones de acuerdo al panel y selección
    private void UpdateTextColors(GameObject selectedObj)
    {
        ResetAllTextColors();

        if (principalPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == playButton.gameObject) playText.color = activeSelectionColor;
            if(selectedObj == optionsButton.gameObject) optionsText.color = activeSelectionColor;
            if(selectedObj == creditsButton.gameObject) creditsText.color = activeSelectionColor;
            if(selectedObj == exitButton.gameObject) exitText.color = activeSelectionColor;
        }

        if (playersPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == startGameButton.gameObject) startGameText.color = activeSelectionColor;
            if(selectedObj == returnPlayersButton.gameObject) returnPlayersText.color = activeSelectionColor;
        }

        if (gamemodePanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == classicGameButton) classicGameText.color = activeSelectionColor;
            if(selectedObj == randomGameButton) randomGameText.color = activeSelectionColor;
            if(selectedObj == returnGameModeButton) returnGameModeText.color = activeSelectionColor;
        }

        if (optionsPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == screenButton.gameObject) screenText.color = activeSelectionColor;
            if(selectedObj == audioButton.gameObject) audioText.color = activeSelectionColor;
            if(selectedObj == returnOptionsButton.gameObject) returnOptionsText.color = activeSelectionColor;
        }

        if (audioPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == returnScreenButton.gameObject) returnScreenText.color = activeSelectionColor;
        }

        if (audioPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == returnAudioButton.gameObject) returnAudioText.color = activeSelectionColor;
        }

        if (creditsPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == returnCreditsButton.gameObject) returnCreditsText.color = activeSelectionColor;
        }
    }

    // Forza la selección de botones al pasar de un panel a otro
    private void ForceSelectionButton(Selectable btnToSelect)
    {
        if(EventSystem.current != null && btnToSelect != null && btnToSelect.gameObject.activeInHierarchy && btnToSelect.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btnToSelect.gameObject);
            UpdateTextColors(btnToSelect.gameObject);
        }
    }

    public void drawMenuPrincipalPanel()
    {
        gamemodePanel.SetActive(false);
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        principalPanel.SetActive(true);
        ForceSelectionButton(playButton);
    }

    public void drawGameModePanel()
    {
        if(gamemodePanel == null) return;

        principalPanel.SetActive(false);
        playersPanel.SetActive(false);
        gamemodePanel.SetActive(true);
        ForceSelectionButton(classicGameButton);
    }

    public void drawPlayersPanel()
    {
        if(playersPanel == null) return;

        gamemodePanel.SetActive(false);
        playersPanel.SetActive(true);
        ForceSelectionButton(player1InputText);
    }

    // Comportamiento de checkbox de la selección de turnos random
    // Modifica el panel de navagación
    public void turnPlayersToggle(bool toggle)
    {
        if(turnPlayersPanel != null) turnPlayersPanel.SetActive(!toggle);

        Navigation navToggle = randomTurnToggle.navigation;
        navToggle.mode = Navigation.Mode.Explicit;

        Navigation navStartButton = startGameButton.navigation;
        navStartButton.mode = Navigation.Mode.Explicit;

        Navigation navReturnButton = returnPlayersButton.navigation;
        navReturnButton.mode = Navigation.Mode.Explicit;

        if (toggle)
        {
            navToggle.selectOnDown = startGameButton;
            navStartButton.selectOnUp = randomTurnToggle;
            if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
        }
        else
        {
            navToggle.selectOnDown = firstPlayerDropdown;
            navStartButton.selectOnUp = fourthPlayerDropdown;
            if(AudioManager.Instance != null) AudioManager.Instance.playToBack();
        }
        
        navReturnButton.selectOnUp = startGameButton;
        navReturnButton.selectOnLeft = startGameButton;

        randomTurnToggle.navigation = navToggle;
        startGameButton.navigation = navStartButton;
        returnPlayersButton.navigation = navReturnButton;
    }

    public void drawOptionsPanel()
    {
        if(optionsPanel == null) return;
        
        principalPanel.SetActive(false);
        screenPanel.SetActive(false);
        audioPanel.SetActive(false);
        optionsPanel.SetActive(true);
        ForceSelectionButton(screenButton); 
    }

    public void drawScreenPanel()
    {
        if(screenPanel == null) return;

        optionsPanel.SetActive(false);
        screenPanel.SetActive(true);
        ForceSelectionButton(resolutionDropDown);
    }

    public void drawAudioPanel()
    {
        if(audioPanel == null) return;

        optionsPanel.SetActive(false);
        audioPanel.SetActive(true);
        ForceSelectionButton(musicSlider);
    }

    public void drawCreditsPanel()
    {
        if(creditsPanel == null) return;
        
        principalPanel.SetActive(false);
        creditsPanel.SetActive(true);
        ForceSelectionButton(returnCreditsButton);
    }

    public void LoadAudioSliderSettings()
    {
        if(masterSlider != null) masterSlider.SetValueWithoutNotify(gameSettingsSO.masterVolume);
        if(musicSlider!= null) musicSlider.SetValueWithoutNotify(gameSettingsSO.musicVolume);
        if(sfxSlider != null) sfxSlider.SetValueWithoutNotify(gameSettingsSO.sfxVolume);
    }

    public void exitGame()
    {
        Application.Quit();
    }
}
