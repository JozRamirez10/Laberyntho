using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject principalPanel;
    public GameObject optionsPanel;
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

    [Header("Options panel")]
    public Button screenButton;
    public Button audioButton;
    public Button returnOptionsButton;
    public TextMeshProUGUI screenText;
    public TextMeshProUGUI audioText;
    public TextMeshProUGUI returnOptionsText;

    [Header("Credits")]
    public Button returnCreditsButton;
    public TextMeshProUGUI returnCreditsText;

    private GameObject lastSelectObject;

    public static MenuManager Instance {get; private set;}

    private void Awake()
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
        if(playButton != null) ForceSelectionButton(playButton);
    }

    void Update()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if(currentSelected == null || currentSelected == lastSelectObject) return;
        
        lastSelectObject = currentSelected;
        UpdateTextColors(currentSelected);
    }

    private void ResetAllTextColors()
    {
        if (principalPanel.gameObject.activeInHierarchy)
        {
            if(playText) playText.color = disableSelectionColor;
            if(optionsText) optionsText.color = disableSelectionColor;
            if(creditsText) creditsText.color = disableSelectionColor;
            if(exitText) exitText.color = disableSelectionColor;
        }

        if (optionsPanel.gameObject.activeInHierarchy)
        {
            if(screenText) screenText.color = disableSelectionColor;
            if(audioText) audioText.color = disableSelectionColor;
            if(returnOptionsText) returnOptionsText.color = disableSelectionColor;
        }

        if (creditsPanel.gameObject.activeInHierarchy)
        {
            if(returnCreditsText) returnCreditsText.color = disableSelectionColor;
        }
    }

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

        if (optionsPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == screenButton.gameObject) screenText.color = activeSelectionColor;
            if(selectedObj == audioButton.gameObject) audioText.color = activeSelectionColor;
            if(selectedObj == returnOptionsButton.gameObject) returnOptionsText.color = activeSelectionColor;
        }

        if (creditsPanel.gameObject.activeInHierarchy)
        {
            if(selectedObj == returnCreditsButton.gameObject) returnCreditsText.color = activeSelectionColor;
        }
    }

    private void ForceSelectionButton(Button btnToSelect)
    {
        if(EventSystem.current != null && btnToSelect != null && btnToSelect.gameObject.activeInHierarchy && btnToSelect.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btnToSelect.gameObject);
            UpdateTextColors(btnToSelect.gameObject);
        }
    }

    public void playClassicGame()
    {
        SceneManager.LoadScene("Classic", LoadSceneMode.Single);
    }

    public void drawMenuPrincipalPanel()
    {
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        principalPanel.SetActive(true);
        ForceSelectionButton(playButton);
    }

    public void drawOptionsPanel()
    {
        if(optionsPanel == null) return;
        
        principalPanel.SetActive(false);
        optionsPanel.SetActive(true);
        ForceSelectionButton(screenButton); 
    }

    public void drawCreditsPanel()
    {
        if(creditsPanel == null) return;
        
        principalPanel.SetActive(false);
        creditsPanel.SetActive(true);
        ForceSelectionButton(returnCreditsButton);
    }

    public void exitGame()
    {
        Application.Quit();
    }
}
