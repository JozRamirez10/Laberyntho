using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [System.Serializable]
    public struct RendererTarget
    {
        public Renderer renderer;
        public int targetMaterialIndex;
    }

    [Header("Target Setup")]
    public RendererTarget[] renderersToAffect;

    [Header("Material Settings")]
    public Material opaqueMaterial;
    public Material transparentMaterial;

    private Animator animator;
    private BoxCollider doorCollider;
    public float openDuration = 1.0f;
    public bool isOpen {get; private set;} = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider>();

        if(animator == null) Debug.LogWarning("Falta el animator");
        if(doorCollider == null) Debug.LogWarning("Falta el collider");

        SetTransparent(false);
        
        if(animator != null) animator.SetBool("IsOpen", false);
    }

    public IEnumerator OpenDoorRoutine()
    {
        if(animator != null && !isOpen)
        {
            isOpen = true;

            if(doorCollider != null) doorCollider.enabled = false;
            animator.SetBool("IsOpen", true);
            
            yield return new WaitForSeconds(openDuration);
        }
    }

    public IEnumerator CloseDoorRoutine()
    {
        if(animator != null && isOpen)
        {
            animator.SetBool("IsOpen", false);
            yield return new WaitForSeconds(openDuration);

            if(doorCollider != null) doorCollider.enabled = true;
            isOpen = false;
        }
    }

    public void ForceCloseDoor()
    {
        if(animator != null && isOpen)
        {
            if(doorCollider != null) doorCollider.enabled = true;
            animator.SetBool("IsOpen", false);
            isOpen = false;
        }
    }

    public void SetTransparent(bool isTransparent)
    {
        Material targetMat = isTransparent ? transparentMaterial : opaqueMaterial;

        if(targetMat == null || renderersToAffect == null) return;

        foreach(RendererTarget target in renderersToAffect)
        {
            if(target.renderer != null)
            {
                Material[] currentMaterials = target.renderer.materials;

                if(target.targetMaterialIndex >= 0 && target.targetMaterialIndex < currentMaterials.Length)
                {
                    currentMaterials[target.targetMaterialIndex] = targetMat;
                    target.renderer.materials = currentMaterials;
                }
            }
        }
    }
}
