using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// Carga ajustes para configurar las escenas del juego
public class LevelDataLoader : MonoBehaviour
{
    public GameplaySetupSO setupSO; // Scriptable Object

    [Serializable]
    public class LevelData
    {
        public List<Vector3> playerPositions;
        public Vector3 minotaurPosition;
    }

    public void LoadLevelData()
    {
        string filename = "LevelConfig.json";
        
        string filePath = Path.Combine(Application.streamingAssetsPath, filename); // Ruta del archivo

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath); // Obtiene todo el contenido en formato json

            // Transforma el Json al objeto LevelData
            LevelData data = JsonUtility.FromJson<LevelData>(jsonContent);

            // Carga el contenido de data al scriptable object
            setupSO.loadedPlayerPositions = data.playerPositions;
            setupSO.loadedMinotaurPosition = data.minotaurPosition;

            Debug.Log("Se cargaron las configuraciones del nivel");
            
        } else Debug.Log("El archivo de configuración del nivel no se encontró");
    }
}
