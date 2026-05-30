using UnityEngine;

/// <summary>
/// UV Offset 방식의 무한 패럴랙스 스크롤러.
/// Renderer의 material.mainTextureOffset을 매 프레임 밀어내는 방식으로
/// 스프라이트 복사 없이 WebGL에서도 메모리 효율적으로 동작합니다.
///
/// [사용법]
/// 각 Quad(배경 쿼드, 전경 쿼드)에 이 스크립트를 하나씩 붙여 사용합니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ParallaxUVScroller : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────
    // Inspector 노출 변수
    // ────────────────────────────────────────────────────────────────

    [Header("=== 스크롤 속도 설정 ===")]
    [Tooltip("X축 스크롤 속도 (음수 = 왼쪽으로 흐름, 권장: 도시 -0.03 / 전경 -0.12)")]
    public float scrollSpeedX = -0.05f;

    [Tooltip("Y축 스크롤 속도 (보통 0으로 두세요)")]
    public float scrollSpeedY = 0f;

    [Header("=== 게임 속도 연동 ===")]
    [Tooltip("True로 설정하면 GameSpeedProvider의 속도 배율에 비례해 스크롤됩니다.")]
    public bool syncWithGameSpeed = true;

    [Tooltip("syncWithGameSpeed가 꺼져 있을 때 고정 배율 (기본 1.0)")]
    public float fixedSpeedMultiplier = 1.0f;

    [Header("=== 디버그 ===")]
    [Tooltip("현재 UV Offset 값을 화면에 표시합니다 (빌드 시 끄세요)")]
    public bool showDebugInfo = false;

    // ────────────────────────────────────────────────────────────────
    // 내부 변수
    // ────────────────────────────────────────────────────────────────

    private Material _instanceMaterial; // Renderer가 소유한 인스턴스 머티리얼
    private float    _currentOffsetX;
    private float    _currentOffsetY;

    // ────────────────────────────────────────────────────────────────
    // MonoBehaviour
    // ────────────────────────────────────────────────────────────────

    void Start()
    {
        // Renderer.material은 자동으로 인스턴스 복사본을 반환합니다.
        // (원본 에셋 머티리얼은 수정되지 않으므로 다른 오브젝트에 영향 없음)
        _instanceMaterial = GetComponent<Renderer>().material;

        // 시작 오프셋 초기화
        _currentOffsetX = _instanceMaterial.mainTextureOffset.x;
        _currentOffsetY = _instanceMaterial.mainTextureOffset.y;
    }

    void Update()
    {
        float multiplier = syncWithGameSpeed
            ? GameSpeedProvider.CurrentMultiplier   // 게임 속도 연동
            : fixedSpeedMultiplier;                 // 고정 배율

        _currentOffsetX += scrollSpeedX * multiplier * Time.deltaTime;
        _currentOffsetY += scrollSpeedY * multiplier * Time.deltaTime;

        // UV 값이 무한히 커지지 않도록 0~1 범위로 감싸줍니다 (Wrap Mode: Repeat과 함께 사용)
        _currentOffsetX = Mathf.Repeat(_currentOffsetX, 1f);
        _currentOffsetY = Mathf.Repeat(_currentOffsetY, 1f);

        _instanceMaterial.mainTextureOffset = new Vector2(_currentOffsetX, _currentOffsetY);
    }

    void OnDestroy()
    {
        // ★ WebGL 메모리 누수 방지 ★
        // Renderer.material로 생성된 인스턴스 머티리얼은 자동 해제되지 않으므로
        // 오브젝트가 파괴될 때 명시적으로 Destroy 합니다.
        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;
        GUI.Label(new Rect(10, 10, 300, 40),
            $"[{gameObject.name}] UV Offset: ({_currentOffsetX:F3}, {_currentOffsetY:F3})");
    }

    // ────────────────────────────────────────────────────────────────
    // 외부 호출 API (게임 로직 스크립트에서 사용)
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 일시정지/재개 시 호출합니다.
    /// </summary>
    public void SetPaused(bool isPaused)
    {
        fixedSpeedMultiplier = isPaused ? 0f : 1f;
    }
}
