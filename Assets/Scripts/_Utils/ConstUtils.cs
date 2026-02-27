// Constantes utilizadas para el videojuego

public static class Scenes
{
    public const string MENU = "Menu";
    public const string CLASSIC = "Classic";
    public const string RANDOM = "LaberynthRandom";
}

public enum GameState
{
    Intro,
    Setup,
    WaitingForRoll,
    Rolling,
    TurnPlanning,
    Moving,
    ResolvingTurn,
    GameOver,
    FreeRoam,
    MoveWall
}

public static class Settings
{
    public const string RES_WIDTH = "ResolutionWidth";
    public const string RES_HEIGHT = "ResolutionHeight";

    public const string MASTER_VOLUME = "MasterVolume"; 
    public const string MUSIC_VOLUME = "MusicVolume"; 
    public const string SFX_VOLUME = "SFXVolume"; 
}
