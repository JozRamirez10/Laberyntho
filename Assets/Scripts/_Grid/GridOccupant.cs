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
    
    private Material originalMaterial;
    private bool isVisualOverridden = false;

    private MaterialPropertyBlock propBlock;
    private int selectablePropID;
    private int dissolvePropID;


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
    }

    protected virtual void Start()
    {
        RegisterSelf();

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(selectablePropID, 0f);
        propBlock.SetFloat(dissolvePropID, 1f);
        meshRenderer.SetPropertyBlock(propBlock);
    }

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

    public void SetDissolveValule(float value)
    {
        if (meshRenderer == null) return;
        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetFloat(dissolvePropID, value);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public void RegisterSelf()
    {
        var board = GetBoard();
        if(board != null) board.RegisterOccupancy(this);
    }

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

    public void SetMoveFeedbackState(bool isValid)
    {
        if(meshRenderer == null) return;

        isVisualOverridden = true;
        meshRenderer.material = isValid ? validMaterial : invalidMaterial;
    }

    public void RestoreVisuals()
    {
        if(meshRenderer == null) return;

        isVisualOverridden = false;
        meshRenderer.material = originalMaterial;
    }

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