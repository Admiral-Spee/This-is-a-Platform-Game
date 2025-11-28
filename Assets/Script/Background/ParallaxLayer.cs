using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    public float parallaxMultiplier = 0.5f; // 0=跟着相机走, 1=静止, 0.5=视差一半
    private Vector3 lastCameraPos;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        lastCameraPos = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 delta = cameraTransform.position - lastCameraPos;
        // 只在x轴上视差移动，y轴可自定要不要参与
        transform.position += new Vector3(delta.x * parallaxMultiplier, 0f, 0f);
        lastCameraPos = cameraTransform.position;
    }
}
