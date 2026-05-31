using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어(공) 제어 - 단순 버전
/// 클릭 or 스페이스 → 중력 반전 → 반대편 난간으로 이동
/// (난간에 닿아 있을 때만 반전 가능)
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("=== 물리 설정 ===")]
    public float gravityStrength = 28f;
    public float maxSpeed       = 15f;
    public float boundaryY      = 2.2f;

    private float _vy        = 0f;
    private int   _dir       = -1;   // -1 아래, +1 위
    private bool  _grounded  = true;

    void Start()
    {
        // Rigidbody2D 완전 비활성화
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType     = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        // 아래 난간에서 시작
        var p = transform.position;
        p.y = -boundaryY;
        transform.position = p;

        _vy       = 0f;
        _dir      = -1;
        _grounded = true;

        Debug.Log("[Player] Start OK - pos=" + transform.position + " grounded=" + _grounded);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        bool pressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                   || (Mouse.current    != null && Mouse.current.leftButton.wasPressedThisFrame);
        // 터치 스크린 지원
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            pressed = true;

        if (pressed)
        {
            Debug.Log("[Player] Input detected! grounded=" + _grounded);
            if (_grounded)
            {
                _dir     *= -1;
                _vy       = 0f;
                _grounded = false;
                Debug.Log("[Player] Gravity flipped to dir=" + _dir);
            }
        }
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;

        _vy += _dir * gravityStrength * Time.fixedDeltaTime;
        _vy  = Mathf.Clamp(_vy, -maxSpeed, maxSpeed);

        var p = transform.position;
        p.y += _vy * Time.fixedDeltaTime;

        if (p.y <= -boundaryY)
        {
            p.y      = -boundaryY;
            _vy      = 0f;
            _grounded = (_dir == -1);
        }
        else if (p.y >= boundaryY)
        {
            p.y      = boundaryY;
            _vy      = 0f;
            _grounded = (_dir == 1);
        }
        else
        {
            _grounded = false;
        }

        transform.position = p;
    }
}
