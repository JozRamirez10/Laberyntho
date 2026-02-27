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

    private struct MoveDecision {
        public GridOccupant wall;
        public Vector3 finalPos;
        public Quaternion finalRot;
        public bool isValid;
        public float score;
        public float distToGoal;
    }

    void Awake() {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitializeWinningPositions() {
        allWinningPositions.Clear();
        GridOccupant[] occupants = FindObjectsByType<GridOccupant>(FindObjectsSortMode.None);
        foreach(var occ in occupants) {
            if (occ.isWinZone) allWinningPositions.AddRange(occ.GetOccupiedWorldCenters());
        }
    }

    public void StartTurn(Player playerToMove, int stepsAvailable, bool isMinotaurTurn, Action onTurnComplete) {
        if(stepsAvailable == 7) StartCoroutine(HandleWallMoveTurn(playerToMove, onTurnComplete));
        else StartCoroutine(ExecuteTurnRoutine(playerToMove, stepsAvailable, isMinotaurTurn, onTurnComplete));
    }

    private IEnumerator ExecuteTurnRoutine(Player playerToMove, int stepsAvailable, bool isMinotaurTurn, Action onTurnComplete) {
        if (Pathfinding.Instance == null || GameManager.Instance.inputController == null) { onTurnComplete?.Invoke(); yield break; }

        if (GameManager.Instance.cameraManager != null)
            GameManager.Instance.cameraManager.ForceTopDownView(true);

        yield return new WaitForSeconds(thinkingTime);

        Vector3 target = Vector3.zero;
        int availableKeys = (playerToMove as ExplorerPlayer)?.GetKeyCount() ?? 0;
        if(isMinotaurTurn) availableKeys = 0;

        if (isMinotaurTurn) {
            Player leader = GetLeadingPlayer(playerToMove);
            if(leader != null) target = leader.transform.position;
        } else if(allWinningPositions.Count > 0) {
            target = GetClosestWinPosition(playerToMove.transform.position);
        }

        List<Vector3> fullPath = Pathfinding.Instance.FindPath(playerToMove.transform.position, target, availableKeys);
        
        if(fullPath == null || fullPath.Count == 0) {
            if (GameManager.Instance.cameraManager != null) GameManager.Instance.cameraManager.ForceTopDownView(false);
            onTurnComplete?.Invoke(); 
            yield break; 
        }

        int stepsToTake = Mathf.Min(stepsAvailable, fullPath.Count);
        List<Vector3> finalPath = fullPath.GetRange(0, stepsToTake);

        GameManager.Instance.inputController.StartTurnPlanning(playerToMove, stepsAvailable, isMinotaurTurn);

        // CORRECCIÓN VISUAL: Forzamos que la posición inicial de referencia para los fantasmas 
        // esté alineada al grid. Si el minotauro está en el centro (0,0,0), esto evitará 
        // que el primer cálculo de dirección sea diagonal.
        Vector3 currentPhantomPosReference = GetSnappedPosition(playerToMove.transform.position);
        currentPhantomPosReference.y = 0.05f; 

        bool eventTriggered = false;

        foreach (Vector3 nextStepPos in finalPath) {
            yield return new WaitForSeconds(stepSelectionDelay);
            
            Vector3 targetVisualPos = new Vector3(nextStepPos.x, 0.05f, nextStepPos.z);
            
            // Calculamos la dirección usando la referencia alineada
            Vector3 direction = (targetVisualPos - currentPhantomPosReference).normalized;
            
            // Si la dirección es oblicua por errores de precisión, forzamos ejes cardinales
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                direction = new Vector3(Mathf.Sign(direction.x), 0, 0);
            else
                direction = new Vector3(0, 0, Mathf.Sign(direction.z));

            if (direction == Vector3.zero) direction = playerToMove.transform.forward;

            GameManager.Instance.inputController.SimulateCPUStep(targetVisualPos, direction);
            
            // Actualizamos la referencia para el siguiente fantasma
            currentPhantomPosReference = targetVisualPos;

            if (UIManager.Instance != null && UIManager.Instance.confirmationPanel.activeSelf) {
                eventTriggered = true;
                break;
            }
        }

        yield return new WaitForSeconds(thinkingTime);

        if (!eventTriggered) {
            GameManager.Instance.inputController.ConfirmMovement();
            yield return new WaitForSeconds(0.6f); 
        }

        int safetyLimit = 2;
        while (UIManager.Instance != null && UIManager.Instance.confirmationPanel.activeSelf && safetyLimit > 0) {
            if (AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
            UIManager.Instance.popupConfirmButton.onClick.Invoke();
            safetyLimit--;
            yield return new WaitForSeconds(0.7f); 
        }

        if (GameManager.Instance.cameraManager != null)
            GameManager.Instance.cameraManager.ForceTopDownView(false);
    }

    // [Resto del código sin cambios]
    private GridOccupant GetBestWallBlockingMePathfinding(Player self, List<GridOccupant> walls) {
        if (Pathfinding.Instance == null) return null;
        Vector3 targetMeta = GetClosestWinPosition(self.transform.position);
        int myKeys = (self as ExplorerPlayer)?.GetKeyCount() ?? 0;
        List<Vector3> idealPath = Pathfinding.Instance.FindPath(self.transform.position, targetMeta, myKeys);
        if (idealPath == null || idealPath.Count == 0) return null;
        Dictionary<GridOccupant, int> blockagePower = new Dictionary<GridOccupant, int>();
        foreach (var wall in walls) {
            int pointsIntersected = 0;
            List<Vector3> wallOccupiedCells = wall.GetOccupiedWorldCenters();
            foreach (Vector3 pathNode in idealPath)
                foreach (Vector3 wallCell in wallOccupiedCells)
                    if (Vector3.Distance(new Vector3(pathNode.x, 0, pathNode.z), new Vector3(wallCell.x, 0, wallCell.z)) < 0.5f)
                        pointsIntersected++;
            if (pointsIntersected > 0) blockagePower[wall] = pointsIntersected;
        }
        return blockagePower.Count == 0 ? null : blockagePower.OrderByDescending(x => x.Value).First().Key;
    }

    private IEnumerator HandleWallMoveTurn(Player cpuPlayer, Action onTurnComplete) {
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(true);
        yield return new WaitForSeconds(thinkingTime);
        Physics.SyncTransforms(); 
        MoveDecision decision = CalculateBestWallMove(cpuPlayer);
        if (!decision.isValid) {
            if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);
            onTurnComplete?.Invoke();
            yield break;
        }
        yield return StartCoroutine(AnimateCpuMoveSequence(decision, onTurnComplete, false));
    }

    private MoveDecision CalculateBestWallMove(Player cpuPlayer) {
        var rivals = GameManager.Instance.players.Where(p => p != cpuPlayer && !(p is MinotaurPlayer))
            .OrderBy(p => GetDistanceToClosestWin(p.transform.position)).ToList();
        if (rivals.Count == 0) return new MoveDecision { isValid = false };
        List<GridOccupant> allMovableWalls = new List<GridOccupant>();
        foreach(var obj in FindObjectsByType<GridOccupant>(FindObjectsSortMode.None))
            if(obj.isMovable && (obj.baseSize.x >= 2 || obj.baseSize.y >= 2)) allMovableWalls.Add(obj);
        if(allMovableWalls.Count == 0) return new MoveDecision { isValid = false };
        GridOccupant selectedWall = GetBestWallBlockingMePathfinding(cpuPlayer, allMovableWalls);
        if (selectedWall == null) selectedWall = allMovableWalls.OrderBy(w => Vector3.Distance(w.transform.position, cpuPlayer.transform.position)).Take(3).First();
        MoveDecision bestDecision = new MoveDecision { isValid = false, score = -9999f };
        foreach (var rival in rivals) {
            Vector3 rivalTargetWinPos = GetClosestWinPosition(rival.transform.position);
            MoveDecision potentialDecision;
            if(GetBestBlockDecision(selectedWall, rival, rivalTargetWinPos, out potentialDecision))
                if (potentialDecision.score > bestDecision.score) bestDecision = potentialDecision;
            if (bestDecision.isValid && bestDecision.score > 50f) return bestDecision;
        }
        return bestDecision.isValid ? bestDecision : new MoveDecision { isValid = false };
    }

    private bool GetBestBlockDecision(GridOccupant wall, Player targetPlayer, Vector3 targetWinPos, out MoveDecision decision) {
        decision = new MoveDecision { isValid = false, score = -9999f };
        Vector3 dirToWin = (targetWinPos - targetPlayer.transform.position).normalized;
        Vector3[] potentialDirections = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        var sortedDirections = potentialDirections.OrderByDescending(d => Vector3.Dot(d, dirToWin)).ToArray();
        float gridSize = Pathfinding.Instance.gridSize;
        foreach(Vector3 dir in sortedDirections) {
            if(Vector3.Dot(dir, dirToWin) < 0.1f) continue;
            for (int dist = 1; dist <= 2; dist++) {
                Vector3 targetPos = targetPlayer.transform.position + (dir * (gridSize * dist));
                targetPos = GetSnappedPosition(targetPos);
                GridOccupant occupier = GetWallAtPosition(targetPos);
                if (occupier != null && occupier != wall) continue;
                MoveDecision candidate;
                if(EvaluateMove(wall, targetPos, dir, targetWinPos, out candidate)) {
                    candidate.score = 100f + (Vector3.Dot(dir, dirToWin) * 100f) + (3 - dist) * 20f + ((Vector3.Distance(wall.transform.position, candidate.finalPos) < 0.1f) ? 200f : 0f) - candidate.distToGoal;
                    if(candidate.score > decision.score) decision = candidate;
                    break; 
                }
            }
        }
        return decision.isValid;
    }

    private bool EvaluateMove(GridOccupant wall, Vector3 targetTileCenter, Vector3 blockDirection, Vector3 finalGoalPos, out MoveDecision result) {
        result = new MoveDecision { isValid = false, score = -9999f };
        Vector3 originalPos = wall.transform.position; Quaternion originalRot = wall.transform.rotation;
        Quaternion[] rotations = { originalRot, originalRot * Quaternion.Euler(0, 90, 0) };
        var validCandidates = new List<(Vector3 pos, Quaternion rot, float distToGoal)>();
        if (Vector3.Distance(originalPos, targetTileCenter) < 2.0f)
             validCandidates.Add((originalPos, originalRot, Vector3.Distance(originalPos, finalGoalPos)));
        foreach(var rot in rotations) {
            wall.transform.position = originalPos; wall.transform.rotation = rot;
            List<Vector3> centers = wall.GetOccupiedWorldCenters();
            if(centers.Count == 0) continue;
            foreach(var anchor in centers) {
                Vector3 trialPivotPos = targetTileCenter + (originalPos - anchor);
                if (Vector3.Distance(trialPivotPos, originalPos) < 0.1f && Quaternion.Angle(rot, originalRot) < 5f) continue;
                wall.transform.position = trialPivotPos;
                List<Vector3> pts = wall.GetOccupiedWorldCenters();
                if(BoardManager.Instance.ArePointWithinBounds(pts) && BoardManager.Instance.IsAreaFree(pts, wall) && !IsPlayerInArea(pts)) {
                    Vector3 c = Vector3.zero; foreach(var p in pts) c += p; c /= pts.Count;
                    validCandidates.Add((trialPivotPos, rot, Vector3.Distance(c, finalGoalPos)));
                }
            }
        }
        wall.transform.position = originalPos; wall.transform.rotation = originalRot;
        if(validCandidates.Count > 0) {
            var best = validCandidates.OrderBy(c => c.distToGoal).First();
            result = new MoveDecision { wall = wall, finalPos = best.pos, finalRot = best.rot, isValid = true, score = best.distToGoal, distToGoal = best.distToGoal };
            return true;
        }
        return false;
    }

    private IEnumerator AnimateCpuMoveSequence(MoveDecision decision, Action onTurnComplete, bool isSamePos) {
        GridOccupant wall = decision.wall; Vector3 startPos = wall.transform.position;
        Vector3 targetPos = decision.finalPos; Quaternion targetRot = decision.finalRot;
        if(BoardManager.Instance != null) BoardManager.Instance.ToggleHighlightMovableObjects(false);
        if (isSamePos) {
            if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
            float t = 0; while(t < 1f) { t += Time.deltaTime * 8f; wall.transform.position = Vector3.Lerp(startPos, startPos + Vector3.up * 0.5f, Mathf.PingPong(t * 2, 1)); yield return null; }
            wall.transform.position = startPos; yield return new WaitForSeconds(0.5f); GameManager.Instance.EndMovementState(); yield break;
        }
        GameObject ghost = null;
        if(ghostMaterial != null) {
            ghost = new GameObject($"{wall.name}_GhostCPU");
            ghost.transform.position = startPos; ghost.transform.rotation = wall.transform.rotation;
            ghost.transform.localScale = wall.transform.localScale;
            MeshFilter mf = wall.GetComponentInChildren<MeshFilter>();
            if(mf) { ghost.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh; ghost.AddComponent<MeshRenderer>().material = ghostMaterial; }
        }
        if(AudioManager.Instance != null) AudioManager.Instance.playToConfirm();
        float liftHeight = 0.7f; Vector3 liftedStartPos = startPos + Vector3.up * liftHeight; Vector3 liftedTargetPos = targetPos + Vector3.up * liftHeight;
        float timer = 0; while(timer < 1f) { timer += Time.deltaTime * 5f; wall.transform.position = Vector3.Lerp(startPos, liftedStartPos, timer); yield return null; }
        BoardManager.Instance.UnregisterOccupancy(wall);
        if(Quaternion.Angle(wall.transform.rotation, targetRot) > 1f) {
            yield return new WaitForSeconds(0.1f); if(AudioManager.Instance != null) AudioManager.Instance.playRotateWall();
            timer = 0; Quaternion currentRot = wall.transform.rotation;
            while(timer < 1f) { timer += Time.deltaTime * 5f; wall.transform.rotation = Quaternion.Lerp(currentRot, targetRot, timer); yield return null; }
        }
        yield return new WaitForSeconds(0.2f); 
        while (Vector3.Distance(wall.transform.position, liftedTargetPos) > 0.05f) {
            wall.transform.position = Vector3.MoveTowards(wall.transform.position, liftedTargetPos, moveSpeed * Time.deltaTime);
            Vector3 tempPos = wall.transform.position; wall.transform.position = new Vector3(tempPos.x, 0, tempPos.z); 
            List<Vector3> pts = wall.GetOccupiedWorldCenters();
            wall.SetMoveFeedbackState(BoardManager.Instance.IsAreaFree(pts, wall) && BoardManager.Instance.ArePointWithinBounds(pts) && !IsPlayerInArea(pts));
            wall.transform.position = tempPos; yield return null;
        }
        wall.transform.position = liftedTargetPos; 
        wall.transform.rotation = targetRot; 
        yield return new WaitForSeconds(0.2f);

        timer = 0; 
        while(timer < 1f) 
        { 
            timer += Time.deltaTime * 8f; 
            wall.transform.position = Vector3.Lerp(liftedTargetPos, targetPos, timer); 
            yield return null; 
        }
        wall.transform.position = targetPos; 
        Physics.SyncTransforms();

        wall.RestoreVisuals(); 
        wall.SetDissolveValule(0f);

        if(AudioManager.Instance != null) AudioManager.Instance.playSetWall();
        yield return StartCoroutine(wall.AppearRoutine(1.2f));
        
        BoardManager.Instance.RegisterOccupancy(wall);
        if(ghost != null) Destroy(ghost); 
        
        
        yield return new WaitForSeconds(0.5f);

        GameManager.Instance.EndMovementState();
    }

    private Player GetLeadingPlayer(Player cpuSelf) {
        // 1. Obtenemos quién es el jugador humano/CPU que posee el turno según el GameManager
        Player ownerOfTurn = GameManager.Instance.players[GameManager.Instance.currenPlayerIndex];

        // 2. Filtramos rivales: 
        // - No puede ser un Minotauro.
        // - No puede ser el dueño del turno (el que lanzó el dado).
        var rivals = GameManager.Instance.players
            .Where(p => !(p is MinotaurPlayer) && p != ownerOfTurn)
            .ToList();

        if (rivals.Count == 0) return null;

        // 3. Ordenamos por cercanía a la meta
        var rankedRivals = rivals
            .Select(p => new { Player = p, Dist = GetDistanceToClosestWin(p.transform.position) })
            .OrderBy(x => x.Dist)
            .ToList();

        return rankedRivals.FirstOrDefault()?.Player;
    }
    private Vector3 GetClosestWinPosition(Vector3 pos) {
        if(allWinningPositions.Count == 0) return Vector3.zero;
        float min = float.MaxValue; Vector3 closest = Vector3.zero;
        foreach(var winPos in allWinningPositions) { float d = Vector3.Distance(pos, winPos); if(d < min) { min = d; closest = winPos; } }
        return closest;
    }
    private float GetDistanceToClosestWin(Vector3 pos) { return Vector3.Distance(pos, GetClosestWinPosition(pos)); }
    private bool IsPlayerInArea(List<Vector3> areaPoints) {
        foreach(var p in GameManager.Instance.players) if(CheckOverlap(p.transform.position, areaPoints)) return true;
        if(GameManager.Instance.minotaurInstance != null && GameManager.Instance.minotaurInstance.gameObject != this.gameObject)
            if(CheckOverlap(GameManager.Instance.minotaurInstance.transform.position, areaPoints)) return true;
        return false;
    }
    private bool CheckOverlap(Vector3 unitPos, List<Vector3> wallPoints) {
        foreach(var pt in wallPoints) if(Vector2.Distance(new Vector2(unitPos.x, unitPos.z), new Vector2(pt.x, pt.z)) < 1.1f) return true;
        return false;
    }
    private Vector3 GetSnappedPosition(Vector3 rawPos) {
        float gs = Pathfinding.Instance.gridSize;
        return new Vector3(Mathf.Floor(rawPos.x / gs) * gs + (gs / 2f), 0, Mathf.Floor(rawPos.z / gs) * gs + (gs / 2f));
    }
    private GridOccupant GetWallAtPosition(Vector3 position) {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        foreach(var col in colliders) {
            GridOccupant other = col.GetComponentInParent<GridOccupant>();
            if(other != null) return other;
        }
        return null;
    }
}