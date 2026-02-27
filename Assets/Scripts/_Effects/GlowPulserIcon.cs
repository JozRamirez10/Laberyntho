using UnityEngine;

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

    void Awake() {
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

    void OnDisable()
    {
        ApplySettings(baseColor, 0f);
    }

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

    public void SetGlowState(bool isActive, bool shouldPulse)
    {
        gameObject.SetActive(isActive);
        float pulseValue = shouldPulse ? 1.0f : 0.0f;
        ApplySettings(baseColor, pulseValue);
    }
}
