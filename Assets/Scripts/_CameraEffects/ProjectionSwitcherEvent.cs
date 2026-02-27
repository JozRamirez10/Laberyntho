using UnityEngine;
using Unity.Cinemachine;

// Obliga a cambiar la cámara de perspectiva a ortográfica y visceversa
public class ProjectionSwitcherEvent : MonoBehaviour
{
    private Camera mainCamera;
    private CinemachineBrain brain;

    void OnEnable()
    {
        mainCamera = GetComponent<Camera>();
        brain = GetComponent<CinemachineBrain>();
    }

    void LateUpdate()
    {
        if (brain == null || !brain.enabled || mainCamera == null) return;

        // Interfaz de la cámara virtual actual
        ICinemachineCamera activeICam = brain.ActiveVirtualCamera;

        // Intentamos convertirla al componente concreto 'CinemachineCamera'.
        // El operador 'as' devolverá null si la cámara activa es una mezcla (BlendListCamera) o algo complejo.
        // Como estás usando Cortes (Cuts), esto debería obtener la cámara correcta instantáneamente.
        CinemachineCamera activeVcam = activeICam as CinemachineCamera;

        if (activeVcam != null)
        {
            // .State == "Lens -> Mode Override".
            LensSettings.OverrideModes mode = activeVcam.Lens.ModeOverride;

            // Solo si es diferente al actual
            switch (mode)
            {
                case LensSettings.OverrideModes.Perspective:
                    if (mainCamera.orthographic)
                    {
                        mainCamera.orthographic = false; // Forzar Perspectiva
                    }
                    break;

                case LensSettings.OverrideModes.Orthographic:
                    if (!mainCamera.orthographic)
                    {
                        mainCamera.orthographic = true; // Forzar Ortográfica
                    }
                    break;
                
                // Si está en None, no tocamos la cámara física.
                case LensSettings.OverrideModes.None:
                default:
                    break;
            }
        }
    }
}
