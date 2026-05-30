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
    private Button _btnMainMenu;

    // 오버레이
    private VisualElement _pauseOverlay;
    private VisualElement _feverFlash;

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

        // 버튼
        _btnPause    = root.Q<Button>("BtnPause");
        _btnResume   = root.Q<Button>("BtnResume");
        _btnMainMenu = root.Q<Button>("BtnMainMenu");

        if (_btnPause    != null) _btnPause.clicked    += TogglePause;
        if (_btnResume   != null) _btnResume.clicked   += TogglePause;
        if (_btnMainMenu != null) _btnMainMenu.clicked += GoToMainMenu;

        // 저장된 최고 점수 표시
        if (_lblBest != null)
            _lblBest.text = PlayerPrefs.GetInt("BestScore", 0).ToString("N0");

        // 초기 상태
        if (_pauseOverlay != null) _pauseOverlay.style.display = DisplayStyle.None;
        if (_feverFlash   != null) _feverFlash.style.display   = DisplayStyle.None;
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;

        if (_pauseOverlay != null)
            _pauseOverlay.style.display = _isPaused ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }
}
