using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public abstract class Player : MonoBehaviour
{
    [Header("Entity")]
    public string characterName;
    public Color playerColor = Color.white;
    public Vector3 startPosition {get; set;}

    [Header("Components")]
    private Animator animator;

    [Header("IconGlow")]
    [SerializeField] private GameObject iconGlowObject;
    protected GameObject IconGlowObject => iconGlowObject;
    private GlowPulserIcon glowPulser;

    [Header("Movement Adjustments")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rotationSpeed = 360f;
    private float raycastOriginHeight = 5f;
    private float raycastMaxDistance = 10f;

    private bool isItMyTurn = false;
    private bool hasHadFirstPlanningTurn = false;

    public virtual void Initialize(Vector3 initialPos)
    {
        this.startPosition = initialPos;
        InstantMoveTo(initialPos);
    }

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if(animator == null) Debug.Log($"Falta el componente Animator en {characterName}");

        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            if(GameManager.Instance.players.Count > 0 && GameManager.Instance.currenPlayerIndex > -1)
            {
                Player currentPlayer = GameManager.Instance.players[GameManager.Instance.currenPlayerIndex];
                HandleTurnChanged(currentPlayer);
            }
        }

        if(iconGlowObject != null)
        {
            glowPulser = iconGlowObject.GetComponent<GlowPulserIcon>();
            if(glowPulser != null)
            {
                glowPulser.SetGlowState(false, false);
            }
            else
            {
                iconGlowObject.SetActive(false);
            }
        }
    }

    public void InstantMoveTo(Vector3 targetPos)
    {
        SnapToGround(targetPos);
        transform.LookAt(Vector3.zero);
    }

    public void LookAt(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        if(direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void ActiveGlow(bool enablePulse)
    {
        if(glowPulser != null)
        {
            glowPulser.SetGlowState(true, enablePulse);
        }else if(iconGlowObject != null && !enablePulse)
        {
            iconGlowObject.SetActive(true);
        }
    }

    private void HandleTurnChanged(Player currentPlayer)
    {
        isItMyTurn = (currentPlayer == this);
        if(animator != null)
        {
            animator.SetBool("isMyTurn", isItMyTurn);
        }

        if (!isItMyTurn)
        {
            if(glowPulser != null)
            {
                glowPulser.SetGlowState(false, false);
            }
            else if(iconGlowObject != null)
            {
                iconGlowObject.SetActive(false);
            }
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if(state == GameState.TurnPlanning && isItMyTurn && !hasHadFirstPlanningTurn)
        {

            Debug.Log($"{characterName}: Aplicando alineación cardinal inicial.");
            Vector3 dirToCenter = (Vector3.zero - transform.position).normalized;
            Vector3 snappedDirToCenter = Vector3.forward;

            if(Mathf.Abs(dirToCenter.x) > Mathf.Abs(dirToCenter.z))
            {
                snappedDirToCenter = dirToCenter.x > 0 ? Vector3.right : Vector3.left;
            }
            else
            {
                snappedDirToCenter = dirToCenter.z > 0 ? Vector3.forward : Vector3.back;
            }
            
            transform.rotation = Quaternion.LookRotation(snappedDirToCenter);
            
            hasHadFirstPlanningTurn = true;
        }
    }

    public void Move(List<Vector3> finalPath, Action onMovementComplete)
    {
        if(finalPath == null || finalPath.Count == 0)
        {
            onMovementComplete?.Invoke();
            return;
        }

        StartCoroutine(MovementRoutine(finalPath, onMovementComplete));
    }

    protected virtual IEnumerator PreStepCheck(Vector3 currentPos, Vector3 nextPos)
    {
        yield break;
    }

    protected IEnumerator MovementRoutine(List<Vector3> path, Action onComplete)
    {
        Debug.Log($"{characterName} comienza a moverse... ");

        if(animator != null) animator.SetBool("isMoving", true);

        foreach(Vector3 targetStepPosFlat in path)
        {
            yield return StartCoroutine(PreStepCheck(transform.position, targetStepPosFlat));

            float timeToMove = 1f;
            float elapsedTime = 0f;

            Vector3 startStepPos = transform.position;

            Vector3 directionToLook = (targetStepPosFlat - new Vector3(startStepPos.x, 0, startStepPos.z)).normalized;
            Quaternion startRotation = transform.rotation;

            Quaternion targetRotation = transform.rotation;
            if(directionToLook != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(directionToLook, Vector3.up);
            }
            else
            {
                targetRotation = startRotation;
            }

            while(elapsedTime < timeToMove)
            {

                elapsedTime += Time.deltaTime;
                float percentageComplete = elapsedTime / timeToMove;

                float newX = Mathf.Lerp(startStepPos.x, targetStepPosFlat.x, percentageComplete);
                float newZ = Mathf.Lerp(startStepPos.z, targetStepPosFlat.z, percentageComplete);

                Vector3 rayOrigin = new Vector3(newX, transform.position.y + raycastOriginHeight, newZ);
                
                RaycastHit hit;
                float finalHeight = transform.position.y;

                if(Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastMaxDistance, groundLayer))
                {
                    finalHeight = hit.point.y;
                }
                transform.position = new Vector3(newX, finalHeight, newZ);

                float rotationStep = rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed);
                
                yield return null;
            }
            SnapToGround(targetStepPosFlat);
            transform.rotation = targetRotation;
        }
        Debug.Log($"{characterName} terminó de moverse");

        if(animator != null) animator.SetBool("isMoving", false);

        onComplete?.Invoke();
    }

    private void SnapToGround(Vector3 targetPos)
    {
        RaycastHit hit;
        Vector3 origin = targetPos + Vector3.up * 50f;
        if(Physics.Raycast(origin, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            transform.position = hit.point;
        }
        else
        {
            transform.position = new Vector3(targetPos.x, 0f, targetPos.z);
        }
    }

    void OnDestroy()
    {
        if(GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
        }
    }

}
