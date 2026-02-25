using UnityEngine;
using System.Collections.Generic;

public class ProceduralMazeGenerator : MonoBehaviour
{
    [Header("Prefabs 1x1 (2x2 unidades)")]
    public GameObject wall1x1Prefab;
    public GameObject doorPrefab;

    [Header("Prefabs 2x1 (4x2 unidades)")]
    // Ahora usamos un arreglo para que puedas arrastrar los 2 modelos diferentes desde el Inspector
    public GameObject[] wall2x1Prefabs; 

    [Header("Configuración del Tablero")]
    public float gridSize = 2f;    
    public int quadrantSize = 15;  
    
    [Range(0, 50)]
    public int extraPaths = 5;     
    [Range(0f, 1f)]
    public float doorChance = 0.15f; 

    private bool[,] mazeGrid;      
    private bool[,] visitedNodes;
    private bool[,] processedGrid; 

    // ... (GenerateFullMaze, GenerateQuadrant y RecursiveBacktracking se mantienen iguales)

    public void GenerateFullMaze()
    {
        foreach (Transform child in transform) 
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        for (int i = 0; i < 4; i++)
        {
            GenerateQuadrant(i);
        }
        // GenerateQuadrant(0);

        Physics.SyncTransforms();
        Debug.Log("<color=cyan>[MAZE]</color> Laberinto generado: Variedad de muros 2x1 aplicada.");
    }

    private void GenerateQuadrant(int index)
    {
        mazeGrid = new bool[quadrantSize, quadrantSize];
        visitedNodes = new bool[quadrantSize, quadrantSize];
        processedGrid = new bool[quadrantSize, quadrantSize]; 

        for (int x = 0; x < quadrantSize; x++)
        {
            for (int y = 0; y < quadrantSize; y++)
            {
                if (x % 2 != 0 || y % 2 != 0) mazeGrid[x, y] = true; 
                else mazeGrid[x, y] = false;
                processedGrid[x, y] = false; 
            }
        }

        Vector2Int start = GetLocalStartCorner(index);
        RecursiveBacktracking(start.x, start.y);

        for (int i = 0; i < extraPaths; i++)
        {
            int rx = Random.Range(1, quadrantSize - 1);
            int ry = Random.Range(1, quadrantSize - 1);
            if (mazeGrid[rx, ry]) mazeGrid[rx, ry] = false;
        }

        SpawnQuadrant(index);
    }

    private void RecursiveBacktracking(int x, int y)
    {
        visitedNodes[x, y] = true;
        List<Vector2Int> directions = new List<Vector2Int> {
            new Vector2Int(0, 2), new Vector2Int(0, -2),
            new Vector2Int(2, 0), new Vector2Int(-2, 0)
        };

        for (int i = 0; i < directions.Count; i++) {
            Vector2Int temp = directions[i];
            int r = Random.Range(i, directions.Count);
            directions[i] = directions[r];
            directions[r] = temp;
        }

        foreach (Vector2Int dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            if (nx >= 0 && nx < quadrantSize && ny >= 0 && ny < quadrantSize && !visitedNodes[nx, ny])
            {
                int wallX = x + (dir.x / 2);
                int wallY = y + (dir.y / 2);
                mazeGrid[wallX, wallY] = false;
                RecursiveBacktracking(nx, ny);
            }
        }
    }

    private void SpawnQuadrant(int quadrantIndex)
    {
        Vector3 quadrantOffset = GetQuadrantOffset(quadrantIndex);

        for (int x = 0; x < quadrantSize; x++)
        {
            for (int y = 0; y < quadrantSize; y++)
            {
                if (!mazeGrid[x, y] || processedGrid[x, y]) continue;

                float standardCenterX = (x * gridSize) + (gridSize / 2f);
                float standardCenterZ = (y * gridSize) + (gridSize / 2f);
                Vector3 currentCellCenter = quadrantOffset + new Vector3(standardCenterX, 0, standardCenterZ);

                if (Random.value < doorChance)
                {
                    Quaternion doorRot = (x % 2 != 0) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
                    PlaceAndSnapPiece(doorPrefab, currentCellCenter, doorRot);
                    processedGrid[x, y] = true;
                    continue;
                }

                // --- FUSIÓN HORIZONTAL ---
                if (x + 1 < quadrantSize && mazeGrid[x + 1, y] && !processedGrid[x + 1, y])
                {
                    float midPointX = (x + 1) * gridSize;
                    Vector3 midPointTarget = quadrantOffset + new Vector3(midPointX, 0, standardCenterZ);

                    // Elegimos uno de los modelos 2x1 al azar
                    GameObject selectedPrefab = GetRandom2x1Prefab();
                    PlaceAndSnapPiece(selectedPrefab, midPointTarget, Quaternion.identity);

                    processedGrid[x, y] = true;
                    processedGrid[x + 1, y] = true;
                    continue;
                }

                // --- FUSIÓN VERTICAL ---
                if (y + 1 < quadrantSize && mazeGrid[x, y + 1] && !processedGrid[x, y + 1])
                {
                    float midPointZ = (y + 1) * gridSize;
                    Vector3 midPointTarget = quadrantOffset + new Vector3(standardCenterX, 0, midPointZ);

                    // Elegimos uno de los modelos 2x1 al azar
                    GameObject selectedPrefab = GetRandom2x1Prefab();
                    PlaceAndSnapPiece(selectedPrefab, midPointTarget, Quaternion.Euler(0, 90, 0));

                    processedGrid[x, y] = true;
                    processedGrid[x, y + 1] = true;
                    continue;
                }

                Quaternion wallRot = (x % 2 != 0) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
                PlaceAndSnapPiece(wall1x1Prefab, currentCellCenter, wallRot);
                processedGrid[x, y] = true;
            }
        }
    }

    // Método auxiliar para obtener un prefab aleatorio del arreglo
    private GameObject GetRandom2x1Prefab()
    {
        if (wall2x1Prefabs == null || wall2x1Prefabs.Length == 0) return null;
        return wall2x1Prefabs[Random.Range(0, wall2x1Prefabs.Length)];
    }

    private void PlaceAndSnapPiece(GameObject prefab, Vector3 targetCenter, Quaternion rotation)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, targetCenter, rotation, transform);
        GridOccupant occ = go.GetComponent<GridOccupant>();
        
        if (occ != null)
        {
            List<Vector3> centers = occ.GetOccupiedWorldCenters();
            
            if (centers.Count > 0)
            {
                Vector3 actualObjectCenter = Vector3.zero;
                foreach (Vector3 c in centers) actualObjectCenter += c;
                actualObjectCenter /= centers.Count;

                Vector3 correction = targetCenter - actualObjectCenter;
                go.transform.position += correction;
            }
            occ.RegisterSelf();
        }
    }

    #region Helpers
    private Vector3 GetQuadrantOffset(int index)
    {
        float totalUnits = quadrantSize * gridSize;
        switch (index)
        {
            case 0: return new Vector3(-totalUnits, 0, 0);    
            case 1: return new Vector3(0, 0, 0);             
            case 2: return new Vector3(0, 0, -totalUnits);   
            case 3: return new Vector3(-totalUnits, 0, -totalUnits); 
        }
        return Vector3.zero;
    }

    private Vector2Int GetLocalStartCorner(int index)
    {
        if (index == 0) return new Vector2Int(0, quadrantSize - 1);
        if (index == 1) return new Vector2Int(quadrantSize - 1, quadrantSize - 1);
        if (index == 2) return new Vector2Int(quadrantSize - 1, 0);
        return new Vector2Int(0, 0);
    }
    #endregion
}