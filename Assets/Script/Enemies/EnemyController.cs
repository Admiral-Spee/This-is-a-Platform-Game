using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 2f;

    [Header("检测点")]
    public Transform wallCheck;     // 检测前方墙体（默认放左前方）
    public Transform groundCheck;   // 检测前下方地面（默认放左前下）
    public LayerMask whatIsGround;
    public float checkRadius = 0.07f;

    [Header("踩死弹跳力度")]
    public float stompBounce = 12f;

    private bool movingLeft = true;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isDead = false;

    // 检测点状态
    private bool isWall, isGround;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead) return;

        Patrol();

        anim.SetBool("IsWalk", Mathf.Abs(rb.velocity.x) > 0.01f && IsOnGround());
    }

    void Patrol()
    {
        float moveDir = movingLeft ? -1 : 1;
        rb.velocity = new Vector2(moveDir * moveSpeed, rb.velocity.y);

        // 整体翻转对象
        transform.localScale = new Vector3(movingLeft ? 2 : -2, 2, 2);

        // 只检测左侧的检测点，翻转后自动对应“前方”
        Vector3 wallCheckPos = wallCheck.position;
        Vector3 groundCheckPos = groundCheck.position;



        isWall = Physics2D.OverlapCircle(wallCheckPos, checkRadius, whatIsGround);
        isGround = Physics2D.OverlapCircle(groundCheckPos, checkRadius, whatIsGround);

        if (isWall || !isGround)
        {
            movingLeft = !movingLeft;
        }
    }

    // 获取检测点镜像位置
    Vector3 MirrorLocalPosition(Transform t)
    {
        // t.localPosition.x取反
        return transform.TransformPoint(new Vector3(-t.localPosition.x, t.localPosition.y, t.localPosition.z));
    }

    bool IsOnGround()
    {
        // 可用中心射线
        return Physics2D.Raycast(transform.position, Vector2.down, 0.2f, whatIsGround);
    }

    // 玩家碰撞检测
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.collider.CompareTag("Player"))
        {
            float playerY = collision.transform.position.y;
            float enemyY = transform.position.y;

            // 判断玩家是否从上踩
            if (playerY > enemyY + 0.2f && collision.relativeVelocity.y <= 0f)
            {
                Die();
                Rigidbody2D playerRb = collision.collider.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                    playerRb.velocity = new Vector2(playerRb.velocity.x, stompBounce);
            }
            else
            {
                PlayerMovement pm = collision.collider.GetComponent<PlayerMovement>();
                if (pm != null)
                    pm.KillByEnemy();
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.velocity = Vector2.zero;
        anim.SetTrigger("IsDeath");
        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmos()
    {
        // 显示左侧检测点
        if (wallCheck)
        {
            // 显示当前运动方向前方检测点
            Vector3 pos = wallCheck.position;
            Gizmos.color = isWall ? Color.green : Color.red;
            Gizmos.DrawWireSphere(pos, checkRadius);
        }
        if (groundCheck)
        {
            Vector3 pos = groundCheck.position;
            Gizmos.color = isGround ? Color.green : Color.red;
            Gizmos.DrawWireSphere(pos, checkRadius);
        }
    }

    // 避免和地刺碰撞
    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Enemies"))
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.collider);
        }
    }
}
