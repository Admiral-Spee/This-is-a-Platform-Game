using UnityEngine;
using System.Collections;

public class SpikeController : MonoBehaviour
{
    public float upOffset = 1f;     // 升起的相对高度（单位:米）
    public float downOffset = 0f;   // 降下的相对高度（一般为0）
    public float moveSpeed = 2f;      // 移动速度
    public float stayTime = 1f;     // 升起降下后停留时间


    private Vector3 originPos;      // 记录初始位置

    private void Start()
    {
        originPos = transform.position;
        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            // 升起
            yield return StartCoroutine(MoveToY(originPos.y + upOffset));
            yield return new WaitForSeconds(stayTime);
            // 降下
            yield return StartCoroutine(MoveToY(originPos.y + downOffset));
            yield return new WaitForSeconds(stayTime);
        }
    }

    IEnumerator MoveToY(float targetY)
    {
        Vector3 pos = transform.position;
        while (Mathf.Abs(transform.position.y - targetY) > 0.01f)
        {
            pos.y = Mathf.MoveTowards(transform.position.y, targetY, moveSpeed * Time.deltaTime);
            transform.position = pos;
            yield return null;
        }
    }
}
