using UnityEngine;
using System.Collections.Generic;

public class ProceduralMazeGenerator : MonoBehaviour
{
    [Header("Prefabs 1x1 (2x2 unidades)")]
    public GameObject wall1x1Prefab;
    public GameObject doorPrefab;

    [Header("Prefabs 2x1 (4x2 unidades)")]
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

    // Genera el laberinto completo
    public void GenerateFullMaze()
    {
        foreach (Transform child in transform) 
        {
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        // Construye un laberinto para cada cuadrante del tablero
        for (int i = 0; i < 4; i++)
        {
            GenerateQuadrant(i);
        }

        Physics.SyncTransforms();
        Debug.Log("<color=cyan>[MAZE]</color> Laberinto generado: Variedad de muros 2x1 aplicada.");
    }

    // Genera el cuadrante
    private void GenerateQuadrant(int index)
    {
        mazeGrid = new bool[quadrantSize, quadrantSize]; // lista del laberinto real
        visitedNodes = new bool[quadrantSize, quadrantSize]; // lista del laberinto para saber cuales visitño
        processedGrid = new bool[quadrantSize, quadrantSize];  // lista para saber que objetos ya se instanciaron

        for (int x = 0; x < quadrantSize; x++)
        {
            for (int y = 0; y < quadrantSize; y++)
            {
                if (x % 2 != 0 || y % 2 != 0) mazeGrid[x, y] = true; 
                else mazeGrid[x, y] = false;
                processedGrid[x, y] = false; 
            }
        }

        // Define la esquina del cuadrante
        Vector2Int start = GetLocalStartCorner(index);
        RecursiveBacktracking(start.x, start.y); // Construye el camino del cuadrante a la meta

        // Abre el camino para tener caminos adicionales
        for (int i = 0; i < extraPaths; i++)
        {
            int rx = Random.Range(1, quadrantSize - 1);
            int ry = Random.Range(1, quadrantSize - 1);
            if (mazeGrid[rx, ry]) mazeGrid[rx, ry] = false;
        }

        // Construye el laberinto
        SpawnQuadrant(index);
    }

    private void RecursiveBacktracking(int x, int y) // Busqueda en profundidad con retroceso
    {
        visitedNodes[x, y] = true; // Marca la casilla como visitada

        // Define las direcciones hacia donde debe mirar
        // Salta dos casillas para que siempre haya un espacio entre ellas (donde estará el muro)
        List<Vector2Int> directions = new List<Vector2Int> {
            new Vector2Int(0, 2), new Vector2Int(0, -2),
            new Vector2Int(2, 0), new Vector2Int(-2, 0)
        };

        // Coloca las direcciones al azar
        for (int i = 0; i < directions.Count; i++) {
            Vector2Int temp = directions[i];
            int r = Random.Range(i, directions.Count);
            directions[i] = directions[r];
            directions[r] = temp;
        }

        // Calcula la coordenada destino por cada coordenada al azar
        foreach (Vector2Int dir in directions)
        {
            int nx = x + dir.x;
            int ny = y + dir.y;
            // Valida si no se sale del tablero y si no ha sido visitado
            if (nx >= 0 && nx < quadrantSize && ny >= 0 && ny < quadrantSize && !visitedNodes[nx, ny])
            {
                int wallX = x + (dir.x / 2);
                int wallY = y + (dir.y / 2);
                mazeGrid[wallX, wallY] = false; // Destruye el muro matemáticamente -> Define que es un pasillo
                RecursiveBacktracking(nx, ny); // Aplica recursividad con las coordenadas de la nueva casilla
            }
        }
    }

    // Instancia los muros en el tablero
    private void SpawnQuadrant(int quadrantIndex)
    {
        // Obtiene la referencia del cuadrante donde le toca construir
        Vector3 quadrantOffset = GetQuadrantOffset(quadrantIndex);

        for (int x = 0; x < quadrantSize; x++)
        {
            for (int y = 0; y < quadrantSize; y++)
            {
                // Pregunta si la casilla está vacía
                if (!mazeGrid[x, y] || processedGrid[x, y]) continue;

                // Establece las coordenadas dentro del grid para que queden en casillas del tablero
                float standardCenterX = (x * gridSize) + (gridSize / 2f);
                float standardCenterZ = (y * gridSize) + (gridSize / 2f);
                Vector3 currentCellCenter = quadrantOffset + new Vector3(standardCenterX, 0, standardCenterZ);

                // Decide, en base al Random, si debe colocar una puerta en lugar de un muro
                if (Random.value < doorChance)
                {
                    // Rota la puerta si esta en una columna impar
                    Quaternion doorRot = (x % 2 != 0) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
                    PlaceAndSnapPiece(doorPrefab, currentCellCenter, doorRot); // Coloca la puerta y la adapta al grid
                    processedGrid[x, y] = true; // La marca como "construida"
                    continue;
                }

                // Fusión horizontal
                // Pregunta si a sus lado derecho hay un muro construido
                if (x + 1 < quadrantSize && mazeGrid[x + 1, y] && !processedGrid[x + 1, y])
                {
                    float midPointX = (x + 1) * gridSize; // Punto medio exacto entre las dos casillas 
                    Vector3 midPointTarget = quadrantOffset + new Vector3(midPointX, 0, standardCenterZ);

                    // Elegimos uno de los modelos 2x1 al azar (Arbuto o piedra)
                    GameObject selectedPrefab = GetRandom2x1Prefab();
                    // Lo coloca en el punto medio
                    PlaceAndSnapPiece(selectedPrefab, midPointTarget, Quaternion.identity); 

                    // Marca ambas casillas como "construidas"
                    processedGrid[x, y] = true;
                    processedGrid[x + 1, y] = true;
                    continue;
                }

                // Fusión vertical
                // Pregunta si hacia arriba hay un muro
                if (y + 1 < quadrantSize && mazeGrid[x, y + 1] && !processedGrid[x, y + 1])
                {
                    float midPointZ = (y + 1) * gridSize; // Punto medio exacto entre las dos casillas 
                    Vector3 midPointTarget = quadrantOffset + new Vector3(standardCenterX, 0, midPointZ);

                    // Elegimos uno de los modelos 2x1 al azar (Arbusto o piedra)
                    GameObject selectedPrefab = GetRandom2x1Prefab();
                    PlaceAndSnapPiece(selectedPrefab, midPointTarget, Quaternion.Euler(0, 90, 0));

                    // Marca ambas casillas como "construidas"
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

    // Método auxiliar para obtener un prefab aleatorio del arreglo (arbusto o piedra)
    private GameObject GetRandom2x1Prefab()
    {
        if (wall2x1Prefabs == null || wall2x1Prefabs.Length == 0) return null;
        return wall2x1Prefabs[Random.Range(0, wall2x1Prefabs.Length)];
    }

    // Instancia el objeto y lo adapta al grid del tablero
    private void PlaceAndSnapPiece(GameObject prefab, Vector3 targetCenter, Quaternion rotation)
    {
        if (prefab == null) return; // Valida si el modelo está vacío

        // Instancia el objeto colocándolo en el centro de la coordenada
        GameObject go = Instantiate(prefab, targetCenter, rotation, transform);
        GridOccupant occ = go.GetComponent<GridOccupant>();
        
        if (occ != null)
        {
            List<Vector3> centers = occ.GetOccupiedWorldCenters(); // Obtiene sus centro
            
            if (centers.Count > 0)
            {
                // Promedio para al centro de masa absoluto del objeto
                Vector3 actualObjectCenter = Vector3.zero;
                foreach (Vector3 c in centers) actualObjectCenter += c;
                actualObjectCenter /= centers.Count;

                // Diferencia entre el targetCenter y donde está realmente su centro
                Vector3 correction = targetCenter - actualObjectCenter;
                go.transform.position += correction; // Corrige la posición del muro
            }
            occ.RegisterSelf(); // Lo registra en el board
        }
    }

    // Devuelve las coordenadas del cuadrante de acuerdo al índice en el que va a construir los muros
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

    // Devuelve las esquinas exteriores más lejanas del cuadrante
    private Vector2Int GetLocalStartCorner(int index)
    {
        if (index == 0) return new Vector2Int(0, quadrantSize - 1);
        if (index == 1) return new Vector2Int(quadrantSize - 1, quadrantSize - 1);
        if (index == 2) return new Vector2Int(quadrantSize - 1, 0);
        return new Vector2Int(0, 0);
    }
}