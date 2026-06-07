using UnityEngine;

/// <summary>
/// 가시(Spike) / 공중 차단막(Barrier) 개별 오브젝트
/// - 왼쪽으로 이동하며 화면 밖으로 나가면 삭제
/// - 플레이어 충돌 시 GameOver
/// - 플레이어를 아슬아슬하게 지나치면 피버 게이지 충전
/// </summary>
public class Spike : MonoBehaviour
{
    [Tooltip("가시 기본 이동 속도 (GameSpeedProvider 배율에 곱해짐)")]
    public float baseSpeed = 5f;

    [Tooltip("Near Miss 판정 Y 거리 (이 거리 이내로 지나치면 피버 충전)")]
    public float nearMissDistance = 1.0f;

    [Tooltip("Near Miss 시 충전되는 피버 게이지 양 (0~1)")]
    public float nearMissFeverAmount = 0.08f;

    private float _destroyX = -12f;
    private bool  _nearMissChecked = false;    // 이미 근접 판정했는지
    private Transform _player;

    void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        float speed = baseSpeed * GameSpeedProvider.CurrentMultiplier;
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // Near Miss 체크 : 장애물이 플레이어 X를 막 통과하는 순간
        if (!_nearMissChecked && _player != null)
        {
            float playerX = _player.position.x;
            float xDist = transform.position.x - playerX;

            // X축 기준으로 플레이어를 스쳐 지나가는 구간 (전후 0.6 범위)
            if (Mathf.Abs(xDist) < 0.6f)
            {
                float yDist = Mathf.Abs(transform.position.y - _player.position.y);

                Collider2D col = GetComponent<Collider2D>();
                float myExtentY = col != null ? col.bounds.extents.y : 0.35f;
                float playerExtentY = 0.05f;
                float safeDist = myExtentY + playerExtentY + 0.05f;

                // 해당 구간 내에서 Y축 거리가 '아슬아슬한 회피' 조건에 딱 들어맞는 순간 즉시 발동! (판정 크기 0.8배 축소)
                float currentNearMissDist = nearMissDistance * 0.8f;
                if (yDist > safeDist && yDist < safeDist + currentNearMissDist)
                {
                    _nearMissChecked = true;

                    if (GameManager.Instance != null && !GameManager.Instance.IsFeverActive)
                    {
                        GameManager.Instance.AddFever(nearMissFeverAmount);
                        SpawnGrazeEffect((transform.position + _player.position) * 0.5f);
                    }
                }
            }
            // 플레이어를 완전히 지나쳤는데도 회피 판정이 안 났다면(거리가 멀거나 맞았거나) 체크 종료
            else if (xDist < -0.6f)
            {
                _nearMissChecked = true;
            }
        }

        if (transform.position.x < _destroyX)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pc = other.GetComponent<PlayerController>();
            
            // 피버 중이면 파괴
            if (GameManager.Instance != null && GameManager.Instance.IsFeverActive)
            {
                Destroy(gameObject);
                return;
            }

            if (pc != null)
            {
                if (pc.HasShield || pc.IsGiantActive)
                {
                    pc.Die();
                    Destroy(gameObject);
                    return;
                }
            }

            // 일반 상태 → 즉시 사망 처리
            if (pc != null) pc.Die();
        }
    }

    private void SpawnGrazeEffect(Vector3 pos)
    {
        // Fever Gauge exact color (#ff003c)
        Color feverColor;
        ColorUtility.TryParseHtmlString("#ff003c", out feverColor);

        // 1. Floating Text
        GameObject textGO = new GameObject("GrazeText");
        textGO.transform.position = pos + Vector3.up * 0.5f;
        
        var tm = textGO.AddComponent<TextMesh>();
        tm.text = "GREAT!";
        tm.color = feverColor;
        tm.fontSize = 40;
        tm.characterSize = 0.072f; // 이전 대비 추가로 1.2배 확대 (0.06 -> 0.072)
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontStyle = FontStyle.Bold;
        
        var mr = textGO.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 50;

        textGO.AddComponent<GrazeTextFX>();

        // 2. Simple Spark Particles
        GameObject sparkGO = new GameObject("GrazeSpark");
        sparkGO.transform.position = pos;
        var ps = sparkGO.AddComponent<ParticleSystem>();
        
        // 에러 방지를 위해 설정 전 파티클을 멈춥니다.
        ps.Stop();
        
        var main = ps.main;
        main.startColor = feverColor;
        main.startSize = 0.2f;
        main.startSpeed = 5f;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 10) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var psRenderer = sparkGO.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        psRenderer.material = mat;

        // 설정 완료 후 재생
        ps.Play();

        Destroy(sparkGO, 1f);
    }
}
