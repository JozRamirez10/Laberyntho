using UnityEngine;

// Se usa para rotar todo el tiempo la llave del HUD
public class KeySpinner : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 0, 90);
    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
