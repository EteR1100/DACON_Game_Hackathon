using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 흐름을 제어합니다.
/// - 시간에 따른 점수 증가
/// - 시간에 따른 속도 증가
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

    // ── 내부 상태 ──
    private float _elapsedTime  = 0f;
    private float _score        = 0f;
    private int   _coin         = 0;
    private float _feverGauge   = 0f;   // 0~1
    private bool  _isPaused     = false;
    private bool  _isGameOver   = false;

    private GamePlayUIController _ui;

    // ── 현재 속도 배율 (외부에서 읽기 가능) ──
    public float CurrentSpeedMultiplier { get; private set; }

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

        // Load coins from PlayerPrefs
        _coin = PlayerPrefs.GetInt("Coins", 1250);
        if (_ui != null) _ui.SetCoin(_coin);
    }

    void Update()
    {
        if (_isPaused || _isGameOver) return;

        _elapsedTime += Time.deltaTime;

        // ── 속도 증가 (선형 보간) ──
        float t = Mathf.Clamp01(_elapsedTime / timeToMaxSpeed);
        CurrentSpeedMultiplier = Mathf.Lerp(initialSpeedMultiplier, maxSpeedMultiplier, t);
        GameSpeedProvider.CurrentMultiplier = CurrentSpeedMultiplier;

        // ── 점수 증가 (속도에 비례) ──
        _score += baseScorePerSecond * CurrentSpeedMultiplier * Time.deltaTime;

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
        
        // 실시간으로 저장하거나 게임 종료시에 한꺼번에 저장할 수 있습니다.
        // 여기서는 실시간으로 저장하여 비정상 종료 시에도 코인을 유지합니다.
        PlayerPrefs.SetInt("Coins", _coin);
        PlayerPrefs.Save();
    }

    public void AddFever(float amount)
    {
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
        
        SaveFinalStats();
        
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
