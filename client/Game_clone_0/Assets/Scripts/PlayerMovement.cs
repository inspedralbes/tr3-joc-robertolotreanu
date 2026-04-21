using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Controla el movimiento horizontal y el salto del jugador.
/// </summary>
public class PlayerMovement : NetworkBehaviour
{
    [Header("Configuración de Salto")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float jumpCooldown = 0.2f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;
    private float lastJumpTime;

    [Header("Bot AI")]
    public bool isBot = false;
    public float botHorizontalInput;
    public bool botJumpRequested;

    public static PlayerMovement LocalPlayer; // Referència única per a la càmera i HUD

    /// <summary>Nom del jugador sincronitzat a tots els clients via servidor.</summary>
    public NetworkVariable<FixedString64Bytes> playerName =
        new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes("Jugador"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>Variable sincronitzada per a saber si el personatge està viu i visible.</summary>
    public NetworkVariable<bool> isAlive = 
        new NetworkVariable<bool>(
            true, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // ¡Magia aquí! Usamos 'GetComponentInChildren' porque sabemos que el dibujo
        // y el animador los tienes colgados en el "hijo" dentro del Prefab principal.
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && NetworkObject.IsPlayerObject)
        {
            LocalPlayer = this;
            Debug.Log($"[PlayerMovement] LocalPlayer assignat: {gameObject.name} (ID: {OwnerClientId})");

            // Enviar el nom al servidor per a que l'emmagatzemi i altres el puguin llegir
            string myName = PlayerPrefs.GetString("PlayerName", $"Jugador{OwnerClientId}");
            SetPlayerNameServerRpc(new FixedString64Bytes(myName));

            // Teletransport inicial al carregar escena
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += (sceneName, loadSceneMode, clientsCompleted, clientsTimedOut) =>
            {
                transform.position = new Vector3(0, 1, 0);
                rb.linearVelocity = Vector2.zero;
            };
        }
        else
        {
            // --- OPTIMITZACIÓ MULTIJUGADOR ---
            // Si NO som els amos, desactivem la simulació física.
            // Així evitem que la gravetat local lluiti contra el NetworkTransform.
            if (rb != null) rb.simulated = false;
        }

        // Subscripció al canvi d'estat de vida per a ocultar/mostrar el personatge
        isAlive.OnValueChanged += (oldVal, newVal) => {
            if (spriteRenderer != null) spriteRenderer.enabled = newVal;
        };

        // Estat inicial
        if (spriteRenderer != null) spriteRenderer.enabled = isAlive.Value;
    }

    public override void OnNetworkDespawn()
    {
        if (LocalPlayer == this) LocalPlayer = null;
    }

    public override void OnDestroy()
    {
        if (LocalPlayer == this) LocalPlayer = null;
        base.OnDestroy(); // Important cridar al pare en NetworkBehaviour
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(FixedString64Bytes name)
    {
        playerName.Value = name;
        Debug.Log($"[PlayerName] Client {OwnerClientId} -> '{name}'");
    }

    private void Update()
    {
        if (isBot)
        {
            horizontalInput = botHorizontalInput;
            if (botJumpRequested && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
            {
                jumpRequested = true;
                botJumpRequested = false;
            }
        }
        else if (this == LocalPlayer)
        {
            // 1. Lectura de inputs (Humà - Solo tu mueves a tu personaje)
            horizontalInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetButtonDown("Jump") && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
            {
                jumpRequested = true;
            }
        }

        // --- ANIMACIONES Y GRÁFICOS ---
        if (animator != null)
        {
            // Le pasamos la velocidad (en positivo) a la animación "Speed"
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
            
            // Le decimos al Animator si estamos despegados del suelo
            animator.SetBool("IsJumping", !isGrounded);
        }

        if (spriteRenderer != null)
        {
            // Girar el sprite si vamos a la izquierda
            if (horizontalInput > 0.01f) spriteRenderer.flipX = false;
            else if (horizontalInput < -0.01f) spriteRenderer.flipX = true;
        }
    }

    // Un método público para que la IA ordene saltar a la fuerza, pero SOLO si toca el suelo
    public void ForceBotJump()
    {
        if (isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            jumpRequested = true;
            botJumpRequested = false;
        }
    }

    private void FixedUpdate()
    {
        // IMPORTANTE: Solo aplicamos velocidad si somos el OWNER (tú)
        // o si somos el Servidor y es un Bot. Evitamos que los clientes
        // remotos pongan la velocidad a 0 y "frenen" al personaje.
        if (!IsOwner && !isBot) return;

        // 2. Aplicación de movimiento
        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            ApplyJump();
        }
    }

    private void ApplyJump()
    {
        // AGRESIVO: Asignar velocidad directamente (ignora la masa del jugador)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        jumpRequested = false;
        isGrounded = false;
        lastJumpTime = Time.time;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluarSuelo(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluarSuelo(collision);
    }

    private void EvaluarSuelo(Collision2D collision)
    {
        // Detección de suelo por ángulo de contacto
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.6f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }
}