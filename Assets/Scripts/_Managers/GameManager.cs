using UnityEngine;
using UnityEngine.Playables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}

    [Header("Setup Data")]
    public GameplaySetupSO gameplaySetupSO;
    public GameSettingsSO gameSettingsSO;

    [Header("Timeline Settings")]
    public PlayableDirector introDirector;
    public CanvasGroup blackScreenFader;

    [Header("Controllers")]
    public TurnInputController inputController;
    public MazeInputController mazeInputController;
    public DiceController diceController;
    
    [Header("Camera")]
    public CameraManager cameraManager;

    public Player[] playersTemplate = new Player[4]; // Debug 
    public List<Player> players = new List<Player>();
    public int totalTurnCount = 0;

    public MinotaurPlayer minotaurInstance;

    private List<DoorController> allDoors = new List<DoorController>();

    public int currenPlayerIndex = 0; // Index del jugador actual
    public bool isTurnCPU = false;
    public bool isMinotaurTurn = false;

    public bool playerIsHeadingToWin = false;

    public event Action<Player> OnTurnChanged; // Evento cuando cambia el turno

    public event Action<GameState> OnGameStateChanged; // Evento cuando cambia el estado del juego

    private GameState _currentState;

    public GameState currentState
    {
        get {return _currentState;}
        set
        {
            if(_currentState != value)
            {
                _currentState = value;
                Debug.Log($"Game State cambiado a {_currentState}");
                OnGameStateChanged?.Invoke(_currentState);
                HandleGameStateChangeInternal(_currentState);
            }
        }
    }

    private void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnDestroy()
    {
        OnTurnChanged = null;
        OnGameStateChanged = null;
        if(Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        Time.timeScale = gameSettingsSO.speedGame;

        LevelDataLoader loader = GetComponent<LevelDataLoader>();
        if(loader != null) loader.LoadLevelData(); // Carga las posiciones de los jugadores y el minotauro

        if(cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();

        // Obtiene la instancia del minotauro
        if(minotaurInstance == null) minotaurInstance = FindFirstObjectByType<MinotaurPlayer>();

        currentState = GameState.Intro;
        Setup(); // Configura la escena

        // StartCoroutine(StartGameWithDelay()); // Elimina la cinématica inicial - Debug

        if(introDirector != null) introDirector.Play();
        else StartGameFromTimelineSignal();
    }

    private IEnumerator StartGameWithDelay()
    {
        yield return null;
        StartGameFromTimelineSignal();
        
        if(gameplaySetupSO != null) gameplaySetupSO.ResetData();
        
    }

    public void StartGameFromTimelineSignal()
    {
        NexTurn();
    }

    private void Setup()
    {
        string currentSceneName = SceneManager.GetActiveScene().name; // Nombre de la escena actual

        if(currentSceneName == Scenes.RANDOM)
        {
            ProceduralMazeGenerator generator = FindFirstObjectByType<ProceduralMazeGenerator>();
            if(generator != null) generator.GenerateFullMaze(); // Genera laberinto aleatorio
        }

        // Configura el board para las casillas ganadoras
        if(CPUController.Instance != null) CPUController.Instance.InitializeWinningPositions();

        // Juego sin Scriptables Objects (Debug) - Instancia a los jugadores
        if(gameplaySetupSO == null || gameplaySetupSO.orderedPlayers.Count == 0)
        {
            List<Player> tempTemplates = new List<Player>(playersTemplate);
            tempTemplates = tempTemplates.OrderBy(x => Guid.NewGuid()).ToList();
            
            for(int i = 0 ; i < tempTemplates.Count(); i++)
            {   
                Player template = tempTemplates[i];
                Vector3 initialPos = gameplaySetupSO.loadedPlayerPositions[i];

                Player newPlayerInstance = Instantiate(template, initialPos, Quaternion.identity);
                newPlayerInstance.gameObject.name = $"{template.characterName}_Instance_{i}";

                newPlayerInstance.Initialize(initialPos, template.IsCPU);
                players.Add(newPlayerInstance);
            }
        }
        else
        {
            // Instancia a los jugadores de acuerdo a las configuraciones del menú principal
            // * Nombre y Orden
            List<GameplaySetupSO.PlayerSetupData> playersList = gameplaySetupSO.orderedPlayers;

            for(int i = 0 ; i < playersList.Count ; i++)
            {
                if( i >= gameplaySetupSO.loadedPlayerPositions.Count) break;

                GameplaySetupSO.PlayerSetupData data = playersList[i];
                Vector3 initialPos = gameplaySetupSO.loadedPlayerPositions[i];

                if(data.playerPrefab == null)
                {
                    Debug.Log($"El jugador {i} en el SO no tiene prefab asignado");
                    continue;
                }

                Player newPlayerInstance = Instantiate(data.playerPrefab, initialPos, Quaternion.identity);
                newPlayerInstance.characterName = data.playerName;
                newPlayerInstance.gameObject.name = $"{data.playerName}_Instance_{i+1}";
                
                newPlayerInstance.Initialize(initialPos, data.isCPU);
                players.Add(newPlayerInstance);
            }
        }
        
        // Coloca al Minotauro en su posición
        minotaurInstance.transform.position = gameplaySetupSO.loadedMinotaurPosition;
        
        // Crea una lista con las posiciones de todas las puertas en el tablero
        allDoors = new List<DoorController>(FindObjectsByType<DoorController>(FindObjectsSortMode.None));

        // Coloca el índice para el jugador actual antes del primero
        this.currenPlayerIndex = -1; 
        Debug.Log("Terminé de configurar la escena");
    }

    public void NexTurn()
    {
        if(players.Count == 0) return; // Valida si hay jugadores

        if(AudioManager.Instance != null) AudioManager.Instance.playStartTurn();

        totalTurnCount++;

        // Obtiene al jugador actual, siempre dentro del rango de 1 a 4
        currenPlayerIndex = (currenPlayerIndex + 1) % players.Count;

        // Obtiene la instancia del jugador actual
        Player currentPlayer = players[currenPlayerIndex];
        isTurnCPU = currentPlayer.IsCPU;

        Debug.Log($"Turno de: {currentPlayer.characterName}");

        // Si el jugador tiene llaves, las puertas se transparentan
        UpdateDoorState(currentPlayer);

        OnTurnChanged?.Invoke(currentPlayer);
        
        // Activa el efecto Glow del icono del jugador
        currentPlayer.ActiveGlow(true);

        playerIsHeadingToWin = false;
        
        currentState = GameState.WaitingForRoll;
    }

    private void UpdateDoorState(Player player)
    {  
        // Valida si el jugador tiene llaves
        bool playerHasKeys = false;
        if(player is ExplorerPlayer explorer)
        {
            playerHasKeys = explorer.GetKeyCount() > 0;
        }

        // Si el jugador tiene llaves transparenta las puertas
        foreach(var door in allDoors)
        {
            if(door != null)
            {
                door.SetTransparent(playerHasKeys);
            }
        }
    }

    public void RollDiceAction()
    {
        if(currentState != GameState.WaitingForRoll) return;

        // int diceResult = UnityEngine.Random.Range(1, 9); // 1 a 8
        
        // El juego va de 3 a 8 para agilizar movimientos
        int diceResult = UnityEngine.Random.Range(3, 10); // 3 a 9
        // diceResult = 6;

        Debug.Log($"Resultado del dado: {diceResult}");

        currentState = GameState.Rolling;

        // Corrutina de lanzar el dado
        StartCoroutine(RollDiceSequence(diceResult));
    }

    private IEnumerator RollDiceSequence(int result)
    {
        // Reproduce la animación de dado dependiendo de result
        bool animationFinished = false;
        if(diceController != null)
        {
            diceController.PlayDiceAnimation(result, () => animationFinished = true);
            while(!animationFinished) yield return null;
        }
        else yield return new WaitForSeconds(0.5f);
        
        // Muestra un mensaje al jugador con el resultado del dado
        if(UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowDiceResultRoutine(result, 3.0f));
        }

        // Acciones dependiendo del resultado
        ProcessDiceResult(result);
    }

    private void ProcessDiceResult(int diceResult)
    {
        Player currentPlayer = players[this.currenPlayerIndex];

        isMinotaurTurn = false;
        bool isWinningKey = (diceResult == 7);
        bool isMovingWall = (diceResult == 8);

        if (isWinningKey) // Ganar una llave
        {
            currentState = GameState.Rolling;

            // Se visualiza la animación de ganar llave y acumula una llave al jugador actual
            if(UIManager.Instance != null) UIManager.Instance.ShowKeyGainSequence(() => AddKey(currentPlayer));
            else AddKey(currentPlayer);
        }
        else if(isMovingWall) // Mover un bloque
        {
            currentState = GameState.MoveWall;
            currentPlayer.ActiveGlow(true);

            if (currentPlayer.IsCPU)
            {
                // Comportamiento del CPU para mover un bloque
                CPUController.Instance.StartTurn(currentPlayer, diceResult, isMinotaurTurn, EndMovementState);
            }
            else
            {
                // Inicializa el comportamiento para mover un bloque (Ilumina todos los bloques para que selecciones uno)
                if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(true);
            }
        }
        else
        {
            isMinotaurTurn = (diceResult == 9); // Movimiento del minotauro

            Player playerToMove = currentPlayer;
            currentState = GameState.TurnPlanning;

            if(isMinotaurTurn && minotaurInstance != null)
            {
                playerToMove = minotaurInstance;
                currentPlayer.ActiveGlow(false);
                minotaurInstance.ActiveGlow(true);
                diceResult = 5; // El minotauro se mueve 5 posiciones
                ResetAllDoorsToOpaque(); // Todas las puertas se muestran sin transparentarse

                // El target de la cámara es el Minotauro
                if(cameraManager != null) cameraManager.SetCameraTarget(minotaurInstance.transform);
            }
            else playerToMove.ActiveGlow(true);
            
            if (currentPlayer.IsCPU)
            {
                // Comportamiento del CPU si es minotauro
                CPUController.Instance.StartTurn(playerToMove, diceResult, isMinotaurTurn, EndMovementState);
            }
            else
            {
                // Activa el input controller para que el jugador se mueva
                inputController.StartTurnPlanning(playerToMove, diceResult, isMinotaurTurn);
            }
        }
    }
    
    // Añade una llave al jugador que recibe como parámetro y le permite moverse 3 espacios
    private void AddKey(Player currentPlayer)
    {
        ExplorerPlayer explorer = currentPlayer as ExplorerPlayer;
        if(explorer != null) explorer.AddKey();

        currentState = GameState.TurnPlanning;
        currentPlayer.ActiveGlow(true);

        if (currentPlayer.IsCPU) CPUController.Instance.StartTurn(currentPlayer, 3, isMinotaurTurn, EndMovementState);
        else inputController.StartTurnPlanning(currentPlayer, 3, isMinotaurTurn);
    }

    // Secuencia para terminar el turno
    public void EndMovementState()
    {
        StartCoroutine(ResolveTurnSequence());
    }

    // Secuencia para terminar el turno
    private IEnumerator ResolveTurnSequence()
    {
        float timeEndTurn = 1.5f;

        currentState = GameState.ResolvingTurn;

        List<Coroutine> closingAnimations = new List<Coroutine>();
        bool anyDoorWasOpen = false;

        // Valida si las puertas se han abierto, si no hay algún jugador obstruyendo, la puerta se cierra
        // Si el jugador esta obstruyendo, la puerta se mantiene abierta
        foreach(var door in allDoors)
        {
            if(door != null && door.isOpen)
            {
                Vector3 doorPosFlat = new Vector3(door.transform.position.x, 0, door.transform.position.z);
                bool isOccupied = false;

                foreach(var p in players)
                {
                    if(p == null) continue;
                    Vector3 pPos = new Vector3(p.transform.position.x, 0, p.transform.position.z);

                    // Si el jugador esta en la misma posición que la puerta
                    if(Vector3.Distance(pPos, doorPosFlat) < 0.9f)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if(!isOccupied && minotaurInstance != null)
                {
                    Vector3 mPos = new Vector3(minotaurInstance.transform.position.x, 0, minotaurInstance.transform.position.z);
                    if(Vector3.Distance(mPos, doorPosFlat) < 0.9f) isOccupied = true;
                }

                if (!isOccupied)
                {
                    // Añade la corrutina de cerra puerta
                    closingAnimations.Add(door.StartCoroutine(door.CloseDoorRoutine()));
                    anyDoorWasOpen = true;
                }
                else
                {
                    Debug.Log("Jugador parado en la puerta, se mantiene abierta");
                }
            }
        }

        // Reproduce las animaciones de cerrar puerta
        if (anyDoorWasOpen)
        {
            foreach(var animCoroutine in closingAnimations) yield return animCoroutine;
            yield return new WaitForSeconds(timeEndTurn);
        }
        else yield return new WaitForSeconds(timeEndTurn);

        // Si el jugador esta en la casilla ganadora
        if (playerIsHeadingToWin)
        {
            Player winnerRef = players[currenPlayerIndex];
            StartCoroutine(HandleWin(winnerRef)); // Corrutina de ganar
            yield break;
        }

        NexTurn();
    }

    // Corrutina de ganar
    private IEnumerator HandleWin(Player winner)
    {
        ExplorerPlayer explorerWinner = winner as ExplorerPlayer;
        if(explorerWinner != null) explorerWinner.HandleWin(); // Animación de ganador

        yield return new WaitForSeconds(3f);

        // Muestra el canvas de ganador
        if(UIManager.Instance != null) UIManager.Instance.ShowWinScreen(winner);

        winner.gameObject.SetActive(false);

        currentState = GameState.GameOver;
    }

    // Cambio de cámara
    public void UI_ToggleMapAction()
    {
        if(cameraManager != null && currentState == GameState.WaitingForRoll)
        {
            cameraManager.ToggleMapUI();
        }
    }

    // Cambio a cámara libre
    public void UI_ToggleFreeCamAction()
    {
        if(cameraManager != null && currentState == GameState.WaitingForRoll)
        {
            cameraManager.ToggleFreeRoamUI();
        }
    }

    private void HandleGameStateChangeInternal(GameState state)
    {
        if(state == GameState.Moving || state == GameState.ResolvingTurn)
        {
            ResetAllDoorsToOpaque();
        }
    }

    private void ResetAllDoorsToOpaque()
    {
        foreach(var door in allDoors)
        {
            if(door != null)
            {
                door.SetTransparent(false);
            }
        }
    }

    private void CloseAllDoors()
    {
        foreach(var door in allDoors)
        {
            if(door != null)
            {
                door.ForceCloseDoor();
            }
        }
    }

}
