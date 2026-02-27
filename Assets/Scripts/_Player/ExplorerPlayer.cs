using UnityEngine;
using System;
using System.Collections;

public class ExplorerPlayer : Player // Hereda de Player
{
    [Header("Explorer Data")]
    [SerializeField] private int keysCollected = 0;
    [SerializeField] private LayerMask doorLayer;

    [Header("VFX Settings")]
    [SerializeField] private GameObject deathParticlesPrefab;
    [SerializeField] private GameObject respawnParticlesPrefab;
    private Renderer[] childRenderers;

    protected override void Start()
    {
        base.Start();
        childRenderers = GetComponentsInChildren<Renderer>();
    }

    public override void Initialize(Vector3 initialPos, bool isCPU = false)
    {
        base.Initialize(initialPos, isCPU);
        ResetVisuals();
    }

    // Animación de partículas cuando el jugador vuelve a su posición original
    // después de ser atacado por el minotauro
    public IEnumerator PlayRespawnSequence(Action onComplete)
    {
        float vfxDuration = 1.0f;
        if(respawnParticlesPrefab != null)
        {
            GameObject vfxInstance = SpawnVFX(respawnParticlesPrefab);
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if(ps != null)
            {
                vfxDuration = ps.main.duration + ps.main.startLifetime.constantMax;
            }
        }

        if(AudioManager.Instance != null) AudioManager.Instance.playRebirthPlayer();

        yield return new WaitForSeconds(vfxDuration * 0.8f);
        ResetVisuals();
        yield return new WaitForSeconds(vfxDuration * 0.2f);
        onComplete?.Invoke();
    }

    // Animación de partículas (desintegración) cuando el jugador es atacado por el minotauro
    private GameObject SpawnVFX(GameObject prefab)
    {
        if(prefab != null)
        {
            GameObject vfxInstance = Instantiate(prefab, transform.position, Quaternion.identity);
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if(ps != null)
            {
                var main = ps.main;
                main.startColor = this.playerColor;
            }
            return vfxInstance;
        }
        return null;
    }

    // Reproducción de muerte del jugador
    public void PlayDeathEffect()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playDiePlayer();
        
        SpawnVFX(deathParticlesPrefab);
        SetRenderersVisibility(false);
    }

    // Regresa los objetos a su render original
    public void ResetVisuals()
    {
        SetRenderersVisibility(true);
        if(AudioManager.Instance != null) AudioManager.Instance.playWinKey();
    }

    private void SetRenderersVisibility(bool isVisible)
    {
        if(childRenderers != null)
        {
            foreach(Renderer r in childRenderers)
            {
                if(r.gameObject != this.IconGlowObject)
                {
                    r.enabled = isVisible;
                }
            }
        }
    }

    public event Action<int> OnKeysChanged;

    // Corrutina preparada para interactuar con las puertas
    protected override IEnumerator PreStepCheck(Vector3 currentPos, Vector3 nextPos)
    {
        Vector3 direction = (nextPos - currentPos).normalized;
        float distance = Vector3.Distance(currentPos, nextPos);

        RaycastHit hit;
        if(Physics.Raycast(currentPos + Vector3.up * 0.5f, direction, out hit, distance, doorLayer))
        {
            DoorController door = hit.collider.GetComponent<DoorController>();
            if(door != null && !door.isOpen)
            {
                Animator anim = GetComponentInChildren<Animator>(); // Antes de abrir una puerta
                if(anim != null) anim.SetBool("isMoving", false); // Se detiene

                yield return StartCoroutine(door.OpenDoorRoutine()); // Se abre la puerta

                if(anim != null) anim.SetBool("isMoving", true); // El jugador vuelva a caminar
            }
        }
    }

    public int GetKeyCount()
    {
        return keysCollected;
    }

    public void AddKey()
    {
        keysCollected++;
        OnKeysChanged?.Invoke(keysCollected);
    }

    public bool TryRemoveKey()
    {
        if(keysCollected > 0)
        {
            keysCollected--;

            if(AudioManager.Instance != null) AudioManager.Instance.playSpendKey();

            OnKeysChanged?.Invoke(keysCollected);
            return true;
        }
        return false;
    }

    public void HandleWin()
    {
        Vector3 currentPos = transform.position;
        currentPos.y = 0.1f;
        transform.position = currentPos;

        if(AudioManager.Instance != null) AudioManager.Instance.playWinKey();

        Animator anim = GetComponentInChildren<Animator>();
        if(anim != null) anim.Play("Victory");
    }
}
