using UnityEngine;
using System;
using System.Collections.Generic;

public class MazeInputController : MonoBehaviour
{
    [Header("Settings")]
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

    public event Action<bool> OnSelectionModeActive;
    public event Action<bool> OnObjectSelected;

    void Start()
    {
        if(BoardManager.Instance != null) tileSize = BoardManager.Instance.tileSize;
        else tileSize = 2f;

        if(GameManager.Instance != null) GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;     
    }

    void OnDestroy()
    {
        if(GameManager.Instance != null) GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if(state == GameState.MoveWall) InitializeMoveState();
        else ExitMoveState();
    }

    private void InitializeMoveState()
    {
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(true);
        OnSelectionModeActive?.Invoke(true);
        isSelectionModeActive = true;
        selectedOcuppant = null;
    }

    private void ExitMoveState()
    {
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);
        DestroyGhost();

        OnSelectionModeActive?.Invoke(false);
        OnObjectSelected?.Invoke(false);

        isSelectionModeActive = false;
        selectedOcuppant = null;
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

        if(UIPauseManager.Instace.isPaused) return;

        if(selectedOcuppant == null)
        {
            HandleSelection();
        }
        else
        {
            HandleMovement();
            HandleRotation();
            HandleConfirmation();
            HandleCancellation();
        }
    }

    private void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, 100f, movableLayer))
            {
                GridOccupant occupant = hit.collider.GetComponentInParent<GridOccupant>();
                if(occupant != null && occupant.isMovable) SelectedObject(occupant);
            }
        }
    }

    private void SelectedObject(GridOccupant occupant)
    {
        selectedOcuppant = occupant;

        originalPosition = occupant.transform.position;
        originalRotation = occupant.transform.rotation;

        CreateGhost(occupant);

        // Temporary unregister because avoid autocollisions
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
        currentGhost = new GameObject($"{original.name}_Ghost");
        currentGhost.transform.position = original.transform.position;
        currentGhost.transform.rotation = original.transform.rotation;
        currentGhost.transform.localScale = original.transform.localScale;

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

    private void HandleMovement()
    {
        Vector3 currentPos = selectedOcuppant.transform.position;
        Vector3 targetPos = currentPos;
        bool inputDetected = false;

        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)){ targetPos.z += tileSize; inputDetected = true; }
        if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)){ targetPos.z -= tileSize; inputDetected = true;}
        if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)){ targetPos.x += tileSize; inputDetected = true;}
        if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)){ targetPos.x -= tileSize; inputDetected = true;}

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

    private void HandleRotation()
    {
        bool isRotated = false;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(selectedOcuppant.baseSize.x == 1 && selectedOcuppant.baseSize.y == 1)
            {
                if(AudioManager.Instance != null) AudioManager.Instance.playToError();
                return;
            }

            selectedOcuppant.transform.Rotate(0, 90, 0);
            isRotated = true;

            List<Vector3> potentialPoints = selectedOcuppant.GetOccupiedWorldCenters();
            
            if (!BoardManager.Instance.ArePointWithinBounds(potentialPoints))
            {
                selectedOcuppant.transform.Rotate(0, -90, 0);
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

    private void UpdateVisualFeedback()
    {
        List<Vector3> currentPoints = selectedOcuppant.GetOccupiedWorldCenters();
        bool isValid = BoardManager.Instance.IsAreaFree(currentPoints, selectedOcuppant);
        selectedOcuppant.SetMoveFeedbackState(isValid);
    }

    private void HandleConfirmation()
    {
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            List<Vector3> currentPoints = selectedOcuppant.GetOccupiedWorldCenters();
            bool isValid = BoardManager.Instance.IsAreaFree(currentPoints, selectedOcuppant);

            if (isValid)
            {
                if(UIManager.Instance != null) 
                    UIManager.Instance.ShowWallMoveConfirmation(ConfirmMove, null);
                else ConfirmMove();
            }
            else if(AudioManager.Instance != null) AudioManager.Instance.playToError();
        }
    }

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
        if(Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1))
        {
            CancelMove();
        }
    }

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
        selectedOcuppant = null;

        if(AudioManager.Instance != null) AudioManager.Instance.playToBack();

        OnObjectSelected?.Invoke(false);
        OnSelectionModeActive?.Invoke(true);

        isSelectionModeActive = true;
    }
}