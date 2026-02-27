using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance {get; private set;}

    public class Node
    {
        public Vector3 worldPosition;
        public float gCost; // Costo del inicio al nodo actual
        public float hCost; // Qué tan lejos está de la meta
        public Node parent; // Nodo anterior
        public int keysRemaining; // Considera si el jugador tiene llaves

        // Suma de los costos
        // Siempre busca el costo más bajo
        public float FCost { get { return gCost + hCost; } }

        public Node(Vector3 _worldPos, int _keys)
        {
            worldPosition = _worldPos;
            keysRemaining = _keys;
        }
    }

    [Header("Settings")]
    public float gridSize = 2f;
    public LayerMask obstacleLayer;
    public LayerMask doorLayer;

    public int maxSearchDeepth = 5000;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);    
    }

    // Busca el camino
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos, int keyCount = 0)
    {
        // Revisa si es el turno del minotauro
        bool isMinotaur = false;
        if(GameManager.Instance.minotaurInstance != null)
        {
            isMinotaur = Vector3.Distance(startPos, GameManager.Instance.minotaurInstance.transform.position) < 0.5f;
        } 

        // Evita que el ajuste al grid inicial rompa la detección del centro
        Node startNode;
        if (isMinotaur && IsWinningZone(startPos)) 
        {
            // Si está en el centro, mantenemos su posición exacta (ej. 0,0,0) para que GetNeighbors lo detecte
            startNode = new Node(startPos, keyCount);
            Debug.Log("<color=yellow>[Pathfinding]</color> Inicio detectado en ZONA CENTRAL.");
        }
        else 
        {
            startNode = new Node(GetTileCenter(startPos), keyCount); // Casilla de inicio
        }
        
        Node targetNode = new Node(GetTileCenter(targetPos), 0); // Casilla objetivo

        Debug.Log($"<color=cyan>[Pathfinding INIT]</color> Inicio: {startNode.worldPosition} | Meta: {targetNode.worldPosition}");

        List<Node> openSet = new List<Node>(); // Casillas por explorar
        HashSet<Vector3> closedSet = new HashSet<Vector3>(); // Casillas ya evaluadas

        openSet.Add(startNode);
        int safetyCounter = 0; // Contador que evita que el juego se congele si el laberinto no tiene salida

        while(openSet.Count > 0)
        {
            safetyCounter++;
            if(safetyCounter > maxSearchDeepth) // Mientras haya casillas por explorar, sigue buscando
            {
                Debug.LogWarning($"Pathfinding agotado tras {maxSearchDeepth} iteraciones.");
                return null;
            } 

            // Revisa la lista de nodos para obtener quién tiene el costo más bajo
            Node currentNode = openSet[0];
            for(int i = 1; i < openSet.Count; i++)
            {
                if(openSet[i].FCost < currentNode.FCost || openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            // Saca el nodo con menos costo de la lista de casillas por explorar y lo guarda en la lista de casillas evaluadas
            openSet.Remove(currentNode); 
            closedSet.Add(currentNode.worldPosition);

            // Verificación de llegada a la meta
            if(Vector3.Distance(currentNode.worldPosition, targetNode.worldPosition) < 0.5f)
            {
                // Construye el camino a la meta
                List<Vector3> path = RetracePath(startNode, currentNode);
                
                // DEBUG LOG: Imprimir coordenadas de la ruta para validar el salto
                string pathCoords = "";
                foreach(Vector3 p in path) pathCoords += p.ToString() + " -> ";
                Debug.Log($"<color=green>[Pathfinding SUCCESS]</color> Ruta: {pathCoords}");
                
                return path;
            }

            // Permite saltar al minotauro (su primer paso)
            bool canJump = isMinotaur && (currentNode.worldPosition == startNode.worldPosition) && IsWinningZone(currentNode.worldPosition);
            List<Node> neighbors = GetNeighbors(currentNode, canJump);
            Debug.Log($"<color=white>[PF]</color> Evaluando {neighbors.Count} vecinos de {currentNode.worldPosition}. Iteración: {safetyCounter}");

            foreach(Node neighbor in neighbors)
            {
                // Si ya evaluamos al vecino, lo ignoramos
                if(closedSet.Contains(neighbor.worldPosition)) continue;

                // Evitar al Minotauro si no somos él (usando la posición inicial como referencia)
                if(!isMinotaur && GameManager.Instance.minotaurInstance != null)
                {
                    float distToMinotaur = Vector3.Distance(neighbor.worldPosition, GetTileCenter(GameManager.Instance.minotaurInstance.transform.position));
                    float distToTarget = Vector3.Distance(neighbor.worldPosition, targetNode.worldPosition);

                    if(distToMinotaur < 0.1f && distToTarget > 0.1f) continue;
                }

                // Si hay un muro, lo evitamos
                if(HashWall(currentNode.worldPosition, neighbor.worldPosition))
                {
                    Debug.Log($"<color=red>[PF Block]</color> Muro detectado hacia {neighbor.worldPosition}");
                    continue;
                } 

                // Si hay una puerta en el camino, revisamos si el jugador tiene llaves
                int keysForNexStep = currentNode.keysRemaining;
                if(HasDoor(currentNode.worldPosition, neighbor.worldPosition))
                {
                    if(currentNode.keysRemaining > 0) { // Si tiene llaves, gastamos la llave para pasar por la puerta
                        keysForNexStep = currentNode.keysRemaining - 1;
                        Debug.Log($"<color=blue>[PF Door]</color> Usando llave hacia {neighbor.worldPosition}. Quedan: {keysForNexStep}");
                    }
                    else // Si no, evitamos el camino
                    {
                        Debug.Log($"<color=red>[PF Block]</color> Puerta bloqueada hacia {neighbor.worldPosition} (Sin llaves)");
                        continue;
                    } 
                }

                // Calcula el costo del viaje
                float newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                Node existingNeighbor = openSet.FirstOrDefault(n => Vector3.Distance(n.worldPosition, neighbor.worldPosition) < 0.1f);

                // Asignamos la información del nodo: costos, llaves, parent. Y lo almacenamos en openSet
                if(existingNeighbor == null || newMovementCostToNeighbor < existingNeighbor.gCost)
                {
                    neighbor.keysRemaining = keysForNexStep;
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    
                    if(existingNeighbor == null) openSet.Add(neighbor);
                }
            }
        }
        Debug.LogError("<color=red>[Pathfinding FAILED]</color> No se encontró ruta entre " + startNode.worldPosition + " y " + targetNode.worldPosition);
        return null;
    }

    // Obtiene las casillas vecinas
    private List<Node> GetNeighbors(Node node, bool canJump)
    {
        List<Node> neighbors = new List<Node>();
        Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        foreach(Vector3 dir in directions)
        {
            // Si estamos en el centro, el primer paso DEBE ser de 1.5x para caer en el centro de la casilla exterior
            float stepDistance = canJump ? (gridSize * 1.5f) : gridSize;
            Vector3 targetPos = node.worldPosition + (dir * stepDistance);

            targetPos = GetTileCenter(targetPos);
            neighbors.Add(new Node(targetPos, 0));
        }
        return neighbors;
    }

    // Valida si está en la zona de las casillas ganadoras
    private bool IsWinningZone(Vector3 position)
    {
        // El centro absoluto suele estar en el offset del grid
        float offset = gridSize / 2f;
        // Comprobamos si la posición X y Z están en el rango del centro (0 +/- offset)
        return Mathf.Abs(position.x) < offset && Mathf.Abs(position.z) < offset;
    }

    // Valida si la casilla tiene un muro
    private bool HashWall(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - currentPos).normalized;
        float dist = Vector3.Distance(currentPos, targetPos);
        // Ajustamos el raycast para que no choque con el suelo pero detecte muros
        if(Physics.Raycast(currentPos + Vector3.up * 0.5f, dir, dist - 0.1f, obstacleLayer)) return true;
        return false;
    }

    // Valida si la casilla tiene una puerta
    private bool HasDoor(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 dir = (targetPos - currentPos).normalized;
        float dist = Vector3.Distance(currentPos, targetPos);
        if(Physics.Raycast(currentPos + Vector3.up * 0.5f, dir, dist - 0.1f, doorLayer)) return true;
        return false;
    }

    // De acuerdo al camino, lo reconstruye para que el jugador pueda caminar a través de el
    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while(currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        // Usamos distancia entera para el costo G y H
        return Mathf.RoundToInt(Vector3.Distance(nodeA.worldPosition, nodeB.worldPosition) * 10);
    }

    // Obtiene el centro de la casilla 
    private Vector3 GetTileCenter(Vector3 pos)
    {
        float x = Mathf.Floor(pos.x / gridSize) * gridSize + (gridSize / 2f);
        float z = Mathf.Floor(pos.z / gridSize) * gridSize + (gridSize / 2f);
        return new Vector3(x, 0, z);
    }
}