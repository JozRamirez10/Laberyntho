using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class LevelDataLoader : MonoBehaviour
{
    public GameplaySetupSO setupSO;

    [Serializable]
    public class LevelData
    {
        public List<Vector3> playerPositions;
        public Vector3 minotaurPosition;
    }

    public void LoadLevelData()
    {
        string filename = "LevelConfig.json";
        string filePath = Path.Combine(Application.streamingAssetsPath, filename);

        if (File.Exists(filePath))
        {
            string jsonContent = File.ReadAllText(filePath);
            LevelData data = JsonUtility.FromJson<LevelData>(jsonContent);
            setupSO.loadedPlayerPositions = data.playerPositions;
            setupSO.loadedMinotaurPosition = data.minotaurPosition;
            Debug.Log("Se cargaron las configuraciones del nivel");
        } else Debug.Log("El archivo de configuración del nivel no se encontró");
    }
}
