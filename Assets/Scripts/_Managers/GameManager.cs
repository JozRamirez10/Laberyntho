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

    public TurnInputController inputController;
    public MazeInputController mazeInputController;
    public DiceController diceController;
    
    public CameraManager cameraManager;

    public Player[] playersTemplate = new Player[4]; // Solo se usa si se ejecuta la escena classic (sin menu)
    public List<Player> players = new List<Player>();
    public int totalTurnCount = 0;

    public MinotaurPlayer minotaurInstance;

    private List<DoorController> allDoors = new List<DoorController>();

    // private Vector3[] startPositions = new Vector3[]
    // {
    //     new Vector3(-29f, 0.2f, -29f),
    //     new Vector3(29f, 0.2f, -29f),
    //     new Vector3(29f, 0.2f, 29),
    //     new Vector3(-29f, 0.2f, 29f)

    //     // new Vector3(-19f, 0f, -19f),
    //     // new Vector3(19f, 0f, -19f),
    //     // new Vector3(19f, 0f, 19f),
    //     // new Vector3(-19f, 0f, 19f)

    //     // new Vector3(-5f, 0f, -5f),
    //     // new Vector3(5f, 0f, -5f),
    //     // new Vector3(5f, 0f, 5),
    //     // new Vector3(-5f, 0f, 5f)

    //     // new Vector3(-9f, 0f, -9f),
    //     // new Vector3(9f, 0f, -9f),
    //     // new Vector3(9f, 0f, 9f),
    //     // new Vector3(-9f, 0f, 9f)
    // };    

    public int currenPlayerIndex = 0;
    public bool isTurnCPU = false;
    public bool isMinotaurTurn = false;

    public bool playerIsHeadingToWin = false;

    public event Action<Player> OnTurnChanged;

    public event Action<GameState> OnGameStateChanged;

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
        if(loader != null) loader.LoadLevelData();

        if(cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();

        if(minotaurInstance == null)
        {
            minotaurInstance = FindFirstObjectByType<MinotaurPlayer>();
        }

        currentState = GameState.Intro;
        Setup();

        // StartCoroutine(StartGameWithDelay());

        if(introDirector != null) introDirector.Play();
        else StartGameFromTimelineSignal();
    }

    private IEnumerator StartGameWithDelay()
    {
        yield return null;
        StartGameFromTimelineSignal();
    }

    public void StartGameFromTimelineSignal()
    {
        NexTurn();
    }

    private void Setup()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if(currentSceneName == Scenes.RANDOM)
        {
            ProceduralMazeGenerator generator = FindFirstObjectByType<ProceduralMazeGenerator>();
            if(generator != null) generator.GenerateFullMaze();
        }

        if(CPUController.Instance != null) CPUController.Instance.InitializeWinningPositions();

        if(gameplaySetupSO == null || gameplaySetupSO.orderedPlayers.Count == 0)
        {
            List<Player> tempTemplates = new List<Player>(playersTemplate);
            tempTemplates = tempTemplates.OrderBy(x => Guid.NewGuid()).ToList();
            
            for(int i = 0 ; i < tempTemplates.Count(); i++)
            {   
                Player template = tempTemplates[i];
                // Vector3 initialPos = startPositions[i];
                Vector3 initialPos = gameplaySetupSO.loadedPlayerPositions[i];

                Player newPlayerInstance = Instantiate(template, initialPos, Quaternion.identity);
                newPlayerInstance.gameObject.name = $"{template.characterName}_Instance_{i}";

                newPlayerInstance.Initialize(initialPos, template.IsCPU);
                players.Add(newPlayerInstance);
            }
        }
        else
        {
            List<GameplaySetupSO.PlayerSetupData> playersList = gameplaySetupSO.orderedPlayers;

            for(int i = 0 ; i < playersList.Count ; i++)
            {
                // if( i >= startPositions.Length) break;
                if( i >= gameplaySetupSO.loadedPlayerPositions.Count) break;

                GameplaySetupSO.PlayerSetupData data = playersList[i];
                // Vector3 initialPos = startPositions[i];
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
        
        minotaurInstance.transform.position = gameplaySetupSO.loadedMinotaurPosition;
        allDoors = new List<DoorController>(FindObjectsByType<DoorController>(FindObjectsSortMode.None));

        this.currenPlayerIndex = -1;
        Debug.Log("Terminé de configurar la escena");
    }

    public void NexTurn()
    {
        if(players.Count == 0) return;

        if(AudioManager.Instance != null) AudioManager.Instance.playStartTurn();

        totalTurnCount++;

        currenPlayerIndex = (currenPlayerIndex + 1) % players.Count;

        Player currentPlayer = players[currenPlayerIndex];
        if(currentPlayer.IsCPU) isTurnCPU = true;
        else isTurnCPU = false;

        Debug.Log($"Turno de: {currentPlayer.characterName}");

        UpdateDoorState(currentPlayer);

        OnTurnChanged?.Invoke(currentPlayer);
        currentPlayer.ActiveGlow(true);

        playerIsHeadingToWin = false;
        
        currentState = GameState.WaitingForRoll;
    }

    private void UpdateDoorState(Player player)
    {
        bool playerHasKeys = false;
        if(player is ExplorerPlayer explorer)
        {
            playerHasKeys = explorer.GetKeyCount() > 0;
        }

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

        // int diceResult = 7; 
        // int diceResult = UnityEngine.Random.Range(1, 9); // 1 a 8
        int diceResult = UnityEngine.Random.Range(3, 9); // 3 a 8
        // int diceResult = UnityEngine.Random.Range(1, 6); // 1 a 5
        //diceResult = UnityEngine.Random.Range(7, 9);

        // if(diceResult == 1) diceResult = 3;
        // else diceResult = 8;

        Debug.Log($"Resultado del dado: {diceResult}");

        currentState = GameState.Rolling;

        StartCoroutine(RollDiceSequence(diceResult));
    }

    private IEnumerator RollDiceSequence(int result)
    {
        bool animationFinished = false;
        if(diceController != null)
        {
            diceController.PlayDiceAnimation(result, () => animationFinished = true);
            while(!animationFinished) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if(UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowDiceResultRoutine(result, 3.0f));
        }

        ProcessDiceResult(result);
    }

    private void ProcessDiceResult(int diceResult)
    {
        Player currentPlayer = players[this.currenPlayerIndex];

        isMinotaurTurn = false;
        bool isWinningKey = (diceResult == 6);
        bool isMovingWall = (diceResult == 7);

        if (isWinningKey)
        {
            currentState = GameState.ResolvingTurn;

            if(UIManager.Instance != null) UIManager.Instance.ShowKeyGainSequence(() => AddKey(currentPlayer));
            else AddKey(currentPlayer);
        }
        else if(isMovingWall)
        {
            currentState = GameState.MoveWall;
            currentPlayer.ActiveGlow(true);

            if (currentPlayer.IsCPU)
            {
                CPUController.Instance.StartTurn(currentPlayer, diceResult, isMinotaurTurn, EndMovementState);
            }
            else
            {
                if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(true);
            }
        }
        else
        {
            isMinotaurTurn = (diceResult == 8);
            // diceResult = (diceResult == 2) ? 6 : diceResult;

            Player playerToMove = currentPlayer;
            currentState = GameState.TurnPlanning;

            if(isMinotaurTurn && minotaurInstance != null)
            {
                playerToMove = minotaurInstance;
                currentPlayer.ActiveGlow(false);
                minotaurInstance.ActiveGlow(true);
                diceResult = 5;
                ResetAllDoorsToOpaque();

                if(cameraManager != null) cameraManager.SetCameraTarget(minotaurInstance.transform);
            }
            else playerToMove.ActiveGlow(true);
            
            if (currentPlayer.IsCPU)
            {
                CPUController.Instance.StartTurn(playerToMove, diceResult, isMinotaurTurn, EndMovementState);
            }
            else
            {
                inputController.StartTurnPlanning(playerToMove, diceResult, isMinotaurTurn);
            }
        }
    }
    

    private void AddKey(Player currentPlayer)
    {
        ExplorerPlayer explorer = currentPlayer as ExplorerPlayer;
        if(explorer != null) explorer.AddKey();
        EndMovementState();
    }

    public void EndMovementState()
    {
        StartCoroutine(ResolveTurnSequence());
    }

    private IEnumerator ResolveTurnSequence()
    {
        float timeEndTurn = 1.5f;

        currentState = GameState.ResolvingTurn;

        List<Coroutine> closingAnimations = new List<Coroutine>();
        bool anyDoorWasOpen = false;

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
                    closingAnimations.Add(door.StartCoroutine(door.CloseDoorRoutine()));
                    anyDoorWasOpen = true;
                }
                else
                {
                    Debug.Log("Jugador parado en la puerta, se mantiene abierta");
                }
            }
        }

        if (anyDoorWasOpen)
        {
            foreach(var animCoroutine in closingAnimations) yield return animCoroutine;
            yield return new WaitForSeconds(timeEndTurn);
        }
        else yield return new WaitForSeconds(timeEndTurn);

        if (playerIsHeadingToWin)
        {
            Player winnerRef = players[currenPlayerIndex];
            StartCoroutine(HandleWin(winnerRef));
            yield break;
        }

        NexTurn();
    }

    private IEnumerator HandleWin(Player winner)
    {
        ExplorerPlayer explorerWinner = winner as ExplorerPlayer;
        if(explorerWinner != null) explorerWinner.HandleWin();

        yield return new WaitForSeconds(3f);

        if(UIManager.Instance != null) UIManager.Instance.ShowWinScreen(winner);

        // Destroy(winner);
        winner.gameObject.SetActive(false);

        currentState = GameState.GameOver;
    }

    public void UI_ToggleMapAction()
    {
        if(cameraManager != null && currentState == GameState.WaitingForRoll)
        {
            cameraManager.ToggleMapUI();
        }
    }

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
