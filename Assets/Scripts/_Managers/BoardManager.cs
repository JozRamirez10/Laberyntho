using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance {get; private set;}

    [Header("Board Settings")]
    public int totalColumns = 30;
    public int totalRows = 30;
    public float tileSize = 2f; 

    private List<GridOccupant> registeredOccupants = new List<GridOccupant>();

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterOccupancy(GridOccupant occupant)
    {
        if (!registeredOccupants.Contains(occupant))
        {
            // --- DEBUG LOG ---
            List<Vector3> points = occupant.GetOccupiedWorldCenters();
            string pointsStr = "";
            foreach(var p in points) pointsStr += $"({p.x:F1}, {p.z:F1}) ";
            Debug.Log($"[BOARD] Registrando {occupant.name}. Ocupa: {pointsStr}");
            // -----------------

            if (!IsAreaFree(points, occupant))
            {
                Debug.LogWarning($"¡COLISIÓN! {occupant.name} choca con otro objeto.");
            }
            registeredOccupants.Add(occupant);
        }
    }

    public void UnregisterOccupancy(GridOccupant occupant)
    {
        if (registeredOccupants.Contains(occupant)) 
        {
            // Debug.Log($"[BOARD] Des-registrando {occupant.name}");
            registeredOccupants.Remove(occupant);
        }
    }

    // --- NUEVA FUNCIÓN DE DIAGNÓSTICO ---
    public void DebugOccupantStatus(GridOccupant occupant)
    {
        if(registeredOccupants.Contains(occupant))
        {
            List<Vector3> currentPoints = occupant.GetOccupiedWorldCenters();
            string pStr = "";
            foreach(var p in currentPoints) pStr += $"[{p.x:F1}, {p.z:F1}] ";
            Debug.Log($"[BOARD DIAGNOSTICO] El muro {occupant.name} ESTÁ registrado. Ocupa: {pStr}");
        }
        else
        {
            Debug.LogError($"[BOARD DIAGNOSTICO] ¡El muro {occupant.name} NO está en la lista de registrados!");
        }
    }
    // ------------------------------------

    public bool IsAreaFree(List<Vector3> pointsToCheck, GridOccupant ignoredObject = null)
    {
        float collisionThreshold = tileSize * 0.45f;

        foreach (var existingOccupant in registeredOccupants)
        {
            if (existingOccupant == ignoredObject) continue;

            List<Vector3> otherPoints = existingOccupant.GetOccupiedWorldCenters();

            foreach (Vector3 myPoint in pointsToCheck)
            {
                foreach (Vector3 otherPoint in otherPoints)
                {
                    Vector2 myPos2D = new Vector2(myPoint.x, myPoint.z);
                    Vector2 otherPos2D = new Vector2(otherPoint.x, otherPoint.z);

                    if (Vector3.Distance(myPos2D, otherPos2D) < collisionThreshold) return false;
                }
            }
        }
        return true; 
    }

    public bool ArePointWithinBounds(List<Vector3> points)
    {
        float halfWidth = (totalColumns * tileSize) / 2f - (tileSize * 0.1f);
        float halfHeight = (totalRows * tileSize) / 2f - (tileSize * 0.1f);

        foreach(Vector3 pt in points)
        {
            if(pt.x < -halfWidth || pt.x > halfWidth ||
                pt.z < -halfHeight || pt.z > halfHeight) return false;
        }
        return true;
    }

    public void ToggleHighlightMovableObjects(bool active)
    {
        foreach(var occupant in registeredOccupants)
        {
            if(occupant != null && occupant.isMovable) occupant.SetSelectableState(active);
        }
    }

    public bool isWinningTile(Vector3 position)
    {
        Vector3 origin = new Vector3(position.x, 20f, position.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 50f);

        foreach(RaycastHit hit in hits)
        {
            GridOccupant occupant = hit.collider.GetComponent<GridOccupant>();
            if(occupant != null && occupant.isWinZone)
            {
                Debug.DrawRay(origin, Vector3.down * hit.distance, Color.green, 2f);
                return true;
            }
        }
        return false;
    }
}