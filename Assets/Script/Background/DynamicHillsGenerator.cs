using UnityEngine;
using UnityEngine.Tilemaps;

public class DynamicHillsGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase surfaceTile;
    public TileBase fillTile;
    public int minHeight = 2;
    public int maxHeight = 8;
    public float smoothness = 0.15f;
    public int chunkWidth = 40; // 每段生成多少格
    public float chunkTriggerOffset = 10f; // 距离右边界多少格时触发新生成

    private int currentMaxX = 0; // 已生成到的最大X

    // 用于让每一层用不同seed产生不同形状
    public float noiseSeed = 0f;

    /// <summary>
    /// 自动动态生成，只需给定世界坐标
    /// </summary>
    public void GenerateIfNeeded(float worldX)
    {
        int checkX = Mathf.FloorToInt(worldX);
        if (checkX > currentMaxX - chunkTriggerOffset)
        {
            GenerateHills(currentMaxX, chunkWidth);
            currentMaxX += chunkWidth;
        }
    }

    public void GenerateHills(int startX, int width)
    {
        for (int x = startX; x < startX + width; x++)
        {
            float noise = Mathf.PerlinNoise(x * smoothness + noiseSeed, noiseSeed); // 用seed差异区分多层
            int hillHeight = Mathf.RoundToInt(Mathf.Lerp(minHeight, maxHeight, noise));
            for (int y = 0; y < hillHeight; y++)
            {
                bool isSurface = (y == hillHeight - 1);
                tilemap.SetTile(
                    new Vector3Int(x, y, 0),
                    isSurface ? surfaceTile : fillTile
                );
            }
        }
    }
}
