using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance {get; private set;}

    public bool isUsingMouseInput {get; private set;} = false;
    public float lastSubmitTime {get; private set;}

    public bool IsUpPressed {get; private set;}
    public bool IsDownPressed {get; private set;}
    public bool IsRightPressed {get; private set;}
    public bool IsLeftPressed {get; private set;}
    public bool IsConfirmPressed {get; private set;}
    public bool IsCancelPressed {get; private set;}
    public bool IsRotatePressed {get; private set;}
    public bool IsPausePressed {get; private set;}
    public bool IsToggleMapPressed {get; private set;}
    public bool IsToggleFreeCamPressed {get; private set;}

    public float MoveAxisX {get; private set;}
    public float MoveAxisY {get; private set;}

    private bool axisInUseHorizontal = false;
    private bool axisInUseVertical = false;

    private float lastMouseTime;
    private float lastKeyboardTime;
    private Vector3 lastMousePosition;

    private bool isTurnCPU = false;
    private GameState currentGameState = GameState.Intro;

    private InputAction submitAction;
    private InputAction cancelAction;

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);

        submitAction = new InputAction(binding: "*/{Submit}");
        cancelAction = new InputAction(binding: "*/{Cancel}");

        submitAction.Enable();
        cancelAction.Enable();
    }

    private void Start()
    {
        lastMousePosition = Input.mousePosition;

        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
        }

        if(CameraManager.Instance != null)
        {
            CameraManager.Instance.OnCameraModeChanged += HandleCameraModeChanged;
        }
    }

    private void OnDestroy()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
        }

        if(CameraManager.Instance != null)
        {
            CameraManager.Instance.OnCameraModeChanged -= HandleCameraModeChanged;
        }

        if(submitAction != null)
        {
            submitAction.Disable();
            submitAction.Dispose();
        }

        if(cancelAction != null)
        {
            cancelAction.Disable();
            cancelAction.Dispose();
        }
    }

    
    void Update()
    {
        DetectInputMethod();
        UpdateCursorVisibility();
        ProcessGameInputs();
    }

    private void ProcessGameInputs()
    {
        // Se resetea el valor en cada frame
        IsUpPressed = false;
        IsDownPressed = false;
        IsRightPressed = false;
        IsLeftPressed = false;
        IsConfirmPressed = false;
        IsCancelPressed = false;
        IsRotatePressed = false;
        IsPausePressed = false;
        IsToggleMapPressed = false;
        IsToggleFreeCamPressed = false;

        // Teclado (Discreto)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) IsUpPressed = true;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) IsDownPressed = true;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) IsRightPressed = true;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) IsLeftPressed = true;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) IsConfirmPressed = true;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1)) IsCancelPressed = true;
        if (Input.GetKeyDown(KeyCode.Space)) IsRotatePressed = true;
        if (Input.GetKeyDown(KeyCode.Q)) IsToggleMapPressed = true;
        if (Input.GetKeyDown(KeyCode.E)) IsToggleFreeCamPressed = true;
        if (Input.GetKeyDown(KeyCode.Escape)) IsPausePressed = true;

        // Teclado continuo
        float rawX = 0f;
        float rawY = 0f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) rawX = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) rawX = -1f;
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) rawY = 1f;
        else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) rawY = -1f;

        if(submitAction.triggered) IsConfirmPressed = true;
        if(cancelAction.triggered) IsCancelPressed = true;

        Gamepad activeGamepad = Gamepad.current;

        float h = 0f;
        float v = 0f;

        if(activeGamepad != null)
        {
            Vector2 stick = activeGamepad.leftStick.ReadValue();
            Vector2 dpad = activeGamepad.dpad.ReadValue();

            h = Mathf.Abs(dpad.x) > Mathf.Abs(stick.x) ? dpad.x : stick.x;
            v = Mathf.Abs(dpad.y) > Mathf.Abs(stick.y) ? dpad.y : stick.y;

            if (activeGamepad.buttonNorth.wasPressedThisFrame) IsRotatePressed = true;
            if (activeGamepad.startButton.wasPressedThisFrame) IsPausePressed = true;
            if (activeGamepad.leftShoulder.wasPressedThisFrame) IsToggleMapPressed = true;  // LB / L1
            if (activeGamepad.rightShoulder.wasPressedThisFrame) IsToggleFreeCamPressed = true; // RB / R1
        }

        MoveAxisX = Mathf.Abs(h) > Mathf.Abs(rawX) ? h : rawX;
        MoveAxisY = Mathf.Abs(v) > Mathf.Abs(rawY) ? v : rawY;

        float threshold = 0.5f;

        // Vertical
        if (v > threshold) { if (!axisInUseVertical) { IsUpPressed = true; axisInUseVertical = true; } }
        else if (v < -threshold) { if (!axisInUseVertical) { IsDownPressed = true; axisInUseVertical = true; } }
        else { axisInUseVertical = false; }

        // Horizontal
        if (h > threshold) { if (!axisInUseHorizontal) { IsRightPressed = true; axisInUseHorizontal = true; } }
        else if (h < -threshold) { if (!axisInUseHorizontal) { IsLeftPressed = true; axisInUseHorizontal = true; } }
        else { axisInUseHorizontal = false; }
    }

    private void DetectInputMethod()
    {
        // Mouse
        bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        bool mouseActive = Input.GetMouseButton(0) || Input.GetMouseButtonUp(0);
        
        bool mouseMoved = false;
        if(Cursor.lockState == CursorLockMode.Locked)
        {
            mouseMoved = Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 1.0f || Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 1.0f;
        }
        else
        {
            mouseMoved = (Input.mousePosition - lastMousePosition).sqrMagnitude > 5.0f;
        }

        // Keyboard or gamepad
        bool keyboardActive = (Input.anyKeyDown && !mouseClicked) || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        
        bool gamepadActive = false;
        Gamepad activeGamepad = Gamepad.current;
        if(activeGamepad != null)
        {
            gamepadActive = activeGamepad.wasUpdatedThisFrame;
        }

        // Priority
        if (mouseMoved || mouseClicked) 
        {
            isUsingMouseInput = true;
            lastMouseTime = Time.unscaledTime;
        }
        else if (keyboardActive || (gamepadActive && Time.unscaledTime - lastMouseTime > 3f))
        {
            isUsingMouseInput = false;
            lastKeyboardTime = Time.unscaledTime;
        }
        
        if(mouseActive || IsConfirmPressed)
        {
            lastSubmitTime = Time.unscaledTime;
        }

        lastMousePosition = Input.mousePosition;
    }

    private void HandleGameStateChanged(GameState state)
    {
        currentGameState = state;
        UpdateCursorVisibility();
    }

    private void HandleTurnChanged(Player currentPlayer)
    {
        if(currentPlayer != null) isTurnCPU = currentPlayer.IsCPU;
        UpdateCursorVisibility();
    }

    private void HandleCameraModeChanged()
    {
        UpdateCursorVisibility();
    }

    private void UpdateCursorVisibility()
    {
        bool isPaused = UIPauseManager.Instance != null && UIPauseManager.Instance.isPaused;
        CursorLockMode cursorLocked = CursorLockMode.Locked;
        CursorLockMode cursorNone = CursorLockMode.None;

        if (!isUsingMouseInput)
        {
            SetCursorState(false, cursorLocked);
            return;
        }

        if (isPaused)
        {
            SetCursorState(true, cursorNone);
            return;
        }

        if (currentGameState == GameState.GameOver)
        {
            SetCursorState(true, cursorNone);
            return;
        }

        if(isTurnCPU)
        {
            SetCursorState(false, cursorLocked);
            return;
        }

        bool forceHideCursor = false;

        if(currentGameState == GameState.WaitingForRoll && CameraManager.Instance != null)
        {
            if(CameraManager.Instance.IsMapModeActive || CameraManager.Instance.IsFreeRoamActive)
            {
                forceHideCursor = true;
            }
        }

        if(currentGameState == GameState.TurnPlanning || currentGameState == GameState.FreeRoam)
        {
            forceHideCursor = true;
        }

        if (forceHideCursor)
        {
            SetCursorState(false, cursorLocked);
        }
        else
        {
            SetCursorState(true, cursorNone);
        }

    }

    private void SetCursorState(bool visible, CursorLockMode lockMode)
    {
        if(Cursor.visible != visible) Cursor.visible = visible;
        if(Cursor.lockState != lockMode) Cursor.lockState = lockMode;
    }
}
