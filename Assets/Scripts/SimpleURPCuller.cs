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

    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private HashSet<Renderer> currentlyHitRenderers = new HashSet<Renderer>();

    void Start()
    {
        brain = GetComponent<CinemachineBrain>();
        if(brain == null)
        {
            Debug.Log("No se encontró Cinemachine Brain");
        }
    }

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
            // Calculamos el tamaño total de la caja (Ancho, Alto, Profundidad hacia atrás)
            Vector3 boxSize = new Vector3(backwardBoxSize.x, backwardBoxSize.y, backwardDetectionDistance);
            
            // Calculamos el centro de la caja.
            // Empieza en la cámara y se mueve hacia atrás la mitad de su distancia total.
            Vector3 boxCenter = transform.position - (transform.forward * (backwardDetectionDistance * 0.5f));

            // Usamos OverlapBox. Necesita el centro, la mitad del tamaño (halfExtents) y la rotación de la cámara.
            Collider[] overlaps = Physics.OverlapBox(boxCenter, boxSize * 0.5f, transform.rotation, obstacleLayerMask);

            foreach (Collider col in overlaps)
            {
                Renderer rend = col.GetComponent<Renderer>();
                ProcessRenderer(rend);
            }

            // --- PASO 2: Lanzar rayo hacia el objetivo (Lo que está en el camino frontal) ---
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

        if (!originalColors.ContainsKey(rend))
        {
            if (rend.material.HasProperty("_BaseColor"))
            {
                originalColors.Add(rend, rend.material.GetColor("_BaseColor"));
            }
        }
    }

    private void UpdateMaterialTransparencies()
    {
        List<Renderer> renderersToRemove = new List<Renderer>();

        foreach (var kvp in originalColors)
        {
            Renderer rend = kvp.Key;
            Color originalColor = kvp.Value;

            if (rend == null)
            {
                renderersToRemove.Add(rend);
                continue;
            }

            Material mat = rend.material;

            if (!mat.HasProperty("_BaseColor")) continue;

            Color currentColor = mat.GetColor("_BaseColor");
            Color targetColor;

            if (currentlyHitRenderers.Contains(rend))
            {
                targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, fadedOpacity);
            }
            else
            {
                targetColor = originalColor;
            }

            Color newColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * fadeSpeed);
            mat.SetColor("_BaseColor", newColor);

            if (!currentlyHitRenderers.Contains(rend) && Mathf.Abs(newColor.a - originalColor.a) < 0.01f)
            {
                renderersToRemove.Add(rend);
            }
        }

        foreach (var rend in renderersToRemove)
        {
            originalColors.Remove(rend);
        }
    }

    // Dibujamos la caja en el editor para poder ajustarla visualmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 boxSize = new Vector3(backwardBoxSize.x, backwardBoxSize.y, backwardDetectionDistance);
        Vector3 boxCenter = transform.position - (transform.forward * (backwardDetectionDistance * 0.5f));

        // Para dibujar una caja rotada, necesitamos guardar la matriz de transformación actual y aplicar una nueva
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        
        Gizmos.DrawWireCube(Vector3.zero, boxSize);

        // Restauramos la matriz original
        Gizmos.matrix = oldMatrix;
    }
}