using UnityEngine;

public class KeySpinner : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 0, 90);
    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
