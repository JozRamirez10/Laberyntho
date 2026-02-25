using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MinotaurPlayer : Player
{
    [Header("Attack Settings - Minotaur")]
    [SerializeField] private string roarBool = "isAttacking";
    [SerializeField] private float postRoarWaitTime = 0.5f;
    [SerializeField] private float preRespawnCameraDelay = 1.0f;

    public Quaternion startRotation;

    private TurnInputController turnController;

    protected override void Start()
    {
        base.Start();
        startRotation = transform.rotation;
    }

    public override void InstantMoveTo(Vector3 targetPos)
    {
        base.SnapToGround(targetPos);
        transform.rotation = startRotation;
    }
    
    public void BeginAttackSequence(List<Vector3> approachPath, Transform victimTransform, Action onSequenceComplete, TurnInputController turnController)
    {
        this.turnController = turnController;
        StartCoroutine(AttackSequenceRoutine(approachPath, victimTransform, onSequenceComplete));
    }

    private IEnumerator AttackSequenceRoutine(List<Vector3> approachPath, Transform victimTransform, Action onComplete)
    {
        Animator anim = GetComponentInChildren<Animator>();
        
        if(approachPath != null && approachPath.Count > 0)
        {
            yield return StartCoroutine(MovementRoutine(approachPath, null));
        }

        if(victimTransform == null)
        {
            onComplete?.Invoke();
            yield break;
        }
        
        Vector3 directionToVictim = (victimTransform.position - transform.position).normalized;
        directionToVictim.y = 0;
        if(directionToVictim != Vector3.zero) transform.rotation = Quaternion.LookRotation(directionToVictim);

        Player victimPlayer = victimTransform.GetComponent<Player>();
        if(victimPlayer != null) victimPlayer.LookAt(this.transform.position);

        if(anim != null)
        {
            anim.SetBool(roarBool, true);
            
            // yield return new WaitForSeconds(0.1f);
            yield return null;
            yield return new WaitForEndOfFrame();

            while (anim.IsInTransition(0)) yield return null;
            
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            float animationDuration = stateInfo.length;

            yield return new WaitForSeconds(animationDuration);

            anim.SetBool(roarBool, false);

            yield return new WaitForSeconds(postRoarWaitTime);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        ExplorerPlayer victimExplorer = victimTransform.GetComponent<ExplorerPlayer>();
        if(victimExplorer != null)
        {
            victimExplorer.PlayDeathEffect();
            yield return new WaitForSeconds(preRespawnCameraDelay);

            if(turnController != null) turnController.ForceCameraFocus(victimTransform);
            else if(GameManager.Instance != null && GameManager.Instance.cameraManager != null)
            {
                GameManager.Instance.cameraManager.SetCameraTarget(victimTransform);
            }

            victimExplorer.InstantMoveTo(victimExplorer.startPosition);
            this.InstantMoveTo(this.startPosition);
            
            yield return new WaitForSeconds(0.5f);

            bool respawnFinished = false;
            yield return StartCoroutine(victimExplorer.PlayRespawnSequence( () => respawnFinished = true));

            while(!respawnFinished) yield return null;

            yield return new WaitForSeconds(0.5f);

            if(turnController != null) turnController.ForceCameraFocus(this.transform);
            else if(GameManager.Instance != null && GameManager.Instance.cameraManager != null)
            {
                GameManager.Instance.cameraManager.SetCameraTarget(this.transform);
            }
            
            yield return new WaitForSeconds(0.8f);
        }

        onComplete?.Invoke();
    }

    public void PlayRoarSFX()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playRoarMinotaur();
    }

}
