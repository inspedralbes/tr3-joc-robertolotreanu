using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class BotAI : NetworkBehaviour
{
    private PlayerMovement _movement;
    private Transform _targetPlayer;
    private float _repathTimer;

    public override void OnNetworkSpawn()
    {
        // Un objeto de red solo debe ser un Bot si NO es un PlayerObject
        if (!IsServer || NetworkObject.IsPlayerObject) 
        {
            enabled = false;
            return;
        }
        
        _movement = GetComponent<PlayerMovement>();
        if (_movement != null) 
        {
            _movement.isBot = true;
        }
    }

    private void Update()
    {
        if (!IsServer || _movement == null) return;

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            FindTarget();
            _repathTimer = 0.5f; // Buscar objetivo cada medio segundo
        }

        if (_targetPlayer != null)
        {
            float distX = _targetPlayer.position.x - transform.position.x;
            float distY = _targetPlayer.position.y - transform.position.y;

            // SI LA PLATAFORMA ESTÁ MÁS ARRIBA QUE NOSOTROS
            if (distY > 0.8f)
            {
                // Calculamos nuestro punto de salto a 1.2 metros para hacer un salto en arco perfecto
                float puntoSaltoX = (distX >= 0) ? 1.2f : -1.2f;
                float errorDist = distX - puntoSaltoX;

                // Si aún no hemos llegado al punto de salto, nos acercamos caminando
                if (Mathf.Abs(errorDist) > 0.2f) 
                {
                    _movement.botHorizontalInput = (errorDist > 0) ? 1f : -1f;
                } 
                else 
                {
                    // Estamos en el punto: ¡Mantenemos hacia delante y SALTAMOS!
                    _movement.botHorizontalInput = (distX > 0) ? 1f : -1f;
                    _movement.ForceBotJump();
                }
            }
            // SI LA PLATAFORMA ESTÁ MÁS O MENOS A NUESTRA ALTURA O DEBAJO
            else 
            {
                // Vamos directos al centro
                if (distX > 0.2f) _movement.botHorizontalInput = 1f;
                else if (distX < -0.2f) _movement.botHorizontalInput = -1f;
                else _movement.botHorizontalInput = 0f;

                // Un saltito menor por si acaso hay un micro-escalón o un enemigo
                if (distY > 0.2f && Mathf.Abs(distX) < 1.5f) {
                    _movement.ForceBotJump();
                }
            }

            // Desatasco: Si se quedan tontos contra la pared caminando sin avanzar
            if (Mathf.Abs(_movement.GetComponent<Rigidbody2D>().linearVelocity.x) < 0.1f && Mathf.Abs(_movement.botHorizontalInput) > 0.1f)
            {
                if (Random.value < 0.05f) _movement.ForceBotJump();
            }
        }
        else
        {
            // Si no hay jugador, se queda quieto
            _movement.botHorizontalInput = 0f;
        }
    }

    private void FindTarget()
    {
        // En vez de requerir etiquetas en Unity, miramos la lista oficial de plataformas generadas
        List<GameObject> platforms = PlatformSpawner.activePlatforms;

        float closestDist = float.MaxValue;
        Transform bestPlatform = null;

        if (platforms != null)
        {
            foreach (var p in platforms)
            {
                if (p == null) continue; // Ignoramos plataformas comidas por la lava

                // Solo nos interesan las plataformas que estén POR ENCIMA del bot
                if (p.transform.position.y > transform.position.y + 0.5f)
                {
                    float dist = Vector2.Distance(transform.position, p.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestPlatform = p.transform;
                    }
                }
            }
        }

        // Si encontramos una plataforma segura para subir, intentar ir ahí. Si no, seguimos al jugador.
        if (bestPlatform != null)
        {
            _targetPlayer = bestPlatform;
        }
        else if (PlayerMovement.LocalPlayer != null)
        {
            _targetPlayer = PlayerMovement.LocalPlayer.transform;
        }
        else
        {
            _targetPlayer = null;
        }
    }
}
