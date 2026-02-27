using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MusicCollection", menuName = "Scriptable Objects/MusicCollection")]
public class MusicCollection : ScriptableObject
{
    [Header("Musics")]
    public AudioClip musicMenu;
    public AudioClip musicGame;
    public AudioClip musicMinotaur;
    public AudioClip musicWin;
    public AudioClip musicLoose;

    [Header("SFX Buttons")]
    public AudioClip sfxSelect;
    public AudioClip sfxConfirm;
    public AudioClip sfxBack;
    public AudioClip sfxError;

    [Header("SFX Gameplay")]
    public AudioClip startTurn;
    public AudioClip throwDice;
    public AudioClip winKey;
    public AudioClip spendKey;
    public AudioClip openDoor;
    public AudioClip creakingDoor;
    public AudioClip closeDoor;
    public AudioClip confirmAttack;

    [Header("SFX Player")]
    public AudioClip stepsPlayer;
    public AudioClip diePlayer;
    public AudioClip rebirthPlayer;

    [Header("SFX Minotaur")]
    public AudioClip stepsMinotaur;
    public AudioClip roarMinotaur;

    [Header("SFX Wall")]
    public AudioClip setWall;
    public AudioClip rotateWall;
}
