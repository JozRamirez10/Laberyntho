using UnityEngine;

// Controla el pulso del brillo de las casillas de movimiento 
// y del icono del jugador
public class GlowPulserIcon : MonoBehaviour
{
    [Header("Pulse Settings")]
    [ColorUsage(true, true)]
    public Color baseColor = new Color(1f, 0.8f, 0f);

    [Header("Reference Shader")]
    public string colorPropertyName = "_Glow";
    public string isPulsingPropertyName = "_isPulsing";

    private Renderer[] targetRenderers;
    private MaterialPropertyBlock propBlock;
    
    private int colorPropID;
    private int isPulsingID;

    // Optiene las propiedades del material y los shaders
    void Awake() 
    {
        targetRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        colorPropID = Shader.PropertyToID(colorPropertyName);
        isPulsingID = Shader.PropertyToID(isPulsingPropertyName);

        if(targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.Log("No encontró ningun Renderer en él o en sus hijos");
        }
        ApplySettings(baseColor, 0f);
    }

    // Si se deshabilita, regresa el material a su estado original
    void OnDisable()
    {
        ApplySettings(baseColor, 0f);
    }

    // Modifica el color y el pulso del material
    private void ApplySettings(Color color, float pulsingValue)
    {
        if(targetRenderers == null || targetRenderers.Length == 0) return;

        foreach(Renderer rend in targetRenderers)
        {
            if(rend != null)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(colorPropID, color);
                propBlock.SetFloat(isPulsingID, pulsingValue);
                rend.SetPropertyBlock(propBlock);
            }
        }
    }

    // Método que activa o desactiva el estado desde el exterior del script
    public void SetGlowState(bool isActive, bool shouldPulse)
    {
        gameObject.SetActive(isActive);
        float pulseValue = shouldPulse ? 1.0f : 0.0f;
        ApplySettings(baseColor, pulseValue);
    }
}
