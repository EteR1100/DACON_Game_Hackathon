using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// GamePlayScene의 HUD를 제어하는 컨트롤러.
/// 점수/코인/피버 게이지/속도 표시 및 일시정지 오버레이를 관리합니다.
/// </summary>
public class GamePlayUIController : MonoBehaviour
{
    private UIDocument _uiDocument;

    // HUD 라벨
    private Label _lblScore;
    private Label _lblBest;
    private Label _lblCoin;
    private Label _lblFeverPct;
    private Label _lblFeverStatus;
    private Label _lblSpeed;

    // 피버 게이지 바
    private VisualElement _feverFill;

    // 버튼
    private Button _btnPause;
    private Button _btnResume;
    private Button _btnRestart;
    private Button _btnMainMenu;
    
    // 종료 확인 팝업 버튼
    private Button _btnConfirmExit;
    private Button _btnCancelExit;

    // 오버레이
    private VisualElement _pauseOverlay;
    private VisualElement _feverFlash;
    private VisualElement _confirmExitOverlay;
    private VisualElement _countdownOverlay;
    private Label         _lblCountdown;

    // ── 현재 값 (외부 게임 로직에서 프로퍼티로 업데이트) ──
    private int   _score    = 0;
    private int   _coin     = 0;
    private float _fever    = 0f;   // 0 ~ 1
    private float _speed    = 1.0f;
    private bool  _isPaused = false;

    // ───── 외부 게임 로직이 호출하는 공개 메서드 ─────

    public void SetScore(int score)
    {
        _score = score;
        if (_lblScore != null) _lblScore.text = score.ToString("N0");

        // 최고 점수 갱신
        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt("BestScore", best);
        }
        if (_lblBest != null) _lblBest.text = best.ToString("N0");
    }

    public void SetCoin(int coin)
    {
        _coin = coin;
        if (_lblCoin != null) _lblCoin.text = coin.ToString();
    }

    /// <param name="value">0.0 ~ 1.0</param>
    public void SetFever(float value)
    {
        _fever = Mathf.Clamp01(value);
        int pct = Mathf.RoundToInt(_fever * 100);

        if (_lblFeverPct != null) _lblFeverPct.text = pct + "%";
        if (_feverFill    != null) _feverFill.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));

        // 피버 발동 상태 표시
        bool feverActive = pct >= 100;
        if (_lblFeverStatus != null)
            _lblFeverStatus.text = feverActive ? "FEVER!" : "FEVER";

        if (_feverFlash != null)
            _feverFlash.style.display = feverActive ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void SetSpeed(float multiplier)
    {
        _speed = multiplier;
        if (_lblSpeed != null) _lblSpeed.text = "x" + multiplier.ToString("F1");
    }

    // ───── MonoBehaviour ─────

    void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("GamePlayUIController: UIDocument not found on " + gameObject.name);
            return;
        }

        var root = _uiDocument.rootVisualElement;

        // 라벨 바인딩
        _lblScore       = root.Q<Label>("LblScore");
        _lblBest        = root.Q<Label>("LblBest");
        _lblCoin        = root.Q<Label>("LblCoin");
        _lblFeverPct    = root.Q<Label>("LblFeverPct");
        _lblFeverStatus = root.Q<Label>("LblFeverStatus");
        _lblSpeed       = root.Q<Label>("LblSpeed");

        // 피버 바
        _feverFill = root.Q<VisualElement>("FeverFill");

        // 오버레이
        _pauseOverlay = root.Q<VisualElement>("PauseOverlay");
        _feverFlash   = root.Q<VisualElement>("FeverFlash");
        _confirmExitOverlay = root.Q<VisualElement>("ConfirmExitOverlay");
        _countdownOverlay   = root.Q<VisualElement>("CountdownOverlay");
        _lblCountdown       = root.Q<Label>("CountdownLabel");

        // 버튼
        _btnPause    = root.Q<Button>("BtnPause");
        _btnResume   = root.Q<Button>("BtnResume");
        _btnRestart  = root.Q<Button>("BtnRestart");
        _btnMainMenu = root.Q<Button>("BtnMainMenu");
        
        // 확인 팝업 버튼
        _btnConfirmExit = root.Q<Button>("BtnConfirmExit");
        _btnCancelExit  = root.Q<Button>("BtnCancelExit");

        if (_btnPause    != null) _btnPause.clicked    += TogglePause;
        if (_btnResume   != null) _btnResume.clicked   += TogglePause;
        if (_btnRestart  != null) _btnRestart.clicked  += RestartGame;
        if (_btnMainMenu != null) _btnMainMenu.clicked += ShowExitConfirmation;
        
        if (_btnConfirmExit != null) _btnConfirmExit.clicked += ConfirmGoToMainMenu;
        if (_btnCancelExit  != null) _btnCancelExit.clicked  += HideExitConfirmation;

        // 저장된 최고 점수 표시
        if (_lblBest != null)
            _lblBest.text = PlayerPrefs.GetInt("BestScore", 0).ToString("N0");

        // 초기 상태
        if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
        if (_feverFlash   != null) _feverFlash.style.display   = DisplayStyle.None;
        if (_confirmExitOverlay != null) _confirmExitOverlay.style.display = DisplayStyle.None;
        if (_countdownOverlay   != null) _countdownOverlay.style.display   = DisplayStyle.None;
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_confirmExitOverlay != null && _confirmExitOverlay.style.display == DisplayStyle.Flex)
            {
                HideExitConfirmation();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        // 종료 확인창이 떠있을 때는 일시정지 해제 불가 (우선순위)
        if (_confirmExitOverlay != null && _confirmExitOverlay.style.display == DisplayStyle.Flex) return;
        
        // 카운트다운 중에는 일시정지 토글 방지
        if (_countdownOverlay != null && _countdownOverlay.style.display == DisplayStyle.Flex) return;

        if (!_isPaused)
        {
            // Pause
            _isPaused = true;
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
            else Time.timeScale = 0f;

            if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.Flex;
        }
        else
        {
            // Resume requested - Start countdown
            if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
            StartCoroutine(StartCountdownAndResume());
        }
    }

    private System.Collections.IEnumerator StartCountdownAndResume()
    {
        if (_countdownOverlay == null || _lblCountdown == null)
        {
            // Fallback if UI is missing
            ResumeImmediately();
            yield break;
        }

        _countdownOverlay.style.display = DisplayStyle.Flex;

        string[] sequence = { "3", "2", "1", "GO!" };
        foreach (var msg in sequence)
        {
            _lblCountdown.text = msg;
            
            // Animation: Scale up and Fade out
            float elapsed = 0f;
            float duration = 0.8f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                
                // Scale from 1.0 to 1.5
                float scale = Mathf.Lerp(1.0f, 1.5f, t);
                _lblCountdown.style.scale = new StyleScale(new Vector2(scale, scale));
                
                // Fade from 1.0 to 0.0
                _lblCountdown.style.opacity = 1.0f - t;
                
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.2f);
        }

        _countdownOverlay.style.display = DisplayStyle.None;
        ResumeImmediately();
    }

    private void ResumeImmediately()
    {
        _isPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        else Time.timeScale = 1f;
        
        if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
    }

    public bool IsPointerOverPauseButton(Vector2 screenPos)
    {
        if (_uiDocument == null || _uiDocument.rootVisualElement == null) return false;
        var panel = _uiDocument.rootVisualElement.panel;
        if (panel == null) return false;

        Vector2 screenTopLeft = new Vector2(screenPos.x, Screen.height - screenPos.y);
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenTopLeft);

        if (_btnPause != null && _btnPause.style.display != DisplayStyle.None && _btnPause.worldBound.Contains(panelPos))
            return true;

        if (_pauseOverlay != null && _pauseOverlay.style.display == DisplayStyle.Flex)
            return true;
            
        if (_confirmExitOverlay != null && _confirmExitOverlay.style.display == DisplayStyle.Flex)
            return true;

        return false;
    }

    private void RestartGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
        else
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void ShowExitConfirmation()
    {
        if (_confirmExitOverlay != null)
        {
            _confirmExitOverlay.style.display = DisplayStyle.Flex;
            // 일시정지창은 잠시 숨김
            if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
        }
    }

    private void HideExitConfirmation()
    {
        if (_confirmExitOverlay != null)
        {
            _confirmExitOverlay.style.display = DisplayStyle.None;
            // 다시 일시정지창 보여줌
            if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.Flex;
        }
    }

    private void ConfirmGoToMainMenu()
    {
        // 1. GameManager를 통해 이번 판의 데이터(점수, 코인 등)를 정해진 시점에 일괄 저장
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu(); // 내부에서 SaveFinalStats() 호출 및 Overlay 표시
        }
        else
        {
            // GameManager가 없는 비정상 상황 대비 (디버깅용)
            PlayerPrefs.SetInt("LastScore", _score);
            PlayerPrefs.Save();
            ShowGameOverOverlay();
        }
    }

    [Header("Game Over Settings")]
    [SerializeField] private VisualTreeAsset gameOverUXML;
    private VisualElement _gameOverOverlay;

    // ... (rest of existing fields)

    // ───── 외부 게임 로직이 호출하는 공개 메서드 ─────

    public void ShowGameOverOverlay()
    {
        // 1. UXML 및 USS 로드
        if (gameOverUXML == null)
        {
            gameOverUXML = Resources.Load<VisualTreeAsset>("UI/GameOverUI");
        }

        if (gameOverUXML == null)
        {
            Debug.LogError("GamePlayUIController: GameOverUXML could not be loaded!");
            return;
        }

        var root = _uiDocument.rootVisualElement;

        // 2. 오버레이 생성 및 전체 화면 설정
        _gameOverOverlay = gameOverUXML.Instantiate();
        _gameOverOverlay.style.position = Position.Absolute;
        _gameOverOverlay.style.left = 0;
        _gameOverOverlay.style.top = 0;
        _gameOverOverlay.style.right = 0;
        _gameOverOverlay.style.bottom = 0;
        
        // 클릭 이벤트가 뒤로 새나가지 않도록 설정
        _gameOverOverlay.pickingMode = PickingMode.Position;
        
        root.Add(_gameOverOverlay);

        // 3. 버튼 바인딩 및 기존 UI 숨김
        if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
        if (_confirmExitOverlay != null) _confirmExitOverlay.style.display = DisplayStyle.None;
        
        // HUD 요소들 숨김 (루트 하위의 모든 요소 중 게임오버가 아닌 것들)
        foreach (var child in root.Children())
        {
            if (child != _gameOverOverlay)
                child.style.display = DisplayStyle.None;
        }

        // 버튼 바인딩
        var btnRestart = _gameOverOverlay.Q<Button>("RESTART_button");
        var btnRank = _gameOverOverlay.Q<Button>("RANK_button");
        var btnMain = _gameOverOverlay.Q<Button>("MAIN_button");

        if (btnRestart != null) btnRestart.clicked += RestartGame;
        if (btnRank != null) btnRank.clicked += () => SceneManager.LoadScene("Ranking Scene");
        if (btnMain != null) btnMain.clicked += () => {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainScene");
        };

        // 데이터 표시 및 애니메이션 시작
        SetupGameOverAnimations(_gameOverOverlay);
    }

    private void SetupGameOverAnimations(VisualElement root)
    {
        var container = root.Q<VisualElement>(className: "gameover-container");
        var scoreHud = root.Q<VisualElement>(className: "score-hud");
        var coinHud = root.Q<VisualElement>(className: "coin-hud");
        var titleArea = root.Q<VisualElement>(className: "gameover-title-area");
        var btnGroup = root.Q<VisualElement>(className: "gameover-button-group");

        var lblMainScore = root.Q<Label>("MainScoreLabel");
        var lblCurrent = root.Q<Label>("CurrentScoreLabel");
        var lblBest = root.Q<Label>("BestScoreLabel");
        var lblCoin = root.Q<Label>("CoinLabel");
        var lblPacketAcquisition = root.Q<Label>("DataPacketAcquisitionLabel");
        var areaPacketAcquisition = root.Q<VisualElement>("DataPacketAcquisitionArea");

        int lastScore = PlayerPrefs.GetInt("LastScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // Data Packets calculation
        int currentTotalPackets = PlayerPrefs.GetInt("Coins", 0);
        int packetsEarnedThisRun = PlayerPrefs.GetInt("LastRunCoins", 0);
        int previousTotalPackets = currentTotalPackets - packetsEarnedThisRun;

        // 초기 상태
        if (container != null) container.style.backgroundColor = new Color(0, 0, 0, 0);
        if (scoreHud != null) scoreHud.style.opacity = 0;
        if (coinHud != null) coinHud.style.opacity = 0;
        if (titleArea != null) titleArea.style.opacity = 0;
        if (btnGroup != null) btnGroup.style.opacity = 0;
        if (areaPacketAcquisition != null) areaPacketAcquisition.style.opacity = 0;

        StartCoroutine(AnimateGameoverSequence(container, scoreHud, coinHud, titleArea, areaPacketAcquisition, btnGroup, lastScore, bestScore, previousTotalPackets, currentTotalPackets, packetsEarnedThisRun, lblMainScore, lblCurrent, lblBest, lblCoin, lblPacketAcquisition));
    }

    private System.Collections.IEnumerator AnimateGameoverSequence(
        VisualElement container, VisualElement scoreHud, VisualElement coinHud, 
        VisualElement titleArea, VisualElement areaPacketAcquisition, VisualElement btnGroup,
        int lastScore, int bestScore, 
        int previousTotalPackets, int currentTotalPackets, int packetsEarned,
        Label lblMainScore, Label lblCurrent, Label lblBest, Label lblCoin, Label lblPacketAcquisition)
    {
        float bgDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < bgDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (container != null) 
                container.style.backgroundColor = new Color(0, 0, 0, Mathf.Lerp(0f, 0.85f, elapsed / bgDuration));
            yield return null;
        }
        if (container != null) container.style.backgroundColor = new Color(0, 0, 0, 0.85f);

        StartCoroutine(FadeAndSlideUp(titleArea, 0.8f, 0.1f));
        StartCoroutine(FadeAndSlideUp(areaPacketAcquisition, 0.8f, 0.2f));
        StartCoroutine(FadeAndSlideUp(scoreHud, 0.8f, 0.3f));
        StartCoroutine(FadeAndSlideUp(coinHud, 0.8f, 0.4f));

        yield return new WaitForSecondsRealtime(0.5f);

        if (lblMainScore != null) StartCoroutine(AnimateScore(lblMainScore, lastScore, "SCORE: "));
        if (lblCurrent != null) StartCoroutine(AnimateScore(lblCurrent, lastScore));
        if (lblBest != null)
        {
            if (lastScore >= bestScore && lastScore > 0)
                StartCoroutine(AnimateScore(lblBest, bestScore, ""));
            else
                lblBest.text = bestScore.ToString("N0");
        }

        // Data Packet Animations
        if (lblPacketAcquisition != null) StartCoroutine(AnimateScore(lblPacketAcquisition, packetsEarned, "+ "));
        if (lblCoin != null) StartCoroutine(AnimateScoreBetweenValues(lblCoin, previousTotalPackets, currentTotalPackets));

        yield return new WaitForSecondsRealtime(1.0f);

        float btnDuration = 0.5f;
        elapsed = 0f;
        while (elapsed < btnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (btnGroup != null) btnGroup.style.opacity = elapsed / btnDuration;
            yield return null;
        }
        if (btnGroup != null) btnGroup.style.opacity = 1f;
    }

    private System.Collections.IEnumerator FadeAndSlideUp(VisualElement element, float duration, float delay)
    {
        if (element == null) yield break;
        yield return new WaitForSecondsRealtime(delay);
        float elapsed = 0f;
        
        // Use style.translate instead of obsolete transform.position
        element.style.translate = new Translate(0, 50, 0);
        element.style.opacity = 0;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            element.style.opacity = t;
            float yPos = Mathf.Lerp(50, 0, easedT);
            element.style.translate = new Translate(0, yPos, 0);
            yield return null;
        }
        element.style.opacity = 1;
        element.style.translate = new Translate(0, 0, 0);
    }

    private System.Collections.IEnumerator AnimateScore(Label label, int targetValue, string prefix = "", float duration = 1.5f)
    {
        if (label == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            int currentValue = Mathf.FloorToInt(targetValue * easedProgress);
            label.text = prefix + currentValue.ToString("N0");
            yield return null;
        }
        label.text = prefix + targetValue.ToString("N0");
    }

    private System.Collections.IEnumerator AnimateScoreBetweenValues(Label label, int startValue, int endValue, float duration = 1.5f)
    {
        if (label == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            int currentValue = Mathf.FloorToInt(Mathf.Lerp(startValue, endValue, easedProgress));
            label.text = currentValue.ToString("N0");
            yield return null;
        }
        label.text = endValue.ToString("N0");
    }

    private void GoToMainMenu()
    {
        // 이제 이 메서드는 직접 호출되지 않고 ShowExitConfirmation을 통해 단계적으로 진행됩니다.
        ShowExitConfirmation();
    }
}
