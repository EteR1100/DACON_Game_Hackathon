using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// 野껊슣???袁⑷퍥 ?癒?カ????뽯선??몃빍??
/// - ??볦퍢???怨뺚뀲 ?癒?땾 筌앹빓? (??곗쒔 餓?2獄?
/// - ??볦퍢???怨뺚뀲 ??얜즲 筌앹빓?
/// - ??곗쒔 野껊슣?좑쭪? ?癒?짗 ?겸뫗??????곗쒔 筌뤴뫀諭??癒?짗 獄쏆뮆猷?
/// - ?癒?땾 疫꿸퀡而???뽰뵠????덇볼 ??볥궗 (1000?癒?뼣 1??덇볼)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== Score Settings ===")]
    [Tooltip("Base score gained per second.")]
    public float baseScorePerSecond = 10f;

    [Header("=== Speed Settings ===")]
    [Tooltip("Initial speed multiplier.")]
    public float initialSpeedMultiplier = 1.0f;
    [Tooltip("Maximum speed multiplier.")]
    public float maxSpeedMultiplier = 4.0f;
    [Tooltip("Seconds until speed reaches its maximum.")]
    public float timeToMaxSpeed = 120f;

    [Header("=== Fever Settings ===")]
    [Tooltip("Seconds required to fully charge fever.")]
    public float feverChargeTime = 30f;
    [Tooltip("Fever duration in seconds.")]
    public float feverDuration = 5f;

    // ???? ??? ?怨밴묶 ????
    private float _elapsedTime   = 0f;
    private float _score         = 0f;
    private int   _totalCoinsAtStart = 0; 
    private int   _runCoins          = 0; 
    private float _feverGauge    = 0f;   
    private bool  _isPaused      = false;
    private bool  _isGameOver    = false;
    private bool  _isFeverActive = false;
    private float _feverTimer    = 0f;

    private GamePlayUIController _ui;
    private ParticleSystem _feverFireVFX;

    // ???? ?紐? ??꾨┛???袁⑥쨮??노뼒 ????
    public float CurrentSpeedMultiplier { get; private set; }
    public bool  IsFeverActive => _isFeverActive;
    public int   CurrentScore  => Mathf.FloorToInt(_score);
    
    // ?袁⑹삺 ??뽰뵠????덇볼 (1000?癒?춳??1???怨몃뱟, 0?봔????뽰삂)
    public int CurrentLevel => Mathf.FloorToInt(_score / 500f);

    // ????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????????

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

        // ?袁⑷퍥 癰귣똻???嚥≪뮆諭?獄???苡?????얜굣???λ뜃由??
        _totalCoinsAtStart = PlayerPrefs.GetInt("Coins", 0);
        _runCoins = 0;
        
        // HUD?癒?뮉 ?袁⑷퍥 癰귣똻?????뽰삂?? ??뽯뻻
        _ui?.SetCoin(_totalCoinsAtStart);

        var fireVFXObj = GameObject.Find("FeverFireVFX");
        if (fireVFXObj != null) _feverFireVFX = fireVFXObj.GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (_isPaused || _isGameOver) return;

        _elapsedTime += Time.deltaTime;

        // ???? ??얜즲 筌앹빓? (?醫륁굨 癰귣떯而? ????
        float t = Mathf.Clamp01(_elapsedTime / timeToMaxSpeed);
        CurrentSpeedMultiplier = Mathf.Lerp(initialSpeedMultiplier, maxSpeedMultiplier, t);
        
        float actualSpeed = _isFeverActive ? CurrentSpeedMultiplier * 3.0f : CurrentSpeedMultiplier;
        GameSpeedProvider.CurrentMultiplier = actualSpeed;

        // ???? ??곗쒔 筌뤴뫀諭?筌ｌ꼶??????
        if (_isFeverActive)
        {
            _feverTimer -= Time.deltaTime;
            
            // ??곗쒔 筌왖??용뻻揶쏄쑴肉???쑬???뤿연 野껊슣?좑쭪???1.0 -> 0.0??곗쨮 餓κ쑴??
            _feverGauge = Mathf.Clamp01(_feverTimer / feverDuration);
            _ui?.SetFever(_feverGauge);

            if (_feverTimer <= 0f)
            {
                _isFeverActive = false;
                _feverGauge    = 0f;
                _ui?.SetFever(0f);
                if (_feverFireVFX != null) _feverFireVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                
                // ??곗쒔 ?ル굝利????遺욧맒 ??ｋ궢 ?袁㏓┛
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    var playerController = player.GetComponent<PlayerController>();
                    playerController?.SetGhostTrail(false);
                    playerController?.StartPostFeverInvincibility(1f);
                }
            }
        }
        else
        {
            // ???? ??곗쒔 野껊슣?좑쭪? ?癒?짗 ?겸뫗??????
            _feverGauge += Time.deltaTime / feverChargeTime;
            if (_feverGauge >= 1f)
            {
                // ??곗쒔 獄쏆뮆猷?
                _feverGauge    = 1f;
                _isFeverActive = true;
                _feverTimer    = feverDuration;
                _ui?.SetFever(1f);
                if (_feverFireVFX != null) _feverFireVFX.Play(true);

                // ??곗쒔 ??뽰삂 ???遺욧맒 ??ｋ궢 ?녹뮄由?
                var player = GameObject.Find("Player");
                if (player != null) player.GetComponent<PlayerController>()?.SetGhostTrail(true);
            }
            else
            {
                _ui?.SetFever(_feverGauge);
            }
        }

        // ???? ?癒?땾 筌앹빓? (??얜즲 ????곗쒔 癰귣?瑗?? ????
        float scoreMultiplier = _isFeverActive ? 2f : 1f;
        _score += baseScorePerSecond * CurrentSpeedMultiplier * scoreMultiplier * Time.deltaTime;

        // ???? UI 揶쏄퉮??????
        if (_ui != null)
        {
            _ui.SetScore(Mathf.FloorToInt(_score));
            _ui.SetSpeed(CurrentSpeedMultiplier);
        }
    }

    // ?????? ?⑤벀而?API ??????????????????????????????????????????????????????????????????????????????????????????????????????

    public void AddCoin(int amount = 1)
    {
        _runCoins += amount;
        
        // HUD?癒?뮉 ?袁⑷퍥 癰귣똻???+ ??苡?????얜굣????뽯뻻
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
        Time.timeScale = 0f; // ??깅뻻?類?
        _ui?.ShowGameOverOverlay();
    }

    public void GameOver()
    {
        if (_isGameOver) return;
        _isGameOver = true;
        
        SaveFinalStats();
        Debug.Log("Game Over! Score: " + Mathf.FloorToInt(_score));

        // ??而?VFX ??源???볦퍢 疫꿸퀡?롧뵳???野껊슣???살쒔 ???앮에??袁れ넎
        StartCoroutine(GameOverRoutine());
    }

    private System.Collections.IEnumerator GameOverRoutine()
    {
        // ??而??怨쀭뀱 ??疫?
        yield return new WaitForSecondsRealtime(0.8f);

        // 野껊슣???遺얇늺 筌╈돦荑?(GameOver ??獄쏄퀗瑗??곗쨮 ????
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

        // ??苡?????얜굣??????獄??袁⑷퍥 ??밴텦
        PlayerPrefs.SetInt("LastRunCoins", _runCoins);
        PlayerPrefs.SetInt("Coins", _totalCoinsAtStart + _runCoins);
        PlayerPrefs.SetInt("DataPacket", _totalCoinsAtStart + _runCoins); // ?닌됱쒔???紐낆넎??
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
        // ?袁⑹삺 ???쟿????뽰젎????볥럢 ??볦퍢(KST) ?醫롮? 獄쏆꼹??
        return DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd");
    }
}
