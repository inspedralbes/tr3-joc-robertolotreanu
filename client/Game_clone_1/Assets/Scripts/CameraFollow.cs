using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Arrossega el Player aquí a l'Inspector
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 2, -10);

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        // Fem que la càmera segueixi al personatge però sense girar-se
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}