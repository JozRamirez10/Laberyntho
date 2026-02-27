using UnityEngine;
using System;
using System.Collections;

// Controlador del dado
public class DiceController : MonoBehaviour
{
    private Animator diceAnimator;
    private Action onRollComplete;

    void Start()
    {
        diceAnimator = GetComponent<Animator>();
    }

    // Animación del dado dependiendo de resultNumber

    public void PlayDiceAnimation(int resultNumber, Action onFinishedCallback)
    {
        onRollComplete = onFinishedCallback;
        string animName = resultNumber.ToString();

        if(diceAnimator != null)
        {
            diceAnimator.Rebind(); // Reinicia la animación
            diceAnimator.Update(0f);

            diceAnimator.Play(animName, -1, 0); // Reproduce la animación
            
            // Espera a que termine la animación
            StartCoroutine(WaitAndCallbackRoutine(resultNumber)); 
        }
        else onFinishedCallback?.Invoke();
    }

    private IEnumerator WaitAndCallbackRoutine(int resultNumber)
    {
        yield return null;

        while(diceAnimator.IsInTransition(0)) yield return null;

        AnimatorStateInfo stateInfo = diceAnimator.GetCurrentAnimatorStateInfo(0);
        
        float animationDuration = stateInfo.length;
        yield return new WaitForSeconds(animationDuration);
        
        onRollComplete?.Invoke();
    }

    // Sonido si en el dado sale el Minotauro
    public void PlayMinotaurSFX()
    {
        if(AudioManager.Instance != null) AudioManager.Instance.playConfirmAttack();
    }

}
