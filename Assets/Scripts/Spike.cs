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
            // 장애물이 플레이어 위치를 지나친 직후 (오른쪽→왼쪽)
            if (transform.position.x < playerX - 0.1f)
            {
                _nearMissChecked = true;

                float yDist = Mathf.Abs(transform.position.y - _player.position.y);

                // 충돌 반경(약 0.3)보다 크고 nearMissDistance 이내면 아슬아슬 통과
                if (yDist > 0.35f && yDist < nearMissDistance)
                {
                    if (GameManager.Instance != null && !GameManager.Instance.IsFeverActive)
                    {
                        GameManager.Instance.AddFever(nearMissFeverAmount);
                        Debug.Log($"[NearMiss] yDist={yDist:F2} → Fever +{nearMissFeverAmount}");
                    }
                }
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
}
