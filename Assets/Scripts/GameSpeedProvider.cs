using UnityEngine;

/// <summary>
/// 게임의 현재 이동 속도 배율을 전역으로 제공하는 정적 클래스.
/// 게임 로직 스크립트에서 CurrentMultiplier 값을 갱신하면
/// ParallaxUVScroller가 자동으로 배경 스크롤 속도를 동기화합니다.
/// </summary>
public static class GameSpeedProvider
{
    /// <summary>
    /// 현재 게임 속도 배율 (기본값 1.0).
    /// 게임 로직 스크립트에서 이 값을 업데이트하세요.
    /// 예) GameSpeedProvider.CurrentMultiplier = 1.5f;
    /// </summary>
    public static float CurrentMultiplier { get; set; } = 1.0f;
}
