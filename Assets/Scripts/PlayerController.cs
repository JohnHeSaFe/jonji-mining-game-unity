using UnityEngine;
using UnityEngine.Tilemaps; 

public class PlayerController : MonoBehaviour
{   
    [Header("ID")]
    public int playerID;
    
    [Header("Controles")]
    public KeyCode keyUp = KeyCode.UpArrow;
    public KeyCode keyDown = KeyCode.DownArrow;
    public KeyCode keyLeft = KeyCode.LeftArrow;
    public KeyCode keyRight = KeyCode.RightArrow;
    public KeyCode keyMine = KeyCode.RightShift;

    [Header("Físicas")]
    public Tilemap tilemap; 
    public float speed = 5f;
    public float jumpForce = 10f;
    public int extraJumps = 2;
    public float miningCooldown = 0.5f;

    [Header("Sprites")]
    public Sprite up;
    public Sprite down;
    public Sprite left;
    public Sprite right;

    [Header("Verificación de Suelo")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;


    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isGrounded;
    private int jumpCount = 0;
    private bool isJumpingUp = false;
    private Vector3Int lastDirection = Vector3Int.down; 
    private float cooldownTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // Detectar suelo y reiniciar saltos
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (isGrounded)
        {
            jumpCount = 0;
        }

        // Movimiento horizontal 
        float moveX = 0f;
        if (Input.GetKey(keyLeft)) moveX = -1f;
        if (Input.GetKey(keyRight)) moveX = 1f;

        // Saltos
        if (Input.GetKeyDown(keyUp) && jumpCount < extraJumps)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
            isJumpingUp = true;
        }

        // Aplicar el movimiento
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        // Guardar la última dirección para saber donde picar
        if (Input.GetKey(keyUp)) lastDirection = Vector3Int.up;
        else if (Input.GetKey(keyDown)) lastDirection = Vector3Int.down;
        else if (Input.GetKey(keyLeft)) lastDirection = Vector3Int.left;
        else if (Input.GetKey(keyRight)) lastDirection = Vector3Int.right;

        // Picar con cooldown
        if (Input.GetKeyDown(keyMine) && cooldownTimer <= 0)
        {
            PicarBloque();
            cooldownTimer = miningCooldown; 
        }

        // Actualizar sprite
        if (!isGrounded && isJumpingUp)
        {
            sr.sprite = up;
        }
        else
        {
            if (moveX > 0) sr.sprite = right;
            else if (moveX < 0) sr.sprite = left;
            else sr.sprite = down;
        }
    }

    void PicarBloque()
    {
        Vector3Int playerCell = tilemap.WorldToCell(transform.position);
        Vector3Int targetCell = playerCell + lastDirection; 
        
        TileBase tilePicado = tilemap.GetTile(targetCell); 

        if (tilePicado != null)
        {
            GameManager.game.AddPoints(playerID, tilePicado); 
            tilemap.SetTile(targetCell, null);
        }
    }
}