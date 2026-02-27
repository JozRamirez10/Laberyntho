using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set;}

    [Header("UI Layers")]
    public GameObject centerViewLayer;
    public GameObject sideViewLayer;
    public GameObject topViewLayer;
    public GameObject turnPlanningPanel;
    
    [Header("Global Elements")]
    public Image gloablTurnCounterImage;
    public TextMeshProUGUI globalTurnCounterText;
    public Button pauseButton;

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

    [Header("Move Walls UI")]
    public GameObject moveWallContainer;
    public GameObject selectionInstructions;
    public GameObject controlsInstructions;

    [Header("Reward UI (Key)")]
    public GameObject keyRewardPanel;
    public RawImage keyRewardRawImage;
    public Button keyRewardConfirmButton;

    [Header("Dice Result")]
    public TextMeshProUGUI diceResultText;

    [Header("Victory UI")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryWinnerText;
    public Button restartButton;
    public TextMeshProUGUI restartText;
    public Button exitMenuButton;
    public TextMeshProUGUI exitMenuText;
    public VictoryStageController victoryStage;

    private bool isWaitingForRoll = false;
    private bool isTurnPlanningOrResolving = false;
    private bool showingActionPhase = false;

    private Action onKeyRewardComplete;

    private ExplorerPlayer currentExplorer;

    private GameObject lastSelectObject;

    public bool IsConfirmationPopupActive {get; private set;} = false;

    // CPU
    private bool hasStartedCPU = false;
    private bool isRollingDiceCPU = false;

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else { Destroy(gameObject); return;}
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

        if(GameManager.Instance.mazeInputController != null)
        {
            GameManager.Instance.mazeInputController.OnSelectionModeActive += ToggleWallSelection;
            GameManager.Instance.mazeInputController.OnObjectSelected += ToggleWallControls;
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
                ForceSelectButton(tpConfirmButton);
            });
        }
        if(confirmationPanel != null) confirmationPanel.SetActive(false);

        if(keyRewardConfirmButton != null) keyRewardConfirmButton.onClick.AddListener(HideKeyReward);

        if(keyRewardPanel != null) keyRewardPanel.SetActive(false);

        if(victoryPanel != null) victoryPanel.SetActive(false);

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

            if(GameManager.Instance.mazeInputController != null)
            {
                GameManager.Instance.mazeInputController.OnSelectionModeActive -= ToggleWallSelection;
                GameManager.Instance.mazeInputController.OnObjectSelected -= ToggleWallControls;
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
        if(UIPauseManager.Instace.isPaused) return;

        if (isWaitingForRoll && centerViewLayer != null && centerViewLayer.activeSelf && phase1Container != null && phase1Container.activeSelf)
        {
            if(GameManager.Instance != null)
            {
                if (GameManager.Instance.isTurnCPU && !hasStartedCPU)
                {
                    hasStartedCPU = true;
                    StartCoroutine(InitialStartCPU());
                } 
                else if(Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    initialStartButton.onClick.Invoke();
                }
            } 
        }

        bool isDicePhaseActive = (isWaitingForRoll && centerViewLayer != null && centerViewLayer.activeSelf && phase2Container != null && phase2Container.activeSelf);
        bool isPopupActive = (confirmationPanel != null && confirmationPanel.activeSelf);
        bool isVictoryActive = (victoryPanel != null &&  victoryPanel.activeSelf);

        if (isDicePhaseActive && GameManager.Instance != null && GameManager.Instance.isTurnCPU && !isRollingDiceCPU)
        {
            isRollingDiceCPU = true;
            StartCoroutine(RollDiceActionCPU());
        }

        if (isDicePhaseActive || isPopupActive || isVictoryActive)
        {
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if(currentSelected == null || currentSelected == lastSelectObject) return;

            if(!Input.GetButtonDown("Submit") && AudioManager.Instance != null && lastSelectObject != null)
            {
                AudioManager.Instance.playToSelect();
            }

            lastSelectObject = currentSelected;
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

    private void ToggleWallSelection(bool isActive)
    {
        if(selectionInstructions != null) selectionInstructions.SetActive(isActive);
        if(controlsInstructions != null) controlsInstructions.SetActive(!isActive);
    }

    private void ToggleWallControls(bool isObjectSelected)
    {
        if(controlsInstructions != null) controlsInstructions.SetActive(isObjectSelected);
        if(selectionInstructions != null) selectionInstructions.SetActive(!isObjectSelected);
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
            hasStartedCPU = false;
            isRollingDiceCPU = false;
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

        bool isIntro = (GameManager.Instance.currentState == GameState.Intro);
        if(gloablTurnCounterImage != null) gloablTurnCounterImage.gameObject.SetActive(!isIntro);
        if(pauseButton != null) pauseButton.gameObject.SetActive(!isIntro);

        bool isMapMode = GameManager.Instance.cameraManager.IsMapModeActive;
        bool IsFreeRoamMode = GameManager.Instance.cameraManager.IsFreeRoamActive;
        bool isRolling = (GameManager.Instance.currentState == GameState.Rolling);
        bool isNormalMode = !isMapMode && !IsFreeRoamMode;
        bool isStrictlyPlanning = (GameManager.Instance.currentState == GameState.TurnPlanning);
        bool isMoveWallMode = (GameManager.Instance.currentState == GameState.MoveWall);

        bool isPopupVisible = (confirmationPanel != null && confirmationPanel.activeSelf);

        if(centerViewLayer != null) centerViewLayer.SetActive(isNormalMode && isWaitingForRoll);
        if(sideViewLayer != null) sideViewLayer.SetActive(isMapMode || isMoveWallMode);
        if(topViewLayer != null) topViewLayer.SetActive(IsFreeRoamMode || (isNormalMode && isTurnPlanningOrResolving) || isRolling);

        bool isTurnCPU = false;
        if(GameManager.Instance != null) isTurnCPU = GameManager.Instance.isTurnCPU;

        bool isMinotaurTurn = false;
        if(GameManager.Instance != null) isMinotaurTurn = GameManager.Instance.isMinotaurTurn;

        if(turnPlanningPanel != null)
        {
            bool shouldPanelBeActive = isTurnPlanningOrResolving || (isWaitingForRoll && !isNormalMode);
            turnPlanningPanel.SetActive(shouldPanelBeActive);
        }

        if(stepsContainer != null) stepsContainer.SetActive(isTurnPlanningOrResolving);

        if(keysContainer != null)
        {
            bool showKeys = (isTurnPlanningOrResolving || (isWaitingForRoll && !isNormalMode)) && !isMinotaurTurn;
            keysContainer.SetActive(showKeys);
        }

        if(centerViewLayer != null && centerViewLayer.activeSelf)
        {
            if(phase1Container != null) phase1Container.SetActive(!showingActionPhase);
            if(phase2Container != null) phase2Container.SetActive(showingActionPhase);
            if(showingActionPhase) ForceSelectButton(rollDiceButton);
        }

        if(tpIndicatorsContainer != null ) tpIndicatorsContainer.SetActive(isNormalMode && isStrictlyPlanning && !isTurnCPU);

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
            bool shouldBeVisible = isStrictlyPlanning && (isNormalMode || isMapMode) && !isPopupVisible;
            tpConfirmButton.gameObject.SetActive(shouldBeVisible);
        }

        if(moveWallContainer != null)
        {
            if(!isMoveWallMode) moveWallContainer.SetActive(false);
            else {
                moveWallContainer.SetActive(true);

                if(selectionInstructions != null ) selectionInstructions.SetActive(true);
                if(controlsInstructions != null) controlsInstructions.SetActive(false);
            }
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
                if(EventSystem.current.currentSelectedGameObject != tpConfirmButton.gameObject)
                {
                    ForceSelectButton(tpConfirmButton);
                    if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
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

    public void ShowKeyGainSequence(Action onComplete)
    {
        onKeyRewardComplete = onComplete;

        if(keyRewardPanel != null)
        {
            keyRewardPanel.SetActive(true);

            if(keyRewardRawImage != null)
            {
                keyRewardRawImage.transform.localScale = Vector3.zero;
                StartCoroutine(AnimatePop(keyRewardRawImage.transform));
            }
            ForceSelectButton(keyRewardConfirmButton);
            if(AudioManager.Instance != null) AudioManager.Instance.playWinKey();

            if (GameManager.Instance != null && GameManager.Instance.isTurnCPU) StartCoroutine(GetKeyCPU());
        }
        else onComplete?.Invoke();
    }

    private void HideKeyReward()
    {
        if(keyRewardPanel != null) keyRewardPanel.SetActive(false);

        onKeyRewardComplete?.Invoke();
        onKeyRewardComplete = null;
    }

    private IEnumerator AnimatePop(Transform target)
    {
        float timer = 0f;
        float duration = 0.5f;

        while(timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            float scale = Mathf.Sin(t * Mathf.PI) * 0.2f + t;
            if(t >= 1f) scale = 1f;

            target.localScale = Vector3.one * scale;
            yield return null;
        }
        target.localScale = Vector3.one;
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
        if (currentPlayer.IsCPU) turnText = $"Turno de {currentPlayer.characterName} [CPU]";
        
        Color pColor = currentPlayer.playerColor;

        if(centerBannerNameText != null) centerBannerNameText.text = turnText;
        if(sideBannerNameText != null) sideBannerNameText.text = turnText;
        if(topBannerNameText != null) topBannerNameText.text = turnText;
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

            if(tpConfirmButton != null) tpConfirmButton.gameObject.SetActive(false);

            ForceSelectButton(popupCancelButton);
        }
    }

    public void ShowWallMoveConfirmation(Action onConfirm, Action onCancel)
    {
        string message = "¿Confirmar nueva posición de muro?";
        ShowMovementConfirmation(onConfirm, onCancel, message);
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

        RefreshUILayoutBasedOnCamera();
    }

    private System.Collections.IEnumerator DisablePopupFlagAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        if(confirmationPanel != null && !confirmationPanel.activeSelf) IsConfirmationPopupActive = false;
    }

    public IEnumerator ShowDiceResultRoutine(int number, float duration)
    {
        if(diceResultText != null && number != 6)
        {
            if(number <= 5) diceResultText.text = $"Muévete {number} unidades";
            if(number == 7) diceResultText.text = "¡Mueve un muro!";
            if(number == 8) diceResultText.text = "¡Mueve al Minotauro!";

            diceResultText.gameObject.SetActive(true);

            diceResultText.transform.localScale = Vector3.zero;
            float timer = 0f;
            while(timer < 0.2f)
            {
                timer += Time.deltaTime;
                float s = Mathf.Lerp(0, 1.5f, timer/0.2f);
                diceResultText.transform.localScale = Vector3.one * s;
                yield return null;
            }
            diceResultText.transform.localScale = Vector3.one;

            if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();

            yield return new WaitForSeconds(duration);
            
            diceResultText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ShowWinScreen(Player winner)
    {
        if(victoryPanel != null) victoryPanel.SetActive(true);

        if(victoryWinnerText != null) victoryWinnerText.text = $"¡{winner.characterName} ha ganado!";

        if(victoryStage != null) victoryStage.SetupWinner(winner);

        ForceSelectButton(restartButton);
    }

    // CPU
    private IEnumerator InitialStartCPU()
    {
        yield return new WaitForSeconds(2f);
        SwitchToActionsPhase();
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
    }

    private IEnumerator RollDiceActionCPU()
    {
        yield return new WaitForSeconds(1f);
        GameManager.Instance.RollDiceAction();
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
    }

    private IEnumerator GetKeyCPU()
    {
        yield return new WaitForSeconds(2.5f);
        HideKeyReward();
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
    }
}
