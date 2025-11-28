using UnityEngine;

public class Cloud : MonoBehaviour
{
    public float speed = 0.5f; // 漂浮速度
    private float baseY;
    private float moveOffset;

    void Start()
    {
        baseY = transform.position.y;
        moveOffset = Random.Range(0, 100f);
        speed *= Random.Range(0.6f, 1.2f);
    }

    void Update()
    {
        // 横向缓慢漂移
        transform.position += Vector3.right * speed * Time.deltaTime;
        // 或可加y方向的小幅波动
        float y = baseY + Mathf.Sin(Time.time * 0.2f + moveOffset) * 0.2f;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // 如果超出一定距离自动销毁
        //if (Camera.main != null)
        //{
        //    float camX = Camera.main.transform.position.x;
        //    if (transform.position.x < camX - 30f || transform.position.x > camX + 50f)
        //    {
        //        Destroy(gameObject);
        //    }
        //}
    }
}
