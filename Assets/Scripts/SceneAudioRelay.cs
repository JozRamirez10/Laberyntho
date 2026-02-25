using UnityEngine;

public class SceneAudioRelay : MonoBehaviour
{
    public void PlayConfirm()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
    }

    public void PlayBack()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playToBack();
    }

    public void PlaySelect()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playToSelect();
    }

    public void PlayStartTurn()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playStartTurn();
    }

    public void PlayWinKey()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playWinKey();
    }

    public void PlaySpendKey()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playSpendKey();
    }

    public void PlayOpenDoor()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playOpenDoor();
    }

    public void PlayCreakingDoor()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playCreakingDoor();
    }

    public void PlayCloseDoor()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playCloseDoor();
    }

    public void PlayDiePlayer()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playDiePlayer();
    }

    public void PlayRebirthPlayer()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playRebirthPlayer();
    }

    public void PlayRoarMinotaur()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playRoarMinotaur();
    }
}
