using UnityEngine;

/// <summary>
/// Controla el movimiento horizontal y el salto del jugador.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración de Salto")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float jumpCooldown = 0.2f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;
    private float lastJumpTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 1. Lectura de inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
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


    private void OnCollisionStay2D(Collision2D collision)
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