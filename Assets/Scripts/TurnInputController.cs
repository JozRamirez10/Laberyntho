using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class TurnInputController : MonoBehaviour
{
    [SerializeField] private GameObject phantomPrefab;
    [SerializeField] private GameObject attackPhantomPrefab;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask doorLayer;
    private LayerMask allBlockingLayers;

    public float gridSize = 1f;
    private Player currentPlayer;
    private ExplorerPlayer currentExplorer;
    private MinotaurPlayer currentMinotaurPlayer;

    private bool isControllingMinotaur = false;
    private bool isFirstMinotaurStep = false;

    private int stepsRemaining;
    private int totalStepsAvailable;
    private Vector3 currentPhantomPos;
    private Vector3 initialPlanningPos;
    private List<GameObject> phantoms = new List<GameObject>();
    private List<Vector3> pathPositions = new List<Vector3>();
    private bool isPlanning = false;

    private Transform mainCameraTransform;
    private CameraManager cameraManager;

    public event Action<Transform> OnPhantomTargetChanged;
    public event Action<int, int> OnStepsChanged;
    public event Action<Transform> OnFocusUnitChanged;

    void Awake()
    {
        if(Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.Log("No se encuentra la Main Camera");
        }
        allBlockingLayers = obstacleLayer | doorLayer;
    }

    void Start()
    {
        cameraManager = FindFirstObjectByType<CameraManager>();
        if(cameraManager == null)
        {
            Debug.Log("No se encontró el CameraManager");
        }
    }

    public void StartTurnPlanning(Player player, int diceRoll, bool isMoninotaurTurn)
    {
        this.currentPlayer = player;
        this.currentExplorer = player as ExplorerPlayer;
        this.currentMinotaurPlayer = player as MinotaurPlayer;

        OnFocusUnitChanged?.Invoke(this.currentPlayer.transform);

        this.isControllingMinotaur = isMoninotaurTurn;
        this.isFirstMinotaurStep = isMoninotaurTurn;

        this.stepsRemaining = diceRoll;
        this.totalStepsAvailable = diceRoll;

        Vector3 startPosFixedY = new Vector3(player.transform.position.x, 0.05f, player.transform.position.z);
        this.currentPhantomPos = startPosFixedY;
        this.initialPlanningPos = startPosFixedY;

        if (isMoninotaurTurn)
        {
            this.isFirstMinotaurStep = IsInWinningZone(this.initialPlanningPos);
        }
        else
        {
            this.isFirstMinotaurStep = false;
        }

        ClearPhantoms();
        this.pathPositions.Clear();

        isPlanning = true;

        OnPhantomTargetChanged?.Invoke(currentPlayer.transform);
        OnStepsChanged?.Invoke(stepsRemaining, totalStepsAvailable);
    }

    void Update()
    {
        if(!isPlanning) return;

        if(UIManager.Instance != null && UIManager.Instance.IsConfirmationPopupActive) return;

        if(cameraManager != null && cameraManager.IsFreeRoamActive) return;
        
        GameState currentState = GameManager.Instance.currentState;
        if(currentState != GameState.TurnPlanning) return;

        if(stepsRemaining > 0 || pathPositions.Count > 0)
        {
            if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) TryAddStep(GetCameraRelativeDirection(Vector3.forward));
            if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) TryAddStep(GetCameraRelativeDirection(Vector3.left));
            if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TryAddStep(GetCameraRelativeDirection(Vector3.right));

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                if(cameraManager != null && cameraManager.IsMapModeActive)
                {
                    TryAddStep(GetCameraRelativeDirection(Vector3.back));
                }
                else
                {
                    UndoLastStep();
                }
            }
        }     

        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmMovement();
        }
    }

    private Vector3 GetCameraRelativeDirection(Vector3 direction)
    {
        if(cameraManager != null && cameraManager.IsMapModeActive)
        {
            return direction;
        }

        if(mainCameraTransform == null) return direction;

        Vector3 cameraForward = mainCameraTransform.forward;
        Vector3 cameraRight = mainCameraTransform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 relativeDirection = (cameraForward * direction.z) + (cameraRight * direction.x);
        Vector3 snappedDirection = Vector3.zero;

        if(Mathf.Abs(relativeDirection.x) > Mathf.Abs(relativeDirection.z))
        {
            snappedDirection = relativeDirection.x > 0 ? Vector3.right : Vector3.left;
        }
        else 
        {
            snappedDirection = relativeDirection.z > 0 ? Vector3.forward : Vector3.back;
        }

        return snappedDirection;
    }

    private bool IsInWinningZone(Vector3 position)
    {
        float snappedX = Mathf.Floor(position.x / gridSize) * gridSize + (gridSize / 2f);
        float snappedZ = Mathf.Floor(position.z / gridSize) * gridSize + (gridSize / 2f);
        float offset = gridSize / 2f;

        bool xIsCentral = Mathf.Abs(Mathf.Abs(snappedX) - offset) < 0.01f;
        bool zIsCentral = Mathf.Abs(Mathf.Abs(snappedZ) - offset) < 0.01f;

        return xIsCentral && zIsCentral;
    }

    private void TryAddStep(Vector3 direction)
    {
        float currentStepDistance = gridSize;
        if(isControllingMinotaur && isFirstMinotaurStep)
        {
            currentStepDistance = gridSize * 1.5f;
        }

        Vector3 calculatedPos = this.currentPhantomPos + (direction * currentStepDistance);

        if(isControllingMinotaur && isFirstMinotaurStep)
        {
            Vector3 centeredPos = GetTileCenterFlat(calculatedPos);
            calculatedPos = new Vector3(centeredPos.x, calculatedPos.y, centeredPos.z);
        }
        Vector3 potentialTargetPost = new Vector3(calculatedPos.x, 0.05f, calculatedPos.z);

        RaycastHit hit;
        float distanceToCheck = Vector3.Distance(this.currentPhantomPos, potentialTargetPost);

        if(Physics.Raycast(this.currentPhantomPos + Vector3.up * 0.1f, direction, out hit, distanceToCheck * 0.9f, allBlockingLayers))
        {
            if(doorLayer == (doorLayer | (1 << hit.collider.gameObject.layer)))
            {
                if(currentExplorer != null && currentExplorer.GetKeyCount() > 0)
                {
                    Debug.Log("Atravesando puerta con llave");
                }
                else
                {
                    Debug.Log("Camino bloqueado por puerta");
                    return;
                }
            }
            else
            {
                Debug.Log("Camino bloqueado por muro");
                return;
            }
        }

        if(isControllingMinotaur && pathPositions.Count == 1)
        {
            float currentDistToStart = Vector3.Distance(this.currentPhantomPos, this.initialPlanningPos);
            float newDistToStart = Vector3.Distance(potentialTargetPost, this.initialPlanningPos);
            if(newDistToStart < currentDistToStart - 0.5f)
            {
                UndoLastStep();
                return;
            }
        }

        bool isBacktracking = false;
        if(pathPositions.Count > 0)
        {
            Vector3 previousPosToCheck;

            if(pathPositions.Count == 1)
            {
                
                previousPosToCheck = this.initialPlanningPos;
            }
            else
            {
                previousPosToCheck = pathPositions[pathPositions.Count - 2];
            }
            
            if(Vector3.Distance(potentialTargetPost, previousPosToCheck) < 0.01f)
            {
                isBacktracking = true;
            }
        }

        if (isBacktracking)
        {
            UndoLastStep();
            return;
        }

        if(stepsRemaining <= 0)
        {
            Debug.Log("No te quedan más pasos");
            return;
        }

        foreach(Vector3 existingPos in pathPositions)
        {
            if(Vector3.Distance(existingPos, potentialTargetPost) < 0.01f)
            {
                Debug.Log("Casilla ya marcada");
                return;
            }
        }

        if (isControllingMinotaur)
        {
            Vector3 targetTileCenter = GetTileCenterFlat(potentialTargetPost);
            ExplorerPlayer[] explorers = FindObjectsByType<ExplorerPlayer>(FindObjectsSortMode.None);
            foreach(var explorer in explorers)
            {
                Vector3 explorerPos = GetTileCenterFlat(explorer.transform.position);
                if(Vector3.Distance(targetTileCenter, explorerPos) < 0.01f)
                {
                    Debug.Log("Planeando ataque del minotauro");
                    PlaceAttackStep(potentialTargetPost, direction, explorer);
                    return;
                }
            }
        }

        PlaceNormalStep(potentialTargetPost, direction);
    }

    private void PlaceNormalStep(Vector3 pos, Vector3 dir)
    {
        Quaternion phantomRotation = Quaternion.LookRotation(dir, Vector3.up);
        GameObject newPhantom = Instantiate(phantomPrefab, pos , phantomRotation);
        RegisterStep(newPhantom, pos);
    }

    private void PlaceAttackStep(Vector3 pos, Vector3 dir, ExplorerPlayer victim)
    {
        if(attackPhantomPrefab == null) return;
        Quaternion phantomRotation = Quaternion.LookRotation(dir, Vector3.up);
        GameObject redPhantom = Instantiate(attackPhantomPrefab, pos, phantomRotation);
        RegisterStep(redPhantom, pos);
        
        ShowAttackConfirmationUI(victim);
    }

    private void RegisterStep(GameObject phantom, Vector3 pos)
    {
        phantoms.Add(phantom);
        this.pathPositions.Add(pos);
        this.currentPhantomPos = pos;
        stepsRemaining--;

        if(isControllingMinotaur && isFirstMinotaurStep) isFirstMinotaurStep = false;

        OnPhantomTargetChanged?.Invoke(phantom.transform);
        UpdatePhantomPulsing();
        OnStepsChanged?.Invoke(stepsRemaining, totalStepsAvailable);
    }

    private void ShowAttackConfirmationUI(ExplorerPlayer victim)
    {
        if(cameraManager != null) cameraManager.ForceTopDownView(true);
        UIManager.Instance.ShowAttackConfirmationUI(
            onConfirm: () =>
            {
                PerformMinotaurAttack(victim);
            },
            onCancel: () =>
            {
                if(cameraManager != null) cameraManager.ForceTopDownView(false);
                UndoLastStep();
            },
            victim.characterName
        );
    }

    private void PerformMinotaurAttack(ExplorerPlayer victim)
    {
        if(cameraManager != null) cameraManager.ForceTopDownView(false);

        isPlanning = false;
        GameManager.Instance.currentState = GameState.Moving;
        ClearPhantoms();

        List<Vector3> approachPath = new List<Vector3>(this.pathPositions);
        if(approachPath.Count > 0)
        {
            approachPath.RemoveAt(approachPath.Count - 1);
        }

        Action onAttackFinishedCallback = () =>
        {
            // victim.InstantMoveTo(victim.startPosition);
            // currentPlayer.InstantMoveTo(currentPlayer.startPosition);    

            this.pathPositions.Clear();
            stepsRemaining = 0;

            GameManager.Instance.currentState = GameState.ResolvingTurn;
            GameManager.Instance.EndMovementState();
        };

        if(currentMinotaurPlayer != null)
        {
            currentMinotaurPlayer.BeginAttackSequence(approachPath, victim.transform, onAttackFinishedCallback, this);
        }
        else
        {
            onAttackFinishedCallback.Invoke();
        }
    }

    public void ConfirmMovement()
    {
        if(stepsRemaining > 0) return;

        if(cameraManager != null) cameraManager.ForceTopDownView(true);

        int doorsCrossed = CounterDoorsInPath();
        
        Action onCancelCommon = () =>
        {
            if(cameraManager != null) cameraManager.ForceTopDownView(false);    
        };

        Action onFinalConfirm = () =>
        {
            if(doorsCrossed > 0 && currentExplorer != null)
            {
                for(int i = 0; i < doorsCrossed; i++) currentExplorer.TryRemoveKey();
            }
            ProceedWithMovement();
        };

        Action showMovementPopup = () =>
        {
            UIManager.Instance.ShowMovementConfirmation(onFinalConfirm, onCancelCommon);
        };

        if(doorsCrossed > 0 && currentExplorer != null && currentExplorer.GetKeyCount() >= doorsCrossed)
        {
            string keyMessage = $"Gastar {doorsCrossed} llave(s) para abrir las puertas?";
            UIManager.Instance.ShowMovementConfirmation(
                onConfirm: () =>
                {
                    showMovementPopup.Invoke();
                },
                onCancel: onCancelCommon,
                customMessage: keyMessage
            );
        }
        else
        {
            showMovementPopup.Invoke();
        }
    }

    private int CounterDoorsInPath()
    {
        int doorCount = 0;
        if(pathPositions.Count == 0) return 0;

        Vector3 startPos = this.initialPlanningPos;
        foreach(Vector3 targetPos in pathPositions)
        {
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);

            RaycastHit hit;
            if(Physics.Raycast(startPos + Vector3.up * 0.1f, direction, out hit, distance, doorLayer))
            {
                doorCount++;
            }
            startPos = targetPos;
        }
        return doorCount;
    }

    private void ProceedWithMovement()
    {
        if(cameraManager != null && cameraManager.IsMapModeActive)
        {
            cameraManager.ToggleMapUI();
        }

        isPlanning = false;
        GameManager.Instance.currentState = GameState.Moving;
        ClearPhantoms();
        this.currentPlayer.Move(new List<Vector3>(this.pathPositions), GameManager.Instance.EndMovementState);
        this.pathPositions.Clear();
    }

    private void UndoLastStep()
    {
        if(pathPositions.Count == 0 || phantoms.Count == 0) return;
        int lastIndex = phantoms.Count - 1;

        Destroy(phantoms[lastIndex]);
        phantoms.RemoveAt(lastIndex);

        pathPositions.RemoveAt(lastIndex);

        stepsRemaining++;

        if(pathPositions.Count > 0)
        {
            this.currentPhantomPos = pathPositions.Last();
            OnPhantomTargetChanged?.Invoke(phantoms.Last().transform);
        }
        else
        {
            this.currentPhantomPos = this.initialPlanningPos;
            OnPhantomTargetChanged?.Invoke(currentPlayer.transform);

            if (isControllingMinotaur)
            {
                isFirstMinotaurStep = IsInWinningZone(this.initialPlanningPos);
            }
        }

        UpdatePhantomPulsing();
        OnStepsChanged?.Invoke(stepsRemaining, totalStepsAvailable);
    }

    private Vector3 GetTileCenterFlat(Vector3 position)
    {
        float snappedX = Mathf.Floor(position.x / gridSize) * gridSize + (gridSize / 2f);
        float snappedZ = Mathf.Floor(position.z / gridSize) * gridSize + (gridSize / 2f);
        return new Vector3(snappedX, 0f, snappedZ);
    }

    public void ForceCameraFocus(Transform target)
    {
        OnFocusUnitChanged?.Invoke(target);
    }

    private void UpdatePhantomPulsing()
    {
        if(phantoms.Count == 0) return;

        for(int i = 0; i < phantoms.Count - 1 ; i++)
        {
            if(phantoms[i] != null)
            {
                GlowPulserIcon pulser = phantoms[i].GetComponent<GlowPulserIcon>();
                if(pulser != null)
                {
                    pulser.SetGlowState(true, false);
                }
            }
        }

        GameObject lastPhantom = phantoms.Last();
        if(lastPhantom != null)
        {
            GlowPulserIcon lastPulser = lastPhantom.GetComponent<GlowPulserIcon>();
            if(lastPulser != null)
            {
                lastPulser.SetGlowState(true, true);
            }
        }
    }

    private void ClearPhantoms()
    {
        foreach(GameObject p in phantoms)
        {
            if(p != null) Destroy(p);
        }
        phantoms.Clear();
    }
}
