using UnityEngine;

public class TurretAI : MonoBehaviour
{
    [Header("炮台参数")]
    public Transform gunPivot;         // 炮口旋转部分
    public float minAngle = -90f;      // 最小角度（本地）
    public float maxAngle = 90f;       // 最大角度（本地）
    public float rotateSpeed = 60f;    // 自动扫描速度
    public float detectRange = 10f;     // 检测范围
    public LayerMask playerLayer;

    [Header("射击参数")]
    public float aimTime = 2f;       // 射击准备时间
    public Transform firePoint;        // 炮弹发射点
    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;

    [Header("激光显示")]
    public LineRenderer aimLine;       // 用于显示射线的LineRenderer
    public Color readyColor = Color.green;
    public Color fireColor = Color.red;

    private float currentAngle;
    private float aimTimer = 0f;
    private bool aiming = false;
    private Transform player;
    private bool rotatingRight = true;

    void Start()
    {
        currentAngle = 0f;
        aimLine.enabled = false;
    }

    void Update()
    {
        player = FindPlayerInRange();

        if (player)
        {
            Transform aimPoint = player ? player.Find("aimTarget") : null;
            Vector3 targetPos = aimPoint ? aimPoint.position : player.position; // 没找到备用用玩家中心
            Vector3 dir = targetPos - gunPivot.position;
            if (dir.magnitude < 0.01f) dir = gunPivot.right; // 防止玩家正好和炮台重合，射线为零长

            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // 修正角度
            float localTargetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
            gunPivot.localRotation = Quaternion.Euler(0, 0, localTargetAngle);
            currentAngle = localTargetAngle;

            // 计算炮口实际方向
            Vector3 fireDir = gunPivot.up; // 如果gunPivot本地Y+是正前方
            aimLine.enabled = true;
            aimLine.SetPosition(0, firePoint.position);

            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, fireDir, detectRange, playerLayer);
            Vector3 endPoint = firePoint.position + fireDir * detectRange;
            if (hit.collider)
                endPoint = hit.point;
            aimLine.SetPosition(1, endPoint);

            // 颜色渐变
            aimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(aimTimer / aimTime);
            aimLine.startColor = aimLine.endColor = Color.Lerp(readyColor, fireColor, t);

            if (aimTimer >= aimTime)
            {
                Fire(dir.normalized);
                aimTimer = 0f;
                aimLine.startColor = aimLine.endColor = readyColor;
            }
        }
        else
        {
            // 扫描模式：在最大/最小角之间来回旋转
            aimLine.enabled = false;
            aimTimer = 0f;
            Scan();
        }
    }

    void Scan()
    {
        float delta = rotateSpeed * Time.deltaTime * (rotatingRight ? 1 : -1);
        currentAngle += delta;
        if (currentAngle > maxAngle)
        {
            currentAngle = maxAngle;
            rotatingRight = false;
        }
        else if (currentAngle < minAngle)
        {
            currentAngle = minAngle;
            rotatingRight = true;
        }
        gunPivot.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }

    Transform FindPlayerInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(gunPivot.position, detectRange, playerLayer);
        if (hits.Length == 0) return null;

        // 只锁定第一个检测到的玩家
        return hits[0].transform;
    }

    void Fire(Vector2 dir)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<TurretBullet>().Init(dir, bulletSpeed, detectRange, playerLayer);
    }

    // 可视化检测范围
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(gunPivot ? gunPivot.position : transform.position, detectRange);
    }
}
