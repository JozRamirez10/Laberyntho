using UnityEngine;

// Cuando el sistema de particulas termina de reproducirse, lo destruye
public class AutoDestroyParticle : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();    
    }

    void Update()
    {
        if(ps != null && !ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}
