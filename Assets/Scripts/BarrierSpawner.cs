using UnityEngine;

/// <summary>
/// 공중 차단막(Barrier) 스포너
/// - 공중에 랜덤한 높이로 차단막 생성
/// - 바닥과 천장 가시와는 별개로 랜덤 생성됨
/// </summary>
public class BarrierSpawner : MonoBehaviour
{
    public GameObject barrierPrefab;
    public float minY = -1.5f;
    public float maxY = 1.5f;
    public float spawnX = 10f;
    
    public float initialInterval = 4.0f;
    public float minInterval = 1.5f;
    
    private float _timer = 0f;
    private float _interval = 4.0f;

    void Start()
    {
        _interval = initialInterval;
        _timer = _interval * 0.8f; // 가시와 타이밍 겹치지 않게 엇갈려서 시작
    }

    void Update()
    {
        if (Time.timeScale == 0f || barrierPrefab == null) return;

        if (GameManager.Instance != null)
        {
            float speed = GameManager.Instance.CurrentSpeedMultiplier;
            _interval = Mathf.Max(minInterval, initialInterval / speed);
        }

        _timer += Time.deltaTime;
        if (_timer >= _interval)
        {
            _timer = 0f;
            SpawnBarrier();
        }
    }

    void SpawnBarrier()
    {
        float y = Random.Range(minY, maxY);
        Vector3 pos = new Vector3(spawnX, y, 0f);
        Instantiate(barrierPrefab, pos, Quaternion.identity);
    }
}
