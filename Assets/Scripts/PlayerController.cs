using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ?뚮젅?댁뼱(怨? ?쒖뼱
/// - ?대┃/?ㅽ럹?댁뒪 ??以묐젰 諛섏쟾 (?쒓컙???우븘 ?덉쓣 ?뚮쭔)
/// - 泥쒖옣/諛붾떏???뺥솗??遺숈쓬
/// - ?대룞 諛⑺뼢???곕씪 怨듭씠 ?뚯쟾
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("=== Physics Settings ===")]
    public float gravityStrength = 28f;
    public float maxSpeed        = 15f;
    public float flipKickSpeed   = 4.5f;
    public float flipNudgeDistance = 0.03f;
    public float gravityRampTime = 0.48f;
    public float maxGravityRampMultiplier = 1.45f;
    public float postFeverInvincibleDuration = 1f;
    public float invincibleBlinkInterval = 0.1f;
    [Tooltip("Floor/ceiling boundary Y value.")]
    public float boundaryY       = 2.2f;
    [Tooltip("Snap offset when attached to the ceiling.")]
    public float ceilSnapOffset  = 0.0f;
    [Tooltip("Snap offset when attached to the floor.")]
    public float floorSnapOffset = 0.0f;

    [Header("=== Rotation Settings ===")]
    [Tooltip("Ball rotation speed in degrees per second.")]
    public float rotateSpeed = 180f;

    [Header("=== VFX Settings ===")]
    public GameObject shieldBreakPrefab;
    public GameObject ballExplosionPrefab;

    [Header("=== Audio Settings ===")]
    public AudioClip gravityFlipSound;
    private AudioSource _audioSource;

    private float _vy        = 0f;
    private float _airTime   = 0f;
    private int   _dir       = -1;   // -1 ?꾨옒, +1 ??
    private bool  _grounded  = true;
    private bool  _wasPaused = false;
    private bool  _isDead    = false;

    // ?? ?꾩씠???④낵 ?곹깭 ??
    public bool IsMagnetActive { get; private set; }
    public bool HasShield { get; private set; }
    public bool IsGiantActive { get; private set; }

    private float _magnetTimer = 0f;
    private float _giantTimer  = 0f;
    private Vector3 _normalScale;

    private GameObject _vfxShield;
    private GameObject _vfxMagnet;

    private Renderer _renderer;
    private SpriteRenderer _spriteRenderer;
    private GhostTrail _ghostTrail;
    private GamePlayUIController _gameplayUI;
    private AudioClip _shieldBreakSound;
    private AudioClip _giantShrinkSound;
    private AudioClip _deathSound;
    private float _invincibleTimer = 0f;
    private float _blinkTimer = 0f;
    private bool _blinkVisible = true;

    void Start()
    {
        // Rigidbody2D 臾쇰━ 媛꾩꽠 ?쒓굅
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.gravityScale   = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        // ?ㅻ뵒??
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D ?ъ슫???ㅼ젙 (吏??諛⑹?)
        _audioSource.priority = 0;      // ?곗꽑?쒖쐞 理쒓퀬 ?ㅼ젙
        _audioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // ?ъ슫??濡쒕뱶
        _shieldBreakSound = Resources.Load<AudioClip>("SFX/방어막_깨짐_2");
        _giantShrinkSound = Resources.Load<AudioClip>("SFX/크기축소_1");
        _deathSound       = Resources.Load<AudioClip>("SFX/죽음소리_5");

        // VFX 罹먯떛
        var childS = transform.Find("VFX_Shield");
        if (childS != null) _vfxShield = childS.gameObject;
        
        var childM = transform.Find("VFX_Magnet");
        if (childM != null) _vfxMagnet = childM.gameObject;

        // --- ?ㅽ궓 ?곸슜 ---
        _renderer = GetComponent<Renderer>();
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        
        // ?붿긽 ?④낵 而댄룷?뚰듃 異붽? 諛?珥덇린??
        _ghostTrail = GetComponent<GhostTrail>();
        if (_ghostTrail == null) _ghostTrail = gameObject.AddComponent<GhostTrail>();
        _ghostTrail.SetActive(false); 

        _gameplayUI = UnityEngine.Object.FindFirstObjectByType<GamePlayUIController>();
        
        string hexColor = PlayerPrefs.GetString("EquippedSkinColor", "#00F2FF");
        if (!ColorUtility.TryParseHtmlString(hexColor, out Color skinColor))
            skinColor = new Color(0f, 0.95f, 1f); // 湲곕낯 ?쒖븞

        string spriteName = PlayerPrefs.GetString("EquippedSkinSprite", "");
        Sprite equippedSprite = null;
        if (!string.IsNullOrEmpty(spriteName))
        {
            equippedSprite = Resources.Load<Sprite>("ball_skin/" + spriteName);
            // 異붽????ㅽ궓??寃쎌슦 ?ш린瑜?50% ?ㅼ? (湲곗〈 1.0 -> 1.5)
            transform.localScale *= 1.5f;
        }

        // SpriteRenderer ?됱긽 諛??ㅽ봽?쇱씠???곸슜
        _spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = skinColor;
            if (equippedSprite != null)
            {
                _spriteRenderer.sprite = equippedSprite;
            }
        }

        // Material ?됱긽 & 諛쒓킅 ?곸슜
        if (_renderer != null)
        {
            _renderer.material.color = skinColor;
            if (_renderer.material.HasProperty("_EmissionColor"))
            {
                _renderer.material.SetColor("_EmissionColor", skinColor * 2f);
                _renderer.material.EnableKeyword("_EMISSION");
            }
        }

        // ?꾨옒 ?쒓컙?먯꽌 ?쒖옉
        var p = transform.position;
        p.y = -boundaryY + floorSnapOffset;
        transform.position = p;

        _vy      = 0f;
        _airTime = 0f;
        _dir     = -1;
        _grounded = true;
        _normalScale = transform.localScale;
    }

    public void Die()
    {
        if (_invincibleTimer > 0f)
            return;

        if (IsGiantActive)
            return; // 嫄곕????곹깭硫?臾댁쟻?대?濡?諛⑹뼱留됰룄 ?뚮え?섏? ?딆쓬

        if (HasShield)
        {
            HasShield = false;
            if (_vfxShield != null) _vfxShield.SetActive(false);
            if (shieldBreakPrefab != null) Instantiate(shieldBreakPrefab, transform.position, Quaternion.identity);
            
            // 諛⑹뼱留?源⑥쭚 ?뚮━ ?ъ깮
            if (_audioSource != null && _shieldBreakSound != null)
                _audioSource.PlayOneShot(_shieldBreakSound);

            return; // 諛⑹뼱留됱씠 ?덉쑝硫?二쎌? ?딆쓬
        }

        if (_isDead) return; // 以묐났 ?몄텧 諛⑹?
        _isDead = true;

        // 諛곌꼍 ?ㅽ겕濡??뺤?
        GameSpeedProvider.CurrentMultiplier = 0f;

        // 怨??됱긽 ?쎄린 - SpriteRenderer?먯꽌 吏곸젒 ?쎌뼱??(媛???뺥솗??
        Color ballColor = new Color(0f, 0.95f, 1f); // 湲곕낯 ?쒖븞
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

        // ??컻 VFX ?앹꽦 諛??됱긽 ?곸슜
        if (ballExplosionPrefab != null)
        {
            var vfxInstance = Instantiate(ballExplosionPrefab, transform.position, Quaternion.identity);
            Debug.Log($"[Die] ballColor = {ballColor}");

            foreach (var ps in vfxInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                // ?뚰떚??硫덉텛怨???諛붽씔 ???ㅼ떆 ?ъ깮
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(ballColor);

                // colorOverLifetime 鍮꾪솢?깊솕 (startColor媛 ?곗꽑?섎룄濡?
                var colOverLife = ps.colorOverLifetime;
                colOverLife.enabled = false;

                ps.Play();
            }
        }

        // 怨??뚮뜑???④린湲?(??컻 ?곗텧 ?숈븞)
        if (_renderer != null) _renderer.enabled = false;
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // VFX ?④린湲?
        if (_vfxShield != null) _vfxShield.SetActive(false);
        if (_vfxMagnet != null) _vfxMagnet.SetActive(false);

        // 二쎌쓬 ?뚮━ ?ъ깮
        if (_audioSource != null && _deathSound != null)
            _audioSource.PlayOneShot(_deathSound);

        // GameManager??寃뚯엫?ㅻ쾭 ?뚮┝ ?????꾪솚
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
        transform.localScale = _normalScale * 2f; // 2諛?嫄곕???
    }

    public void SetGhostTrail(bool active)
    {
        if (_ghostTrail != null) _ghostTrail.SetActive(active);
    }

    public void StartPostFeverInvincibility(float duration = -1f)
    {
        if (_isDead) return;

        _invincibleTimer = duration > 0f ? duration : postFeverInvincibleDuration;
        _blinkTimer = 0f;
        _blinkVisible = true;
        SetCharacterVisible(true);
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

        // ?꾩씠????대㉧ 媛깆떊
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
                transform.localScale = _normalScale; // ?ш린 ?먯긽 蹂듦뎄

                // ?ш린 異뺤냼 ?뚮━ ?ъ깮
                if (_audioSource != null && _giantShrinkSound != null)
                    _audioSource.PlayOneShot(_giantShrinkSound);
            }
        }

        UpdateInvincibilityBlink();

        bool pressed = false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            pressed = true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool overUI = _gameplayUI != null && _gameplayUI.IsPointerOverPauseButton(Mouse.current.position.ReadValue());
            if (!overUI) pressed = true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            bool overUI = _gameplayUI != null && _gameplayUI.IsPointerOverPauseButton(Touchscreen.current.primaryTouch.position.ReadValue());
            if (!overUI) pressed = true;
        }

        if (pressed && _grounded)
        {
            FlipGravity();
        }

        // ?? ?뚯쟾 (?대룞 諛⑺뼢 諛??띾룄 諛곗쑉???곕씪) ??
        // _dir == -1 : ?꾨옒濡????쒓퀎諛⑺뼢(?뚯닔 z)
        // _dir == +1 : ?꾨줈  ??諛섏떆怨꾨갑???묒닔 z)
        float rotDir = (_dir == 1) ? 1f : -1f;
        // ?띾룄 諛곗쑉???곹뼢?μ쓣 1.5諛곕줈 ?믪뿬 媛?띻컧????媛뺥븯寃??쒗쁽
        float currentRotateSpeed = rotateSpeed * (GameSpeedProvider.CurrentMultiplier * 1.5f);
        transform.Rotate(0f, 0f, rotDir * currentRotateSpeed * Time.deltaTime);
    }

    private void UpdateInvincibilityBlink()
    {
        if (_invincibleTimer <= 0f) return;

        _invincibleTimer -= Time.deltaTime;
        _blinkTimer -= Time.deltaTime;

        if (_blinkTimer <= 0f)
        {
            _blinkTimer = invincibleBlinkInterval;
            _blinkVisible = !_blinkVisible;
            SetCharacterVisible(_blinkVisible);
        }

        if (_invincibleTimer <= 0f)
        {
            _invincibleTimer = 0f;
            _blinkVisible = true;
            SetCharacterVisible(true);
        }
    }

    private void SetCharacterVisible(bool visible)
    {
        if (_renderer != null) _renderer.enabled = visible;
        if (_spriteRenderer != null) _spriteRenderer.enabled = visible;
    }

    private void FlipGravity()
    {
        _dir *= -1;

        _vy = _dir * flipKickSpeed;
        _airTime = 0f;
        _grounded = false;

        var p = transform.position;
        p.y += _dir * flipNudgeDistance;
        transform.position = p;

        if (_audioSource != null && gravityFlipSound != null)
            _audioSource.PlayOneShot(gravityFlipSound);
    }

    void FixedUpdate()
    {
        if (_isDead || Time.timeScale == 0f) return;

        float speedMult = GameSpeedProvider.CurrentMultiplier;
        if (!_grounded)
        {
            _airTime += Time.fixedDeltaTime;
        }

        float ramp = Mathf.Clamp01(_airTime / gravityRampTime);
        float gravityMultiplier = Mathf.Lerp(1f, maxGravityRampMultiplier, ramp);

        _vy += _dir * (gravityStrength * gravityMultiplier * speedMult) * Time.fixedDeltaTime;
        _vy  = Mathf.Clamp(_vy, -(maxSpeed * speedMult), (maxSpeed * speedMult));

        var p = transform.position;
        p.y += _vy * Time.fixedDeltaTime;

        // ?? 諛붾떏 寃쎄퀎 ??
        float floorY = -boundaryY + floorSnapOffset;
        float ceilY  =  boundaryY + ceilSnapOffset;

        if (p.y <= floorY)
        {
            p.y       = floorY;
            _vy       = 0f;
            _airTime  = 0f;
            _grounded = (_dir == -1);
        }
        else if (p.y >= ceilY)
        {
            p.y       = ceilY;
            _vy       = 0f;
            _airTime  = 0f;
            _grounded = (_dir == 1);
        }
        else
        {
            _grounded = false;
        }

        transform.position = p;
    }
}
