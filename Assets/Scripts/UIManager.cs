using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI Layers")]
    public GameObject centerViewLayer;
    public GameObject sideViewLayer;
    public GameObject topViewLayer;
    public GameObject turnPlanningPanel;
    
    [Header("Global Elements")]
    public TextMeshProUGUI globalTurnCounterText;

    [Header("Player Banners (Shared)")]
    [Header("Center Position")]
    public Image centerBannerImage;
    public TextMeshProUGUI centerBannerNameText;

    [Header("Side Position")]
    public Image sideBannerImage;
    public TextMeshProUGUI sideBannerNameText;

    [Header("Top Position")]
    public Image topBannerImage;
    public TextMeshProUGUI topBannerNameText;

    [Header("View Specific Elements")]
    [Header("Center View: Actions")]
    public GameObject phase1Container;
    public GameObject phase2Container;
    public Button initialStartButton;
    public Button rollDiceButton;
    public Button viewBoardButton;
    public Button freeCamButton;

    [Header("Center view: TP Indicators")]
    public GameObject tpIndicatorsContainer;

    [Header("Side view: Interactive vs Indicator")]
    public Button returnButton;
    public GameObject mapIndicatorReturnLabel;

    [Header("Top view: Interactive vs Indicator")]
    public Button freeRoamReturnButton;
    public GameObject freeRoamIndicatorReturnLabel;

    [Header("Turn planning context")]
    public GameObject stepsContainer;
    public GameObject keysContainer;
    public TextMeshProUGUI stepsCounterText;
    public TextMeshProUGUI keysCounterText;
    public Button tpConfirmButton;
    public TextMeshProUGUI tpConfirmButtonText;
    public Image tpConfirmImage;
    public Color confirmTextActiveColor = Color.white;
    public Color confirmTextDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Global Popups")]
    public GameObject confirmationPanel;
    public Button popupConfirmButton;
    public Button popupCancelButton;
    public TextMeshProUGUI confirmationText;
    private string defaultConfirmationMessage;
    private Action onPopupConfirmAction;
    private Action onPopupCancelAction;

    private bool isWaitingForRoll = false;
    private bool isTurnPlanningOrResolving = false;
    private bool showingActionPhase = false;

    private ExplorerPlayer currentExplorer;

    public static UIManager Instance { get; private set;}
    public bool IsConfirmationPopupActive {get; private set;} = false;

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
        if(confirmationText != null)
        {
            defaultConfirmationMessage = confirmationText.text;
        }

        if(GameManager.Instance == null) return;
        
        GameManager.Instance.OnTurnChanged += UpdatePlayerInfoUI;
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        if(GameManager.Instance.inputController != null)
        {
            GameManager.Instance.inputController.OnStepsChanged += UpdateStepsUI;
            this.tpConfirmButton.onClick.AddListener(() => GameManager.Instance.inputController.ConfirmMovement());
        }

        this.initialStartButton.onClick.AddListener(SwitchToActionsPhase);
        this.rollDiceButton.onClick.AddListener(() => GameManager.Instance.RollDiceAction());

        this.viewBoardButton.onClick.AddListener(() => 
        {
            ToggleCameraMode(true);
            ForceSelectButton(returnButton);
        });
        this.freeCamButton.onClick.AddListener(() => 
        {
            ToggleCameraMode(false);
            ForceSelectButton(freeRoamReturnButton);
        });

        this.returnButton.onClick.AddListener( () => ToggleCameraMode(true));
        this.freeRoamReturnButton.onClick.AddListener( () => ToggleCameraMode(false));

        if(popupConfirmButton != null)
        {
            popupConfirmButton.onClick.AddListener( () =>
            {
                HideConfirmationPopup();
                onPopupConfirmAction?.Invoke();
            });
        }

        if(popupCancelButton != null)
        {
            popupCancelButton.onClick.AddListener( () =>
            {
                HideConfirmationPopup();
                onPopupCancelAction?.Invoke();
            });
        }
        if(confirmationPanel != null) confirmationPanel.SetActive(false);

        HandleGameStateChanged(GameManager.Instance.currentState);
        
        if(GameManager.Instance.cameraManager != null)
        {
            GameManager.Instance.cameraManager.OnCameraModeChanged += RefreshUILayoutBasedOnCamera;
        }

        UpdateTurnCounterUI();
        RefreshUILayoutBasedOnCamera();
    }

    void OnDestroy() {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= UpdatePlayerInfoUI;
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;

            if(GameManager.Instance.inputController != null)
            {
                GameManager.Instance.inputController.OnStepsChanged -= UpdateStepsUI;
            }

            if(GameManager.Instance.cameraManager != null)
            {
                GameManager.Instance.cameraManager.OnCameraModeChanged -= RefreshUILayoutBasedOnCamera;
            }
        }

        if(currentExplorer != null)
        {
            currentExplorer.OnKeysChanged -= UpdateKeysUI;
        }
    }

    void Update()
    {
        if (isWaitingForRoll && centerViewLayer != null && centerViewLayer.activeSelf && phase1Container != null && phase1Container.activeSelf)
        {
            if(Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                initialStartButton.onClick.Invoke();
            }
        }
    }

    private void ForceSelectButton(Button btnToSelect)
    {
        if(EventSystem.current != null && btnToSelect != null && btnToSelect.gameObject.activeInHierarchy && btnToSelect.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btnToSelect.gameObject);
        }
    }

    private void ToggleCameraMode(bool isMapToggle)
    {
        if(isMapToggle) GameManager.Instance.UI_ToggleMapAction();
        else GameManager.Instance.UI_ToggleFreeCamAction();
        RefreshUILayoutBasedOnCamera();
    }

    private void HandleGameStateChanged(GameState state)
    {
        isWaitingForRoll = (state == GameState.WaitingForRoll);
        isTurnPlanningOrResolving = (state == GameState.TurnPlanning || state == GameState.Moving || state == GameState.ResolvingTurn);

        if (isWaitingForRoll)
        {
            UpdateTurnCounterUI();
            showingActionPhase = false;
            UpdatePhaseVisibility();
            if(EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        else if (isTurnPlanningOrResolving)
        {
            UpdateTurnCounterUI();
            if(tpConfirmButton != null && state == GameState.TurnPlanning)
            {
                tpConfirmButton.interactable = false;
                tpConfirmImage.color = confirmTextDisabledColor;
                if(tpConfirmButtonText != null) tpConfirmButtonText.color = confirmTextDisabledColor;
            } 
        }
        RefreshUILayoutBasedOnCamera();
    }

    private void SwitchToActionsPhase()
    {
        showingActionPhase = true;
        UpdatePhaseVisibility();

        ForceSelectButton(rollDiceButton);
    }

    private void UpdatePhaseVisibility()
    {
        if(phase1Container != null) phase1Container.SetActive(!showingActionPhase);
        if(phase2Container != null) phase2Container.SetActive(showingActionPhase);
    }

    public void RefreshUILayoutBasedOnCamera()
    {
        if(GameManager.Instance == null || GameManager.Instance.cameraManager == null) return;

        bool isMapMode = GameManager.Instance.cameraManager.IsMapModeActive;
        bool IsFreeRoamMode = GameManager.Instance.cameraManager.IsFreeRoamActive;
        bool isNormalMode = !isMapMode && !IsFreeRoamMode;
        bool isStrictlyPlanning = (GameManager.Instance.currentState == GameState.TurnPlanning);

        if(centerViewLayer != null) centerViewLayer.SetActive(isNormalMode && isWaitingForRoll);
        if(sideViewLayer != null) sideViewLayer.SetActive(isMapMode);
        if(topViewLayer != null) topViewLayer.SetActive(IsFreeRoamMode || (isNormalMode && isTurnPlanningOrResolving));

        if(turnPlanningPanel != null)
        {
            bool shouldPanelBeActive = isTurnPlanningOrResolving || (isWaitingForRoll && !isNormalMode);
            turnPlanningPanel.SetActive(shouldPanelBeActive);
        }

        if(stepsContainer != null)
        {
            stepsContainer.SetActive(isTurnPlanningOrResolving);
        }

        if(keysContainer != null)
        {
            bool showKeys = isTurnPlanningOrResolving || (isWaitingForRoll && !isNormalMode);
            keysContainer.SetActive(showKeys);
        }

        if(centerViewLayer != null && centerViewLayer.activeSelf)
        {
            if(phase1Container != null) phase1Container.SetActive(!showingActionPhase);
            if(phase2Container != null) phase2Container.SetActive(showingActionPhase);
            if(showingActionPhase) ForceSelectButton(rollDiceButton);
        }

        if(tpIndicatorsContainer != null ) tpIndicatorsContainer.SetActive(isNormalMode && isStrictlyPlanning);

        if(sideViewLayer != null && sideViewLayer.activeSelf)
        {
            returnButton.gameObject.SetActive(isWaitingForRoll);
            mapIndicatorReturnLabel.SetActive(isTurnPlanningOrResolving);
        }

        if(topViewLayer != null && topViewLayer.activeSelf)
        {
            bool showReturnElements = IsFreeRoamMode;
            freeRoamReturnButton.gameObject.SetActive(showReturnElements && isWaitingForRoll);
            freeRoamIndicatorReturnLabel.SetActive(showReturnElements && isTurnPlanningOrResolving);
        }

        if(tpConfirmButton != null)
        {
            bool shouldBeVisible = isStrictlyPlanning && (isNormalMode || isMapMode);
            tpConfirmButton.gameObject.SetActive(shouldBeVisible);
        }
    }

    private void UpdateStepsUI(int remaining, int total)
    {
        if(stepsCounterText != null)
        {
            int stepsTaken = total - remaining;
            stepsCounterText.text = $"Pasos\n{stepsTaken} / {total}";
        }

        if(tpConfirmButton != null && GameManager.Instance != null && GameManager.Instance.cameraManager != null)
        {
            if (GameManager.Instance.cameraManager.IsFreeRoamActive)
            {
                tpConfirmButton.gameObject.SetActive(false);
                return;
            }

            bool isStrictlyPlanning = (GameManager.Instance.currentState == GameState.TurnPlanning);
            if(!isStrictlyPlanning || !tpConfirmButton.gameObject.activeSelf) return;

            bool canConfirm = (remaining == 0 && total > 0);
            tpConfirmButton.interactable = canConfirm;
            tpConfirmImage.color = canConfirm ? confirmTextActiveColor : confirmTextDisabledColor;
            
            if(tpConfirmButtonText != null)
            {
                tpConfirmButtonText.color = canConfirm ? confirmTextActiveColor : confirmTextDisabledColor;
            }

            if (tpConfirmButton.interactable)
            {
                if(EventSystem.current.currentSelectedGameObject == null)
                {
                    ForceSelectButton(tpConfirmButton);
                }
            }
            else if(EventSystem.current.currentSelectedGameObject == tpConfirmButton.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    private void UpdateKeysUI(int keyCount)
    {
        if(keysCounterText != null)
        {
            keysCounterText.text = "x" + keyCount.ToString();
        }
    }

    private void UpdatePlayerInfoUI(Player currentPlayer)
    {
        if(currentPlayer == null) return;

        if(currentExplorer != null)
        {
            currentExplorer.OnKeysChanged -= UpdateKeysUI;
            currentExplorer = null;
        }

        if(currentPlayer is ExplorerPlayer explorer)
        {
            currentExplorer = explorer;
            currentExplorer.OnKeysChanged += UpdateKeysUI;
            UpdateKeysUI(currentExplorer.GetKeyCount());
        }
        else
        {
            UpdateKeysUI(0);
        }
        
        string turnText = $"Turno de {currentPlayer.characterName}";
        Color pColor = currentPlayer.playerColor;

        if(centerBannerNameText != null) centerBannerNameText.text = turnText;
        // if(centerBannerImage != null) centerBannerImage.color = pColor;

        if(sideBannerNameText != null) sideBannerNameText.text = turnText;
        // if(sideBannerImage != null) sideBannerImage.color = pColor;

        if(topBannerNameText != null) topBannerNameText.text = turnText;
        //if(topBannerImage != null) topBannerImage.color = pColor;
    }

    private void UpdateTurnCounterUI()
    {
        if(GameManager.Instance != null && globalTurnCounterText != null)
        {
            globalTurnCounterText.text = $"Turno {GameManager.Instance.totalTurnCount}";
        }
    }

    public void ShowMovementConfirmation(Action onConfirm, Action onCancel, string customMessage = null)
    {
        this.onPopupConfirmAction = onConfirm;
        this.onPopupCancelAction = onCancel;

        if(confirmationPanel != null)
        {
            if(confirmationText != null)
            {
                if (!string.IsNullOrEmpty(customMessage)) confirmationText.text = customMessage;
                else confirmationText.text = defaultConfirmationMessage;
            }

            IsConfirmationPopupActive = true;
            confirmationPanel.SetActive(true);
            ForceSelectButton(popupCancelButton);
        }
    }

    public void ShowAttackConfirmationUI(Action onConfirm, Action onCancel, string victimName)
    {
        string attackMessage = $"¿Seguro que quieres atacar a {victimName} y terminar el turno?";
        ShowMovementConfirmation(onConfirm, onCancel, attackMessage);
    }

    private void HideConfirmationPopup()
    {
        if(confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
            StartCoroutine(DisablePopupFlagAtEndOfFrame());
        }
    }

    private System.Collections.IEnumerator DisablePopupFlagAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        if(confirmationPanel != null && !confirmationPanel.activeSelf) IsConfirmationPopupActive = false;
    }
}
