using UnityEngine;

public class TurretBullet : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;
    private float maxDistance;
    private float traveled = 0f;
    private LayerMask playerLayer;

    public void Init(Vector2 dir, float spd, float range, LayerMask playerLayer)
    {
        moveDir = dir.normalized;
        speed = spd;
        maxDistance = range;
        this.playerLayer = playerLayer;
    }

    void Update()
    {
        float moveStep = speed * Time.deltaTime;
        transform.position += (Vector3)(moveDir * moveStep);
        traveled += moveStep;

        // 撞到玩家
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.18f, playerLayer);
        if (hit)
        {
            PlayerMovement pm = hit.GetComponent<PlayerMovement>();
            if (pm)
                pm.OnTriggerEnter2D(GetComponent<Collider2D>()); // 触发死亡
            Destroy(gameObject);
        }

        // 超出最大射程自动销毁
        if (traveled > maxDistance)
            Destroy(gameObject);
    }
}
