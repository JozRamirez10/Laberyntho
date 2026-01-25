using UnityEngine;

public class CameraTargetSmoother : MonoBehaviour
{
    public float smoothSpeed = 10f;

    private Transform rawTargetToFollow;

    void Start()
    {
        transform.parent = null;
        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null) rb.isKinematic = true;

        Collider col = GetComponent<Collider>();
        if(col != null) col.enabled = false;
    }

    public void SetTarget(Transform newTarget)
    {
        if(rawTargetToFollow == null && newTarget != null)
        {
            transform.position = newTarget.position;
            transform.rotation = newTarget.rotation;
        }
        rawTargetToFollow = newTarget;
    }

    void LateUpdate() 
    {
        if(rawTargetToFollow == null) return;
        transform.position = Vector3.Lerp(transform.position, rawTargetToFollow.position, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, rawTargetToFollow.rotation, Time.deltaTime * smoothSpeed);
    }
}
