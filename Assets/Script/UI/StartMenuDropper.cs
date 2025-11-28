using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuDropper : MonoBehaviour
{
    [System.Serializable]
    public class DropItem
    {
        [Tooltip("要下坠的对象（UI: RectTransform，世界物体: Transform）")]
        public Transform target;

        [Header("可选：按钮与场景名")]
        public Button button;
        public string loadSceneName = "";

        // 运行时
        [HideInInspector] public bool isUI;
        [HideInInspector] public Vector3 targetPos;
    }

    [Header("—— 对象列表（顺序 = 叠落顺序 Top→Bottom）——")]
    public List<DropItem> items = new List<DropItem>(3);

    [Header("共同起点（重叠处，本地坐标）")]
    public Vector3 commonStartLocal = Vector3.zero; // (0,0,0)

    [Header("目标位置（等差纵向）")]
    public float baseTargetX = 0f;
    public float baseTargetY = -2f;   // 第一块：0,-2
    public float baseTargetZ = 0f;
    public float stepY = -2f;         // 后续 -4、-6

    [Header("时序")]
    [Tooltip("每一段（整段曲线）的时长")]
    public float segmentDuration = 0.75f;
    [Tooltip("两段之间的短暂停顿")]
    public float stagePause = 0.03f;
    [Tooltip("使用不受 Time.timeScale 影响的计时")]
    public bool useUnscaledTime = true;

    [Header("手动速度曲线（0→1 映射到 起点→目标）。允许>1 以产生过冲回弹")]
    public AnimationCurve segmentCurve;

    [Header("开发便捷")]
    public bool playOnStart = true;

    void Reset()
    {
        // 提供一个顺滑“下坠→轻微过冲→回位”的默认曲线（你可以在 Inspector 里自由编辑）
        segmentCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.2f),   // 起步慢 -> 加速下坠
            new Keyframe(0.7f, 1.05f, 0f, 0f), // 稍过 1 产生过冲
            new Keyframe(1f, 1f, 0f, 0f)      // 回到 1
        );
        // 让中间关键帧切线更顺
        segmentCurve.SmoothTangents(0, 0.5f);
        segmentCurve.SmoothTangents(1, 0.5f);
        segmentCurve.SmoothTangents(2, 0.5f);
    }

    void Awake()
    {
        // 目标位置：等差叠放（0,-2）→（0,-4）→（0,-6）
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null || it.target == null) continue;
            it.isUI = it.target is RectTransform;
            it.targetPos = new Vector3(baseTargetX, baseTargetY + stepY * i, baseTargetZ);
        }

        // 把所有对象放到共同起点（重叠起始）
        foreach (var it in items)
        {
            if (it == null || it.target == null) continue;
            if (it.isUI)
                ((RectTransform)it.target).anchoredPosition = new Vector2(commonStartLocal.x, commonStartLocal.y);
            else
                it.target.localPosition = commonStartLocal;
        }

        // 自动绑定按钮
        foreach (var it in items)
        {
            if (it?.button == null) continue;
            if (!string.IsNullOrEmpty(it.loadSceneName))
            {
                string scene = it.loadSceneName;
                it.button.onClick.AddListener(() => LoadScene(scene));
            }
        }

        // 若用户没设曲线，给一个线性兜底
        if (segmentCurve == null || segmentCurve.length == 0)
            segmentCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    void Start()
    {
        if (playOnStart) StartCoroutine(PlayStackDrop());
    }

    public void PlayNow()
    {
        StopAllCoroutines();
        StartCoroutine(PlayStackDrop());
    }

    private IEnumerator PlayStackDrop()
    {
        if (items.Count == 0) yield break;

        Vector3 stackPos = commonStartLocal;

        for (int stage = 0; stage < items.Count; stage++)
        {
            var anchor = items[stage];
            Vector3 start = stackPos;
            Vector3 target = anchor.targetPos;

            float t = 0f;
            while (t < segmentDuration)
            {
                t += DeltaTime();
                float a = Mathf.Clamp01(t / segmentDuration);

                // 关键：用你手动编辑的曲线决定“位移进度 k”
                float k = segmentCurve.Evaluate(a); // 可>1或<0
                stackPos = Vector3.LerpUnclamped(start, target, k);

                for (int j = stage; j < items.Count; j++)
                    ApplyPosition(items[j], stackPos);

                yield return null;
            }

            // 对齐到目标，避免累计误差
            stackPos = target;
            for (int j = stage; j < items.Count; j++)
                ApplyPosition(items[j], stackPos);

            if (stagePause > 0f) yield return WaitForSecondsSmart(stagePause);
        }
    }

    private void ApplyPosition(DropItem it, Vector3 pos)
    {
        if (it.isUI)
            ((RectTransform)it.target).anchoredPosition = new Vector2(pos.x, pos.y);
        else
            it.target.localPosition = pos;
    }

    private float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    private WaitForSecondsRealtime WaitForSecondsSmart(float s) => new WaitForSecondsRealtime(s);

    // —— 按钮回调 —— //
    public void LoadMode1() => LoadScene("Mode1");
    public void LoadMode2() => LoadScene("Mode2");
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (useUnscaledTime) Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
