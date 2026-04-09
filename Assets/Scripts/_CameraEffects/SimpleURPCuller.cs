using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

// Vuelve transparentes los objetos marcados con la capa
// y cuando están entre el jugador y la cámara
public class SimpleURPCuller : MonoBehaviour
{
    [Header("Obstacle Layer Mask")]
    public LayerMask obstacleLayerMask;
    
    [Tooltip("Camera target")]
    public Transform targetToLookAt;

    [Header("Trasnparent settings")]
    [Range(0f, 1f)] public float fadedOpacity = 0.2f;
    public float fadeSpeed = 10f;

    [Header("Box camera settings")]
    public float backwardDetectionDistance = 1.0f;
    public Vector2 backwardBoxSize = new Vector2(0.5f, 0.8f); // X = Ancho, Y = Alto

    private CinemachineBrain brain;

    // Esta clase nos ayuda a validar si el material es configurado como shader propio
    private class MaterialState
    {
        public bool isCustomShader;
        public Color originalColor;
        public float originalAlpha;
    }

    private Dictionary<Renderer, MaterialState> originalStates = new Dictionary<Renderer, MaterialState>();
    private HashSet<Renderer> currentlyHitRenderers = new HashSet<Renderer>();

    private MaterialPropertyBlock tempPropBlock;

    void Start()
    {
        brain = GetComponent<CinemachineBrain>();
        if(brain == null) Debug.Log("No se encontró Cinemachine Brain");
    }

    void OnDisable()
    {
        // Cuando CameraManager pone este script en enabled = false,
        // simplemente removemos el PropertyBlock para que el objeto 
        // regrese a mostrar su material original intacto
        foreach (var kvp in originalStates)
        {
            Renderer rend = kvp.Key;
            if (rend != null)
            {
                rend.SetPropertyBlock(null);
            }
        }

        // Limpiamos las listas para la siguiente vez que se active
        originalStates.Clear();
        currentlyHitRenderers.Clear();
    }

    void LateUpdate()
    {
        if (targetToLookAt == null) return;

        bool isActiveEffect = false;

        if(CameraManager.Instance != null && brain != null && brain.ActiveVirtualCamera != null)
        {
            ICinemachineCamera activeCam = brain.ActiveVirtualCamera;
            Component activeCamComponent = activeCam as Component;

            // Valida si el culling debería activarse
            if(activeCamComponent != null 
                && (activeCamComponent == CameraManager.Instance.vcamFaceToFace
                    || activeCamComponent == CameraManager.Instance.vcamThirdPerson))
            {
                isActiveEffect = true;
            }
        }

        currentlyHitRenderers.Clear();

        if (isActiveEffect)
        {
            // Detección con caja hacia atrás // Deja un espacio hacia atrás para que nigún muro bloque la vista 
            // en primera persona
            Vector3 boxSize = new Vector3(backwardBoxSize.x, backwardBoxSize.y, backwardDetectionDistance);
            Vector3 boxCenter = transform.position - (transform.forward * (backwardDetectionDistance * 0.5f));

            Collider[] overlaps = Physics.OverlapBox(boxCenter, boxSize * 0.5f, transform.rotation, obstacleLayerMask);

            foreach (Collider col in overlaps)
            {
                Renderer rend = col.GetComponent<Renderer>();
                ProcessRenderer(rend);
            }

            // Lanzar rayo hacia el objetivo
            Vector3 dir = targetToLookAt.position - transform.position;
            float dist = dir.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, dist, obstacleLayerMask);

            foreach (RaycastHit hit in hits)
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                ProcessRenderer(rend);
            }
        }
        
        // Actualizar transparencias
        UpdateMaterialTransparencies();
    }

    // Renderiza al material de acuerdo a si tiene un shader personalizado o no
    private void ProcessRenderer(Renderer rend)
    {
        if (rend == null) return;
        currentlyHitRenderers.Add(rend);

        if (!originalStates.ContainsKey(rend))
        {
            Material mat = rend.sharedMaterial;
            if(mat == null) return;

            MaterialState newState = new MaterialState();

            if (mat.HasProperty("_Alpha"))
            {
                newState.isCustomShader = true;
                newState.originalAlpha = mat.GetFloat("_Alpha");
                originalStates.Add(rend, newState);
            }
            else if (mat.HasProperty("_BaseColor"))
            {
                newState.isCustomShader = false;
                newState.originalColor = mat.GetColor("_BaseColor");
                originalStates.Add(rend, newState);
            }
        }
    }

    // Actualiza la transparencia de los materiales
    private void UpdateMaterialTransparencies()
    {
        if(tempPropBlock == null) tempPropBlock = new MaterialPropertyBlock();

        List<Renderer> renderersToRemove = new List<Renderer>();

        foreach (var kvp in originalStates)
        {
            Renderer rend = kvp.Key;
            MaterialState state = kvp.Value;

            if (rend == null)
            {
                renderersToRemove.Add(rend);
                continue;
            }

            bool isHit = currentlyHitRenderers.Contains(rend);
            rend.GetPropertyBlock(tempPropBlock);

            if (state.isCustomShader)
            {
                float currentAlpha = rend.sharedMaterial.GetFloat("_Alpha");
                float targetAlpha = isHit ? fadedOpacity : state.originalAlpha;

                tempPropBlock.SetFloat("_Alpha", targetAlpha);
            }
            else
            {
                Color targetColor = isHit ? 
                    new Color(state.originalColor.r, state.originalColor.g, state.originalColor.b, fadedOpacity) :
                    state.originalColor;

                tempPropBlock.SetColor("_BaseColor", targetColor);
            }

            rend.SetPropertyBlock(tempPropBlock);

            if (!isHit)
            {
                renderersToRemove.Add(rend);
            }
        }

        foreach (var rend in renderersToRemove)
        {
            if(rend != null) rend.SetPropertyBlock(null);
            originalStates.Remove(rend);
        }
    }

    // Dibuja un gizmo para obtener referencia de las distancias (debug)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 boxSize = new Vector3(backwardBoxSize.x, backwardBoxSize.y, backwardDetectionDistance);
        Vector3 boxCenter = transform.position - (transform.forward * (backwardDetectionDistance * 0.5f));

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        Gizmos.matrix = oldMatrix;
    }
}