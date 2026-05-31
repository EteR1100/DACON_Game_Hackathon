using UnityEngine;

/// <summary>
/// 카메라 종횡비가 바뀌어도 배경 Quad가 항상 화면을 꽉 채우도록
/// 자동으로 스케일을 조정합니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class BgScaler : MonoBehaviour
{
    [Tooltip("여유 배율 (1.0 = 딱 맞춤, 1.05 = 5% 여유)")]
    public float paddingMultiplier = 1.02f;

    private Camera _cam;
    private float  _lastAspect = 0f;

    void Start()
    {
        _cam = Camera.main;
        UpdateScale();
    }

    void Update()
    {
        // 종횡비 변경 감지 시에만 재계산 (성능 최적화)
        if (_cam != null && Mathf.Abs(_cam.aspect - _lastAspect) > 0.001f)
            UpdateScale();
    }

    void UpdateScale()
    {
        if (_cam == null) return;

        float h = _cam.orthographicSize * 2f;
        float w = h * _cam.aspect;

        transform.localScale = new Vector3(w * paddingMultiplier, h * paddingMultiplier, 1f);
        _lastAspect = _cam.aspect;
    }
}
