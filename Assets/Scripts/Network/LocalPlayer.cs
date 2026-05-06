using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Network-aware replacement for PlayerController.
/// Handles local input (WASD + Space + F) and sends position /
/// mine-event packets to the server via NetworkClient.
///
/// Preserves all original game mechanics: physics movement, double-jump,
/// directional sprites, mining with cooldown, and GameManager score tracking.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class LocalPlayer : MonoBehaviour
{
    // -----------------------------------------------------------------
    //  Inspector Fields
    // -----------------------------------------------------------------

    [Header("Player Identity")]
    public int playerID = 1;

    [Header("Controls — now WASD for every player")]
    public KeyCode keyUp    = KeyCode.W;
    public KeyCode keyDown  = KeyCode.S;
    public KeyCode keyLeft  = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;
    public KeyCode keyJump  = KeyCode.Space;
    public KeyCode keyMine  = KeyCode.F;

    [Header("Physics")]
    public Tilemap tilemap;
    public float speed       = 5f;
    public float jumpForce   = 10f;
    public int   extraJumps  = 2;
    public float miningCooldown = 0.5f;

    [Header("Sprites")]
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float     checkRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Network")]
    [Tooltip("NetworkClient on this GameObject or the NetworkManager object.")]
    public NetworkClient networkClient;

    [Tooltip("How many position updates per second to send over the network.")]
    public float networkTickRate = 20f;

    // -----------------------------------------------------------------
    //  Private State
    // -----------------------------------------------------------------

    private SpriteRenderer sr;
    private Rigidbody2D    rb;
    private bool  isGrounded;
    private int   jumpCount;
    private bool  isJumpingUp;
    private float cooldownTimer;
    private float networkTickTimer;

    // Tracks the last direction faced so the mine-key knows where to dig
    private Vector3Int lastDirection = Vector3Int.down;

    // -----------------------------------------------------------------
    //  Unity Lifecycle
    // -----------------------------------------------------------------

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Derive tick interval from rate (e.g. 20 Hz → 0.05 s)
        networkTickTimer = 1f / networkTickRate;
    }

    void Update()
    {
        HandleCooldowns();
        CheckGround();
        HandleMovement();
        HandleJump();
        HandleFacing();
        HandleMining();
        UpdateSprite();
        SendNetworkPosition();
    }

    // -----------------------------------------------------------------
    //  Movement & Input
    // -----------------------------------------------------------------

    private void HandleCooldowns()
    {
        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        networkTickTimer -= Time.deltaTime;
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (isGrounded)
        {
            jumpCount  = 0;
            isJumpingUp = false;
        }
    }

    private void HandleMovement()
    {
        float moveX = 0f;
        if (Input.GetKey(keyLeft))  moveX = -1f;
        if (Input.GetKey(keyRight)) moveX =  1f;

        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        // Space OR W triggers a jump (W is also used for facing/mining direction)
        bool jumpPressed = Input.GetKeyDown(keyJump) || Input.GetKeyDown(keyUp);
        if (jumpPressed && jumpCount < extraJumps)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
            isJumpingUp = true;
        }
    }

    private void HandleFacing()
    {
        // Record the most recent directional key so mining always goes the right way
        if      (Input.GetKey(keyUp))    lastDirection = Vector3Int.up;
        else if (Input.GetKey(keyDown))  lastDirection = Vector3Int.down;
        else if (Input.GetKey(keyLeft))  lastDirection = Vector3Int.left;
        else if (Input.GetKey(keyRight)) lastDirection = Vector3Int.right;
    }

    private void HandleMining()
    {
        if (!Input.GetKeyDown(keyMine) || cooldownTimer > 0) return;

        MineBlock();
        cooldownTimer = miningCooldown;
    }

    private void MineBlock()
    {
        Vector3Int playerCell = tilemap.WorldToCell(transform.position);
        Vector3Int targetCell = playerCell + lastDirection;
        TileBase   tile       = tilemap.GetTile(targetCell);

        if (tile == null) return;

        // Award points locally (GameManager is already a singleton)
        GameManager.game.AddPoints(playerID, tile);

        // Remove the tile on this client
        tilemap.SetTile(targetCell, null);

        // Tell the server (which tells the opponent) which tile we removed
        networkClient?.SendMine(targetCell.x, targetCell.y);
    }

    private void UpdateSprite()
    {
        if (!isGrounded && isJumpingUp)
        {
            sr.sprite = spriteUp;
            return;
        }

        float moveX = rb.linearVelocity.x;
        if      (moveX > 0.05f)  sr.sprite = spriteRight;
        else if (moveX < -0.05f) sr.sprite = spriteLeft;
        else                     sr.sprite = spriteDown;
    }

    // -----------------------------------------------------------------
    //  Network Position Sending
    // -----------------------------------------------------------------

    private void SendNetworkPosition()
    {
        // Only send at the configured tick rate, not every frame
        if (networkTickTimer > 0) return;

        networkTickTimer = 1f / networkTickRate;
        networkClient?.SendPosition(transform.position.x, transform.position.y);
    }
}
