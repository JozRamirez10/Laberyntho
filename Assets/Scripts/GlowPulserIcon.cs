using UnityEngine;

public class GlowPulserIcon : MonoBehaviour
{
    [Header("Pulse Settings")]
    [ColorUsage(true, true)]
    public Color baseColor = new Color(1f, 0.8f, 0f);
    public float pulseSpeed = 2f;
    
    public float minIntensity = 1.0f;
    public float maxIntensity = 3.0f;

    [Header("Reference Shader")]
    public string colorPropertyName = "_Glow";

    private Renderer[] targetRenderers;
    private MaterialPropertyBlock propBlock;
    private int colorPropID;

    private bool isPulsing = false;

    void Awake() {
        targetRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        colorPropID = Shader.PropertyToID(colorPropertyName);

        if(targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.Log("No encontró ningun Renderer en él o en sus hijos");
        }
    }

    void Update()
    {
        if(targetRenderers == null || !isPulsing) return;

        float sineValue = Mathf.Sin(Time.time * pulseSpeed);
        float t = (sineValue + 1.0f) / 2.0f;
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, t);

        ApplyColor(currentIntensity);
    }

    void OnDisable()
    {
        ApplyColor(1.0f);
    }

    private void ApplyColor(float intensity)
    {
        if(targetRenderers == null || targetRenderers.Length == 0) return;
        Color finalColor = baseColor * intensity;

        foreach(Renderer rend in targetRenderers)
        {
            if(rend != null)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetColor(colorPropID, finalColor);
                rend.SetPropertyBlock(propBlock);
            }
        }
    }

    public void SetGlowState(bool isActive, bool shouldPulse)
    {
        gameObject.SetActive(isActive);
        isPulsing = shouldPulse;
        if(isActive && !isPulsing)
        {
            ApplyColor(1.0f);
        }
    }
}
