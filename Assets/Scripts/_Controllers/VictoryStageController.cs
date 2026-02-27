using UnityEngine;

// Animación si un jugador gana la partida
public class VictoryStageController : MonoBehaviour
{
    [Header("Settings")]
    public Transform spawnPoit;
    public string victoryAnimationName = "Victory";

    private GameObject currentModel; // Jugador ganador
    
    public void SetupWinner(Player winnerPlayer)
    {
        if(currentModel != null) Destroy(currentModel);

        // Instancia al jugador ganador
        currentModel = Instantiate(winnerPlayer.gameObject, spawnPoit.position, spawnPoit.rotation, spawnPoit);

        // Reprouduce la animación de victoria
        Animator anim = currentModel.GetComponentInChildren<Animator>();
        if(anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
            anim.Play(victoryAnimationName);
        }
    }
}
