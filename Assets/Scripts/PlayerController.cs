using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("ID")]
    public int playerID;

    [Header("Network")]
    // Set true for the character this client controls locally
    public bool isLocalPlayer = false;

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

    // Networking: lerp target for remote player
    private Vector2 _remoteTargetPos;
    private Vector2 _remoteTargetVel;
    private bool _hasRemoteData = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        _remoteTargetPos = transform.position;

        // In a networked game, determine who controls this character
        if (GameNetworkManager.Instance != null)
        {
            isLocalPlayer = (GameNetworkManager.Instance.LocalPlayerID == playerID);
        }

        // Subscribe to remote move events for our remote character
        if (GameNetworkManager.Instance != null && !isLocalPlayer)
        {
            GameNetworkManager.Instance.OnRemotePlayerMove += HandleRemoteMove;
            GameNetworkManager.Instance.OnTileMinedRemote += HandleRemoteTile;
        }
    }

    void OnDestroy()
    {
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.OnRemotePlayerMove -= HandleRemoteMove;
            GameNetworkManager.Instance.OnTileMinedRemote -= HandleRemoteTile;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        if (isLocalPlayer)
            LocalUpdate();
        else
            RemoteUpdate();
    }

    // ── Local player: read input, send state ──────────────────────────────

    void LocalUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (isGrounded) jumpCount = 0;

        float moveX = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  moveX = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX =  1f;

        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && jumpCount < extraJumps)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
            isJumpingUp = true;
        }

        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))         lastDirection = Vector3Int.up;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  lastDirection = Vector3Int.down;
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  lastDirection = Vector3Int.left;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) lastDirection = Vector3Int.right;

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.RightShift) ||
             Input.GetKeyDown(KeyCode.LeftShift)) && cooldownTimer <= 0)
        {
            PicarBloque();
            cooldownTimer = miningCooldown;
        }

        UpdateSprite(moveX);

        // Send position to server every frame
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.SendPlayerMove(
                (byte)playerID,
                transform.position.x, transform.position.y,
                rb.linearVelocity.x, rb.linearVelocity.y);
        }
    }

    // ── Remote player: dead-reckoning + smooth correction ────────────────

    void RemoteUpdate()
    {
        if (!_hasRemoteData) return;
        // Extrapolate the target using last known velocity so the ghost keeps
        // moving between packets instead of freezing until the next one arrives.
        _remoteTargetPos += _remoteTargetVel * Time.deltaTime;
        // Snap toward the extrapolated position quickly to hide any drift.
        transform.position = Vector2.Lerp(transform.position, _remoteTargetPos, Time.deltaTime * 25f);
        UpdateSpriteFromVelocity(_remoteTargetVel.x);
    }

    public void HandleRemoteMove(byte pid, float px, float py, float vx, float vy)
    {
        if (pid != playerID) return;
        _remoteTargetPos = new Vector2(px, py);
        _remoteTargetVel = new Vector2(vx, vy);
        _hasRemoteData = true;
    }

    void HandleRemoteTile(byte pid, int tx, int ty)
    {
        if (pid != playerID) return;
        if (tilemap == null) return;

        var cell = new Vector3Int(tx, ty, 0);
        TileBase tile = tilemap.GetTile(cell);

        // Award points and update mineral counts on THIS machine before removing the tile.
        // This keeps both GameManagers in sync without needing extra network messages.
        if (tile != null && GameManager.game != null)
            GameManager.game.AddPoints((int)pid, tile);

        tilemap.SetTile(cell, null);
    }

    // ── Mining ────────────────────────────────────────────────────────────

    void PicarBloque()
    {
        Vector3Int playerCell = tilemap.WorldToCell(transform.position);
        Vector3Int targetCell = playerCell + lastDirection;
        TileBase tile = tilemap.GetTile(targetCell);

        if (tile != null)
        {
            GameManager.game.AddPoints(playerID, tile);
            tilemap.SetTile(targetCell, null);

            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.SendTileMined(
                    (byte)playerID, targetCell.x, targetCell.y);
            }
        }
    }

    // ── Sprite helpers ────────────────────────────────────────────────────

    void UpdateSprite(float moveX)
    {
        if (!isGrounded && isJumpingUp) { sr.sprite = up; return; }
        UpdateSpriteFromVelocity(moveX);
    }

    void UpdateSpriteFromVelocity(float moveX)
    {
        if (moveX > 0.01f)       sr.sprite = right;
        else if (moveX < -0.01f) sr.sprite = left;
        else                     sr.sprite = down;
    }
}
