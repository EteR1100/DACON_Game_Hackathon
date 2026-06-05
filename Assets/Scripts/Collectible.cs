using UnityEngine;

public enum CollectibleType
{
    DataPacket,
    Magnet,
    Shield,
    Giant,
    Random
}

/// <summary>
/// 날아오는 아이템 / 데이터 패킷
/// </summary>
public class Collectible : MonoBehaviour
{
    public CollectibleType type;
    public float baseSpeed = 5f;
    private float _destroyX = -12f;
    private Transform _playerTransform;
    private PlayerController _playerController;
    private AudioSource _playerAudioSource;
    private bool _isMagnetized = false;
    private float _magnetSpeed = 15f;

    void Start()
    {
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
            _giantClip = Resources.Load<AudioClip>("SFX/크기증가");
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
            transform.Translate(Vector3.left * speed * Time.deltaTime);
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
            // 재생 반응 속도를 높이기 위해 피치를 살짝 올림 (더 경쾌하고 빠른 느낌)
            _playerAudioSource.pitch = 1.15f; 
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
