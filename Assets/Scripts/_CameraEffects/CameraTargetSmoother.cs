using UnityEngine;

// Fantasma para la cámara
// Persigue al jugador de forma suave
public class CameraTargetSmoother : MonoBehaviour
{
    public float smoothSpeed = 10f;

    private Transform rawTargetToFollow; // Guarda el transform del jugador actual

    void Start()
    {
        transform.parent = null;

        // Por validación vuelve al objeto cinemático, no le afectan las físicas
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null) rb.isKinematic = true;

        // Apaga las colisiones del objeto
        Collider col = GetComponent<Collider>();
        if(col != null) col.enabled = false;
    }

    // Define el objeto al que seguirá la cámara
    public void SetTarget(Transform newTarget)
    {
        if(rawTargetToFollow == null && newTarget != null)
        {
            transform.position = newTarget.position;
            transform.rotation = newTarget.rotation;
        }
        rawTargetToFollow = newTarget; // Guarda el nuevo objetivo
    }

    void LateUpdate() // Se ejecuta después de todos los demás objetos
    {
        if(rawTargetToFollow == null) return;
        // Hace una transición suave del objeto a su nueva posición
        transform.position = Vector3.Lerp(transform.position, rawTargetToFollow.position, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rawTargetToFollow.rotation, Time.deltaTime * smoothSpeed);
    }
}
