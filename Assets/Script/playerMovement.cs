using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动和跳跃参数")]
    public float speed = 8f;
    public float jumpF = 12f;
    public float fallMutiplier = 4f;
    public float lowJumpMutiplier = 7f;

    public float jumpBufferTime = 0.1f; // 缓冲区间（秒）
    private float jumpBufferCounter = 0f;

    [Header("地面检测")]
    public Transform[] groundChecks; // 多点地面检测
    public float checkRadius = 0.1f;
    public LayerMask whatIsGround;

    [Header("侧面检测")]
    public Transform[] wallChecksLeft;
    public Transform[] wallChecksRight;
    public float wallCheckDistance = 0.1f;
    public LayerMask wallLayer;

    [Header("死亡参数")]
    public float spikeBounceForce = 3f;   // 死亡时弹跳力度
    public float deathY = -1f;            // 掉屏幕死亡判定
    private bool isDead = false;           // 玩家死亡标志

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer renderer;
    private bool isGround;
    private bool isJumpPressed;
    private bool isTouchingWall;
    private enum MoveState { Idle, Walk }
    private MoveState state;

    private float runTime = 0f;
    private float runDistance = 0f;
    private Vector2 startPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        renderer = GetComponent<SpriteRenderer>();
        startPos = transform.position;
    }

    void Update()
    {
        isGround = false;

        // 死亡时不能操作
        if (isDead) return;

        runTime += Time.deltaTime;
        // 以x轴正方向为前进距离
        runDistance = Mathf.Max(runDistance, transform.position.x - startPos.x);

        // 多点地面检测

        foreach (Transform t in groundChecks)
        {
            if (Physics2D.OverlapCircle(t.position, checkRadius, whatIsGround))
            {
                isGround = true;
                break;
            }
        }
        // 多点侧面检测
        float horizontal = Input.GetAxisRaw("Horizontal");
        isTouchingWall = false;
        if (horizontal < 0 && wallChecksLeft != null && wallChecksLeft.Length > 0)
        {
            foreach (Transform t in wallChecksLeft)
            {
                if (Physics2D.OverlapCircle(t.position, wallCheckDistance, wallLayer))
                {
                    isTouchingWall = true;
                    break;
                }
            }
        }
        if (horizontal > 0 && wallChecksRight != null && wallChecksRight.Length > 0)
        {
            foreach (Transform t in wallChecksRight)
            {
                if (Physics2D.OverlapCircle(t.position, wallCheckDistance, wallLayer))
                {
                    isTouchingWall = true;
                    break;
                }
            }
        }

        // 跳跃按键检测
        // 只在“按下”时记录缓冲（不会因长按一直递增）
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime; // 重置缓冲计时器
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime; // 缓冲时间递减
        }


        // Debug可视化
        foreach (Transform t in groundChecks)
            Debug.DrawRay(t.position, Vector2.down * checkRadius, Color.red);
        foreach (Transform t in wallChecksLeft)
            Debug.DrawRay(t.position, Vector2.left * wallCheckDistance, Color.blue);
        foreach (Transform t in wallChecksRight)
            Debug.DrawRay(t.position, Vector2.right * wallCheckDistance, Color.blue);
    }

    void FixedUpdate()
    {
        // 如果玩家Y坐标掉出屏幕，则GameOver
        if (transform.position.y < deathY)
        {
            isDead = true;
            GameOver();
        }

        // 死亡时不能操作
        if (!isDead)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");

            // 粘墙优化
            if (!(isTouchingWall && !isGround))
                rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
            else
                rb.velocity = new Vector2(0, rb.velocity.y);

            // 跳跃
            // 满足“地面”和“在缓冲时间内有按下跳跃键”
            if (jumpBufferCounter > 0 && isGround)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpF);
                jumpBufferCounter = 0; // 跳完就清零，不会排队/连跳
            }

            // 跳跃高度可控
            if (rb.velocity.y < 0)
                rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMutiplier - 1) * Time.fixedDeltaTime;
            else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
                rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMutiplier - 1) * Time.fixedDeltaTime;
        }

        UpdateAnimation();
        anim.SetBool("IsJump", isGround);
    }

    void UpdateAnimation()
    {
        if (rb.velocity.x > 0.01f)
        {
            state = MoveState.Walk;
            renderer.flipX = false;
        }
        else if (rb.velocity.x < -0.01f)
        {
            state = MoveState.Walk;
            renderer.flipX = true;
        }
        else
        {
            state = MoveState.Idle;
        }
        anim.SetInteger("State", (int)state);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // 判断碰到地刺
        if (other.CompareTag("Enemies") && !isDead)
        {
            isDead = true;
            rb.velocity = Vector2.zero; // 先清零速度
            rb.AddForce(new Vector2(0, spikeBounceForce), ForceMode2D.Impulse);

            // 让玩家变成Trigger（穿透一切）
            GetComponent<Collider2D>().isTrigger = true;

            // 这里可以加死亡动画/音效

            // 禁用输入（isDead=true后Update/FixedUpdate已屏蔽操作）
        }
    }

    public void KillByEnemy()
    {
        if (!isDead)
        {
            isDead = true;
            rb.velocity = Vector2.zero; // 先清零速度
            rb.AddForce(new Vector2(0, spikeBounceForce), ForceMode2D.Impulse);

            // 让玩家变成Trigger（穿透一切）
            GetComponent<Collider2D>().isTrigger = true;

            // 这里可以加死亡动画/音效

            // 禁用输入（isDead=true后Update/FixedUpdate已屏蔽操作）
        }
    }

    // 游戏结束逻辑
    void GameOver()
    {
        Debug.Log("Game Over!");

        // 激活结算界面并传递数据
        GameResultUI.Instance.ShowResult(runTime, runDistance);
        // 角色失效/禁用输入等可选
        gameObject.SetActive(false);

        // 可弹出UI、重启等
        // Destroy(gameObject); // 可选：销毁玩家对象
        // Time.timeScale = 0;  // 可选：暂停游戏
    }

    void OnDrawGizmos()
    {
        // 地面检测点
        if (groundChecks != null)
        {
            foreach (Transform t in groundChecks)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(t.position, checkRadius);
            }
        }
        // 侧面检测点
        if (wallChecksLeft != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform t in wallChecksLeft)
                Gizmos.DrawWireSphere(t.position, wallCheckDistance);
        }
        if (wallChecksRight != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform t in wallChecksRight)
                Gizmos.DrawWireSphere(t.position, wallCheckDistance);
        }
    }
}
