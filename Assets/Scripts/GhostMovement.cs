using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Transform mainCameraTransform;

    void Awake()
    {
        // Obtenemos las referencias una sola vez al principio
        controller = GetComponent<CharacterController>();
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("GhostMovement: No se encontró la cámara principal (Camera.main).");
        }
    }

    // ESTA ES LA CLAVE: Se ejecuta cada vez que el script se activa (Tecla N)
    void OnEnable()
    {
        if (controller != null)
        {
            // Truco sucio pero efectivo de Unity:
            // Desactivar y reactivar el CharacterController fuerza un reset de sus físicas.
            // Esto evita que se quede "atascado" después de ser teletransportado.
            controller.enabled = false;
            controller.enabled = true;
            Debug.Log("GhostMovement: CharacterController reiniciado físicamente.");
        }
    }

    void Update()
    {
        // Si algo falló en el Awake, no intentamos movernos para evitar errores
        if (mainCameraTransform == null || controller == null) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 camForward = mainCameraTransform.forward;
        Vector3 camRight = mainCameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * v + camRight * h).normalized;

        if (moveDir.magnitude > 0.1f)
        {
            // Movemos usando el controlador físico
            controller.Move(moveDir * moveSpeed * Time.deltaTime);
            
            // Rotamos visualmente
            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }
}