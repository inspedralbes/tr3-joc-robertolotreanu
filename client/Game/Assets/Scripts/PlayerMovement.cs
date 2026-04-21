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
        
        // Netejar la referència estàtica de la sessió anterior per evitar
        // que apunti a un objecte destuït de la partida anterior.
        if (LocalPlayer != null && LocalPlayer.gameObject == null)
        {
            Debug.Log("<color=blue>[PlayerMovement]</color> Awake: Se encontró un LocalPlayer destruido. Limpiando referencia.");
            LocalPlayer = null;
        }
        
        // ¡Magia aquí! Usamos 'GetComponentInChildren' porque sabemos que el dibujo
        // y el animador los tienes colgados en el "hijo" dentro del Prefab principal.
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (animator == null) Debug.LogWarning("<color=blue>[PlayerMovement]</color> Animator no encontrado en los hijos.");
        if (spriteRenderer == null) Debug.LogWarning("<color=blue>[PlayerMovement]</color> SpriteRenderer no encontrado en los hijos.");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"<color=blue>[PlayerMovement]</color> OnNetworkSpawn llamado para {gameObject.name}. IsOwner: {IsOwner}, IsServer: {IsServer}, IsBot: {isBot}");

        if (IsOwner && NetworkObject.IsPlayerObject)
        {
            LocalPlayer = this;
            isGrounded = false; // Reset per si quedava true de la sessió anterior
            jumpRequested = false;
            Debug.Log($"<color=green>[PlayerMovement OK]</color> LocalPlayer asignado exitosamente: {gameObject.name} (ID: {OwnerClientId})");

            // Enviar el nom al servidor per a que l'emmagatzemi y otros lo lean
            string myName = PlayerPrefs.GetString("PlayerName", $"Jugador{OwnerClientId}");
            Debug.Log($"<color=blue>[PlayerMovement]</color> Enviando mi nombre al servidor: {myName}");
            SetPlayerNameServerRpc(new FixedString64Bytes(myName));

            // Teletransport inicial al carregar escena
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += (sceneName, loadSceneMode, clientsCompleted, clientsTimedOut) =>
            {
                transform.position = new Vector3(0, 1, 0);
                rb.linearVelocity = Vector2.zero;
                isGrounded = false;
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

        // --- SOLUCIÓ ROBUSTA PER AL SUELO ---
        // En lloc de confiar en OnCollisionExit2D (que falla si toques una paret),
        // avaluem TOTS els contactes actuals cada frame físic.
        isGrounded = false;
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int colCount = rb.GetContacts(contacts);
        for(int i = 0; i < colCount; i++) 
        {
            if(contacts[i].normal.y > 0.6f) 
            {
                isGrounded = true; 
                break;
            }
        }

        // 2. Aplicación de movimiento
        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);

        if (jumpRequested && isGrounded)
        {
            ApplyJump();
        }
        else
        {
            jumpRequested = false; // Reset if not grounded but requested
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
}