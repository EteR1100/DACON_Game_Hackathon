using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임오버 씬의 배경으로 이전 게임 화면 캡처를 표시합니다.
/// </summary>
public class GameOverBackground : MonoBehaviour
{
    /// <summary>씬 전환 전에 GameManager가 저장한 스크린샷 텍스처</summary>
    public static Texture2D CapturedTexture;

    [SerializeField] private RawImage backgroundImage;

    void Start()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<RawImage>();

        if (CapturedTexture != null && backgroundImage != null)
        {
            backgroundImage.texture = CapturedTexture;
            backgroundImage.color = Color.white;
        }
    }

    void OnDestroy()
    {
        // 씬 언로드 시 텍스처 메모리 해제
        if (CapturedTexture != null)
        {
            Destroy(CapturedTexture);
            CapturedTexture = null;
        }
    }
}
