using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.UI;
using System;

public class CameraManager : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    public CinemachineCamera vcamTopDown;
    public CinemachineCamera vcamFaceToFace;
    public CinemachineCamera vcamThirdPerson;
    public CinemachineCamera vcamMiniMap;
    public CinemachineCamera vcamFreeRoam; 
    private Unity.Cinemachine.CinemachinePanTilt panTiltController;

    [Header("Smoothing Agent & Ghost")]
    public CameraTargetSmoother targetSmoother;
    public GhostMovement ghostAgentMovementScript;

    [Header("Cinemachine Brain Controller")]
    public CinemachineBrain targetBrain;
    private float defaultBlendTime;
    private Camera mainCamPhysical;

    [Header("Icon")]
    public LayerMask mapIconsLayerMask;
    private int defaultMainCamMask;

    [Header("Minimap Renderer")]
    public Camera minimapPhysicalCamera;
    public GameObject minimapUIContainer;

    [Header("Screen Slide Transition (Solo para Mapa)")]
    public GameObject transitionCanvas;
    public RawImage slidingImage;
    public RenderTexture transitionRT;
    public float slideDuration = 0.5f;
    private bool isTransitioning = false;

    private float topDownNearClip = -10f;
    private float faceToFaceNearClip = -10f;
    private float thirPersonNearClip = 1;
    private float initialTiltAngle = 5f;

    private Transform currentPlayerTarget;

    public SimpleURPCuller uRPCuller;

    private GameState? activeSpecialMode = null; 
    private GameState stateBeforeSpecialMode;

    private GameState currentGameState;

    private const int PRIORITY_HIGH = 20;
    private const int PRIORITY_LOW = 10;

    public bool IsMapModeActive => activeSpecialMode == GameState.Setup;
    public bool IsFreeRoamActive => activeSpecialMode == GameState.FreeRoam;

    public event Action OnCameraModeChanged;

    public void ToggleMapUI()
    {
        ToggleSpecialMode(GameState.Setup);
    }

    public void ToggleFreeRoamUI()
    {
        ToggleSpecialMode(GameState.FreeRoam);
    }


    void Awake()
    {
        GameManager.Instance.OnTurnChanged += HandleTurnChanged;
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        if(GameManager.Instance.inputController != null)
        {
            GameManager.Instance.inputController.OnPhantomTargetChanged += HandlePhantomTargetChanged;
            GameManager.Instance.inputController.OnFocusUnitChanged += HandleFocusUnitChanged;
        }

        currentGameState = GameState.Setup;
        mainCamPhysical = Camera.main;
        if(mainCamPhysical != null)
        {
            defaultMainCamMask = mainCamPhysical.cullingMask;
            mainCamPhysical.cullingMask &= ~mapIconsLayerMask;
        }
        else
        {
            Debug.Log("No se encontró la Main Camera");
        }

        ApplyCameraForState(currentGameState);
    }

    void Start()
    {
        SetCameraNearClipSettings(vcamTopDown, topDownNearClip);
        SetCameraNearClipSettings(vcamFaceToFace, faceToFaceNearClip);
        SetCameraNearClipSettings(vcamThirdPerson, thirPersonNearClip);

        if(vcamThirdPerson != null)
        {
            panTiltController = vcamThirdPerson.GetComponent<Unity.Cinemachine.CinemachinePanTilt>();
            if(panTiltController == null) Debug.Log("No se encontró el Cinemachine Pan Tilt");
            
            if (targetSmoother != null) vcamThirdPerson.Target.TrackingTarget = targetSmoother.transform;
            else Debug.LogError("CameraManager: Falta Target Smoother");
        }

        if(targetBrain == null) targetBrain = Camera.main.GetComponent<CinemachineBrain>();
        if(targetBrain != null) defaultBlendTime = targetBrain.DefaultBlend.Time;

        if(transitionCanvas == null || slidingImage == null || transitionRT == null) Debug.Log("Faltan componentes transición");

        if (ghostAgentMovementScript == null)
        {
            ghostAgentMovementScript = FindFirstObjectByType<GhostMovement>();
            if(ghostAgentMovementScript != null)
            {
                Debug.Log("Encontramos el script GhostMovement");
            }
            else
            {
                Debug.Log("No existe GhostMovement");
            }
        }

        if (ghostAgentMovementScript != null) ghostAgentMovementScript.enabled = false;
    }

    void Update()
    {
        if(isTransitioning) return;

        if(GameManager.Instance.currentState == GameState.TurnPlanning)
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (IsMapModeActive)
                {
                    ToggleSpecialMode(GameState.Setup); 
                }
                else if(activeSpecialMode == null)
                {
                    ToggleSpecialMode(GameState.Setup);
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                if (IsFreeRoamActive)
                {
                    ToggleSpecialMode(GameState.FreeRoam);    
                    
                }
                else if(activeSpecialMode == null)
                {
                    ToggleSpecialMode(GameState.FreeRoam);
                }
            }
        }        

    }

    void OnDestroy()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            if(GameManager.Instance.inputController != null)
            {
                GameManager.Instance.inputController.OnPhantomTargetChanged -= HandlePhantomTargetChanged;
                GameManager.Instance.inputController.OnFocusUnitChanged -= HandleFocusUnitChanged;
            }
        }
    }

    private void SetMinimapActive(bool isActive)
    {
        if(minimapPhysicalCamera != null && minimapPhysicalCamera.gameObject.activeSelf != isActive)
            minimapPhysicalCamera.gameObject.SetActive(isActive);
        if(minimapUIContainer != null && minimapUIContainer.activeSelf != isActive)
            minimapUIContainer.SetActive(isActive);
    }

    private void SetCameraNearClipSettings(CinemachineCamera vcam, float nearClipValue)
    {
        if(vcam == null) return;
        LensSettings lensSettings = vcam.Lens;
        lensSettings.NearClipPlane = nearClipValue;
        vcam.Lens = lensSettings;
    }

    private void SetCameraPriorities(bool topDown = false, bool faceToFace = false, bool thirdPerson = false, bool freeRoam = false)
    {
        vcamTopDown.Priority = topDown ? PRIORITY_HIGH : PRIORITY_LOW;
        vcamFaceToFace.Priority = faceToFace ? PRIORITY_HIGH : PRIORITY_LOW;
        vcamThirdPerson.Priority = thirdPerson ? PRIORITY_HIGH : PRIORITY_LOW;
        if(vcamFreeRoam != null) vcamFreeRoam.Priority = freeRoam ? PRIORITY_HIGH : PRIORITY_LOW;

        if(thirdPerson && vcamThirdPerson != null) { vcamThirdPerson.gameObject.SetActive(false); vcamThirdPerson.gameObject.SetActive(true); }

        bool shouldShowMiniMap = (!topDown && !freeRoam) && (currentGameState != GameState.WaitingForRoll);
        SetMinimapActive(shouldShowMiniMap);

        if (ghostAgentMovementScript == null)
        {
            if (freeRoam) Debug.LogError("ERROR FATAL: Intentando activar FreeRoam pero 'ghostAgentMovementScript' es NULL. Revisa el Inspector del CameraManager.");
        }
        else
        {
            if (freeRoam)
            {
                ghostAgentMovementScript.transform.position = mainCamPhysical.transform.position + Vector3.up * 3.0f;

                if(vcamFreeRoam != null)
                {
                    vcamFreeRoam.Follow = ghostAgentMovementScript.transform;
                }
                
            }
            ghostAgentMovementScript.enabled = freeRoam;
        }
    }

    private void ApplyCameraForState(GameState stateToApply)
    {
        bool showIcons = (stateToApply == GameState.Setup);
        UPdateMainCameraIconVisibility(showIcons);

        switch (stateToApply)
        {
            case GameState.Setup: SetCameraPriorities(topDown: true); break;
            case GameState.WaitingForRoll:
            case GameState.Moving: SetCameraPriorities(faceToFace: true); break;
            case GameState.TurnPlanning: SetCameraPriorities(thirdPerson: true); break;
            case GameState.FreeRoam: SetCameraPriorities(freeRoam: true); break;
            case GameState.GameOver: default: break;
        }
    }

    private void HandlePhantomTargetChanged(Transform newTarget)
    {
        if (mainCamPhysical != null && targetSmoother != null)
        {
            Vector3 cameraForward = mainCamPhysical.transform.forward;
            cameraForward.y = 0; 
            if (cameraForward != Vector3.zero && cameraForward.sqrMagnitude > 0.001f)
                targetSmoother.transform.rotation = Quaternion.LookRotation(cameraForward);
        }
        if(panTiltController != null)
        {
            panTiltController.PanAxis.Value = 0f;
            panTiltController.TiltAxis.Value = initialTiltAngle;
        }
        if (targetSmoother != null) targetSmoother.SetTarget(newTarget);
    }

    private void HandleTurnChanged(Player currentPlayer)
    {
        if (currentPlayer == null) return;
        UpdateAllCameraTargets(currentPlayer.transform);
    }

    private void HandleFocusUnitChanged(Transform transform)
    {
        if(transform == null) return;
        UpdateAllCameraTargets(transform);
    }

    private void UpdateAllCameraTargets(Transform targetTransform)
    {
        currentPlayerTarget = targetTransform;
        if(uRPCuller != null) uRPCuller.targetToLookAt = currentPlayerTarget;
        vcamFaceToFace.Target.TrackingTarget = currentPlayerTarget;
        if (targetSmoother != null && mainCamPhysical != null)
        {
            Vector3 cameraForward = mainCamPhysical.transform.forward;
            cameraForward.y = 0;
            if (cameraForward != Vector3.zero && cameraForward.sqrMagnitude > 0.001f)
                targetSmoother.transform.rotation = Quaternion.LookRotation(cameraForward);
            targetSmoother.SetTarget(currentPlayerTarget);
        }
        if(panTiltController != null) { panTiltController.PanAxis.Value = 0f; panTiltController.TiltAxis.Value = initialTiltAngle; }
        if(vcamMiniMap != null) vcamMiniMap.Target.TrackingTarget = currentPlayerTarget;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if(state == GameState.Moving && activeSpecialMode != null) activeSpecialMode = null;
        if (activeSpecialMode != null) return;

        currentGameState = state;
        ApplyCameraForState(currentGameState);
    }

    private void ToggleSpecialMode(GameState targetMode)
    {
        if(isTransitioning) return;

        bool modeChanged = false;

        if (activeSpecialMode == targetMode)
        {
            if (activeSpecialMode == GameState.Setup)
            {
                StartCoroutine(DoSlideTransitionRoutine(stateBeforeSpecialMode, isEntering: false));
            }
            else if (activeSpecialMode == GameState.FreeRoam)
            {
                if(stateBeforeSpecialMode == GameState.WaitingForRoll)
                {
                    StartCoroutine(PerformCutTransitionRoutine(stateBeforeSpecialMode));
                }
                else
                {
                    PerformSmoothTransition(stateBeforeSpecialMode);
                }
            }
            activeSpecialMode = null;
            modeChanged = true;
        }
        else if (activeSpecialMode == null)
        {
            stateBeforeSpecialMode = currentGameState;
            activeSpecialMode = targetMode;

            if (targetMode == GameState.Setup)
            {
                StartCoroutine(DoSlideTransitionRoutine(targetMode, isEntering: true));
            }
            else if (targetMode == GameState.FreeRoam)
            {
                if(stateBeforeSpecialMode == GameState.WaitingForRoll)
                {
                    StartCoroutine(PerformCutTransitionRoutine(targetMode));
                }
                else
                {
                    PerformSmoothTransition(targetMode);
                }
            }
            modeChanged = true;
        }

        if (modeChanged)
        {
            OnCameraModeChanged?.Invoke();
        }
    }

    // Transición Suave (Cinemachine Blend normal)
    private void PerformSmoothTransition(GameState targetState)
    {
        currentGameState = targetState;
        ApplyCameraForState(currentGameState);
    }

    private IEnumerator PerformCutTransitionRoutine(GameState targetState)
    {
        isTransitioning = true;
        
        float originalBlendTime = defaultBlendTime;
        if(targetBrain != null)
        {
            originalBlendTime = targetBrain.DefaultBlend.Time;
            var blend = targetBrain.DefaultBlend;
            blend.Time = 0;
            targetBrain.DefaultBlend = blend;
        }

        currentGameState = targetState;
        ApplyCameraForState(currentGameState);

        yield return null;

        if(targetBrain != null)
        {
            var blend = targetBrain.DefaultBlend;
            blend.Time = originalBlendTime;
            targetBrain.DefaultBlend = blend;
        }

        isTransitioning = false;
    }

    // Transición de Deslizamiento (Solo para Mapa)
    private IEnumerator DoSlideTransitionRoutine(GameState targetState, bool isEntering)
    {
        isTransitioning = true;
        
        // 1. Captura de pantalla
        if(targetBrain != null)
        {
            var mainCam = targetBrain.GetComponent<Camera>();
            mainCam.targetTexture = transitionRT;
            mainCam.Render();
            mainCam.targetTexture = null;
        }

        // 2. Preparar UI
        RectTransform slideRect = slidingImage.rectTransform;

        float screenWidth = Screen.width;
        Vector2 startPos, endPos;
        
        if (isEntering)
        {
            startPos = Vector2.zero;
            endPos = new Vector2(-screenWidth, 0f);
        }
        else
        {
            startPos = Vector2.zero;
            endPos = new Vector2(screenWidth, 0f);
        }
        slideRect.anchoredPosition = startPos;

        transitionCanvas.SetActive(true);

        // 3. Corte instantáneo de cámara
        if(targetBrain != null)
        {
            var blend = targetBrain.DefaultBlend;
            blend.Time = 0;
            targetBrain.DefaultBlend = blend;
        }

        // 4. Cambiar estado de cámara
        currentGameState = targetState;
        ApplyCameraForState(currentGameState);

        yield return null;

        // 5. Restaurar blend time
        if(targetBrain != null)
        {
            var blend = targetBrain.DefaultBlend;
            blend.Time = defaultBlendTime;
            targetBrain.DefaultBlend = blend;
        }

        // 6. Animación UI
        float elapsedTime = 0f;

        while(elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slideDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            slideRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        slideRect.anchoredPosition = endPos;

        transitionCanvas.SetActive(false);
        isTransitioning = false;
    }

    private void UPdateMainCameraIconVisibility(bool showIcons)
    {
        if(mainCamPhysical == null) return;
        if (showIcons) mainCamPhysical.cullingMask |= mapIconsLayerMask;
        else mainCamPhysical.cullingMask = defaultMainCamMask & ~mapIconsLayerMask;
    }

    public void ForceTopDownView(bool enabled)
    {
        if (enabled)
        {
            SetCameraPriorities(topDown: true);
            UPdateMainCameraIconVisibility(true);
        }
        else
        {
            ApplyCameraForState(currentGameState);
        }
    }
}