using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 怨듭쨷 李⑤떒留?Barrier) ?ㅽ룷??
/// - 怨듭쨷???쒕뜡???믪씠濡?李⑤떒留??앹꽦
/// - 諛붾떏怨?泥쒖옣 媛?쒖???蹂꾧컻濡??쒕뜡 ?앹꽦??
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
        _timer = _interval * 0.8f; // 媛?쒖? ??대컢 寃뱀튂吏 ?딄쾶 ?뉕컝?ㅼ꽌 ?쒖옉
    }

    void Update()
    {
        if (Time.timeScale == 0f || barrierPrefab == null) return;

        if (Time.time < SpikeSpawner.BarrierPauseUntil)
        {
            _timer = 0f;
            return;
        }

        if (GameManager.Instance != null)
        {
            int level = GameManager.Instance.CurrentLevel;

            // ?덈꺼 0 (0~999???먯꽌??李⑤떒留??앹꽦 ?덊븿
            if (level < 1)
            {
                _timer = 0f;
                return;
            }

            float speed = GameManager.Instance.CurrentSpeedMultiplier;
            // ?덈꺼??10%??媛꾧꺽 ?⑥텞
            float levelMultiplier = Mathf.Pow(0.95f, level - 1); // ?덈꺼 1遺??湲곗?

            _interval = Mathf.Max(minInterval, (initialInterval * levelMultiplier) / speed);
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
        int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 0;
        
        // ?? ?숈떆 ?앹꽦 媛쒖닔 寃곗젙 ??
        int count = 1;
        if (level >= 10) count = Random.Range(1, 4); // 理쒕? 3媛?
        else if (level >= 6) count = Random.Range(1, 3); // 理쒕? 2媛?

        // ?앹꽦 ?믪씠 蹂댁옣 (?쒕줈 ?덈Т 寃뱀튂吏 ?딄쾶)
        List<float> spawnYs = new List<float>();
        
        for (int i = 0; i < count; i++)
        {
            float y = Random.Range(minY, maxY);
            
            // ?덈Т 媛源뚯슫 ?꾩튂 諛⑹?
            bool tooClose = false;
            foreach (float existingY in spawnYs)
            {
                if (Mathf.Abs(y - existingY) < 0.8f) { tooClose = true; break; }
            }
            
            if (tooClose && i > 0) continue; // 寃뱀튂硫??대쾲 嫄대꼫?

            spawnYs.Add(y);
            Vector3 pos = new Vector3(spawnX, y, 0f);
            Instantiate(barrierPrefab, pos, Quaternion.identity);
        }
    }
}
