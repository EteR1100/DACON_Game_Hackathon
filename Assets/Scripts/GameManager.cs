using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 게임 전체 흐름을 제어합니다.
/// - 시간에 따른 점수 증가 (피버 중 2배)
/// - 시간에 따른 속도 증가
/// - 피버 게이지 자동 충전 → 피버 모드 자동 발동
/// - 일시정지 / 재시작
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== 점수 설정 ===")]
    [Tooltip("초당 기본 점수 증가량")]
    public float baseScorePerSecond = 10f;

    [Header("=== 속도 설정 ===")]
    [Tooltip("시작 속도 배율")]
    public float initialSpeedMultiplier = 1.0f;
    [Tooltip("최대 속도 배율")]
    public float maxSpeedMultiplier = 4.0f;
    [Tooltip("속도가 최대에 도달하는 데 걸리는 시간(초)")]
    public float timeToMaxSpeed = 120f;

    [Header("=== 피버 설정 ===")]
    [Tooltip("피버 게이지가 100%까지 차는 시간(초)")]
    public float feverChargeTime = 30f;
    [Tooltip("피버 모드 지속 시간(초)")]
    public float feverDuration = 5f;

    // ── 내부 상태 ──
    private float _elapsedTime   = 0f;
    private float _score         = 0f;
    private int   _totalCoinsAtStart = 0; // 시작 시점의 전체 보유량
    private int   _runCoins          = 0; // 이번 판에서 얻은 양
    private float _feverGauge    = 0f;   // 0~1
    private bool  _isPaused      = false;
    private bool  _isGameOver    = false;
    private bool  _isFeverActive = false;
    private float _feverTimer    = 0f;

    private GamePlayUIController _ui;
    private ParticleSystem _feverFireVFX;

    // ── 외부 읽기용 프로퍼티 ──
    public float CurrentSpeedMultiplier { get; private set; }
    public bool  IsFeverActive => _isFeverActive;
    public int   CurrentScore  => Mathf.FloorToInt(_score);

    // ────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _ui = FindFirstObjectByType<GamePlayUIController>();
        CurrentSpeedMultiplier = initialSpeedMultiplier;
        GameSpeedProvider.CurrentMultiplier = CurrentSpeedMultiplier;
        Time.timeScale = 1f;

        // 전체 보유량 로드 및 이번 판 획득량 초기화
        _totalCoinsAtStart = PlayerPrefs.GetInt("Coins", 0);
        _runCoins = 0;
        
        // HUD에는 전체 보유량(시작점) 표시
        _ui?.SetCoin(_totalCoinsAtStart);

        var fireVFXObj = GameObject.Find("FeverFireVFX");
        if (fireVFXObj != null) _feverFireVFX = fireVFXObj.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (_isPaused || _isGameOver) return;

        _elapsedTime += Time.deltaTime;

        // ── 속도 증가 (선형 보간) ──
        float t = Mathf.Clamp01(_elapsedTime / timeToMaxSpeed);
        CurrentSpeedMultiplier = Mathf.Lerp(initialSpeedMultiplier, maxSpeedMultiplier, t);
        
        float actualSpeed = _isFeverActive ? CurrentSpeedMultiplier * 3.0f : CurrentSpeedMultiplier;
        GameSpeedProvider.CurrentMultiplier = actualSpeed;

        // ── 피버 모드 처리 ──
        if (_isFeverActive)
        {
            _feverTimer -= Time.deltaTime;
            
            // 피버 지속시간에 비례하여 게이지를 1.0 -> 0.0으로 줄임
            _feverGauge = Mathf.Clamp01(_feverTimer / feverDuration);
            _ui?.SetFever(_feverGauge);

            if (_feverTimer <= 0f)
            {
                _isFeverActive = false;
                _feverGauge    = 0f;
                _ui?.SetFever(0f);
                if (_feverFireVFX != null) _feverFireVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                
                // 피버 종료 시 잔상 효과 끄기
                var player = GameObject.Find("Player");
                if (player != null) player.GetComponent<PlayerController>()?.SetGhostTrail(false);
            }
        }
        else
        {
            // ── 피버 게이지 자동 충전 ──
            _feverGauge += Time.deltaTime / feverChargeTime;
            if (_feverGauge >= 1f)
            {
                // 피버 발동!
                _feverGauge    = 1f;
                _isFeverActive = true;
                _feverTimer    = feverDuration;
                _ui?.SetFever(1f);
                if (_feverFireVFX != null) _feverFireVFX.Play(true);

                // 피버 시작 시 잔상 효과 켜기
                var player = GameObject.Find("Player");
                if (player != null) player.GetComponent<PlayerController>()?.SetGhostTrail(true);
            }
            else
            {
                _ui?.SetFever(_feverGauge);
            }
        }

        // ── 점수 증가 (속도 × 피버 보너스) ──
        float scoreMultiplier = _isFeverActive ? 2f : 1f;
        _score += baseScorePerSecond * CurrentSpeedMultiplier * scoreMultiplier * Time.deltaTime;

        // ── UI 갱신 ──
        if (_ui != null)
        {
            _ui.SetScore(Mathf.FloorToInt(_score));
            _ui.SetSpeed(CurrentSpeedMultiplier);
        }
    }

    // ─── 공개 API ───────────────────────────────────────────────────

    public void AddCoin(int amount = 1)
    {
        _runCoins += amount;
        
        // HUD에는 전체 보유량 + 이번 판 획득량 표시
        _ui?.SetCoin(_totalCoinsAtStart + _runCoins);
    }

    public void AddFever(float amount)
    {
        if (_isFeverActive) return;
        _feverGauge = Mathf.Clamp01(_feverGauge + amount);
        _ui?.SetFever(_feverGauge);
    }

    public void SetPaused(bool pause)
    {
        _isPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        GameSpeedProvider.CurrentMultiplier = pause ? 0f : CurrentSpeedMultiplier;
    }

    public void RestartGame()
    {
        SaveFinalStats();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        SaveFinalStats();
        Time.timeScale = 0f; // 일시정지
        _ui?.ShowGameOverOverlay();
    }

    public void GameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        
        SaveFinalStats();
        Debug.Log("Game Over! Score: " + Mathf.FloorToInt(_score));

        // 폭발 VFX 재생 시간 기다린 후 게임오버 씬으로 전환
        StartCoroutine(GameOverRoutine());
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        // 폭발 연출 대기
        yield return new WaitForSecondsRealtime(0.8f);

        // 게임 화면 캡처 (GameOver 씬 배경으로 사용)
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        GameOverBackground.CapturedTexture = tex;

        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene("GameOverScene");
    }

private void SaveFinalStats()
{
    int finalScore = Mathf.FloorToInt(_score);
    PlayerPrefs.SetInt("LastScore", finalScore);
    PlayerPrefs.SetString("LastPlayDate", GetKstDateString());

    int best = PlayerPrefs.GetInt("BestScore", 0);
    if (finalScore > best)
    {
        PlayerPrefs.SetInt("BestScore", finalScore);
    }

    // 이번 판 획득량 저장 및 전체 합산
    PlayerPrefs.SetInt("LastRunCoins", _runCoins);
    PlayerPrefs.SetInt("Coins", _totalCoinsAtStart + _runCoins);
    PlayerPrefs.SetInt("DataPacket", _totalCoinsAtStart + _runCoins); // 구버전 호환용
    PlayerPrefs.Save();

    int scoreToSubmit = Mathf.Max(finalScore, PlayerPrefs.GetInt("BestScore", 0));
    if (scoreToSubmit > 0) SubmitBestScore(scoreToSubmit);
}

private void SubmitBestScore(int score)
{
    string nickname = PlayerPrefs.GetString("Nickname", "").Trim();
    if (string.IsNullOrEmpty(nickname))
    {
        nickname = "USER";
    }

    RankingService rankingService = RankingService.Instance;
    if (rankingService == null)
    {
        GameObject serviceObject = new GameObject("RankingService");
        rankingService = serviceObject.AddComponent<RankingService>();
    }

    rankingService.PostScore(
        nickname,
        score,
        onSuccess: () => Debug.Log($"Ranking score submitted: {nickname} - {score}"),
        onError: error => Debug.LogError("Ranking score submit failed: " + error)
    );
}

private string GetKstDateString()
{
    // 현재 플레이 시점의 한국 시간(KST) 날짜 반환
    return DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd");
}
}
