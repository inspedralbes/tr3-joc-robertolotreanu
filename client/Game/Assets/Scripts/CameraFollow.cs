using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Arrossega el Player aquí a l'Inspector
    public float smoothSpeed = 0.125f;
    public Vector3 offset = new Vector3(0, 2, -10);

    // Clear the tracked target when the scene ends so we don't hold a stale reference
    void OnDisable()
    {
        target = null;
    }

    void LateUpdate()
    {
        if (target == null) 
        {
            // Usamos la referencia estática que el propio jugador registra al nacer
            if (PlayerMovement.LocalPlayer != null)
            {
                target = PlayerMovement.LocalPlayer.transform;
                Debug.Log("<color=green>CÀMERA:</color> Objetivo fijado en LocalPlayer.");
            }
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }
}