using UnityEngine;
using System.Collections;

/// <summary>
/// 피버 모드 시 캐릭터 뒤에 잔상을 생성하는 효과입니다.
/// </summary>
public class GhostTrail : MonoBehaviour
{
    [Header("설정")]
    public float ghostInterval = 0.02f; 
    public float fadeDuration = 0.25f;  // 아주 살짝 늘려 부드러움 확보
    public Color ghostColor = new Color(0f, 0.95f, 1f, 0.5f); // 네온 하늘색 (사이버 시안)
    public float driftSpeed = 5f;       // 범위를 줄이기 위해 속도 하향 (12 -> 5)
    public bool autoSyncWithFever = true; 

    private SpriteRenderer _sr;
    private Coroutine _trailCoroutine;
    private bool _isActive = false;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (autoSyncWithFever && GameManager.Instance != null)
        {
            SetActive(GameManager.Instance.IsFeverActive);
        }
    }

    public void SetActive(bool active)
    {
        if (_isActive == active) return;
        _isActive = active;

        if (_isActive)
        {
            if (_trailCoroutine != null) StopCoroutine(_trailCoroutine);
            _trailCoroutine = StartCoroutine(CreateTrail());
        }
        else
        {
            if (_trailCoroutine != null)
            {
                StopCoroutine(_trailCoroutine);
                _trailCoroutine = null;
            }
        }
    }

    private IEnumerator CreateTrail()
    {
        while (_isActive)
        {
            SpawnGhost();
            yield return new WaitForSeconds(ghostInterval);
        }
    }

    private void SpawnGhost()
    {
        if (_sr == null) return;

        // 새 게임 오브젝트 생성
        GameObject ghostObj = new GameObject("Ghost");
        ghostObj.transform.position = transform.position;
        ghostObj.transform.rotation = transform.rotation;
        ghostObj.transform.localScale = transform.lossyScale;

        // 스프라이트 렌더러 복사
        SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
        ghostSr.sprite = _sr.sprite;
        ghostSr.color = ghostColor;
        ghostSr.sortingOrder = _sr.sortingOrder - 1; // 원본보다 뒤에 표시

        // 페이드 아웃 및 왼쪽 이동 시작
        StartCoroutine(FadeAndDriftGhost(ghostSr));
    }

    private IEnumerator FadeAndDriftGhost(SpriteRenderer ghostSr)
    {
        float elapsed = 0f;
        Color startColor = ghostColor;
        Transform ghostTransform = ghostSr.transform;

        // 게임 속도에 비례하여 흩뿌려지는 속도 조절
        float actualDriftSpeed = driftSpeed;
        if (GameSpeedProvider.CurrentMultiplier > 0)
            actualDriftSpeed *= GameSpeedProvider.CurrentMultiplier * 0.6f;

        while (elapsed < fadeDuration)
        {
            if (ghostSr == null) yield break;
            
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // 1. 페이드 아웃 (곡선을 적용하여 점점 더 옅어지며 사라지는 느낌 연출)
            float alpha = Mathf.Lerp(startColor.a, 0f, t * t * t); 
            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            // 2. 왼쪽으로 이동 (캐릭터에서 멀어질수록 가속되는 효과)
            // 처음엔 느리게(0.3배) 시작하여 갈수록 빠르게(2.5배) 이동하여 근처에서 촘촘함을 유지
            float accelerationMultiplier = Mathf.Lerp(0.3f, 2.5f, t);
            ghostTransform.Translate(Vector3.left * actualDriftSpeed * accelerationMultiplier * Time.deltaTime, Space.World);

            yield return null;
        }

        if (ghostSr != null) Destroy(ghostSr.gameObject);
    }
}
