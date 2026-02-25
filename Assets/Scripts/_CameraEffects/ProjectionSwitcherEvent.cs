using UnityEngine;
using Unity.Cinemachine;

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
        // Si faltan componentes o el cerebro no está activo, no hacemos nada.
        if (brain == null || !brain.enabled || mainCamera == null) return;

        // 1. Obtenemos la interfaz de la cámara virtual activa actualmente.
        ICinemachineCamera activeICam = brain.ActiveVirtualCamera;

        // 2. Intentamos convertirla al componente concreto 'CinemachineCamera'.
        // El operador 'as' devolverá null si la cámara activa es una mezcla (BlendListCamera) o algo complejo.
        // Como estás usando Cortes (Cuts), esto debería obtener la cámara correcta instantáneamente.
        CinemachineCamera activeVcam = activeICam as CinemachineCamera;

        if (activeVcam != null)
        {
            // 3. LEEMOS DIRECTAMENTE LA CONFIGURACIÓN DEL INSPECTOR DEL COMPONENTE.
            // No leemos el .State, leemos lo que tú configuraste en la sección "Lens -> Mode Override".
            LensSettings.OverrideModes mode = activeVcam.Lens.ModeOverride;

            // 4. Aplicamos el modo solo si es diferente al actual para ser eficientes.
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
