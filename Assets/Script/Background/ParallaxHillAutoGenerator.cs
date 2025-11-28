using UnityEngine;

public class ParallaxHillAutoGenerator : MonoBehaviour
{
    public DynamicHillsGenerator hillsGenerator;
    public Transform cameraTransform;
    public float layerSpeed = 0.5f; // 与ParallaxLayer保持一致

    void Update()
    {
        // 计算本层“世界X”——根据相机位置和视差比例推算相对世界进度
        float targetX = cameraTransform.position.x * layerSpeed;
        hillsGenerator.GenerateIfNeeded(targetX);
    }
}
