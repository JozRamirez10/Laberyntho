using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// Controla los movimientos para cuando el jugador debe mover algún muro
public class MazeInputController : MonoBehaviour
{
    [Header("Movable Layer Mask")]
    public LayerMask movableLayer;

    [Header("Visual Feedback")]
    public Material ghostMaterial;
    private GameObject currentGhost;

    private bool isSelectionModeActive = false;

    private GridOccupant selectedOcuppant;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isDragginMouse = false;
    private Plane dragPlane;
    private Vector3 dragOffset; 
    
    private float tileSize;
    private float liftHeight = 0.5f;

    private List<GridOccupant> selectableWalls = new List<GridOccupant>();
    private GridOccupant hoveredOccupant = null;

    public event Action<bool> OnSelectionModeActive;
    public event Action<bool> OnObjectSelected;

    void Start()
    {
        // Configura el grid del tablero
        if(BoardManager.Instance != null) tileSize = BoardManager.Instance.tileSize;
        else tileSize = 2f;

        // Configura el evento para cuando cambia el estado del juego
        if(GameManager.Instance != null) GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;     
    }

    void OnDestroy()
    {
        if(GameManager.Instance != null) GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        // Si el estado del juego es para mover un muro, inicializa el movimiento
        if(state == GameState.MoveWall) InitializeMoveState();
        else ExitMoveState();
    }

    private void InitializeMoveState()
    {
        // Enciende el shader de selección en todos los muros
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(true);

        RefreshSelectableWalls();

        OnSelectionModeActive?.Invoke(true);
        isSelectionModeActive = true;
        selectedOcuppant = null;

        if (GameManager.Instance != null && GameManager.Instance.isTurnCPU) return;

        if(selectableWalls.Count > 0)
        {
            StartCoroutine(InitialWarpRoutine());
        } 
    }

    private IEnumerator InitialWarpRoutine()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        GridOccupant centerWall = GetCenterMostWall();
        if(centerWall != null) SetHoveredOccupant(centerWall, true);
    }

    private GridOccupant GetCenterMostWall()
    {
        GridOccupant bestMatch = null;
        float minDistance = float.MaxValue;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        foreach(var wall in selectableWalls)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(wall.transform.position);

            if(screenPos.z < 0) continue;

            Vector2 wallScreenPos = new Vector2(screenPos.x, screenPos.y);

            float dist = Vector2.Distance(screenCenter, wallScreenPos);
            if(dist < minDistance)
            {
                minDistance = dist;
                bestMatch = wall;
            }
        }
        return bestMatch;
    }

    private void ExitMoveState()
    {
        // Apaga el shader de selección
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);
        DestroyGhost(); // Destruye el fantasma del muro que toma la posición inicial del muro

        if(hoveredOccupant != null) hoveredOccupant.SetHoverState(false);

        OnSelectionModeActive?.Invoke(false);
        OnObjectSelected?.Invoke(false);

        isSelectionModeActive = false;
        selectedOcuppant = null;
        hoveredOccupant = null;
    }

    void OnEnable()
    {
        if(GameManager.Instance != null && GameManager.Instance.currentState == GameState.MoveWall)
        {
            InitializeMoveState();
        }
    }

    void OnDisable()
    {
        ExitMoveState();
    }

    void Update()
    {

        if(UIManager.Instance != null && UIManager.Instance.IsConfirmationPopupActive) return;

        if(GameManager.Instance.currentState != GameState.MoveWall) return;

        if (UIPauseManager.Instance.isPaused) return;

        if(GameManager.Instance != null && GameManager.Instance.isTurnCPU) return;

        if(selectedOcuppant == null)
        {
            HandleSelection(); // Obliga al jugador a seleccionar un muro
        }
        else
        {
            // Controla los movimientos del muro
            HandleMovement();
            HandleRotation();
            HandleConfirmation();
            HandleCancellation();
        }
    }

    // Construye una lista con todos los muros seleccionables
    private void RefreshSelectableWalls()
    {
        selectableWalls.Clear();
        GridOccupant[] allOcuppants = FindObjectsByType<GridOccupant>(FindObjectsSortMode.None);
        foreach(var occ in allOcuppants)
        {
            if(occ.isMovable && occ.gameObject.activeInHierarchy) selectableWalls.Add(occ);
        }
    }

    // Actualiza el hover y transporta el cursor
    private void SetHoveredOccupant(GridOccupant hover, bool warpCursor = false)
    {
        bool isNewHover = (hoveredOccupant != hover);

        if(hoveredOccupant != null && isNewHover) hoveredOccupant.SetHoverState(false);
        
        hoveredOccupant = hover;

        if(hoveredOccupant != null)
        {
            if(isNewHover && AudioManager.Instance != null) AudioManager.Instance.playToSelect();
            if(isNewHover) hoveredOccupant.SetHoverState(true);
        }
    }

    // Selecciona el muro dependiendo de la dirección
    private GridOccupant GetNextWallInDirection(Vector3 direction)
    {
        GridOccupant bestMatch = null;
        float minDistance = float.MaxValue;

        foreach(var wall in selectableWalls)
        {
            if(wall == hoveredOccupant || wall == null) continue;

            Vector3 dirToWall = (wall.transform.position - hoveredOccupant.transform.position).normalized;
            float dot = Vector3.Dot(direction, dirToWall);

            // Si esta aproximadamente en la dirección que el jugador presiono
            if(dot > 0.5f)
            {
                float dist = Vector3.Distance(hoveredOccupant.transform.position, wall.transform.position);
                if(dist < minDistance)
                {
                    minDistance = dist;
                    bestMatch = wall;
                }
            }
        }
        return bestMatch;
    }

    // Selección del muro
    private void HandleSelection()
    {
        if (InputManager.Instance.isUsingMouseInput)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit overHit, 100f, movableLayer))
            {
                GridOccupant occupant = overHit.collider.GetComponentInParent<GridOccupant>();
                
                if(occupant != null && occupant.isMovable) SetHoveredOccupant(occupant, false);
                else SetHoveredOccupant(null, false);
            }
        }
        
        // Selección con teclas
        Vector3 inputDir = Vector3.zero;
        if (InputManager.Instance.IsUpPressed) inputDir = Vector3.forward;
        if (InputManager.Instance.IsDownPressed) inputDir = Vector3.back;
        if (InputManager.Instance.IsRightPressed) inputDir = Vector3.right;
        if (InputManager.Instance.IsLeftPressed) inputDir = Vector3.left;

        if(inputDir != Vector3.zero)
        {
            if(hoveredOccupant != null)
            {
                GridOccupant nextWall = GetNextWallInDirection(inputDir);
                if(nextWall != null) SetHoveredOccupant(nextWall, true);
            }
            else
            {
                GridOccupant centerWall = GetCenterMostWall();
                if(centerWall != null) SetHoveredOccupant(centerWall, true);
            }

        }

        if(InputManager.Instance.IsConfirmPressed)
        {
            if(hoveredOccupant != null)
            {
                SelectedObject(hoveredOccupant);
                return;
            }
        }

        // Selección con el mouse
        if (Input.GetMouseButtonDown(0))
        {
            Ray clickRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(clickRay, out RaycastHit hit, 100f, movableLayer))
            {
                GridOccupant occupant = hit.collider.GetComponentInParent<GridOccupant>();
                if(occupant != null && occupant.isMovable) SelectedObject(occupant);
            }
        }
    }

    private void SelectedObject(GridOccupant occupant)
    {
        // Limpia el efecto de selección antes de guardar la posición original
        if(hoveredOccupant != null) hoveredOccupant.SetHoverState(false);
        hoveredOccupant = null;

        selectedOcuppant = occupant;

        // Guarda la posición y rotación original
        originalPosition = occupant.transform.position;
        originalRotation = occupant.transform.rotation;

        CreateGhost(occupant); // Crea un fantasma del muro 

        // Temporalmente desregistra al bloque del board para no preocuparse por colliders
        BoardManager.Instance.UnregisterOccupancy(occupant);
        selectedOcuppant.transform.position += Vector3.up * liftHeight;

        UpdateVisualFeedback();

        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);

        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();

        OnSelectionModeActive?.Invoke(false);
        OnObjectSelected?.Invoke(true);

        isSelectionModeActive = false;
    }

    private void CreateGhost(GridOccupant original)
    {
        // Instancia un fantasma del muro con su posición y rotación original
        currentGhost = new GameObject($"{original.name}_Ghost");
        currentGhost.transform.position = original.transform.position;
        currentGhost.transform.rotation = original.transform.rotation;
        currentGhost.transform.localScale = original.transform.localScale;

        // Modifica el material del fanstasma
        MeshFilter originalFilter = original.GetComponentInChildren<MeshFilter>();
        if(originalFilter != null)
        {
            MeshFilter ghostFilter = currentGhost.AddComponent<MeshFilter>();
            ghostFilter.sharedMesh = originalFilter.sharedMesh;

            MeshRenderer ghostRenderer = currentGhost.AddComponent<MeshRenderer>();
            ghostRenderer.material = ghostMaterial;
        }
    }

    private void DestroyGhost()
    {
        if(currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
    }

    // Controla el movimiento del muro
    private void HandleMovement()
    {
        Vector3 currentPos = selectedOcuppant.transform.position;
        Vector3 targetPos = currentPos;
        bool inputDetected = false;

        if(InputManager.Instance.IsUpPressed){ targetPos.z += tileSize; inputDetected = true; }
        if(InputManager.Instance.IsDownPressed){ targetPos.z -= tileSize; inputDetected = true;}
        if(InputManager.Instance.IsRightPressed){ targetPos.x += tileSize; inputDetected = true;}
        if(InputManager.Instance.IsLeftPressed){ targetPos.x -= tileSize; inputDetected = true;}

        // Mueve el muro a dónde des click con el mouse
        if (Input.GetMouseButtonDown(0))
        {
            dragPlane = new Plane(Vector3.up, selectedOcuppant.transform.position);
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                dragOffset = selectedOcuppant.transform.position - hitPoint;
            }
            isDragginMouse = true;
        }

        // Mueve el muro arrastrando el mouse
        if(Input.GetMouseButton(0) && isDragginMouse)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(dragPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                Vector3 rawPos = hitPoint + dragOffset;
                
                // Snap to grid
                float snapX = Mathf.Round(hitPoint.x / tileSize) * tileSize;
                float snapZ = Mathf.Round(hitPoint.z / tileSize) * tileSize;

                Vector3 potentialTarget = new Vector3(snapX, originalPosition.y + liftHeight, snapZ);

                if(potentialTarget != currentPos)
                {
                    targetPos = potentialTarget;
                    inputDetected = true;
                } 
            }
        }

        if(Input.GetMouseButtonUp(0)) isDragginMouse = false;

        // Mueve el muro con las flechas del teclado
        if (inputDetected)
        {
            Vector3 previousPos = selectedOcuppant.transform.position;
            selectedOcuppant.transform.position = targetPos;

            List<Vector3> potentialPoints = selectedOcuppant.GetOccupiedWorldCenters();
            
            if(!BoardManager.Instance.ArePointWithinBounds(potentialPoints)) selectedOcuppant.transform.position = previousPos;

            if(selectedOcuppant.transform.position != previousPos)
            {
                if(AudioManager.Instance != null) AudioManager.Instance.playToSelect();
            }

            UpdateVisualFeedback();
        }
    }

    // Rota el muro
    private void HandleRotation()
    {
        bool isRotated = false;
        if (InputManager.Instance.IsRotatePressed) 
        {
            // No rota muros de 1x1
            if(selectedOcuppant.baseSize.x == 1 && selectedOcuppant.baseSize.y == 1)
            {
                if(AudioManager.Instance != null) AudioManager.Instance.playToError();
                return;
            }

            // Rotación
            selectedOcuppant.transform.Rotate(0, 90, 0);
            isRotated = true;

            List<Vector3> potentialPoints = selectedOcuppant.GetOccupiedWorldCenters();
            
            // Si la rotación se sale del límite del tablero
            if (!BoardManager.Instance.ArePointWithinBounds(potentialPoints))
            {
                selectedOcuppant.transform.Rotate(0, -90, 0); // Regresa a la rotación original
                isRotated = false;
            }

            if(AudioManager.Instance != null)
            {
                if(isRotated) AudioManager.Instance.playRotateWall();
                else AudioManager.Instance.playToError();
            }

            UpdateVisualFeedback();
        }
    }

    // Si es una posición disponible, el muro se pinta de verde
    // Si es una posición no disponible, el muro se pinta de rojo
    private void UpdateVisualFeedback()
    {
        List<Vector3> currentPoints = selectedOcuppant.GetOccupiedWorldCenters();
        bool isValid = BoardManager.Instance.IsAreaFree(currentPoints, selectedOcuppant);
        selectedOcuppant.SetMoveFeedbackState(isValid);
    }

    // Valida que el muro este en una posición disponible
    //  para colocarlo en ese lugar y lanza el canvas de configuración
    private void HandleConfirmation()
    {
        if(InputManager.Instance.IsConfirmPressed)
        {
            if(UIManager.Instance != null && UIManager.Instance.IsConfirmationPopupActive) return;

            List<Vector3> currentPoints = selectedOcuppant.GetOccupiedWorldCenters();
            bool isValid = BoardManager.Instance.IsAreaFree(currentPoints, selectedOcuppant);

            if (isValid)
            {
                if(UIManager.Instance != null) 
                    UIManager.Instance.ShowWallMoveConfirmation(ConfirmMove, null);
                else ConfirmMove();

                if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
            }
            else if(AudioManager.Instance != null) AudioManager.Instance.playToError();
        }
    }

    // Coloca el muro en la nueva posición y activa los shaders 
    private void ConfirmMove()
    {
        DestroyGhost();

        Vector3 finalPos = selectedOcuppant.transform.position;
        finalPos.y = originalPosition.y;
        selectedOcuppant.transform.position = finalPos;

        selectedOcuppant.RestoreVisuals();
        BoardManager.Instance.RegisterOccupancy(selectedOcuppant);

        selectedOcuppant.SetDissolveValule(0f);
        selectedOcuppant.StartCoroutine(selectedOcuppant.AppearRoutine(1.2f));

        selectedOcuppant = null;

        if(AudioManager.Instance != null) AudioManager.Instance.playSetWall();

        OnObjectSelected?.Invoke(false);
        OnSelectionModeActive?.Invoke(false);

        GameManager.Instance.EndMovementState();
    }

    private void HandleCancellation()
    {
        if(InputManager.Instance.IsCancelPressed)
        {
            CancelMove();
        }
    }

    // Al cancelar el movimiento, el muro regresa a su posición original y 
    // todos los muros vuelven a estado de selección
    private void CancelMove()
    {
        DestroyGhost();

        selectedOcuppant.transform.position = originalPosition;
        selectedOcuppant.transform.rotation = originalRotation;

        selectedOcuppant.RestoreVisuals();

        if(BoardManager.Instance != null)
        {
            BoardManager.Instance.RegisterOccupancy(selectedOcuppant);
            BoardManager.Instance.ToggleHighlightMovableObjects(true);
        }
        GridOccupant canceledOccupant = selectedOcuppant;
        selectedOcuppant = null;

        RefreshSelectableWalls();
        SetHoveredOccupant(canceledOccupant, true);

        if(AudioManager.Instance != null) AudioManager.Instance.playToBack();

        OnObjectSelected?.Invoke(false);
        OnSelectionModeActive?.Invoke(true);

        isSelectionModeActive = true;
    }
}