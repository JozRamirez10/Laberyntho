using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class TurnInputController : MonoBehaviour
{
    [SerializeField] private GameObject phantomPrefab;
    [SerializeField] private GameObject attackPhantomPrefab;
    [SerializeField] private GameObject winPhantomPrefab;
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
        if(Camera.main != null) mainCameraTransform = Camera.main.transform;
        else Debug.Log("No se encuentra la Main Camera");
        
        allBlockingLayers = obstacleLayer | doorLayer;
    }

    void Start()
    {
        cameraManager = FindFirstObjectByType<CameraManager>();
        if(cameraManager == null) Debug.Log("No se encontró el CameraManager");
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

        // Si es el turno del minotauro y es su primer movimiento, debe realizar un salto del centro del tablero
        // al grid del tablero
        if (isMoninotaurTurn) this.isFirstMinotaurStep = IsInWinningZone(this.initialPlanningPos);
        else this.isFirstMinotaurStep = false;

        // Elimina los fantasmas de movimiento que pudieran existir
        ClearPhantoms(); 
        this.pathPositions.Clear();

        isPlanning = true;

        OnPhantomTargetChanged?.Invoke(currentPlayer.transform);
        OnStepsChanged?.Invoke(stepsRemaining, totalStepsAvailable);
    }

    void Update()
    {
        if(!isPlanning) return;

        bool isCPU = false;
        if(GameManager.Instance != null) isCPU = GameManager.Instance.isTurnCPU;

        if(UIManager.Instance != null && UIManager.Instance.IsConfirmationPopupActive) return;

        if(cameraManager != null && cameraManager.IsFreeRoamActive) return;
        
        GameState currentState = GameManager.Instance.currentState;
        if(currentState != GameState.TurnPlanning) return;

        if(InputManager.Instance != null)
        {
            if((stepsRemaining > 0 || pathPositions.Count > 0) && !isCPU)
            {
                // Moverse
                if(InputManager.Instance.IsUpPressed) TryAddStep(GetCameraRelativeDirection(Vector3.forward));
                if(InputManager.Instance.IsLeftPressed) TryAddStep(GetCameraRelativeDirection(Vector3.left));
                if(InputManager.Instance.IsRightPressed) TryAddStep(GetCameraRelativeDirection(Vector3.right));

                // Retroceder
                if (InputManager.Instance.IsDownPressed)
                {
                    if(cameraManager != null && cameraManager.IsMapModeActive) TryAddStep(GetCameraRelativeDirection(Vector3.back));
                    else UndoLastStep(); // Deshace el último movimiento
                }
            }     

            // Confirmar movimiento
            if(InputManager.Instance.IsConfirmPressed && !isCPU) ConfirmMovement();
        }

    }

    // La cámara se mueve relativo al frente del jugador
    private Vector3 GetCameraRelativeDirection(Vector3 direction)
    {
        if(cameraManager != null && cameraManager.IsMapModeActive) return direction;

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
        else snappedDirection = relativeDirection.z > 0 ? Vector3.forward : Vector3.back;

        return snappedDirection;
    }

    // Valida que la posición este en la zona ganadora (el centro del tablero)
    private bool IsInWinningZone(Vector3 position)
    {
        float snappedX = Mathf.Floor(position.x / gridSize) * gridSize + (gridSize / 2f);
        float snappedZ = Mathf.Floor(position.z / gridSize) * gridSize + (gridSize / 2f);
        float offset = gridSize / 2f;

        bool xIsCentral = Mathf.Abs(Mathf.Abs(snappedX) - offset) < 0.01f;
        bool zIsCentral = Mathf.Abs(Mathf.Abs(snappedZ) - offset) < 0.01f;

        return xIsCentral && zIsCentral;
    }

    // Valida que la posición no esta ocupada
    private Player GetOccupantAtPosition(Vector3 position)
    {
        if(GameManager.Instance == null) return null;

        // Valida iterando en la posición de los jugadores
        foreach(var p in GameManager.Instance.players)
        {
            if(p == null || p == currentPlayer) continue;
            if(Vector3.Distance(GetTileCenterFlat(p.transform.position), GetTileCenterFlat(position)) < 0.1f) return p;
        }

        // Valida con la posición del Minotauro
        if(GameManager.Instance.minotaurInstance != null && GameManager.Instance.minotaurInstance != currentPlayer)
        {
            if(Vector3.Distance(GetTileCenterFlat(GameManager.Instance.minotaurInstance.transform.position), GetTileCenterFlat(position)) < 0.1f)
            {
                return GameManager.Instance.minotaurInstance;
            }
        }
        return null;
    }

    // Considera todas las direcciones en que se pueda mover el jugador
    private void TryAddStep(Vector3 direction)
    {
        float currentStepDistance = gridSize;
        if(isControllingMinotaur && isFirstMinotaurStep) currentStepDistance = gridSize * 1.5f;

        Vector3 calculatedPos = this.currentPhantomPos + (direction * currentStepDistance);

        // Considera si es turno del minotauro y es su primer movimiento
        if(isControllingMinotaur && isFirstMinotaurStep)
        {
            Vector3 centeredPos = GetTileCenterFlat(calculatedPos);
            calculatedPos = new Vector3(centeredPos.x, calculatedPos.y, centeredPos.z);
        }

        // Calcula la nueva posición
        Vector3 potentialTargetPost = new Vector3(calculatedPos.x, 0.05f, calculatedPos.z);

        // Si deshaces el movimiento del minotauro y tiene que regresar al centro del tablero
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

        // Consideraciones si el jugador quiere volver a la casilla anterior
        bool isBacktracking = false;
        if(pathPositions.Count > 0)
        {
            Vector3 previousPosToCheck;

            if(pathPositions.Count == 1) previousPosToCheck = this.initialPlanningPos;
            else previousPosToCheck = pathPositions[pathPositions.Count - 2];
            
            if(Vector3.Distance(potentialTargetPost, previousPosToCheck) < 0.01f) isBacktracking = true;
        }

        // Deshace el movimineto del jugador
        if (isBacktracking){ UndoLastStep(); return; }

        // Si hay pasos <= 0, da un mensaje de error y sale de la función
        if(stepsRemaining <= 0)
        {
            if(AudioManager.Instance != null) AudioManager.Instance.playToError();
            return;
        }

        if(!isControllingMinotaur && BoardManager.Instance != null)
        {
            // Si llegas a la casilla ganadora
            if (BoardManager.Instance.isWinningTile(potentialTargetPost)) 
            {
                PlaceWinStep(potentialTargetPost, direction); // Instancia una casilla ganadora
                return;
            }
        }

        RaycastHit hit;
        float distanceToCheck = Vector3.Distance(this.currentPhantomPos, potentialTargetPost);

        // Lanza un rayo para validar si choca contra un objeto
        if(Physics.Raycast(this.currentPhantomPos + Vector3.up * 0.1f, direction, out hit, distanceToCheck * 0.9f, allBlockingLayers))
        {
            // Si el layer es la puerta y el jugador tiene llaves, le permite pasar
            if(doorLayer == (doorLayer | (1 << hit.collider.gameObject.layer)))
            {
                if(currentExplorer != null && currentExplorer.GetKeyCount() > 0) Debug.Log("Atravesando puerta con llave");
                else { if(AudioManager.Instance != null) AudioManager.Instance.playToError(); return; } // Error
            }
            else { if(AudioManager.Instance != null) AudioManager.Instance.playToError(); return; } // Error
        }

        // Obtiene la referencia del jugador o minotauro si ocupa un espacio
        Player tileOccupant = GetOccupantAtPosition(potentialTargetPost);

        if (!isControllingMinotaur && tileOccupant != null)
        {
            if (tileOccupant is MinotaurPlayer) // No permite pasar a través del minotauro
            {
                if(AudioManager.Instance != null) AudioManager.Instance.playToError();
                return;
            }

            if(tileOccupant is ExplorerPlayer)
            {
                // Si te queda una casilla no te permite pasar a través de un explorador
                // Protege que el jugador no pueda confimar su posición en una casilla que ya esta ocupada
                if(stepsRemaining == 1) 
                {
                    if(AudioManager.Instance != null) AudioManager.Instance.playToError();
                    return;
                }
            }
        }

        // No te puedes colocar donde ya existe una casilla fantasma
        foreach(Vector3 existingPos in pathPositions)
        {
            if(Vector3.Distance(existingPos, potentialTargetPost) < 0.01f)
            {
                if(AudioManager.Instance != null) AudioManager.Instance.playToError();
                return;
            }
        }

        // Minotauro
        if (isControllingMinotaur)
        {
            Vector3 targetTileCenter = GetTileCenterFlat(potentialTargetPost);
            ExplorerPlayer[] explorers = FindObjectsByType<ExplorerPlayer>(FindObjectsSortMode.None);
            foreach(var explorer in explorers) // Valida si la casilla es la misma que la posición de un jugador
            {
                Vector3 explorerPos = GetTileCenterFlat(explorer.transform.position);
                if(Vector3.Distance(targetTileCenter, explorerPos) < 0.01f)
                {
                    // Pone una casilla de ataque en la posición del jugador
                    PlaceAttackStep(potentialTargetPost, direction, explorer);
                    return;
                }
            }
        }

        // Pone una casilla fanstasma normal
        PlaceNormalStep(potentialTargetPost, direction);
    }

    private void PlaceNormalStep(Vector3 pos, Vector3 dir)
    {
        Quaternion phantomRotation = Quaternion.LookRotation(dir, Vector3.up);
        GameObject newPhantom = Instantiate(phantomPrefab, pos , phantomRotation);
        RegisterStep(newPhantom, pos); // Registra el fantasma en la lista de fantasmas
    }

    private void PlaceAttackStep(Vector3 pos, Vector3 dir, ExplorerPlayer victim)
    {
        if(attackPhantomPrefab == null) return;
        Quaternion phantomRotation = Quaternion.LookRotation(dir, Vector3.up);
        GameObject redPhantom = Instantiate(attackPhantomPrefab, pos, phantomRotation);
        RegisterStep(redPhantom, pos);
        
        ShowAttackConfirmationUI(victim); // Muestra el mensaje de confirmación para atacar
    }

    private void PlaceWinStep(Vector3 pos, Vector3 dir)
    {
        Quaternion phantomRotation = Quaternion.LookRotation(dir, Vector3.up);
        GameObject newPhantom = Instantiate(winPhantomPrefab, pos , phantomRotation);
        RegisterStep(newPhantom, pos);

        isPlanning = false;
        ShowWinConfirmationUI(); // Muestra el mensaje de confirmación para ganar
    }

    private void RegisterStep(GameObject phantom, Vector3 pos)
    {
        phantoms.Add(phantom); // Añade el fantasma a la lista de fantasmas
        this.pathPositions.Add(pos);
        this.currentPhantomPos = pos;
        stepsRemaining--; // Reduce los pasos disponibles

        if(AudioManager.Instance != null) AudioManager.Instance.playToSelect();

        if(isControllingMinotaur && isFirstMinotaurStep) isFirstMinotaurStep = false;

        OnPhantomTargetChanged?.Invoke(phantom.transform);
        UpdatePhantomPulsing(); // Actualice el último fantasma para que brille y pulse
        OnStepsChanged?.Invoke(stepsRemaining, totalStepsAvailable);
    }

    // Acciones para el canvas de confirmación para ganar
    private void ShowWinConfirmationUI()
    {
        if(cameraManager != null) cameraManager.ForceTopDownView(true);
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
        UIManager.Instance.ShowMovementConfirmation(
            onConfirm: () => PerformWinningMove(),
            onCancel: () =>
            {
                if(cameraManager != null) cameraManager.ForceTopDownView(false);
                isPlanning = true;
                UndoLastStep();
            },
            customMessage: "¡Has encontrado la salida! ¿Quieres salir y ganar la partida?"
        );
    }

    // Acciones para el canvas de confirmación para atacar
    private void ShowAttackConfirmationUI(ExplorerPlayer victim)
    {
        if(cameraManager != null) cameraManager.ForceTopDownView(true);
        if(AudioManager.Instance != null) AudioManager.Instance.playConfirmAttack();
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

    // Acciones para cuando gana un jugador
    private void PerformWinningMove()
    {
        if(cameraManager != null && cameraManager.IsMapModeActive) cameraManager.ToggleMapUI();
        if(cameraManager != null) cameraManager.ForceTopDownView(false);

        isPlanning = false;
        GameManager.Instance.currentState = GameState.Moving;
        ClearPhantoms();

        GameManager.Instance.playerIsHeadingToWin = true;
        this.currentPlayer.Move(new List<Vector3>(this.pathPositions), GameManager.Instance.EndMovementState);
        this.pathPositions.Clear();
    }

    // Acciones para cuando el minotauro ataca
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
            this.pathPositions.Clear();
            stepsRemaining = 0;

            GameManager.Instance.currentState = GameState.ResolvingTurn;
            GameManager.Instance.EndMovementState();
        };

        if(currentMinotaurPlayer != null)
        {
            // Corrutina de ataque del minotauro
            currentMinotaurPlayer.BeginAttackSequence(approachPath, victim.transform, onAttackFinishedCallback, this);
        }
        else onAttackFinishedCallback.Invoke();
    }

    // Confirmación del movimiento
    public void ConfirmMovement()
    {
        if(stepsRemaining > 0 && stepsRemaining != totalStepsAvailable) return;

        if(cameraManager != null) cameraManager.ForceTopDownView(true);

        int doorsCrossed = CounterDoorsInPath(); // Si el jugador cruzo puertas
        
        // Acción para cancelar 
        Action onCancelCommon = () =>
        {
            if(cameraManager != null) cameraManager.ForceTopDownView(false);    
        };

        // Acción para confirmar
        Action onFinalConfirm = () =>
        {
            if(doorsCrossed > 0 && currentExplorer != null)
            {
                // Si el jugador cruzo puertas, reduce su contador de llaves
                for(int i = 0; i < doorsCrossed; i++) currentExplorer.TryRemoveKey();
            }
            ProceedWithMovement(); // Reproduce el movimiento
        };

        Action showMovementPopup = () =>
        {
            string customMsg = (stepsRemaining == totalStepsAvailable) 
            ? "¿Estás seguro de terminar tu turno sin moverte?"
            : null;

            UIManager.Instance.ShowMovementConfirmation(onFinalConfirm, onCancelCommon, customMsg);
        };

        // Si el jugador cruzo puertas, te muestra un mensaje para preguntar si estás seguro
        // de gastar llaves
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

    // Cuenta las puertas que un jugador cruzo en un turno
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

    // Movimiento normal de camninar
    private void ProceedWithMovement()
    {
        if(cameraManager != null && cameraManager.IsMapModeActive)
        {
            cameraManager.ToggleMapUI();
        }

        isPlanning = false;
        GameManager.Instance.currentState = GameState.Moving;
        ClearPhantoms();

        GameManager.Instance.playerIsHeadingToWin = false;

        if(this.pathPositions.Count == 0)
        {
            GameManager.Instance.NexTurn();
        }
        else
        {
            // Reproduce la animación de movimiento
            this.currentPlayer.Move(new List<Vector3>(this.pathPositions), GameManager.Instance.EndMovementState);
            this.pathPositions.Clear();
        }

    }

    // Deshace el último movimiento 
    private void UndoLastStep()
    {
        if(pathPositions.Count == 0 || phantoms.Count == 0) return;
        int lastIndex = phantoms.Count - 1;

        Destroy(phantoms[lastIndex]); // Destruye el fantasma
        phantoms.RemoveAt(lastIndex); // Elimina la referencia del fantasma de la lista

        pathPositions.RemoveAt(lastIndex); // Elimina la última posición de la lista

        stepsRemaining++; // Aumenta el contador de pasos disponibles

        if(pathPositions.Count > 0)
        {
            this.currentPhantomPos = pathPositions.Last();
            OnPhantomTargetChanged?.Invoke(phantoms.Last().transform);
        }
        else
        {
            this.currentPhantomPos = this.initialPlanningPos;
            OnPhantomTargetChanged?.Invoke(currentPlayer.transform);

            if (isControllingMinotaur) isFirstMinotaurStep = IsInWinningZone(this.initialPlanningPos);
        }

        if(AudioManager.Instance != null) AudioManager.Instance.playToBack();

        UpdatePhantomPulsing();
        OnStepsChanged?.Invoke(stepsRemaining, totalStepsAvailable);
    }

    // Obtiene el centro de la casilla, respecto al grid
    private Vector3 GetTileCenterFlat(Vector3 position)
    {
        float snappedX = Mathf.Floor(position.x / gridSize) * gridSize + (gridSize / 2f);
        float snappedZ = Mathf.Floor(position.z / gridSize) * gridSize + (gridSize / 2f);
        return new Vector3(snappedX, 0f, snappedZ);
    }

    // Forza el target de la cámara
    public void ForceCameraFocus(Transform target)
    {
        OnFocusUnitChanged?.Invoke(target);
    }

    // Actualiza el color y pulso del último fantasma
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

    // Limpia la lista de fantasmas
    private void ClearPhantoms()
    {
        foreach(GameObject p in phantoms)
        {
            if(p != null) Destroy(p);
        }
        phantoms.Clear();
    }

    // CPU
    public void SimulateCPUStep(Vector3 targetPos, Vector3 direction)
    {
        // Calcula la rotación
        Quaternion phantomRotation = Quaternion.LookRotation(direction, Vector3.up);

        // Lógica de victoria
        if (!isControllingMinotaur && BoardManager.Instance != null && BoardManager.Instance.isWinningTile(targetPos))
        {
            PlaceWinStep(targetPos, direction);
            return;
        }

        // Lógica de ataque del minotauro
        if (isControllingMinotaur)
        {
            Vector3 targetTileCenter = GetTileCenterFlat(targetPos);
            ExplorerPlayer[] explorers = FindObjectsByType<ExplorerPlayer>(FindObjectsSortMode.None);
            foreach (var explorer in explorers)
            {
                Vector3 explorerPos = GetTileCenterFlat(explorer.transform.position);
                if (Vector3.Distance(targetTileCenter, explorerPos) < 0.01f)
                {
                    PlaceAttackStep(targetPos, direction, explorer);
                    return;
                }
            }
        }

        // Lógica de un paso normal
        PlaceNormalStep(targetPos, direction);
    }
}
