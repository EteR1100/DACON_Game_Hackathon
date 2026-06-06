using UnityEngine;

public enum CollectibleType
{
    DataPacket,
    Magnet,
    Shield,
    Giant,
    Random,
    FeverBoost
}

/// <summary>
/// 날아오는 아이템 / 데이터 패킷
/// </summary>
public class Collectible : MonoBehaviour
{
    public CollectibleType type;
    public float baseSpeed = 5f;

    [Header("Floating Motion")]
    public bool enableFloating = true;
    public float floatAmplitude = 0.18f;
    public float floatFrequency = 2.2f;
    public float packetRotationSpeed = 45f;
    public float pulseAmount = 0.06f;

    private float _destroyX = -12f;
    private Transform _playerTransform;
    private PlayerController _playerController;
    private AudioSource _playerAudioSource;
    private bool _isMagnetized = false;
    private float _magnetSpeed = 15f;
    private float _baseY;
    private float _floatSeed;
    private Vector3 _baseScale;

    void Start()
    {
        _baseY = transform.position.y;
        _floatSeed = Random.Range(0f, Mathf.PI * 2f);
        _baseScale = transform.localScale;

        var player = GameObject.Find("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerController = player.GetComponent<PlayerController>();
            _playerAudioSource = player.GetComponent<AudioSource>();
        }

        // 사운드 미리 로드 및 데이터 로드 강제 (지연 최소화)
        if (_packetClip == null)
        {
            _packetClip = Resources.Load<AudioClip>("SFX/데이터_패킷_획득_1");
            if (_packetClip != null) _packetClip.LoadAudioData();
        }
        if (_shieldClip == null)
        {
            _shieldClip = Resources.Load<AudioClip>("SFX/방어막_1");
            if (_shieldClip != null) _shieldClip.LoadAudioData();
        }
        if (_giantClip == null)
        {
            // 더 개선된 버전일 가능성이 높은 _1 파일을 로드
            _giantClip = Resources.Load<AudioClip>("SFX/크기증가_1");
            if (_giantClip == null) _giantClip = Resources.Load<AudioClip>("SFX/크기증가");
            
            if (_giantClip != null) _giantClip.LoadAudioData();
        }
        if (_magnetClip == null)
        {
            _magnetClip = Resources.Load<AudioClip>("SFX/자석_1");
            if (_magnetClip != null) _magnetClip.LoadAudioData();
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        // 자석 효과 (플레이어가 자석 상태이거나 피버 상태일 때 끌려감)
        if (!(_isMagnetized) && _playerTransform != null)
        {
            bool isFever = GameManager.Instance != null && GameManager.Instance.IsFeverActive;
            bool isMagnet = _playerController != null && _playerController.IsMagnetActive;
            
            if (isFever || isMagnet)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                // 피버 상태면 무조건 끌려가고, 자석 상태면 반경 8f 이내
                if (isFever || dist < 8f) 
                {
                    _isMagnetized = true;
                }
            }
        }

        if (_isMagnetized && _playerTransform != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _magnetSpeed * Time.deltaTime);
        }
        else
        {
            float speed = baseSpeed * GameSpeedProvider.CurrentMultiplier;
            Vector3 pos = transform.position;
            pos.x -= speed * Time.deltaTime;

            if (enableFloating)
            {
                float wave = Mathf.Sin((Time.time * floatFrequency) + _floatSeed);
                pos.y = _baseY + wave * floatAmplitude;

                float pulse = 1f + (Mathf.Sin((Time.time * floatFrequency * 1.4f) + _floatSeed) * pulseAmount);
                transform.localScale = _baseScale * pulse;
                if (type == CollectibleType.DataPacket)
                {
                    transform.Rotate(0f, 0f, packetRotationSpeed * Time.deltaTime);
                }
            }

            transform.position = pos;
        }

        if (transform.position.x < _destroyX)
        {
            Destroy(gameObject);
        }
    }

    private static AudioClip _packetClip;
    private static AudioClip _shieldClip;
    private static AudioClip _giantClip;
    private static AudioClip _magnetClip;

    private void PlayPacketSound()
    {
        if (_packetClip != null && _playerAudioSource != null)
        {
            _playerAudioSource.pitch = 1.0f; // 기본 피치
            _playerAudioSource.PlayOneShot(_packetClip);
        }
    }

    private void PlayShieldSound()
    {
        if (_shieldClip != null && _playerAudioSource != null)
        {
            _playerAudioSource.pitch = 1.0f;
            _playerAudioSource.PlayOneShot(_shieldClip);
        }
    }

    private void PlayGiantSound()
    {
        if (_giantClip != null && _playerAudioSource != null)
        {
            // 피치 변경은 미세한 지연을 유발할 수 있으므로 기본값 유지
            _playerAudioSource.pitch = 1.0f; 
            _playerAudioSource.PlayOneShot(_giantClip);
        }
    }

    private void PlayMagnetSound()
    {
        if (_magnetClip != null && _playerAudioSource != null)
        {
            _playerAudioSource.pitch = 1.0f;
            _playerAudioSource.PlayOneShot(_magnetClip);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ── 1. 소리부터 즉각 재생 (지연 최소화) ──
            if (type == CollectibleType.DataPacket) PlayPacketSound();
            else if (type == CollectibleType.Shield) PlayShieldSound();
            else if (type == CollectibleType.Giant) PlayGiantSound();
            else if (type == CollectibleType.Magnet) PlayMagnetSound();
            else if (type == CollectibleType.FeverBoost) PlayPacketSound();

            // ── 2. 효과 적용 ──
            switch (type)
            {
                case CollectibleType.DataPacket:
                    if (GameManager.Instance != null) GameManager.Instance.AddCoin(1);
                    break;
                case CollectibleType.Magnet:
                    if (_playerController != null) _playerController.ActivateMagnet(5f);
                    break;
                case CollectibleType.Shield:
                    if (_playerController != null) _playerController.ActivateShield();
                    break;
                case CollectibleType.Giant:
                    if (_playerController != null) _playerController.ActivateGiant(5f);
                    break;
                case CollectibleType.FeverBoost:
                    if (GameManager.Instance != null) GameManager.Instance.AddFever(0.1f);
                    break;
                case CollectibleType.Random:
                    if (_playerController != null)
                    {
                        int r = UnityEngine.Random.Range(0, 3);
                        if (r == 0)
                        {
                            _playerController.ActivateMagnet(5f);
                            PlayMagnetSound();
                        }
                        else if (r == 1)
                        {
                            _playerController.ActivateShield();
                            PlayShieldSound();
                        }
                        else
                        {
                            _playerController.ActivateGiant(5f);
                            PlayGiantSound();
                        }
                    }
                    break;
            }

            // 아이템 획득 시 파괴
            Destroy(gameObject);
        }
    }
}
