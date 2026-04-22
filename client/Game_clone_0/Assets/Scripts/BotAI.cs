using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class BotAI : MonoBehaviour
{
    private PlayerMovement _movement;
    private Transform _targetPlayer;
    private float _repathTimer;
    private float _spawnDelay = 2.0f; // Pausa inicial para que no salten como locos al aparecer

    public void Initialize()
    {
        Debug.Log($"<color=magenta>[BOT AI]</color> Inicializando IA para el bot {gameObject.name}");
        _movement = GetComponent<PlayerMovement>();
        if (_movement != null) 
        {
            _movement.isBot.Value = true;
            Debug.Log($"<color=magenta>[BOT AI]</color> PlayerMovement detectado y configurado como bot en {gameObject.name}");
        }
        else
        {
            Debug.LogError($"<color=red>[BOT AI ERROR]</color> {gameObject.name} no tiene PlayerMovement!");
        }
    }

    private Transform _lastTarget;
    private float _stickyTargetTimer;

    private void Update()
    {
        if (_movement == null) return;

        // RETARDO DE SPAWN: No hacemos nada durante los primeros segundos
        if (_spawnDelay > 0)
        {
            _spawnDelay -= Time.deltaTime;
            _movement.botHorizontalInput = 0f;
            return;
        }

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            FindTarget();
            _repathTimer = 0.1f; // Mucho más rápido para encadenar movimientos
        }

        if (_targetPlayer != null)
        {
            float distX = _targetPlayer.position.x - transform.position.x;
            float distY = _targetPlayer.position.y - transform.position.y;

            // 1. MOVIMIENTO HORIZONTAL
            // Nos movemos hacia el objetivo con precisión
            if (Mathf.Abs(distX) > 0.2f)
            {
                _movement.botHorizontalInput = (distX > 0) ? 1f : -1f;
            }
            else
            {
                _movement.botHorizontalInput = 0f;
            }

            // 2. LÓGICA DE SALTO "CALCULADA"
            // Intentamos llegar a la plataforma que está arriba
            if (distY > 1.2f) // Umbral aumentado para evitar saltos nerviosos
            {
                // SOLO saltamos si estamos muy bien alineados horizontalmente
                bool aligned = Mathf.Abs(distX) < 0.6f;
                
                if (aligned)
                {
                    _movement.ForceBotJump();
                }
            }
            
            // 3. SALTO DE SUPERVIVENCIA (Si caemos demasiado o estamos muy cerca de la lava)
            // Aquí se podría añadir lógica con LavaRise.instance para evitar la muerte inminente
            
            // 4. DESATASCO (Si estamos caminando contra algo y no avanzamos)
            Rigidbody2D rb = _movement.GetComponent<Rigidbody2D>();
            if (rb != null && Mathf.Abs(rb.linearVelocity.x) < 0.2f && Mathf.Abs(_movement.botHorizontalInput) > 0.5f)
            {
                if (Random.value < 0.1f) _movement.ForceBotJump();
            }
        }
        else
        {
            _movement.botHorizontalInput = 0f;
        }
    }

    private void FindTarget()
    {
        List<GameObject> platforms = PlatformSpawner.activePlatforms;
        if (platforms == null || platforms.Count == 0) 
        {
            _targetPlayer = null; // No seguimos al jugador nunca
            return;
        }

        float bestScore = float.MinValue;
        Transform bestPlatform = null;

        foreach (var p in platforms)
        {
            if (p == null) continue;

            float dy = p.transform.position.y - transform.position.y;
            float dx = Mathf.Abs(p.transform.position.x - transform.position.x);

            // Rango ampliado: detectamos plataformas mucho más arriba para planificar
            if (dy > 0.1f && dy < 15f && dx < 20f)
            {
                // Score basado en altura extrema
                float score = (dy * 3.0f) - (dx * 0.4f);
                
                // Bonus si es la misma plataforma que antes para evitar saltos de cámara/objetivo
                if (p.transform == _lastTarget) score += 2f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPlatform = p.transform;
                }
            }
        }

        if (bestPlatform != null)
        {
            _targetPlayer = bestPlatform;
            _lastTarget = bestPlatform;
        }
        else
        {
            // Si no hay plataformas arriba, intentamos ir al centro (X=0) por si acaso
            _targetPlayer = null;
            _movement.botHorizontalInput = (transform.position.x > 1f) ? -1f : (transform.position.x < -1f ? 1f : 0f);
        }
    }
}
