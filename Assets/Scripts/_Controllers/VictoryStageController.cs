using UnityEngine;

public class VictoryStageController : MonoBehaviour
{
    [Header("Settings")]
    public Transform spawnPoit;
    public string victoryAnimationName = "Victory";

    private GameObject currentModel;
    
    public void SetupWinner(Player winnerPlayer)
    {
        if(currentModel != null) Destroy(currentModel);

        currentModel = Instantiate(winnerPlayer.gameObject, spawnPoit.position, spawnPoit.rotation, spawnPoit);

        Animator anim = currentModel.GetComponentInChildren<Animator>();
        if(anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
            anim.Play(victoryAnimationName);
        }
    }
}
