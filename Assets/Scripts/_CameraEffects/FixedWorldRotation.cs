using UnityEngine;

// Se usa para que el icono del jugador siempre se vea correcto
public class FixedWorldRotation : MonoBehaviour
{
    void LateUpdate() {
        // La rotación del objeto siempre es de 90 grados en el eje X
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);   
    }
}
