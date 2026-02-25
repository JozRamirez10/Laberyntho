using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float rotationSpeed = 10f;

    private CharacterController controller;
    private Transform mainCameraTransform;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        else return;
    }

    void OnEnable()
    {
        if (controller != null)
        {
            controller.enabled = false;
            controller.enabled = true;
        }
    }

    void Update()
    {
        if(UIPauseManager.Instace.isPaused) return;

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