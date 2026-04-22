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
    public NetworkVariable<bool> isBot = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public float botHorizontalInput;
    public bool botJumpRequested;

    public static PlayerMovement LocalPlayer; // Referència única per a la càmera i HUD

    /// <summary>Nom del jugador sincronitzat a tots els clients via servidor.</summary>
    public NetworkVariable<FixedString64Bytes> playerName =
        new NetworkVariable<FixedString64Bytes>(
            new FixedString64Bytes("Jugador"),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<float> netHorizontalInput = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
        Debug.Log($"<color=blue>[PlayerMovement]</color> OnNetworkSpawn: {gameObject.name}. IsOwner:{IsOwner}, IsBot:{isBot.Value}");

        if (IsServer && !isBot.Value)
        {
            // EL SERVIDOR ÉS L'AUTORITAT DELS NOMS:
            // Recuperem el nom que vam aprovar al Lobby (guardat a LobbySync persistent)
            if (LobbySync.Instance != null && LobbySync.Instance.serverPlayerNames.TryGetValue(OwnerClientId, out string savedName))
            {
                playerName.Value = savedName;
                Debug.Log($"<color=blue>[PlayerMovement]</color> Servidor assigna nom '{savedName}' a Client {OwnerClientId}");
            }
        }

        if (IsOwner && NetworkObject.IsPlayerObject && !isBot.Value)
        {
            LocalPlayer = this;
            rb.simulated = true; 
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.WakeUp(); // Asegurar que no esté en Sleep
            isGrounded = false;
            jumpRequested = false;
            Debug.Log($"<color=cyan>[PLAYER]</color> Control local activat per a {gameObject.name} (ID:{OwnerClientId}). LocalPlayer set.");
        }
        else
        {
            // El SERVIDOR ha de simular a TOTS els personatges (Bots i Clients) per aplicar gravetat i moviment.
            // El CLIENT només simula el seu propi (IsOwner) per a la predicció.
            if (rb != null) rb.simulated = IsServer;
        }

        // Eventos de vida (común a todos)
        isAlive.OnValueChanged += (oldVal, newVal) => {
            if (spriteRenderer != null) spriteRenderer.enabled = newVal;
        };
        // --- VISIBILITAT I ESTAT INICIAL ---
        if (spriteRenderer != null) spriteRenderer.enabled = isAlive.Value;
        Debug.Log($"<color=blue>[PlayerMovement]</color> Spawn completat per a {gameObject.name}. IsAlive: {isAlive.Value}");
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log($"<color=red>[PlayerMovement]</color> OnNetworkDespawn: {gameObject.name} ha deixat la xarxa.");
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        if (LocalPlayer == this) LocalPlayer = null;
        Debug.Log($"<color=red>[PlayerMovement]</color> OnDestroy: {gameObject.name} ha estat destruït.");
        base.OnDestroy(); // Important cridar al pare en NetworkBehaviour
    }



    private void Update()
    {
        if (isBot.Value)
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
            float h = Input.GetAxisRaw("Horizontal");
            bool j = Input.GetButtonDown("Jump") && isGrounded && Time.time >= lastJumpTime + jumpCooldown;

            // Mantener Rigidbody despierto y simulado si somos el dueño
            if (!rb.simulated || rb.bodyType != RigidbodyType2D.Dynamic) {
                rb.simulated = true;
                rb.bodyType = RigidbodyType2D.Dynamic;
            }

            if (netHorizontalInput.Value != h) netHorizontalInput.Value = h;
            UpdateInputsServerRpc(h, j);
            
            horizontalInput = h;
            if (j) jumpRequested = true;
        }
        else
        {
            // Per a jugadors remots, usem el valor sincronitzat per a les animacions
            horizontalInput = netHorizontalInput.Value;
        }

        // --- ANIMACIONES Y GRÁFICOS (Ahora dentro de Update) ---
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
            animator.SetBool("IsJumping", !isGrounded);
        }

        if (spriteRenderer != null)
        {
            if (horizontalInput > 0.01f) spriteRenderer.flipX = false;
            else if (horizontalInput < -0.01f) spriteRenderer.flipX = true;
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateInputsServerRpc(float h, bool j)
    {
        horizontalInput = h;
        if (j) jumpRequested = true;
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
        // El SERVIDOR mueve a todos (Bots y Jugadores remotos)
        // El DUEÑO mueve localmente para predicción rápida
        if (!IsServer && !IsOwner) return;

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

        // Forzar simulación para el dueño (despertar físico)
        if (IsOwner && !isBot.Value)
        {
            rb.simulated = true;
            if (rb.bodyType != RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Dynamic;
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