using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어(공) 제어
/// - 클릭/스페이스 → 중력 반전 (난간에 닿아 있을 때만)
/// - 천장/바닥에 정확히 붙음
/// - 이동 방향에 따라 공이 회전
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("=== 물리 설정 ===")]
    public float gravityStrength = 28f;
    public float maxSpeed        = 15f;
    [Tooltip("바닥/천장 경계 Y 좌표 (절대값)")]
    public float boundaryY       = 2.2f;
    [Tooltip("천장에 붙을 때 오프셋 (천장 스프라이트에 밀착)")]
    public float ceilSnapOffset  = 0.0f;
    [Tooltip("바닥에 붙을 때 오프셋")]
    public float floorSnapOffset = 0.0f;

    [Header("=== 회전 설정 ===")]
    [Tooltip("공이 회전하는 속도 (deg/s)")]
    public float rotateSpeed = 180f;

    [Header("=== VFX 설정 ===")]
    public GameObject shieldBreakPrefab;

    [Header("=== 오디오 설정 ===")]
    public AudioClip gravityFlipSound;
    private AudioSource _audioSource;

    private float _vy        = 0f;
    private int   _dir       = -1;   // -1 아래, +1 위
    private bool  _grounded  = true;
    private bool  _wasPaused = false;
    private bool  _isDead    = false;

    // ── 아이템 효과 상태 ──
    public bool IsMagnetActive { get; private set; }
    public bool HasShield { get; private set; }
    public bool IsGiantActive { get; private set; }

    private float _magnetTimer = 0f;
    private float _giantTimer  = 0f;
    private Vector3 _normalScale;

    private GameObject _vfxShield;
    private GameObject _vfxMagnet;

    private Renderer _renderer;
    private AudioClip _shieldBreakSound;
    private AudioClip _giantShrinkSound;

    void Start()
    {
        // Rigidbody2D 물리 간섭 제거
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.gravityScale   = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        // 오디오
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D 사운드 설정 (지연 방지)
        _audioSource.priority = 0;      // 우선순위 최고 설정
        _audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // 사운드 로드
        _shieldBreakSound = Resources.Load<AudioClip>("SFX/방어막_깨짐_2");
        _giantShrinkSound = Resources.Load<AudioClip>("SFX/크기축소_1");

        // VFX 캐싱
        var childS = transform.Find("VFX_Shield");
        if (childS != null) _vfxShield = childS.gameObject;
        
        var childM = transform.Find("VFX_Magnet");
        if (childM != null) _vfxMagnet = childM.gameObject;

        // --- 스킨 적용 ---
        _renderer = GetComponent<Renderer>();
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        
        if (_renderer != null)
        {
            string hexColor = PlayerPrefs.GetString("EquippedSkinColor", "#00F2FF");
            if (ColorUtility.TryParseHtmlString(hexColor, out Color skinColor))
            {
                // MaterialPropertyBlock을 사용하면 인스턴스 생성을 피할 수 있지만, 
                // 여기서는 간단하게 material.color를 수정하거나 _EmissionColor를 수정합니다.
                _renderer.material.color = skinColor;
                if (_renderer.material.HasProperty("_EmissionColor"))
                {
                    _renderer.material.SetColor("_EmissionColor", skinColor * 2f); // 살짝 발광 효과
                    _renderer.material.EnableKeyword("_EMISSION");
                }
            }
        }

        // 아래 난간에서 시작
        var p = transform.position;
        p.y = -boundaryY + floorSnapOffset;
        transform.position = p;

        _vy      = 0f;
        _dir     = -1;
        _grounded = true;
        _normalScale = transform.localScale;
    }

    public void Die()
    {
        if (IsGiantActive)
            return; // 거대화 상태면 무적이므로 방어막도 소모되지 않음

        if (HasShield)
        {
            HasShield = false;
            if (_vfxShield != null) _vfxShield.SetActive(false);
            if (shieldBreakPrefab != null) Instantiate(shieldBreakPrefab, transform.position, Quaternion.identity);
            
            // 방어막 깨짐 소리 재생
            if (_audioSource != null && _shieldBreakSound != null)
                _audioSource.PlayOneShot(_shieldBreakSound);

            return; // 방어막이 있으면 죽지 않음
        }

        _isDead = true;
        
        // (요청에 따라 임시로 떨어지지 않게 주석 처리)
        // var rb = GetComponent<Rigidbody2D>();
        // if (rb != null)
        // {
        //     rb.bodyType = RigidbodyType2D.Dynamic;
        //     rb.gravityScale = 3f;
        // }
        // GameSpeedProvider.CurrentMultiplier = 0f;
        
        this.enabled = false;
    }

    public void ActivateMagnet(float duration)
    {
        IsMagnetActive = true;
        _magnetTimer = duration;
        if (_vfxMagnet != null) _vfxMagnet.SetActive(true);
    }

    public void ActivateShield()
    {
        HasShield = true;
        if (_vfxShield != null) _vfxShield.SetActive(true);
    }

    public void ActivateGiant(float duration)
    {
        IsGiantActive = true;
        _giantTimer = duration;
        transform.localScale = _normalScale * 2f; // 2배 거대화
    }

    void Update()
    {
        if (_isDead) return;

        if (Time.timeScale == 0f)
        {
            _wasPaused = true;
            return;
        }

        if (_wasPaused)
        {
            _wasPaused = false;
            return;
        }

        // 아이템 타이머 갱신
        if (IsMagnetActive)
        {
            _magnetTimer -= Time.deltaTime;
            if (_magnetTimer <= 0f) 
            {
                IsMagnetActive = false;
                if (_vfxMagnet != null) _vfxMagnet.SetActive(false);
            }
        }
        if (IsGiantActive)
        {
            _giantTimer -= Time.deltaTime;
            if (_giantTimer <= 0f)
            {
                IsGiantActive = false;
                transform.localScale = _normalScale; // 크기 원상 복구

                // 크기 축소 소리 재생
                if (_audioSource != null && _giantShrinkSound != null)
                    _audioSource.PlayOneShot(_giantShrinkSound);
            }
        }

        bool pressed = false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            pressed = true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<GamePlayUIController>();
            bool overUI = ui != null && ui.IsPointerOverPauseButton(Mouse.current.position.ReadValue());
            if (!overUI) pressed = true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<GamePlayUIController>();
            bool overUI = ui != null && ui.IsPointerOverPauseButton(Touchscreen.current.primaryTouch.position.ReadValue());
            if (!overUI) pressed = true;
        }

        if (pressed && _grounded)
        {
            _dir     *= -1;
            _vy       = 0f;
            _grounded = false;
            if (_audioSource != null && gravityFlipSound != null)
                _audioSource.PlayOneShot(gravityFlipSound);
        }

        // ── 회전 (이동 방향에 따라) ──
        // _dir == -1 : 아래로 → 시계방향(음수 z)
        // _dir == +1 : 위로  → 반시계방향(양수 z)
        float rotDir = (_dir == 1) ? 1f : -1f;
        transform.Rotate(0f, 0f, rotDir * rotateSpeed * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (_isDead || Time.timeScale == 0f) return;

        float speedMult = GameSpeedProvider.CurrentMultiplier;
        _vy += _dir * (gravityStrength * speedMult) * Time.fixedDeltaTime;
        _vy  = Mathf.Clamp(_vy, -(maxSpeed * speedMult), (maxSpeed * speedMult));

        var p = transform.position;
        p.y += _vy * Time.fixedDeltaTime;

        // ── 바닥 경계 ──
        float floorY = -boundaryY + floorSnapOffset;
        float ceilY  =  boundaryY + ceilSnapOffset;

        if (p.y <= floorY)
        {
            p.y       = floorY;
            _vy       = 0f;
            _grounded = (_dir == -1);
        }
        else if (p.y >= ceilY)
        {
            p.y       = ceilY;
            _vy       = 0f;
            _grounded = (_dir == 1);
        }
        else
        {
            _grounded = false;
        }

        transform.position = p;
    }
}
