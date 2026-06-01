using UnityEngine;
using UnityEngine.SceneManagement;

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
    private int   _coin          = 0;
    private float _feverGauge    = 0f;   // 0~1
    private bool  _isPaused      = false;
    private bool  _isGameOver    = false;
    private bool  _isFeverActive = false;
    private float _feverTimer    = 0f;

    private GamePlayUIController _ui;

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
        _ui = FindObjectOfType<GamePlayUIController>();
        CurrentSpeedMultiplier = initialSpeedMultiplier;
        GameSpeedProvider.CurrentMultiplier = CurrentSpeedMultiplier;
        Time.timeScale = 1f;

        // 누적 데이터 패킷 불러오기
        _coin = PlayerPrefs.GetInt("DataPacket", PlayerPrefs.GetInt("Coins", 0)); // 호환성 유지
        _ui?.SetCoin(_coin);
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
            if (_feverTimer <= 0f)
            {
                _isFeverActive = false;
                _feverGauge    = 0f;
                _ui?.SetFever(0f);
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
        _coin += amount;
        _ui?.SetCoin(_coin);
        PlayerPrefs.SetInt("DataPacket", _coin);
        PlayerPrefs.SetInt("Coins", _coin);
        PlayerPrefs.Save();
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
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void GameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;
// 최고 점수 및 데이터 저장 함수 호출
    SaveFinalStats();
    
    // 게임오버 로그 출력
    Debug.Log("Game Over! Score: " + Mathf.FloorToInt(_score));

    // GameOver 씬으로 이동 (SceneLoader/GAMEOVER___RANK가 있는 씬)
    SceneManager.LoadScene("GameOverScene");
}

private void SaveFinalStats()
{
    int finalScore = Mathf.FloorToInt(_score);
    PlayerPrefs.SetInt("LastScore", finalScore);

    int best = PlayerPrefs.GetInt("BestScore", 0);
    if (finalScore > best)
    {
        PlayerPrefs.SetInt("BestScore", finalScore);
    }

    PlayerPrefs.SetInt("Coins", _coin);
    PlayerPrefs.Save();
}
    }
}
