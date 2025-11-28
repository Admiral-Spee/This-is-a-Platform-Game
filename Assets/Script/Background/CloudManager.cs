using UnityEngine;
using System.Collections.Generic;

public class CloudManager : MonoBehaviour
{
    public Transform cameraTransform;
    public GameObject[] cloudPrefabs;
    public float spawnRangeX = 40f;
    public float spawnMinY = 10f, spawnMaxY = 20f;
    public float minScale = 0.7f, maxScale = 1.5f;
    public int maxCloudCount = 8;
    public float cloudParallax = 0.5f; // 云漂移速度（和ParallaxLayer保持一致）

    private List<GameObject> clouds = new List<GameObject>();

    void Start()
    {
        for (int i = 0; i < maxCloudCount; i++)
        {
            SpawnCloud(cameraTransform.position.x + Random.Range(-spawnRangeX / 2, spawnRangeX / 2));
        }
    }

    void Update()
    {
        float camX = cameraTransform.position.x; // 只用相机世界坐标
        clouds.RemoveAll(c =>
        {
            if (!c) return true;
            float cloudX = c.transform.position.x;
            if (cloudX < camX - spawnRangeX || cloudX > camX + spawnRangeX)
            {
                Destroy(c);
                return true;
            }
            return false;
        });

        while (clouds.Count < maxCloudCount)
        {
            float halfScreen = Camera.main.orthographicSize * Camera.main.aspect;
            float x = camX + halfScreen + Random.Range(10f, 20f); // 只右侧
            SpawnCloud(x);
        }
    }

    void SpawnCloud(float centerX)
    {
        var prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        float x = centerX + Random.Range(-spawnRangeX / 2, spawnRangeX / 2);
        float y = Random.Range(spawnMinY, spawnMaxY);
        Vector3 pos = new Vector3(x, y, 0f);

        var cloud = Instantiate(prefab, pos, Quaternion.identity, transform);

        float scale = Random.Range(minScale, maxScale);
        cloud.transform.localScale = new Vector3(scale, scale, 1);

        var sr = cloud.GetComponent<SpriteRenderer>();
        if (sr)
        {
            sr.color = new Color(1, 1, 1, Random.Range(0.7f, 1f));
            if (Random.value > 0.5f) sr.flipX = true;
        }
        clouds.Add(cloud);
    }
}
