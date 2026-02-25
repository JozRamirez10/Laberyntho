using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;

public class SimpleURPCuller : MonoBehaviour
{
    [Header("Configuración General")]
    [Tooltip("La capa de los objetos que se deben volver transparentes.")]
    public LayerMask obstacleLayerMask;
    [Tooltip("El objetivo al que mira la cámara (el jugador actual).")]
    public Transform targetToLookAt;
    [Tooltip("Qué tan transparente se vuelve el objeto (0 = invisible, 1 = opaco).")]
    [Range(0f, 1f)] public float fadedOpacity = 0.2f;
    [Tooltip("Velocidad del efecto de desvanecimiento.")]
    public float fadeSpeed = 10f;

    [Header("Configuración de Detección Trasera (Caja)")]
    [Tooltip("Cuánto se extiende la caja de detección hacia ATRÁS de la cámara.")]
    public float backwardDetectionDistance = 1.0f;
    [Tooltip("El ancho y alto de la caja de detección trasera. Mantenlo estrecho para no detectar muros laterales.")]
    public Vector2 backwardBoxSize = new Vector2(0.5f, 0.8f); // X = Ancho, Y = Alto

    [Header("Exclud Camera")]
    public string topDownCameraName = "CM_TopDown";
    private CinemachineBrain brain;

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

    // --- CORRECCIÓN CRÍTICA: RESTAURAR AL DESACTIVAR ---
    void OnDisable()
    {
        // Cuando CameraManager pone este script en enabled = false,
        // restauramos inmediatamente todos los objetos a su color original.
        foreach (var kvp in originalStates)
        {
            Renderer rend = kvp.Key;
            MaterialState state = kvp.Value;

            if (rend != null)
            {
                if(state.isCustomShader && rend.material.HasProperty("_Alpha"))
                {
                    rend.material.SetFloat("_Alpha", state.originalAlpha);
                }
                else if(!state.isCustomShader && rend.material.HasProperty("_BaseColor"))
                {
                    rend.material.SetColor("_BaseColor", state.originalColor);    
                }
            }
        }

        // Limpiamos las listas para empezar frescos la próxima vez que se active
        originalStates.Clear();
        currentlyHitRenderers.Clear();
    }
    // ----------------------------------------------------

    void LateUpdate()
    {
        if (targetToLookAt == null) return;

        bool shouldDisableCulling = false;
        if(brain != null && brain.ActiveVirtualCamera != null)
        {
            ICinemachineCamera activeCam = brain.ActiveVirtualCamera;
            Component activeCamComponent = activeCam as Component;
            if(activeCamComponent != null && activeCamComponent.gameObject.name == topDownCameraName)
            {
                shouldDisableCulling = true;
            }
        }

        currentlyHitRenderers.Clear();

        if (!shouldDisableCulling)
        {
            // --- PASO 1: Detección con CAJA hacia atrás ---
            Vector3 boxSize = new Vector3(backwardBoxSize.x, backwardBoxSize.y, backwardDetectionDistance);
            Vector3 boxCenter = transform.position - (transform.forward * (backwardDetectionDistance * 0.5f));

            Collider[] overlaps = Physics.OverlapBox(boxCenter, boxSize * 0.5f, transform.rotation, obstacleLayerMask);

            foreach (Collider col in overlaps)
            {
                Renderer rend = col.GetComponent<Renderer>();
                ProcessRenderer(rend);
            }

            // --- PASO 2: Lanzar rayo hacia el objetivo ---
            Vector3 dir = targetToLookAt.position - transform.position;
            float dist = dir.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(transform.position, dir, dist, obstacleLayerMask);

            foreach (RaycastHit hit in hits)
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                ProcessRenderer(rend);
            }
        }
        
        // --- PASO 3: Actualizar transparencias ---
        UpdateMaterialTransparencies();
    }

    private void ProcessRenderer(Renderer rend)
    {
        if (rend == null) return;
        currentlyHitRenderers.Add(rend);

        if (!originalStates.ContainsKey(rend))
        {
            Material mat = rend.material;
            MaterialState newState = new MaterialState();

            if (mat.HasProperty("_Alpha"))
            {
                newState.isCustomShader = true;
                newState.originalAlpha = mat.GetFloat("_Alpha");
                originalStates.Add(rend, newState);
            }else if (mat.HasProperty("_BaseColor"))
            {
                newState.isCustomShader = false;
                newState.originalColor = mat.GetColor("_BaseColor");
                originalStates.Add(rend, newState);
            }
        }
    }

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
        }

        foreach (var rend in renderersToRemove)
        {
            originalStates.Remove(rend);
        }
    }

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