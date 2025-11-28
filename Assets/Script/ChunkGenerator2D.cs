using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkGenerator2D : MonoBehaviour
{
    [Header("Grid Size")]
    public int chunkWidth = 10;
    public int chunkHeight = 10;

    [Header("Floor / Pit Settings")]
    public int startFloorY = 4;
    public int maxFloorLen = 10;
    public int maxPitLen = 6;
    public int maxDrop = 3;
    [Range(0f, 1f)] public float pitChance = 0.3f;

    [Header("Platform Settings")]
    [Range(0f, 1f)] public float platformStartChance = 0.2f;
    public int platformMinYDelta = 3;
    public int platformMaxYDelta = 4;
    public int platformMinLen = 2;
    public int platformMaxLen = 10;

    [Header("Tiles")]
    public Tilemap tilemap;
    public TileBase groundTile;          // 地表瓦片
    public TileBase fillTile;            // 地面以下填充瓦片
    public TileBase platformTile;

    [Header("Spike Settings")]
    public GameObject spikePrefab;   // 拖入你的障碍物预制体
    [Range(0f, 1f)] public float spikeChance = 0.2f;  // 每格生成障碍概率
    public float spikeStayTime = 1f;
    private Transform spikeRoot;      // 推荐挂一个空物体用于障碍实例的父物体

    [Header("Slime Settings")]
    public GameObject slimePrefab;   // 拖入你的障碍物预制体
    [Range(0f, 1f)] public float slimeChance = 0.2f;  // 每格生成障碍概率
    public float slimeSpeed = 1f;
    private Transform slimeRoot;      // 推荐挂一个空物体用于障碍实例的父物体

    [Header("Turret Settings")]
    public GameObject turretPrefab;   // 拖入你的障碍物预制体
    public int turretNums = 1;  // 每格生成障碍概率
    public float turretDetectRange = 10f;               // 炮塔射击范围
    public float turretAimTime = 2f;                    // 炮塔准备时间
    private Transform turretRoot;      // 推荐挂一个空物体用于障碍实例的父物体

    private int[,] grid; // 0 空，1 地面，2 平台，3表示地面以下填充
    [HideInInspector] public int[] floorHeights; // 每列地面的Y坐标

    //void Start()
    //{
    //    GenerateFloorChunk(startFloorY);
    //    GeneratePlatforms();
    //    PaintGridToTilemap();
    //    GenerateObstacles();
    //}

    void Awake()
    {
        // 自动创建一个障碍物父物体，挂在本chunk下
        spikeRoot = new GameObject("Spike").transform;
        spikeRoot.SetParent(this.transform);
        spikeRoot.localPosition = Vector3.zero;

        // 自动创建一个障碍物父物体，挂在本chunk下
        slimeRoot = new GameObject("Slime").transform;
        slimeRoot.SetParent(this.transform);
        slimeRoot.localPosition = Vector3.zero;

        // 自动创建一个障碍物父物体，挂在本chunk下
        turretRoot = new GameObject("Turret").transform;
        turretRoot.SetParent(this.transform);
        turretRoot.localPosition = Vector3.zero;
    }

    // 供 ChunkManager 调用
    public void SetParams(LevelParams param)
    {
        //平台
        pitChance = param.pitChance;
        platformStartChance = param.platformChance;
        //地刺
        spikeChance = param.spikeChance;
        spikeStayTime = param.spikeStayTime;
        //史莱姆
        slimeChance = param.slimeChance;
        slimeSpeed = param.slimeSpeed;
        //炮塔
        turretNums = param.turretNums;
        turretDetectRange = param.turretDetectRange;
        turretAimTime = param.turretAimTime;
    }

    public void GenerateFloorChunk(int initialY)
    {
        floorHeights = new int[chunkWidth];
        grid = new int[chunkWidth, chunkHeight];

        int x = 0;
        bool lastWasPit = false; // 防止连续坑洞
        while (x < chunkWidth)
        {
            bool isEdge = (x == 0 || x + 1 >= chunkWidth);
            bool makeFloor = isEdge || lastWasPit || Random.value > pitChance;
            if (makeFloor)
            {
                int len = Random.Range(1, maxFloorLen + 1);
                int sectionBaseY = (x == 0) ? initialY : initialY + Random.Range(-maxDrop, maxDrop + 1);
                sectionBaseY = Mathf.Clamp(sectionBaseY, 1, chunkHeight - 1);
                for (int i = 0; i < len && x < chunkWidth; i++, x++)
                {
                    floorHeights[x] = sectionBaseY;
                    grid[x, sectionBaseY] = 1; // 标记地面
                    for (int y = 0; y < sectionBaseY; y++)
                        grid[x, y] = 3; // 地面以下填充
                }
                lastWasPit = false;
            }
            else
            {
                int len = Random.Range(1, maxPitLen + 1);
                for (int i = 0; i < len && x < chunkWidth; i++, x++)
                {
                    floorHeights[x] = -1; // 坑洞
                }
                lastWasPit = true;
            }
        }

        if (floorHeights[chunkWidth - 1] < 0)
        {
            int fixY = initialY + Random.Range(-maxDrop, maxDrop + 1);
            fixY = Mathf.Clamp(fixY, 1, chunkHeight - 1);
            floorHeights[chunkWidth - 1] = fixY;
            grid[chunkWidth - 1, fixY] = 1;
            for (int y = 0; y < fixY; y++)
                grid[chunkWidth - 1, y] = 3;
        }
    }

    public void GeneratePlatforms()
    {
        for (int layer = 0; layer < 3; layer++) // 生成两层平台，可按需调整
        {
            int x = 0;
            while (x < chunkWidth)
            {
                if (Random.value < platformStartChance)
                {
                    int baseY = -1;
                    // 找到该列最高的地面或平台
                    for (int y = chunkHeight - 1; y >= 0; y--)
                        if (grid[x, y] != 0) { baseY = y; break; }
                    if (baseY < 0) { x++; continue; }

                    int y0 = baseY + Random.Range(platformMinYDelta, platformMaxYDelta + 1);
                    if (y0 >= chunkHeight) { x++; continue; }

                    int maxLen = Random.Range(platformMinLen, platformMaxLen + 1);
                    int len = maxLen;
                    bool canPlace = false;
                    while (len >= platformMinLen && !canPlace)
                    {
                        canPlace = true;
                        for (int lx = 0; lx < len; lx++)
                        {
                            int px = x + lx;
                            int py = y0;
                            // 检查是否越界（横向）
                            if (px < 0 || px >= chunkWidth)
                            {
                                canPlace = false;
                                break;
                            }
                            // 8方向检测
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    int nx = px + dx;
                                    int ny = py + dy;
                                    // 检查越界（纵向/横向）
                                    if (nx < 0 || nx >= chunkWidth || ny < 0 || ny >= chunkHeight)
                                    {
                                        canPlace = false;
                                        break;
                                    }
                                    if (grid[nx, ny] != 0)
                                    {
                                        canPlace = false;
                                        break;
                                    }
                                }
                                if (!canPlace) break;
                            }
                            if (!canPlace) break;
                        }
                        if (!canPlace) len--;
                    }

                    // 只有长度满足要求且检测通过才生成
                    if (canPlace && len >= platformMinLen)
                    {
                        for (int lx = 0; lx < len; lx++)
                            grid[x + lx, y0] = 2;
                        x += len; // 跳过这段
                        continue;
                    }
                }
                x++;
            }
        }
    }


    public void PaintGridToTilemap()
    {
        tilemap.ClearAllTiles();
        for (int x = 0; x < chunkWidth; x++)
        {
            int fy = floorHeights[x];
            if (fy >= 0)
            {
                tilemap.SetTile(new Vector3Int(x, fy, 0), groundTile);
                for (int y = 0; y < fy; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), fillTile);
                }
            }
        }
        // 绘制平台
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                if (grid[x, y] == 2)
                    tilemap.SetTile(new Vector3Int(x, y, 0), platformTile);
            }
        }
    }

    public int GetEndFloorY()
    {
        // 找到chunk最右侧的floorHeights
        for (int x = chunkWidth - 1; x >= 0; x--)
        {
            if (floorHeights[x] >= 0) return floorHeights[x];
        }
        // 找不到则默认用起始高度
        return startFloorY;
    }

    public void GenerateObstacles()
    {
        List<Vector2Int> candidateList = new List<Vector2Int>();

        // 1. 收集所有地面可生成障碍的位置
        for (int x = 0; x < chunkWidth; x++)
        {
            int fy = floorHeights[x];
            if (fy >= 0 && grid[x, fy] == 1)
            {
                candidateList.Add(new Vector2Int(x, fy)); // +1表示生成在地面正上方
            }
        }
        // 2. 收集所有平台可生成障碍的位置
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                if (grid[x, y] == 2)
                {
                    candidateList.Add(new Vector2Int(x, y)); // +1表示平台正上方
                }
            }
        }

        // 3. Fisher-Yates 洗牌乱序
        for (int i = candidateList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = candidateList[i];
            candidateList[i] = candidateList[j];
            candidateList[j] = temp;
        }

        // 4. 按概率决定本chunk实际要生成多少个障碍
        int obsCount = Mathf.RoundToInt(candidateList.Count * spikeChance);

        for (int i = 0; i < obsCount && i < candidateList.Count; i++)
        {
            Vector2Int pos = candidateList[i];
            Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(pos.x, pos.y, 0)) + new Vector3(0.5f, 0.5f, 0f);
            var obj = Instantiate(spikePrefab, worldPos, Quaternion.identity, spikeRoot);
            var obs = obj.GetComponent<SpikeController>();
            if (obs != null)
                obs.stayTime = spikeStayTime;
        }
    }

    public void GenerateSlime()
    {
        // 地面障碍
        //for (int x = 0; x < chunkWidth; x++)
        //{
        //    int fy = floorHeights[x];
        //    if (fy >= 0 && grid[x, fy] == 1) // 地面
        //    {
        //        if (Random.value < slimeChance)
        //        {
        //            Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(x, fy + 1, 0)) + new Vector3(0.5f, 0f, 0f); // 地面正上方
        //            var obj = Instantiate(slimePrefab, worldPos, Quaternion.identity, slimeRoot);
        //            var obs = obj.GetComponent<EnemyController>();
        //            if (obs != null)
        //                obs.moveSpeed = slimeSpeed;
        //        }
        //    }
        //}

        // 平台障碍
        //for (int x = 0; x < chunkWidth; x++)
        //{
        //    for (int y = 0; y < chunkHeight; y++)
        //    {
        //        if (grid[x, y] == 2)
        //        {
        //            if (Random.value < slimeChance)
        //            {
        //                Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(x, y + 1, 0)) + new Vector3(0.5f, 0f, 0f); // 平台正上方
        //                var obj = Instantiate(slimePrefab, worldPos, Quaternion.identity, slimeRoot);
        //                var obs = obj.GetComponent<EnemyController>();
        //                if (obs != null)
        //                    obs.moveSpeed = slimeSpeed;
        //            }
        //        }
        //    }
        //}

        List<Vector2Int> candidateList = new List<Vector2Int>();

        // 1. 收集所有地面可生成障碍的位置
        for (int x = 0; x < chunkWidth; x++)
        {
            int fy = floorHeights[x];
            if (fy >= 0 && grid[x, fy] == 1)
            {
                candidateList.Add(new Vector2Int(x, fy + 1)); // +1表示生成在地面正上方
            }
        }
        // 2. 收集所有平台可生成障碍的位置
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                if (grid[x, y] == 2)
                {
                    candidateList.Add(new Vector2Int(x, y + 1)); // +1表示平台正上方
                }
            }
        }

        // 3. Fisher-Yates 洗牌乱序
        for (int i = candidateList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = candidateList[i];
            candidateList[i] = candidateList[j];
            candidateList[j] = temp;
        }

        // 4. 按概率决定本chunk实际要生成多少个障碍
        int obsCount = Mathf.RoundToInt(candidateList.Count * slimeChance);

        for (int i = 0; i < obsCount && i < candidateList.Count; i++)
        {
            Vector2Int pos = candidateList[i];
            Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(pos.x, pos.y, 0)) + new Vector3(0.5f, 0f, 0f);
            var obj = Instantiate(slimePrefab, worldPos, Quaternion.identity, slimeRoot);
            var obs = obj.GetComponent<EnemyController>();
            if (obs != null)
                obs.moveSpeed = slimeSpeed;
        }
    }

    public void GenerateTurret()
    {
        // 地面障碍
        //for (int x = 0; x < chunkWidth; x++)
        //{
        //    int fy = floorHeights[x];
        //    if (fy >= 0 && grid[x, fy] == 1) // 地面
        //    {
        //        if (Random.value < turretChance)
        //        {
        //            Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(x, fy, 0)) + new Vector3(0.5f, 0f, 0f); // 地面
        //            var obj = Instantiate(turretPrefab, worldPos, Quaternion.identity, turretRoot);
        //            var obs = obj.GetComponent<TurretAI>();
        //            if (obs != null)
        //            {
        //                obs.detectRange = turretDetectRange;
        //                obs.aimTime = turretAimTime;
        //            }
                        
        //        }
        //    }
        //}

        // 平台障碍
        //for (int x = 0; x < chunkWidth; x++)
        //{
        //    for (int y = 2; y < chunkHeight; y++)
        //    {
        //        if (grid[x, y] == 3)
        //        {
        //            if (Random.value < turretChance)
        //            {
        //                Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(x, y, 0)) + new Vector3(0.5f, 0f, 0f); // 平台正上方
        //                var obj = Instantiate(turretPrefab, worldPos, Quaternion.identity, turretRoot);
        //                var obs = obj.GetComponent<TurretAI>();
        //                if (obs != null)
        //                {
        //                    obs.detectRange = turretDetectRange;
        //                    obs.aimTime = turretAimTime;
        //                }
        //            }
        //        }
        //    }
        //}

        List<Vector2Int> candidateList = new List<Vector2Int>();

        
        // 2. 收集所有平台可生成障碍的位置
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 2; y < chunkHeight; y++)
            {
                if (grid[x, y] == 3)
                {
                    candidateList.Add(new Vector2Int(x, y)); 
                }
            }
        }

        // 3. Fisher-Yates 洗牌乱序
        for (int i = candidateList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = candidateList[i];
            candidateList[i] = candidateList[j];
            candidateList[j] = temp;
        }

        // 4. 按概率决定本chunk实际要生成多少个障碍
        int obsCount = turretNums;

        for (int i = 0; i < obsCount && i < candidateList.Count; i++)
        {
            Vector2Int pos = candidateList[i];
            Vector3 worldPos = tilemap.CellToWorld(new Vector3Int(pos.x, pos.y, 0)) + new Vector3(0.5f, 0f, 0f);
            var obj = Instantiate(turretPrefab, worldPos, Quaternion.identity, turretRoot);
            var obs = obj.GetComponent<TurretAI>();
            if (obs != null)
            {
                obs.detectRange = turretDetectRange;
                obs.aimTime = turretAimTime;
            }
        }
    }

}

