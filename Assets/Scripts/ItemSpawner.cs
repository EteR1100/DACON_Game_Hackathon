using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 아이템 및 데이터 패킷 스포너
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("프리팹")]
    public GameObject packetPrefab;
    public GameObject magnetPrefab;
    public GameObject shieldPrefab;
    public GameObject giantPrefab;
    public GameObject randomPrefab;

    [Header("설정")]
    public float minY = -1.5f;
    public float maxY = 1.5f;
    public float spawnX = 10f;
    
    public float initialInterval = 1.5f; // 아이템은 꽤 자주 나오게
    public float minInterval = 0.5f;
    
    private float _timer = 0f;
    private float _interval = 1.5f;

    void Start()
    {
        _interval = initialInterval;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (GameManager.Instance != null)
        {
            float speed = GameManager.Instance.CurrentSpeedMultiplier;
            _interval = Mathf.Max(minInterval, initialInterval / speed);
        }

        _timer += Time.deltaTime;
        if (_timer >= _interval)
        {
            _timer = 0f;
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        // 80% 확률로 패킷, 20% 확률로 특수 아이템(자석, 방어막, 거대화, 랜덤상자)
        GameObject prefabToSpawn = packetPrefab;
        float r = Random.value;
        if (r > 0.8f)
        {
            float r2 = Random.value;
            if (r2 < 0.25f) prefabToSpawn = magnetPrefab;
            else if (r2 < 0.5f) prefabToSpawn = shieldPrefab;
            else if (r2 < 0.75f) prefabToSpawn = giantPrefab;
            else prefabToSpawn = randomPrefab;
        }

        if (prefabToSpawn != null)
        {
            float y = Random.Range(minY, maxY);
            Vector3 pos = new Vector3(spawnX, y, 0f);
            Instantiate(prefabToSpawn, pos, Quaternion.identity);
        }
    }
}
