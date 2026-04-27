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
        // 0. PRIORIDAD ABSOLUTA: Si el jugador local existe y está vivo, lo seguimos a él.
        // Esto corrige el bug donde la cámara se quedaba en un Bot si este spawneaba antes que nosotros.
        if (PlayerMovement.LocalPlayer != null && PlayerMovement.LocalPlayer.isAlive.Value)
        {
            if (target != PlayerMovement.LocalPlayer.transform)
            {
                target = PlayerMovement.LocalPlayer.transform;
                Debug.Log("<color=cyan>[CameraFollow]</color> LocalPlayer detectado. Cambiando cámara al jugador.");
            }
        }

        // 1. Si no hay objetivo o el objetivo ha muerto, buscamos a alguien vivo
        if (target == null || (target.TryGetComponent<PlayerMovement>(out var pm) && !pm.isAlive.Value))
        {
            BuscarNuevoObjetivo();
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }

    private void BuscarNuevoObjetivo()
    {
        // Prioridad 1: Yo mismo (si estoy vivo)
        if (PlayerMovement.LocalPlayer != null && PlayerMovement.LocalPlayer.isAlive.Value)
        {
            target = PlayerMovement.LocalPlayer.transform;
            return;
        }

        // Prioridad 2: Otros jugadores humanos vivos
        var humanos = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var h in humanos)
        {
            if (h != null && h.isAlive.Value && (h.isBot == null || !h.isBot.Value))
            {
                target = h.transform;
                return;
            }
        }

        // Prioridad 3: Cualquier Bot vivo (BotAI)
        var bots = Object.FindObjectsByType<BotAI>(FindObjectsSortMode.None);
        if (bots.Length > 0 && bots[0] != null)
        {
            target = bots[0].transform;
        }
    }
}