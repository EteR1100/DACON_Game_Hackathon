using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 가시(Spike) 스포너
/// - 바닥/천장에 랜덤으로 가시를 생성
/// - 항상 공이 피할 수 있는 안전한 공간을 보장
/// - 게임 속도에 따라 생성 간격 줄어듦
/// </summary>
public class SpikeSpawner : MonoBehaviour
{
    [Header("=== 프리팹 ===")]
    [Tooltip("가시 프리팹 (Spike.cs가 붙어 있어야 함)")]
    public GameObject spikePrefab;

    [Header("=== 스폰 위치 ===")]
    [Tooltip("바닥 가시가 생성될 Y 좌표")]
    public float floorY  = -2.2f;
    [Tooltip("천장 가시가 생성될 Y 좌표")]
    public float ceilY   =  2.2f;
    [Tooltip("스폰 X 좌표 (화면 오른쪽 밖)")]
    public float spawnX  =  10f;

    [Header("=== 스폰 간격 ===")]
    [Tooltip("최초 스폰 간격(초)")]
    public float initialInterval = 2.5f;
    [Tooltip("최소 스폰 간격(초) — 이보다 짧아지지 않음")]
    public float minInterval     = 0.8f;

    [Header("=== 난이도 ===")]
    [Tooltip("가시가 연속으로 같은 쪽에 나올 수 있는 최대 횟수")]
    public int maxConsecutiveSameSide = 2;

    private float _timer        = 0f;
    private float _interval     = 2.5f;
    private int   _lastSide     = -1;  // -1=없음, 0=바닥, 1=천장
    private int   _consecutiveCnt = 0;

    void Start()
    {
        _interval = initialInterval;
        _timer    = _interval * 0.5f; // 처음엔 약간 늦게 시작
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (spikePrefab == null) return;

        // 속도에 따라 스폰 간격 줄이기
        if (GameManager.Instance != null)
        {
            float speed = GameManager.Instance.CurrentSpeedMultiplier;
            _interval = Mathf.Max(minInterval, initialInterval / speed);
        }

        _timer += Time.deltaTime;
        if (_timer >= _interval)
        {
            _timer = 0f;
            SpawnSpike();
        }
    }

    void SpawnSpike()
    {
        // ── 어느 쪽에 스폰할지 결정 ──
        // 같은 쪽이 연속으로 너무 많이 나오면 반대쪽 강제
        int side;
        if (_consecutiveCnt >= maxConsecutiveSameSide)
        {
            // 반대쪽 강제
            side = (_lastSide == 0) ? 1 : 0;
        }
        else
        {
            side = Random.Range(0, 2); // 0=바닥, 1=천장
        }

        // 연속 카운트 갱신
        if (side == _lastSide)
            _consecutiveCnt++;
        else
        {
            _consecutiveCnt = 1;
            _lastSide = side;
        }

        float y = (side == 0) ? floorY : ceilY;
        Vector3 pos = new Vector3(spawnX, y, 0f);

        // 천장 가시는 상하 반전
        Quaternion rot = (side == 1) ? Quaternion.Euler(180f, 0f, 0f) : Quaternion.identity;

        Instantiate(spikePrefab, pos, rot);
    }

    // 에디터 Gizmo로 스폰 위치 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(spawnX, floorY, 0f), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(spawnX, ceilY,  0f), 0.3f);
    }
}
