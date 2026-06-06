using UnityEngine;

public class SpikeSpawner : MonoBehaviour
{
    public static float BarrierPauseUntil { get; private set; }

    [Header("=== Prefab ===")]
    [Tooltip("Spike prefab for the floor.")]
    public GameObject spikeFloorPrefab;
    [Tooltip("Spike prefab for the ceiling.")]
    public GameObject spikeCeilPrefab;

    [Header("=== Spawn Position ===")]
    public float floorY = -2.2f;
    public float ceilY = 2.2f;
    public float spawnX = 10f;

    [Header("=== Spawn Interval ===")]
    public float initialInterval = 2.5f;
    public float minInterval = 0.8f;

    [Header("=== Safety ===")]
    public int maxConsecutiveSameSide = 2;

    [Header("=== Level Pattern Design ===")]
    [Tooltip("X distance between spikes inside one designed pattern.")]
    public float patternSpacing = 1.35f;
    [Tooltip("X distance between floor/ceiling alternating spikes.")]
    public float alternatingPatternSpacing = 2.35f;
    [Tooltip("Alternating floor/ceiling patterns start at this level.")]
    public int alternatingStartLevel = 4;
    [Tooltip("Same-side spike clusters start at this level.")]
    public int clusterStartLevel = 6;
    [Tooltip("Bait patterns start at this level.")]
    public int baitStartLevel = 8;
    [Tooltip("Pressure gate patterns start at this level.")]
    public int gateStartLevel = 10;

    private float _timer = 0f;
    private float _interval = 2.5f;
    private int _lastSide = -1;
    private int _consecutiveCnt = 0;

    void Start()
    {
        _interval = initialInterval;
        _timer = _interval * 0.5f;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (spikeFloorPrefab == null || spikeCeilPrefab == null) return;

        if (GameManager.Instance != null)
        {
            float speed = GameManager.Instance.CurrentSpeedMultiplier;
            int level = GameManager.Instance.CurrentLevel;
            float levelMultiplier = Mathf.Pow(0.95f, level);

            _interval = Mathf.Max(minInterval, (initialInterval * levelMultiplier) / speed);
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
        int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 0;

        if (TrySpawnDesignedPattern(level)) return;

        SpawnSingleSpike();
    }

    bool TrySpawnDesignedPattern(int level)
    {
        if (level < alternatingStartLevel) return false;

        float patternChance = Mathf.Clamp01(0.18f + level * 0.055f);
        if (Random.value > patternChance) return false;

        float roll = Random.value;

        if (level >= gateStartLevel && roll < 0.15f)
        {
            SpawnPressureGatePattern();
            PauseBarriersForPattern(1.4f);
            return true;
        }

        if (level >= baitStartLevel && roll < 0.35f)
        {
            SpawnBaitPattern();
            PauseBarriersForPattern(1.2f);
            return true;
        }

        if (level >= clusterStartLevel && roll < 0.55f)
        {
            SpawnClusterPattern(level);
            PauseBarriersForPattern(1.1f);
            return true;
        }

        SpawnAlternatingPattern(level);
        PauseBarriersForPattern(1.35f);
        return true;
    }

    void SpawnSingleSpike()
    {
        int side;
        if (_consecutiveCnt >= maxConsecutiveSameSide)
            side = (_lastSide == 0) ? 1 : 0;
        else
            side = Random.Range(0, 2);

        RememberSide(side);
        CreateSpikeAt(side);
    }

    void SpawnAlternatingPattern(int level)
    {
        int startSide = Random.Range(0, 2);
        int count = level >= 12 ? Random.Range(3, 5) : 2;

        for (int i = 0; i < count; i++)
        {
            int side = (startSide + i) % 2;
            CreateSpikeAt(side, i * alternatingPatternSpacing);
        }

        ResetSideMemory();
    }

    void SpawnClusterPattern(int level)
    {
        int side = Random.Range(0, 2);
        int count = level >= 12 ? Random.Range(3, 5) : Random.Range(2, 4);

        for (int i = 0; i < count; i++)
        {
            CreateSpikeAt(side, i * patternSpacing);
        }

        RememberSide(side);
    }

    void SpawnBaitPattern()
    {
        int baitSide = Random.Range(0, 2);
        int punishSide = 1 - baitSide;

        CreateSpikeAt(baitSide, 0f);
        CreateSpikeAt(punishSide, patternSpacing * 1.2f);
        CreateSpikeAt(punishSide, patternSpacing * 2.2f);

        ResetSideMemory();
    }

    void SpawnPressureGatePattern()
    {
        int pressureSide = Random.Range(0, 2);

        CreateSpikeAt(0, 0f);
        CreateSpikeAt(1, 0f);
        CreateSpikeAt(pressureSide, patternSpacing * 1.35f);
        CreateSpikeAt(pressureSide, patternSpacing * 2.35f);

        ResetSideMemory();
    }

    void RememberSide(int side)
    {
        if (side == _lastSide)
        {
            _consecutiveCnt++;
        }
        else
        {
            _consecutiveCnt = 1;
            _lastSide = side;
        }
    }

    void ResetSideMemory()
    {
        _lastSide = -1;
        _consecutiveCnt = 0;
    }

    void CreateSpikeAt(int side)
    {
        CreateSpikeAt(side, 0f);
    }

    void CreateSpikeAt(int side, float xOffset)
    {
        float y = (side == 0) ? floorY : ceilY;
        Vector3 pos = new Vector3(spawnX + xOffset, y, 0f);
        
        GameObject prefabToSpawn = (side == 0) ? spikeFloorPrefab : spikeCeilPrefab;
        // Ceiling spike already points down visually, but we can still rely on the prefab's default rotation.
        // Usually, side == 1 meant rotating the single spike prefab 180 degrees.
        // Now that we have dedicated ceiling prefabs, they might be drawn pointing down already!
        // The images uploaded for ceiling (satellite, arm) are already oriented correctly.
        // So we don't rotate them 180 degrees.
        Quaternion rot = Quaternion.identity; 
        
        Instantiate(prefabToSpawn, pos, rot);
    }

    void PauseBarriersForPattern(float seconds)
    {
        BarrierPauseUntil = Mathf.Max(BarrierPauseUntil, Time.time + seconds);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(spawnX, floorY, 0f), 0.3f);
        Gizmos.DrawWireSphere(new Vector3(spawnX, ceilY, 0f), 0.3f);
    }
}
