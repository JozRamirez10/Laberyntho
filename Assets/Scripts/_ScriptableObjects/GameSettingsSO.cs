using UnityEngine;

// Configuración de la resolución, audio y velocidad del juego
[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/Game Settings")]
public class GameSettingsSO : ScriptableObject
{
    public int resolutionWidth;
    public int resolutionHeight;
    
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;

    public float speedGame;
}
