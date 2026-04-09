using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Hacemos la clase abstracta. No puedes poner este script directo en un objeto,
// tienes que usar uno de sus hijos.
public abstract class GridOccupant : MonoBehaviour
{
    [Header("Object Settings")]
    public Vector2Int baseSize = new Vector2Int(1, 1);
    public bool isMovable = false;
    public bool isWinZone = false;

    [Header("Visual Settings")]
    public Renderer meshRenderer;
    public Material validMaterial;
    public Material invalidMaterial;

    [Header("Shader Properties")]
    public string selectablePropertyName = "_IsSelectable";
    public string dissolvePropertyName = "_DissolveAmount";
    public string hoveredPropertyName = "_IsHovered";
    
    private Material originalMaterial;
    private bool isVisualOverridden = false;

    private MaterialPropertyBlock propBlock;
    private int selectablePropID;
    private int dissolvePropID;
    private int hoveredPropID;


    public abstract List<Vector3> GetOccupiedWorldCenters();

    protected BoardManager GetBoard()
    {
        if (BoardManager.Instance != null) return BoardManager.Instance;
        return FindFirstObjectByType<BoardManager>();
    }

    protected virtual void Awake()
    {
        if(meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
        if(meshRenderer != null) originalMaterial = meshRenderer.material;

        propBlock = new MaterialPropertyBlock();
        selectablePropID = Shader.PropertyToID(selectablePropertyName);
        dissolvePropID = Shader.PropertyToID(dissolvePropertyName);
        hoveredPropID = Shader.PropertyToID(hoveredPropertyName);
    }

    protected virtual void Start()
    {
        RegisterSelf(); // Se registra asi mismo en el board

        // Configura el material del objeto para usar shaders
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(selectablePropID, 0f);
        propBlock.SetFloat(dissolvePropID, 1f);
        propBlock.SetFloat(hoveredPropID, 0f);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    // Aplica el shader de aparición del muro
    public IEnumerator AppearRoutine(float duration)
    {
        float elapsed = 0;
        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(dissolvePropID, progress);
            meshRenderer.SetPropertyBlock(propBlock);

            yield return null;
        }
    }

    // Efecto de disolución (shader)
    public void SetDissolveValule(float value)
    {
        if (meshRenderer == null) return;
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(dissolvePropID, value);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    // Se registra así mismo en el board
    public void RegisterSelf()
    {
        var board = GetBoard();
        if(board != null) board.RegisterOccupancy(this);
    }

    // Desregistra asi mismo del board
    public void UnregisterSelf()
    {
        var board = GetBoard();
        if(board != null) board.UnregisterOccupancy(this);
    }

    public void SetSelectableState(bool isSelectable)
    {
        if(meshRenderer == null || !isMovable) return;
        if(isVisualOverridden) return;

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(selectablePropID, isSelectable ? 1.0f : 0.0f);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public void SetHoverState(bool isHovered)
    {
        if(meshRenderer == null || !isMovable) return;
        if(isVisualOverridden) return;

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(hoveredPropID, isHovered ? 1.0f : 0.0f);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    // Configura el material valido o inválido 
    public void SetMoveFeedbackState(bool isValid)
    {
        if(meshRenderer == null) return;

        isVisualOverridden = true;
        meshRenderer.material = isValid ? validMaterial : invalidMaterial;
    }

    // Restaura el material original
    public void RestoreVisuals()
    {
        if(meshRenderer == null) return;

        isVisualOverridden = false;
        meshRenderer.material = originalMaterial;
    }

    // (Debug) dibuja gizmos para saber el centro del objeto
    protected virtual void OnDrawGizmosSelected() 
    {
        var board = GetBoard();
        if(board == null) return;

        // Draw pivot
        // Gizmos.color = new Color(1f, 0.6f, 0f); // Orange
        // Gizmos.DrawSphere(transform.position, 0.15f);

        // Get points depending son implementation
        List<Vector3> points = GetOccupiedWorldCenters();
        
        Vector3 gizmoSize = new Vector3(board.tileSize * 0.95f, 0.1f, board.tileSize * 0.95f);

        foreach (Vector3 pt in points)
        {
            // Draw a blue point in object's center
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(pt, 0.1f); 

            // Draw a color cube depending if is a movable
            Gizmos.color = isMovable ? Color.cyan : Color.magenta;
            Vector3 cubeCenter = pt;
            cubeCenter.y += 0.1f;
            Gizmos.DrawWireCube(cubeCenter, gizmoSize);
        }
    }
}