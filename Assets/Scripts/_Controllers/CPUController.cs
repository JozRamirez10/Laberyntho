using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CPUController : MonoBehaviour
{
    public static CPUController Instance { get; private set; }

    [Header("Animation Settings")]
    public float thinkingTime = 1.0f;
    public float stepSelectionDelay = 0.5f;
    public float moveSpeed = 12f;
    public Material ghostMaterial;
    
    private List<Vector3> allWinningPositions = new List<Vector3>();

    private struct MoveDecision 
    {
        public GridOccupant wall;
        public Vector3 finalPos;
        public Quaternion finalRot;
        public bool isValid;
        public float score;
        public float distToGoal;
    }

    void Awake() 
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Evalua todos los objetos en el tablero y pregunta si son zonas de victoria
    public void InitializeWinningPositions() 
    {
        allWinningPositions.Clear();
        GridOccupant[] occupants = FindObjectsByType<GridOccupant>(FindObjectsSortMode.None);
        foreach(var occ in occupants) {
            if (occ.isWinZone) allWinningPositions.AddRange(occ.GetOccupiedWorldCenters());
        }
    }

    // Define el comportamiento con respecto al resultado del dado y el turno del minotauro
    public void StartTurn(Player playerToMove, int stepsAvailable, bool isMinotaurTurn, Action onTurnComplete) 
    {
        if(stepsAvailable == 7) StartCoroutine(HandleWallMoveTurn(playerToMove, onTurnComplete));
        else StartCoroutine(ExecuteTurnRoutine(playerToMove, stepsAvailable, isMinotaurTurn, onTurnComplete));
    }

    // Corrutina de comportamiento del CPU para caminar
    private IEnumerator ExecuteTurnRoutine(Player playerToMove, int stepsAvailable, bool isMinotaurTurn, Action onTurnComplete) 
    {
        if (Pathfinding.Instance == null || GameManager.Instance.inputController == null) 
        { 
            onTurnComplete?.Invoke(); yield break; 
        }

        // Forza la vista superior del tablero
        if (GameManager.Instance.cameraManager != null)
            GameManager.Instance.cameraManager.ForceTopDownView(true);

        yield return new WaitForSeconds(thinkingTime);

        // Define a quién va seguir la cámara y si tiene llaves
        Vector3 target = Vector3.zero;
        int availableKeys = (playerToMove as ExplorerPlayer)?.GetKeyCount() ?? 0;
        if(isMinotaurTurn) availableKeys = 0;

        if (isMinotaurTurn) 
        {
            Player leader = GetLeadingPlayer(playerToMove); // Obtiene el jugador que está mas cerca de la meta
            if(leader != null) target = leader.transform.position;
        } 
        else if(allWinningPositions.Count > 0) 
        {
            // Busca la casilla de meta más cercana al explorador
            target = GetClosestWinPosition(playerToMove.transform.position);
        }

        // Usamos el pathfindig para buscar la ruta completa hacia la meta
        List<Vector3> fullPath = Pathfinding.Instance.FindPath(playerToMove.transform.position, target, availableKeys);
        
        if(fullPath == null || fullPath.Count == 0) 
        {
            if (GameManager.Instance.cameraManager != null) GameManager.Instance.cameraManager.ForceTopDownView(false);
            onTurnComplete?.Invoke(); 
            yield break; 
        }

        // Recorta la ruta con los pasos disponibles
        int stepsToTake = Mathf.Min(stepsAvailable, fullPath.Count);
        List<Vector3> finalPath = fullPath.GetRange(0, stepsToTake);

        // Le avisamos al inputController que colocaremos fantasmas
        GameManager.Instance.inputController.StartTurnPlanning(playerToMove, stepsAvailable, isMinotaurTurn);

        // Forzamos que la posición inicial de referencia para los fantasmas 
        // esté alineada al grid. Si el minotauro está en el centro (0,0,0), esto evitará 
        // que el primer cálculo de dirección sea diagonal.
        Vector3 currentPhantomPosReference = GetSnappedPosition(playerToMove.transform.position);
        currentPhantomPosReference.y = 0.05f; 

        bool eventTriggered = false;

        foreach (Vector3 nextStepPos in finalPath) 
        {
            yield return new WaitForSeconds(stepSelectionDelay);
            
            // Obtiene la posición del paso
            Vector3 targetVisualPos = new Vector3(nextStepPos.x, 0.05f, nextStepPos.z);
            
            // Calculamos la dirección usando la referencia alineada
            Vector3 direction = (targetVisualPos - currentPhantomPosReference).normalized;
            
            // Si la dirección es oblicua por errores de precisión, forzamos ejes cardinales
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                direction = new Vector3(Mathf.Sign(direction.x), 0, 0);
            else
                direction = new Vector3(0, 0, Mathf.Sign(direction.z));

            if (direction == Vector3.zero) direction = playerToMove.transform.forward;

            // Instancia el fantasma
            GameManager.Instance.inputController.SimulateCPUStep(targetVisualPos, direction);
            
            // Actualizamos la referencia para el siguiente fantasma
            currentPhantomPosReference = targetVisualPos;

            // Revisa si aparecio el panel de confirmación
            if (UIManager.Instance != null && UIManager.Instance.confirmationPanel.activeSelf) 
            {
                eventTriggered = true;
                break;
            }
        }

        yield return new WaitForSeconds(thinkingTime);

        // Si no ha aparecido el panel de confirmación, lanza la confirmación
        if (!eventTriggered) 
        {
            GameManager.Instance.inputController.ConfirmMovement();
            yield return new WaitForSeconds(0.6f); 
        }

        // Simula presionar el botón de confirmación
        int safetyLimit = 2; // Puede simular la confirmación hasta dos paneles
        while (UIManager.Instance != null && UIManager.Instance.confirmationPanel.activeSelf && safetyLimit > 0) 
        {
            if (AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
            UIManager.Instance.popupConfirmButton.onClick.Invoke();
            safetyLimit--;
            yield return new WaitForSeconds(0.7f); 
        }

        // Devuelve la cámara normal
        if (GameManager.Instance.cameraManager != null)
            GameManager.Instance.cameraManager.ForceTopDownView(false);
    }

    
    // Obtiene el muro que más esta bloqueando al jugador 
    private GridOccupant GetBestWallBlockingMePathfinding(Player self, List<GridOccupant> walls) 
    {
        // Obtiene la meta más cercana al jugador
        if (Pathfinding.Instance == null) return null;
        Vector3 targetMeta = GetClosestWinPosition(self.transform.position);
        
        // Valida si el jugador tiene llaves
        int myKeys = (self as ExplorerPlayer)?.GetKeyCount() ?? 0;

        // Obtiene el camino a la meta
        List<Vector3> idealPath = Pathfinding.Instance.FindPath(self.transform.position, targetMeta, myKeys);
        if (idealPath == null || idealPath.Count == 0) return null;
        
        // Diccionario que considera cuanto estorba un muro en el camino
        Dictionary<GridOccupant, int> blockagePower = new Dictionary<GridOccupant, int>();
        foreach (var wall in walls) {
            int pointsIntersected = 0;
            List<Vector3> wallOccupiedCells = wall.GetOccupiedWorldCenters();
            foreach (Vector3 pathNode in idealPath)
            {
                foreach (Vector3 wallCell in wallOccupiedCells)
                {
                    if (Vector3.Distance(new Vector3(pathNode.x, 0, pathNode.z), new Vector3(wallCell.x, 0, wallCell.z)) < 0.5f)
                        pointsIntersected++;
                }
            }
            
            if (pointsIntersected > 0) blockagePower[wall] = pointsIntersected;
        }
        return blockagePower.Count == 0 ? null : blockagePower.OrderByDescending(x => x.Value).First().Key;
    }

    // Muestra el comportamiento de mover el muro
    private IEnumerator HandleWallMoveTurn(Player cpuPlayer, Action onTurnComplete) 
    {
        // Prende todos los muros con el shader de selección
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(true);
        yield return new WaitForSeconds(thinkingTime);
        
        // Actualiza todas las físicas
        Physics.SyncTransforms(); 
        
        // Calcula el muro a mover
        MoveDecision decision = CalculateBestWallMove(cpuPlayer);
        if (!decision.isValid) 
        {
            if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);
            onTurnComplete?.Invoke();
            yield break;
        }

        // Inicia la corrutina de movimiento de muro
        yield return StartCoroutine(AnimateCpuMoveSequence(decision, onTurnComplete, false));
    }

    // Calcula el mejor muro para mover y en donde lo va a colocar
    private MoveDecision CalculateBestWallMove(Player cpuPlayer) 
    {
        // Obtiene el rival mas cerca de la meeta, exceptuando a sí mismo y al minotauro
        var rivals = GameManager.Instance.players.Where(p => p != cpuPlayer && !(p is MinotaurPlayer))
            .OrderBy(p => GetDistanceToClosestWin(p.transform.position)).ToList();
        
        if (rivals.Count == 0) return new MoveDecision { isValid = false };

        // Busca todos los objetos puestos en el tablero, filtra solo los objetos que son movibles y 
        // son de dimensión 2x1
        List<GridOccupant> allMovableWalls = new List<GridOccupant>();
        foreach(var obj in FindObjectsByType<GridOccupant>(FindObjectsSortMode.None))
            if(obj.isMovable && (obj.baseSize.x >= 2 || obj.baseSize.y >= 2)) allMovableWalls.Add(obj);
        
        if(allMovableWalls.Count == 0) return new MoveDecision 
        { 
            isValid = false 
        };
        
        // Busca el muro que más lo está bloqueando
        GridOccupant selectedWall = GetBestWallBlockingMePathfinding(cpuPlayer, allMovableWalls);
        
        // Si ningun muro lo está bloqueando, toma al muro más cercano
        if (selectedWall == null) selectedWall = allMovableWalls.OrderBy(w => Vector3.Distance(w.transform.position, cpuPlayer.transform.position)).Take(3).First();
        
        // Evalua las jugadas hasta que encuentra la jugada con el mejor score
        MoveDecision bestDecision = new MoveDecision 
        { 
            isValid = false, score = -9999f 
        };
        foreach (var rival in rivals) {
            Vector3 rivalTargetWinPos = GetClosestWinPosition(rival.transform.position);
            MoveDecision potentialDecision;
            if(GetBestBlockDecision(selectedWall, rival, rivalTargetWinPos, out potentialDecision))
                if (potentialDecision.score > bestDecision.score) bestDecision = potentialDecision;

            // Si la jugada tiene de score mayor a 50, ya no sigue evaluando
            if (bestDecision.isValid && bestDecision.score > 50f) return bestDecision; 
        }
        
        // Devuelve la decisión
        return bestDecision.isValid ? bestDecision : new MoveDecision 
        { 
            isValid = false 
        };
    }

    // Calcula dónde debe colocarse el muro
    private bool GetBestBlockDecision(GridOccupant wall, Player targetPlayer, Vector3 targetWinPos, out MoveDecision decision) 
    {
        // Prepara una decisión por default
        decision = new MoveDecision 
        { 
            isValid = false, score = -9999f 
        };

        // Tira una línea recta imaginaria desde donde está el jugador rival hasta la meta
        Vector3 dirToWin = (targetWinPos - targetPlayer.transform.position).normalized;

        Vector3[] potentialDirections = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        
        // Evalua que dirección apunta más hacia la meta usando el producto punto y las ordena
        var sortedDirections = potentialDirections.OrderByDescending(d => Vector3.Dot(d, dirToWin)).ToArray();
        float gridSize = Pathfinding.Instance.gridSize;
        foreach(Vector3 dir in sortedDirections) 
        {
            if(Vector3.Dot(dir, dirToWin) < 0.1f) continue; // Solo evalua caminos que lleven a la victoria
            for (int dist = 1; dist <= 2; dist++) 
            {
                Vector3 targetPos = targetPlayer.transform.position + (dir * (gridSize * dist));
                targetPos = GetSnappedPosition(targetPos);
                
                GridOccupant occupier = GetWallAtPosition(targetPos); // Evalua si ya hay un muro en la casilla
                if (occupier != null && occupier != wall) continue;
                
                MoveDecision candidate;
                if(EvaluateMove(wall, targetPos, dir, targetWinPos, out candidate)) // Evalua la decisión
                {
                    candidate.score = 100f + (Vector3.Dot(dir, dirToWin) * 100f) + (3 - dist) * 20f + ((Vector3.Distance(wall.transform.position, candidate.finalPos) < 0.1f) ? 200f : 0f) - candidate.distToGoal;
                    if(candidate.score > decision.score) decision = candidate; // Guarda la decisión
                    break; 
                }
            }
        }
        return decision.isValid;
    }

    // Simula el movimiento del muro y lo evalua
    private bool EvaluateMove(GridOccupant wall, Vector3 targetTileCenter, Vector3 blockDirection, Vector3 finalGoalPos, out MoveDecision result) 
    {
        // Prepara una decisión por default
        result = new MoveDecision 
        { 
            isValid = false, score = -9999f 
        };

        // Guarda la posición y rotación original
        Vector3 originalPos = wall.transform.position; 
        Quaternion originalRot = wall.transform.rotation;
        
        // Define las rotaciones
        Quaternion[] rotations = { originalRot, originalRot * Quaternion.Euler(0, 90, 0) };
        
        var validCandidates = new List<(Vector3 pos, Quaternion rot, float distToGoal)>();
        
        // Si el muro ya esta muy cerca del objetivo 
        if (Vector3.Distance(originalPos, targetTileCenter) < 2.0f)
             validCandidates.Add((originalPos, originalRot, Vector3.Distance(originalPos, finalGoalPos)));

        // Prueba las rotaciones
        foreach(var rot in rotations) 
        {
            wall.transform.position = originalPos; 
            wall.transform.rotation = rot;
            List<Vector3> centers = wall.GetOccupiedWorldCenters();
            if(centers.Count == 0) continue;
            foreach(var anchor in centers) 
            {
                Vector3 trialPivotPos = targetTileCenter + (originalPos - anchor);
                if (Vector3.Distance(trialPivotPos, originalPos) < 0.1f && Quaternion.Angle(rot, originalRot) < 5f) continue;
                wall.transform.position = trialPivotPos;
                List<Vector3> pts = wall.GetOccupiedWorldCenters();
                if(BoardManager.Instance.ArePointWithinBounds(pts) && BoardManager.Instance.IsAreaFree(pts, wall) && !IsPlayerInArea(pts)) 
                {
                    Vector3 c = Vector3.zero; 
                    foreach(var p in pts) c += p; c /= pts.Count;
                    
                    // Guarda el candidato en la lista de opciones
                    validCandidates.Add((trialPivotPos, rot, Vector3.Distance(c, finalGoalPos)));
                }
            }
        }
        
        // Devuelve el transform del muro a su posición y rotación original
        wall.transform.position = originalPos; 
        wall.transform.rotation = originalRot;

        // Si hubo candidatos, los ordena de acuerdo al más cercano al jugador 
        // Empaqueta toda la información del muro y devuelve true
        if(validCandidates.Count > 0) 
        {
            var best = validCandidates.OrderBy(c => c.distToGoal).First();
            result = new MoveDecision 
            { 
                wall = wall, 
                finalPos = best.pos, 
                finalRot = best.rot, 
                isValid = true, 
                score = best.distToGoal, 
                distToGoal = best.distToGoal 
            };
            return true;
        }
        return false;
    }

    // Animación de movimiento del muro
    private IEnumerator AnimateCpuMoveSequence(MoveDecision decision, Action onTurnComplete, bool isSamePos) 
    {
        // Metadatos del muro
        GridOccupant wall = decision.wall; 
        Vector3 startPos = wall.transform.position;
        Vector3 targetPos = decision.finalPos; 
        Quaternion targetRot = decision.finalRot;
        
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);
        
        // Si la mejor jugada no es mover el muro, solo selecciona y vuelve a dejar el muro donde estaba
        if (isSamePos) 
        {
            if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
            float t = 0; while(t < 1f) 
            { 
                t += Time.deltaTime * 8f; 
                wall.transform.position = Vector3.Lerp(startPos, startPos + Vector3.up * 0.5f, Mathf.PingPong(t * 2, 1)); 
                yield return null; 
            }

            wall.transform.position = startPos; 
            yield return new WaitForSeconds(0.5f); 
            GameManager.Instance.EndMovementState(); 
            yield break;
        }

        // Clona el objeto original y lo coloca como un fantasma
        GameObject ghost = null;
        if(ghostMaterial != null) {
            ghost = new GameObject($"{wall.name}_GhostCPU");
            ghost.transform.position = startPos; 
            ghost.transform.rotation = wall.transform.rotation;
            ghost.transform.localScale = wall.transform.localScale;
            MeshFilter mf = wall.GetComponentInChildren<MeshFilter>();
            if(mf) 
            { 
                ghost.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh; 
                ghost.AddComponent<MeshRenderer>().material = ghostMaterial; 
            }
        }

        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();

        // Animación de levantamiento del muro
        float liftHeight = 0.7f; 
        Vector3 liftedStartPos = startPos + Vector3.up * liftHeight; 
        Vector3 liftedTargetPos = targetPos + Vector3.up * liftHeight;
        float timer = 0; 
        while(timer < 1f) 
        { 
            timer += Time.deltaTime * 5f; 
            wall.transform.position = Vector3.Lerp(startPos, liftedStartPos, timer); 
            yield return null; 
        }

        BoardManager.Instance.UnregisterOccupancy(wall);

        // Animación de rotación del muro
        if(Quaternion.Angle(wall.transform.rotation, targetRot) > 1f) 
        {
            yield return new WaitForSeconds(0.1f); 
            if(AudioManager.Instance != null) AudioManager.Instance.playRotateWall();
            timer = 0; 
            Quaternion currentRot = wall.transform.rotation;
            while(timer < 1f) 
            { 
                timer += Time.deltaTime * 5f; 
                wall.transform.rotation = Quaternion.Lerp(currentRot, targetRot, timer); 
                yield return null; 
            }
        }
        yield return new WaitForSeconds(0.2f);

        // Mueve al muro hacia el destino
        while (Vector3.Distance(wall.transform.position, liftedTargetPos) > 0.05f) 
        {
            wall.transform.position = Vector3.MoveTowards(wall.transform.position, liftedTargetPos, moveSpeed * Time.deltaTime);
            Vector3 tempPos = wall.transform.position; 
            wall.transform.position = new Vector3(tempPos.x, 0, tempPos.z); 
            List<Vector3> pts = wall.GetOccupiedWorldCenters();
            
            // Pinta el material del muro dependiendo de si es un lugar disponible o no
            wall.SetMoveFeedbackState(BoardManager.Instance.IsAreaFree(pts, wall) && BoardManager.Instance.ArePointWithinBounds(pts) && !IsPlayerInArea(pts));
            
            wall.transform.position = tempPos; 
            yield return null;
        }

        // Coordenadas para aterrizar el muro
        wall.transform.position = liftedTargetPos; 
        wall.transform.rotation = targetRot; 
        yield return new WaitForSeconds(0.2f);

        // Aterriza el muro suavemente
        timer = 0; 
        while(timer < 1f) 
        { 
            timer += Time.deltaTime * 8f; 
            wall.transform.position = Vector3.Lerp(liftedTargetPos, targetPos, timer); 
            yield return null; 
        }
        wall.transform.position = targetPos; 
        Physics.SyncTransforms();

        // Restaura el material del muro
        wall.RestoreVisuals(); 
        wall.SetDissolveValule(0f); // Oculta el muro para el efecto de shader

        if(AudioManager.Instance != null) AudioManager.Instance.playSetWall();
        yield return StartCoroutine(wall.AppearRoutine(1.2f)); // Ejecuta efecto de shader
        
        BoardManager.Instance.RegisterOccupancy(wall); // Registra al muro en el board
        if(ghost != null) Destroy(ghost);  // Destruye al fantasma
        
        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.EndMovementState();
    }

    // Obtiene el jugador más cerca a la meta
    private Player GetLeadingPlayer(Player cpuSelf) {
        
        // Obtenemos quién es el jugador humano/CPU que posee el turno según el GameManager
        Player ownerOfTurn = GameManager.Instance.players[GameManager.Instance.currenPlayerIndex];

        // Filtramos rivales: 
        // - No puede ser un Minotauro.
        // - No puede ser el dueño del turno (el que lanzó el dado).
        var rivals = GameManager.Instance.players
            .Where(p => !(p is MinotaurPlayer) && p != ownerOfTurn)
            .ToList();

        if (rivals.Count == 0) return null;

        // Ordenamos por cercanía a la meta
        var rankedRivals = rivals
            .Select(p => new { Player = p, Dist = GetDistanceToClosestWin(p.transform.position) })
            .OrderBy(x => x.Dist)
            .ToList();

        return rankedRivals.FirstOrDefault()?.Player;
    }

    // Obtiene la zona de victoria más cercana
    private Vector3 GetClosestWinPosition(Vector3 pos) 
    {
        if(allWinningPositions.Count == 0) return Vector3.zero;

        float min = float.MaxValue; 
        Vector3 closest = Vector3.zero;
        foreach(var winPos in allWinningPositions) 
        { 
            float d = Vector3.Distance(pos, winPos); 
            if(d < min) 
            { 
                min = d; closest = winPos; 
            } 
        }
        return closest;
    }

    // Regresa la distancia a la posición de victoria más cercana
    private float GetDistanceToClosestWin(Vector3 pos) 
    { 
        return Vector3.Distance(pos, GetClosestWinPosition(pos)); 
    }

    // Evalua si ya hay un jugador en una casilla
    private bool IsPlayerInArea(List<Vector3> areaPoints) 
    {
        foreach(var p in GameManager.Instance.players)
        {
            if(CheckOverlap(p.transform.position, areaPoints)) 
                return true;
        }

        if(GameManager.Instance.minotaurInstance != null && GameManager.Instance.minotaurInstance.gameObject != this.gameObject)
        {
            if(CheckOverlap(GameManager.Instance.minotaurInstance.transform.position, areaPoints)) 
                return true;
        }

        return false;
    }

    // Detecta si un punto esta dentro de la ubicación de algún muro
    private bool CheckOverlap(Vector3 unitPos, List<Vector3> wallPoints) 
    {
        foreach(var pt in wallPoints)
        {
            if(Vector2.Distance(new Vector2(unitPos.x, unitPos.z), new Vector2(pt.x, pt.z)) < 1.1f) 
                return true;
        }
        return false;
    }

    // Ajusta la posición al grid del tablero
    private Vector3 GetSnappedPosition(Vector3 rawPos) 
    {
        float gs = Pathfinding.Instance.gridSize;
        return new Vector3(Mathf.Floor(rawPos.x / gs) * gs + (gs / 2f), 0, Mathf.Floor(rawPos.z / gs) * gs + (gs / 2f));
    }

    // Valida si hay un muro en la posición
    private GridOccupant GetWallAtPosition(Vector3 position) 
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        foreach(var col in colliders) 
        {
            GridOccupant other = col.GetComponentInParent<GridOccupant>();
            if(other != null) return other;
        }
        return null;
    }
}