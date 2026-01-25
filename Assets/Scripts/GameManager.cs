using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum GameState
{
    Setup,
    WaitingForRoll,
    TurnPlanning,
    Moving,
    ResolvingTurn,
    GameOver,
    FreeRoam
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}
    public TurnInputController inputController;
    public CameraManager cameraManager;
    public Player[] playersTemplate = new Player[4];
    public List<Player> players = new List<Player>();
    public int totalTurnCount = 0;

    public MinotaurPlayer minotaurInstance;

    private List<DoorController> allDoors = new List<DoorController>();

    private Vector3[] startPositions = new Vector3[]
    {
        new Vector3(-29f, 0.2f, -29f),
        new Vector3(29f, 0.2f, -29f),
        new Vector3(29f, 0.2f, 29),
        new Vector3(-29f, 0.2f, 29f)

        // new Vector3(-19f, 0f, -19f),
        // new Vector3(19f, 0f, -19f),
        // new Vector3(19f, 0f, 19f),
        // new Vector3(-19f, 0f, 19f)
    };    

    public int currenPlayerIndex = 0;

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
        if(cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();

        allDoors = new List<DoorController>(FindObjectsByType<DoorController>(FindObjectsSortMode.None));

        if(minotaurInstance == null)
        {
            minotaurInstance = FindFirstObjectByType<MinotaurPlayer>();
        }

        currentState = GameState.Setup;
        Setup();
    }

    private void Setup()
    {
        List<Player> tempTemplates = new List<Player>(playersTemplate);
        tempTemplates = tempTemplates.OrderBy(x => Guid.NewGuid()).ToList();
        
        for(int i = 0 ; i < tempTemplates.Count(); i++)
        {   
            Player template = tempTemplates[i];
            Vector3 initialPos = startPositions[i];

            Player newPlayerInstance = Instantiate(template, initialPos, Quaternion.identity);
            newPlayerInstance.gameObject.name = $"{template.characterName}_Instance_{i}";
            newPlayerInstance.Initialize(initialPos);

            players.Add(newPlayerInstance);
        }

        this.currenPlayerIndex = -1;
    }

    private bool firstTurnStarted = false;
    void Update()
    {
        if (!firstTurnStarted)
        {
            firstTurnStarted = true;
            NexTurn();
        }
    }

    public void NexTurn()
    {
        if(players.Count == 0) return;

        totalTurnCount++;

        currenPlayerIndex = (currenPlayerIndex + 1) % players.Count;

        Player currentPlayer = players[currenPlayerIndex];
        Debug.Log($"Turno de: {currentPlayer.characterName}");

        UpdateDoorState(currentPlayer);

        currentState = GameState.WaitingForRoll;
        currentPlayer.ActiveGlow(true);

        OnTurnChanged?.Invoke(currentPlayer);
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

        currentState = GameState.TurnPlanning;

        int diceResult = UnityEngine.Random.Range(1, 9); // 1 a 8
        //diceResult = UnityEngine.Random.Range(7, 9);
        Debug.Log($"Resultado del dado: {diceResult}");

        bool isMoninotaurTurn = (diceResult == 8);

        Player currentPlayer = players[this.currenPlayerIndex];
        Player playerToMove = null;

        if(isMoninotaurTurn && minotaurInstance != null)
        {
            playerToMove = minotaurInstance;
            currentPlayer.ActiveGlow(false);
            minotaurInstance.ActiveGlow(true);
            ResetAllDoorsToOpaque();
        }
        else
        {
            playerToMove = currentPlayer;
            playerToMove.ActiveGlow(true);
        }
        
        inputController.StartTurnPlanning(playerToMove, diceResult, isMoninotaurTurn);
    }

    public void EndMovementState()
    {
        StartCoroutine(ResolveTurnSequence());
    }

    private IEnumerator ResolveTurnSequence()
    {
        float timeEndTurn = 3.0f;

        currentState = GameState.ResolvingTurn;

        List<Coroutine> closingAnimations = new List<Coroutine>();
        bool anyDoorWasOpen = false;

        foreach(var door in allDoors)
        {
            if(door != null && door.isOpen)
            {
                closingAnimations.Add(door.StartCoroutine(door.CloseDoorRoutine()));
                anyDoorWasOpen = true;
            }
        }

        if (anyDoorWasOpen)
        {
            foreach(var animCoroutine in closingAnimations)
            {
                yield return animCoroutine;
            }
            yield return new WaitForSeconds(timeEndTurn);
        }
        else
        {
            yield return new WaitForSeconds(timeEndTurn);
        }
        NexTurn();
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
