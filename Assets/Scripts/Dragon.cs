using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class Dragon : PoolableObject
{
    [Header("Zombie")]
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    [SerializeField] private List<Collision2D> collisions = new List<Collision2D>();

    private float waitForJump;
    private float waitForFall;
    private bool isTouchingScreen;
    [SerializeField] private float groundHeight;
    [Header("Jump stats")]
    /// <summary>
    /// -1: Falling, 0: On ground, 1: Jumping, 2: Floating, -2: Logged out
    /// </summary>
    [SerializeField] private int jumpStatus;
    [SerializeField] private float jumpSpeed;
    public float jumpHeightModifier;
    [SerializeField] private float jumpStartTime;

    private float jumpAcceleration;
    private float maxJumpAcceleration;

    protected float maxJumpHeight;
    private float CurrentHeight => transform.position.y - groundHeight;

    private float dForward;
    private float tForward;

    [SerializeField] private float floatingGravityScale;
    [SerializeField] private float fallingGravityScale;

    [SerializeField] private Animator animator;

    public static int SPELLCASTERID => 1;


    public override float Width => boxCollider.size.x;
    public override float Height => boxCollider.size.y;
    public int JumpStatus => jumpStatus;
    public int CollisionNumber => collisions.Count;
    public bool IsTouchingScreen => isTouchingScreen;
    public bool IsFellOffTheGround
    {
        get
        {
            return transform.position.y < groundHeight || (transform.position.y == groundHeight && jumpStatus == -1);
        }
    }

    private const int ZombieLayer0 = 5;
    private int currentLayer;
    public int Layer => currentLayer;
    public bool IsOutGround
    {
        get
        {
            if (boxCollider.isTrigger)
                return true;
            LayerMask mask = LayerMask.GetMask("Road1");
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, mask);
            // if (hit)
            //     Debug.Log(hit.collider.gameObject.tag);
            return (!hit) && (jumpStatus == 0);
        }
    }

    // Thêm method này để vẽ Gizmos
    private void OnDrawGizmos()
    {
        if (!boxCollider) return;

        // Vẽ BoxCollider bounds
        Gizmos.color = Color.blue;
        Vector3 center = transform.position + (Vector3)boxCollider.offset;
        Vector3 size = boxCollider.size;
        Gizmos.DrawWireCube(center, size);

        // Vẽ vị trí lowestPoint (chân dragon)
        float lowestPoint = Height / 2f - boxCollider.offset.y;
        Vector3 bottomPosition = new Vector3(transform.position.x, transform.position.y - lowestPoint, transform.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(bottomPosition, 0.1f);

        // Vẽ raycast từ IsOutGround
        LayerMask mask = LayerMask.GetMask("Road1");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, mask);

        if (hit)
        {
            // Raycast hit - vẽ màu xanh lá
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, hit.point);
            Gizmos.DrawSphere(hit.point, 0.15f);

            // Vẽ Road object được hit
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(hit.collider.transform.position, hit.collider.bounds.size);
        }
        else
        {
            // Raycast miss - vẽ màu đỏ
            Gizmos.color = Color.red;
            Vector3 endPoint = transform.position + Vector3.down * 10f;
            Gizmos.DrawLine(transform.position, endPoint);
        }

        // Vẽ groundHeight line nếu đã được set
        if (groundHeight != float.MaxValue)
        {
            Gizmos.color = Color.black;
            Vector3 groundStart = transform.position + Vector3.left * 2f;
            Vector3 groundEnd = transform.position + Vector3.right * 2f;
            groundStart.y = groundHeight;
            groundEnd.y = groundHeight;
            Gizmos.DrawLine(groundStart, groundEnd);
        }

        // Vẽ text hiển thị jumpStatus
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, $"Jump: {jumpStatus}");
#endif
    }

    // Thêm method để debug collision detection
    private void OnDrawGizmosSelected()
    {
        if (!boxCollider) return;

        // Vẽ collision detection area
        Gizmos.color = Color.cyan;
        float lowestPoint = Height / 2f - boxCollider.offset.y;

        // Vẽ line từ center dragon xuống chân
        Vector3 centerPos = transform.position;
        Vector3 bottomPos = new Vector3(centerPos.x, centerPos.y - lowestPoint, centerPos.z);
        Gizmos.DrawLine(centerPos, bottomPos);

        // Vẽ collision check area
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(bottomPos, 0.2f);
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        Init();
        SetJumpHeight(jumpHeightModifier);
    }

    protected void SetJumpHeight(float modifier)
    {
        maxJumpHeight = boxCollider.size.y * 2f * modifier;
        maxJumpAcceleration = -jumpSpeed * jumpSpeed / (2 * maxJumpHeight);
    }

    public override void Init()
    {
        waitForFall = 0f;
        waitForJump = 0f;
        jumpStatus = -1;
        jumpStartTime = 0f;

        dForward = 0f;
        tForward = 0f;
        groundHeight = float.MaxValue;
        collisions.Clear();

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.transform.position = new Vector3(spriteRenderer.transform.position.x,
                spriteRenderer.transform.position.y, -2.62f);
        }
        boxCollider.isTrigger = false;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        UpdateJumpFall();
    }

    public void PlayAttackAnimation()
    {
        if (!animator) return;
        animator.SetTrigger("attack");
    }

    public void SetLayer(int layer)
    {
        currentLayer = layer;
        foreach (SpriteRenderer renderer in renderers)
            renderer.sortingLayerName = "Layer" + layer.ToString();

        gameObject.layer = layer + ZombieLayer0;
    }

    public void CallTriggerJump(float time) { waitForJump = (waitForJump > 0 ? waitForJump : time); }
    public void CallTriggerFall(float time) { waitForFall = (waitForFall > 0 ? waitForFall : time); }
    public bool isActuallyGrounded;
    private void CheckJump()
    {
        if (waitForJump > 0)
        {
            waitForJump -= Time.deltaTime;
            // Thêm check: phải thực sự trên đất (không đang rơi) mới cho nhảy
            isActuallyGrounded = jumpStatus == 0 && rigidBody.velocity.y >= -2f;
            if (waitForJump < 0 && isActuallyGrounded)
            {
                isTouchingScreen = true;
                jumpStatus = 1;
                jumpStartTime = Time.time;
                animator.SetBool("isMoving", false);
                jumpAcceleration = maxJumpAcceleration;

                dForward = GameManager.ScreenWidth / 20f;
                if (Random.Range(0, 100) < 10 && GameManager.Instance.Zombies.FirstDragon)
                    dForward += (GameManager.Instance.Zombies.FirstDragon.transform.position.x - transform.position.x) * 0.5f;
                if (Random.Range(0, 100) < 10 || transform.position.x + dForward >= -0.1f * GameManager.ScreenWidth)
                    dForward = 0f;
            }
        }
    }
    private void CheckFall()
    {
        if (waitForFall > 0)
        {
            waitForFall -= Time.deltaTime;
            if (waitForFall < 0)
                isTouchingScreen = false;
        }
    }

    private void UpdateJumpFall()
    {
        CheckJump();
        CheckFall();

        if (jumpStatus == 1)
            rigidBody.gravityScale = 0f;
        else if (jumpStatus == 2)
            rigidBody.gravityScale = floatingGravityScale;
        else
            rigidBody.gravityScale = fallingGravityScale;

        if (jumpStatus == 1)
        {
            float v = Mathf.Sqrt(Mathf.Max(2 * jumpAcceleration * CurrentHeight + jumpSpeed * jumpSpeed, 0f));
            transform.position += Vector3.up * v * Time.deltaTime;

            if (CurrentHeight >= maxJumpHeight * 0.75f && isTouchingScreen == false)
                jumpStatus = -1;
            if (CurrentHeight >= maxJumpHeight || Time.time - jumpStartTime >= 0.9f)
                jumpStatus = 2;

            tForward += Time.deltaTime;
            float vF = 2 * dForward * (1 - tForward);
            if (vF < 0f)
                dForward = tForward = 0f;
            else
            {
                float d = vF * Time.deltaTime;
                transform.position += d * Vector3.right;
                if (waitForFall > 0.001f)
                {
                    waitForFall -= d / GameManager.Instance.ScrollBackSpeed;
                    if (waitForFall <= 0f)
                        waitForFall = 0.001f;
                }
            }
        }
        if (jumpStatus == 2 && isTouchingScreen == false)
            jumpStatus = -1;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float lowestPoint = Height / 2f - boxCollider.offset.y;
        if (collision.gameObject.CompareTag("Road") && transform.position.y - lowestPoint >= collision.gameObject.transform.position.y)
        {
            jumpStatus = 0;
            animator.SetBool("isMoving", true);
            groundHeight = collision.transform.position.y;
            //waitForFall = waitForJump = 0f;
            isTouchingScreen = false;
        }
        else if (collision.gameObject.CompareTag("Object"))
        {
            // Allow SPELLCASTER to track collisions for proper interaction with Box objects
            if (!collisions.Contains(collision))
                collisions.Add(collision);
        }

        OnCollisionEnterObjectBehavior(collision);
    }

    protected virtual void OnCollisionEnterObjectBehavior(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bomb"))
        {
            foreach (SpriteRenderer renderer in renderers)
                renderer.color = Color.black;
            boxCollider.isTrigger = true;
            jumpStatus = -2;
            transform.position += Vector3.up * GameManager.ScreenHeight / 8f;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        //if (collision.gameObject.CompareTag("Road") && /*rigidBody.velocity.y < 0*/jumpStatus != 1)
            //jumpStatus = -2;
        if (collision.gameObject.CompareTag("Object") && collisions.Contains(collision))
            collisions.Remove(collision);
    }
}
