using UnityEngine;
using System;
using System.Collections;

public class ExplorerPlayer : Player
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

    public override void Initialize(Vector3 initialPos)
    {
        base.Initialize(initialPos);
        ResetVisuals();
    }

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

        yield return new WaitForSeconds(vfxDuration * 0.8f);
        ResetVisuals();
        yield return new WaitForSeconds(vfxDuration * 0.2f);
        onComplete?.Invoke();
    }

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

    public void PlayDeathEffect()
    {
        SpawnVFX(deathParticlesPrefab);
        SetRenderersVisibility(false);
    }

    public void ResetVisuals()
    {
        SetRenderersVisibility(true);
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
                Debug.Log("Explorador frente a una puerta. Abriendo...");
                Animator anim = GetComponentInChildren<Animator>();
                if(anim != null) anim.SetBool("isMoving", false);

                yield return StartCoroutine(door.OpenDoorRoutine());

                if(anim != null) anim.SetBool("isMoving", true);
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
            OnKeysChanged?.Invoke(keysCollected);
            return true;
        }
        return false;
    }
}
