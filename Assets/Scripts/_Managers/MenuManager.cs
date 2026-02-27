using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Linq;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    public GameplaySetupSO gameplaySetupSO;

    // Scene
    public string nameSceneToPlay;

    [Header("UI References")]
    public TurnPlayerSelector turnSelector;
    public Toggle randomOrderToggle;
    public Button startGameButton;
    public Image startGameButtonImage;

    [Header("UI Nav")]
    public TMP_Dropdown fourthDropdown;
    public Button returnButton;

    private Color disableColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private Color activeColor = Color.white;

    [Serializable]
    public struct NameToCharacterMapping
    {
        public string description;
        public TMP_InputField nameInputField;
        public Sprite sprite;
    }

    [Header("Name Mapping")]
    public List<NameToCharacterMapping> nameMappings = new List<NameToCharacterMapping>();

    [Serializable]
    public struct CharacterDefinition
    {
        public string id;
        public Sprite menuSprite;
        public Player playerPrefab;
    }

    [Header("Character mapping")]
    public List<CharacterDefinition> availableCharacters = new List<CharacterDefinition>();

    [Header("CPU Settings")]
    public List<TMP_Dropdown> playerCPUDropdowns;

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject); 
    }

    void Start()
    {
        ValidateStartButtonState();

        if(randomOrderToggle != null) 
            randomOrderToggle.onValueChanged.AddListener(delegate {ValidateStartButtonState(); });

        if(turnSelector != null && turnSelector.dropdowns != null)
        {
            foreach(var dd in turnSelector.dropdowns)
            {
                dd.onValueChanged.AddListener(delegate {ValidateStartButtonState() ;});
            }
        }
    }

    // Valida si el jugador ha seleccionado todas las opciones
    // para habilitar el botón de inicio del juego
    private void ValidateStartButtonState()
    {
        if(startGameButton == null) return;

        bool isRandom = randomOrderToggle != null && randomOrderToggle.isOn;

        if (isRandom) // Si el checkbox indica que los turnos son aleatorios
        {
            startGameButton.interactable = true; // Habilita el botón de inicio
            setColor(isRandom);
        }
        else
        {
            bool allDropdownsValid = true;
            foreach(var dd in turnSelector.dropdowns)
            {
                if(dd.value == 0) // Valida que se hayan designado los turnos
                {
                    allDropdownsValid = false;
                    break;
                }
            }
            startGameButton.interactable = allDropdownsValid;
            setColor(allDropdownsValid);
            setNav(allDropdownsValid);
        }
    }

    // Coloca el botón de habilitado o deshabilitado al botón de inicio
    private void setColor(bool isValid)
    {
        if (isValid) startGameButtonImage.color = activeColor;
        else startGameButtonImage.color = disableColor;
    }

    // Modifica el panel de navegación entre botones
    private void setNav(bool isValid)
    {
        Navigation fourthNav = fourthDropdown.navigation;
        Navigation returnNav = returnButton.navigation;
        
        if (isValid)
        {
            fourthNav.selectOnDown = startGameButton;
            returnNav.selectOnUp = startGameButton;
            returnNav.selectOnLeft = startGameButton;
        }
        else
        {
            fourthNav.selectOnDown = returnButton;
            returnNav.selectOnUp = fourthDropdown;
            returnNav.selectOnLeft = null;
        }
        fourthDropdown.navigation = fourthNav;
        returnButton.navigation = returnNav;
    }

    public void OnToClassicScene()
    {
        nameSceneToPlay = Scenes.CLASSIC;
    }

    public void OnToLaberynthRandomScene()
    {
        nameSceneToPlay = Scenes.RANDOM;
    }

    // Configura el modo de juego seleccionado (escena)
    public void OnStartGameButtonPressed()
    {
        if(gameplaySetupSO == null) return;

        gameplaySetupSO.ResetData();

        bool isRandom = randomOrderToggle != null && randomOrderToggle.isOn;
        gameplaySetupSO.playerRandomOrder = isRandom;

        bool setupSuccesful = false;

        if (gameplaySetupSO.playerRandomOrder) setupSuccesful = SetupRandomGame();
        else setupSuccesful = SetupManualGame();

        if (setupSuccesful && nameSceneToPlay != null) SceneManager.LoadScene(nameSceneToPlay);
    }

    // Configura el turno de los jugadores de forma manual
    private bool SetupManualGame()
    {
        for(int i = 0; i < turnSelector.dropdowns.Length; i++)
        {
            TMP_Dropdown currentDD = turnSelector.dropdowns[i];
            if(currentDD.value == 0) return false;

            Sprite selectedSprite = currentDD.options[currentDD.value].image;
            Player selectedPrefab = FindPrefabForSprite(selectedSprite);

            if(selectedPrefab != null)
            {
                string finalName = GetNameForSprite(selectedSprite, i);
                bool isCPU = playerCPUDropdowns[i].value == 1; // 0 = Player, 1 = CPU
                // Guadar al jugador en el GameplaySetupSO
                AddPlayerToSO(finalName, selectedPrefab, isCPU);
            }
            else return false;
            
        }
        return true;
    }

    // Configura el turno de los jugadores de forma aleatoria
    private bool SetupRandomGame()
    {
        List<GameplaySetupSO.PlayerSetupData> selectedPlayers = new List<GameplaySetupSO.PlayerSetupData>();

        for(int i = 0 ; i < nameMappings.Count; i++)
        {
            var mapping = nameMappings[i];
            Player prefab = FindPrefabForSprite(mapping.sprite);

            if(prefab != null)
            {
                string name = GetNameForMapping(mapping, i + 1);
                bool isCPU = playerCPUDropdowns[i].value == 1; // Player = 0, CPU = 1
                // Guadar al jugador en el GameplaySetupSO
                selectedPlayers.Add(new GameplaySetupSO.PlayerSetupData
                {
                    playerName = name,
                    playerPrefab = prefab,
                    isCPU = isCPU
                });
            }
        }

        selectedPlayers = selectedPlayers.OrderBy(x => Guid.NewGuid()).ToList();
        gameplaySetupSO.orderedPlayers.AddRange(selectedPlayers);
        return true;
    }

    // Añade la configuración del jugador al GameplaySetupSO
    private void AddPlayerToSO(string name, Player prefab, bool isCPU)
    {
        GameplaySetupSO.PlayerSetupData newData = new GameplaySetupSO.PlayerSetupData
        {
            playerName = name,
            playerPrefab = prefab,
            isCPU = isCPU
        };

        gameplaySetupSO.orderedPlayers.Add(newData);
    }

    // Le asigna nombre al jugador
    private string GetNameForMapping(NameToCharacterMapping mapping, int index)
    {
        if(mapping.nameInputField != null && !string.IsNullOrEmpty(mapping.nameInputField.text))
        {
            return mapping.nameInputField.text;
        }

        if (!string.IsNullOrEmpty(mapping.description))
        {
            return mapping.description;
        }
        return $"Jugador {index}";
    }

    // Obtiene el nombre del jugador que corresponda al sprite
    private string GetNameForSprite(Sprite spritToFind, int index)
    {
        foreach(var mapping in nameMappings)
        {
            if(mapping.sprite == spritToFind && mapping.nameInputField != null)
            {
                if (!string.IsNullOrEmpty(mapping.nameInputField.text))
                {
                    return mapping.nameInputField.text;
                }
                return mapping.description;
            }
        }
        return $"Jugador {index}";
    }

    private Player FindPrefabForSprite(Sprite spriteToFind)
    {
        foreach(var charDef in availableCharacters)
        {
            if(charDef.menuSprite == spriteToFind) return charDef.playerPrefab;
        }
        return null;
    }
}
