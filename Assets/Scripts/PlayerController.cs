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
    public GameObject ballExplosionPrefab;

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
    private AudioClip _deathSound;

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
        _deathSound       = Resources.Load<AudioClip>("SFX/죽음소리_5");

        // VFX 캐싱
        var childS = transform.Find("VFX_Shield");
        if (childS != null) _vfxShield = childS.gameObject;
        
        var childM = transform.Find("VFX_Magnet");
        if (childM != null) _vfxMagnet = childM.gameObject;

        // --- 스킨 적용 ---
        _renderer = GetComponent<Renderer>();
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        
        string hexColor = PlayerPrefs.GetString("EquippedSkinColor", "#00F2FF");
        if (!ColorUtility.TryParseHtmlString(hexColor, out Color skinColor))
            skinColor = new Color(0f, 0.95f, 1f); // 기본 시안

        string spriteName = PlayerPrefs.GetString("EquippedSkinSprite", "");
        Sprite equippedSprite = null;
        if (!string.IsNullOrEmpty(spriteName))
        {
            equippedSprite = Resources.Load<Sprite>("ball_skin/" + spriteName);
            // 추가된 스킨의 경우 크기를 50% 키움 (기존 1.0 -> 1.5)
            transform.localScale *= 1.5f;
        }

        // SpriteRenderer 색상 및 스프라이트 적용
        var spriteRend = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (spriteRend != null)
        {
            spriteRend.color = skinColor;
            if (equippedSprite != null)
            {
                spriteRend.sprite = equippedSprite;
            }
        }

        // Material 색상 & 발광 적용
        if (_renderer != null)
        {
            _renderer.material.color = skinColor;
            if (_renderer.material.HasProperty("_EmissionColor"))
            {
                _renderer.material.SetColor("_EmissionColor", skinColor * 2f);
                _renderer.material.EnableKeyword("_EMISSION");
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

        if (_isDead) return; // 중복 호출 방지
        _isDead = true;

        // 배경 스크롤 정지
        GameSpeedProvider.CurrentMultiplier = 0f;

        // 공 색상 읽기 - SpriteRenderer에서 직접 읽어옴 (가장 정확함)
        Color ballColor = new Color(0f, 0.95f, 1f); // 기본 시안
        var sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.color != Color.white && sr.color.a > 0f)
        {
            ballColor = sr.color;
            ballColor.a = 1f;
        }
        else
        {
            // fallback: PlayerPrefs
            string hexColor = PlayerPrefs.GetString("EquippedSkinColor", "#00F2FF");
            ColorUtility.TryParseHtmlString(hexColor, out ballColor);
        }

        // 폭발 VFX 생성 및 색상 적용
        if (ballExplosionPrefab != null)
        {
            var vfxInstance = Instantiate(ballExplosionPrefab, transform.position, Quaternion.identity);
            Debug.Log($"[Die] ballColor = {ballColor}");

            foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                // 파티클 멈추고 색 바꾼 뒤 다시 재생
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(ballColor);

                // colorOverLifetime 비활성화 (startColor가 우선되도록)
                var colOverLife = ps.colorOverLifetime;
                colOverLife.enabled = false;

                ps.Play();
            }
        }

        // 공 렌더러 숨기기 (폭발 연출 동안)
        if (_renderer != null) _renderer.enabled = false;
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // VFX 숨기기
        if (_vfxShield != null) _vfxShield.SetActive(false);
        if (_vfxMagnet != null) _vfxMagnet.SetActive(false);

        // 죽음 소리 재생
        if (_audioSource != null && _deathSound != null)
            _audioSource.PlayOneShot(_deathSound);

        // GameManager에 게임오버 알림 후 씬 전환
        if (GameManager.Instance != null) GameManager.Instance.GameOver();

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
