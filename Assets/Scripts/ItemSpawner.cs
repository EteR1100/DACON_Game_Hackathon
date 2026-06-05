using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ?袁⑹뵠??獄??怨쀬뵠?????땅 ??쎈７??
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("=== Item Prefabs ===")]
    public GameObject packetPrefab;
    public GameObject magnetPrefab;
    public GameObject shieldPrefab;
    public GameObject giantPrefab;
    public GameObject randomPrefab;

    [Header("=== Spawn Settings ===")]
    public float minY = -1.5f;
    public float maxY = 1.5f;
    public float spawnX = 10f;
    
    public float initialInterval = 1.5f; // ?袁⑹뵠??? ???癒?폒 ??륁궎野?
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
        int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 0;

        // ???? ?袁⑹뵠???類ｌぇ ?④쑴沅?????
        // 疫꿸퀡??10% (0.1) ?癒?퐣 ??덇볼??1.5% (0.015) ??筌앹빓?, 筌ㅼ뮆? 25% (0.25)
        float specialItemChance = Mathf.Min(0.25f, 0.1f + (level * 0.008f));
        
        GameObject prefabToSpawn = packetPrefab;
        float r = Random.value;

        if (r < specialItemChance)
        {
            // ?諭???袁⑹뵠???癒?퐤, 獄쎻뫗堉깍쭕? 椰꾧퀡??? ??뺣쑁?怨몄쁽) 餓???롪돌 ?醫뤾문
            float r2 = Random.value;
            if (r2 < 0.25f) prefabToSpawn = magnetPrefab;
            else if (r2 < 0.5f) prefabToSpawn = shieldPrefab;
            else if (r2 < 0.75f) prefabToSpawn = giantPrefab;
            else prefabToSpawn = randomPrefab;
        }
        else
        {
            // ???땅(疫꿸퀡?????? ??밴쉐
            prefabToSpawn = packetPrefab;
        }

        if (prefabToSpawn != null)
        {
            float y = Random.Range(minY, maxY);
            Vector3 pos = new Vector3(spawnX, y, 0f);
            Instantiate(prefabToSpawn, pos, Quaternion.identity);
        }
    }
}
