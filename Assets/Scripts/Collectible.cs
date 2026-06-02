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
    private bool _isMagnetized = false;
    private float _magnetSpeed = 15f;

    void Start()
    {
        var player = GameObject.Find("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerController = player.GetComponent<PlayerController>();
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 효과 적용
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
                        if (r == 0) _playerController.ActivateMagnet(5f);
                        else if (r == 1) _playerController.ActivateShield();
                        else _playerController.ActivateGiant(5f);
                    }
                    break;
            }

            // 아이템 획득 시 파괴
            Destroy(gameObject);
        }
    }
}
