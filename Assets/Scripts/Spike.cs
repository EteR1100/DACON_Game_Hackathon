using UnityEngine;

/// <summary>
/// 가시(Spike) 개별 오브젝트
/// - 왼쪽으로 이동하며 화면 밖으로 나가면 삭제
/// - 플레이어 충돌 시 GameOver
/// </summary>
public class Spike : MonoBehaviour
{
    [Tooltip("가시 기본 이동 속도 (GameSpeedProvider 배율에 곱해짐)")]
    public float baseSpeed = 5f;

    private float _destroyX = -12f; // 화면 왼쪽 밖

    void Update()
    {
        if (Time.timeScale == 0f) return;

        float speed = baseSpeed * GameSpeedProvider.CurrentMultiplier;
        transform.Translate(Vector3.left * speed * Time.deltaTime);

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
                    pc.Die(); // Die 내부에서 쉴드 차감 등 처리됨
                    Destroy(gameObject);
                    return;
                }
            }

            // 일반 상태에서도 떨어지지 않고(죽지 않고) 통과하도록 임시 비활성화
            // if (GameManager.Instance != null)
            //     GameManager.Instance.GameOver();
            // if (pc != null) pc.Die();
        }
    }
}
