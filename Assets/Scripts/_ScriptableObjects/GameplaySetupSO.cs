using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameplaySetup", menuName = "Scriptable Objects/Gameplay Setup")]
public class GameplaySetupSO : ScriptableObject
{
    [Serializable]
    public class PlayerSetupData
    {
        public string playerName;
        public Player playerPrefab;
        public bool isCPU;
    }

    [Header("Play Settings")]
    public bool playerRandomOrder = true;

    [Header("Player Order")]
    public List<PlayerSetupData> orderedPlayers = new List<PlayerSetupData>();

    [Header("Loaded Data")]
    public List<Vector3> loadedPlayerPositions;
    public Vector3 loadedMinotaurPosition;

    public void ResetData()
    {
        orderedPlayers.Clear();
    }
}
