using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Legacy UGUI")]
    [SerializeField] private UnityEngine.UI.Button rankButton;
    [SerializeField] private UnityEngine.UI.Button mainButton;
    [SerializeField] private UnityEngine.UI.Button restartButton;

    [Header("UI Toolkit (Neon UI)")]
    [SerializeField] private VisualTreeAsset gameOverUXML;
    private UIDocument _uiDocument;

    private void Start()
    {
        // 1. Try UI Toolkit first (Modern Neon UI)
        _uiDocument = GetComponent<UIDocument>();
        bool isUIToolkitActive = _uiDocument != null;

        if (isUIToolkitActive)
        {
            SetupUIToolkit();
        }

        // 2. Setup or Disable Legacy UGUI
        SetupLegacyUGUI(disableIfToolkitActive: isUIToolkitActive);
    }

    private void SetupUIToolkit()
    {
        var root = _uiDocument.rootVisualElement;
        if (root == null) return;

        // 1. Get references to all groups
        var container = root.Q<VisualElement>(className: "gameover-container");
        var scoreHud = root.Q<VisualElement>(className: "score-hud");
        var coinHud = root.Q<VisualElement>(className: "coin-hud");
        var titleArea = root.Q<VisualElement>(className: "gameover-title-area");
        var btnGroup = root.Q<VisualElement>(className: "gameover-button-group");

        // 2. Button references and setup
        var btnRestart = root.Q<UnityEngine.UIElements.Button>("RESTART_button");
        var btnRank = root.Q<UnityEngine.UIElements.Button>("RANK_button");
        var btnMain = root.Q<UnityEngine.UIElements.Button>("MAIN_button");

        if (btnRestart != null) btnRestart.clicked += RestartGame;
        if (btnRank != null) btnRank.clicked += GoToRanking;
        if (btnMain != null) btnMain.clicked += GoToMain;

        // 3. Score display setup
        var lblMainScore = root.Q<UnityEngine.UIElements.Label>("MainScoreLabel");
        var lblCurrent = root.Q<UnityEngine.UIElements.Label>("CurrentScoreLabel");
        var lblBest = root.Q<UnityEngine.UIElements.Label>("BestScoreLabel");
        var lblCoin = root.Q<UnityEngine.UIElements.Label>("CoinLabel");

        int lastScore = PlayerPrefs.GetInt("LastScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int coins = PlayerPrefs.GetInt("Coins", 0);

        // 4. Initial State Setup (Invisible)
        if (container != null) container.style.backgroundColor = new Color(0, 0, 0, 0);
        if (scoreHud != null) scoreHud.style.opacity = 0;
        if (coinHud != null) coinHud.style.opacity = 0;
        if (titleArea != null) titleArea.style.opacity = 0;
        if (btnGroup != null) btnGroup.style.opacity = 0;

        // 5. Start Sequenced Animations
        StartCoroutine(AnimateGameoverSequence(container, scoreHud, coinHud, titleArea, btnGroup, lastScore, bestScore, coins, lblMainScore, lblCurrent, lblBest, lblCoin));

        Debug.Log($"SceneLoader: UI Toolkit Sequenced Animation started. Score: {lastScore}");
    }

    private IEnumerator AnimateGameoverSequence(
        VisualElement container, 
        VisualElement scoreHud, 
        VisualElement coinHud, 
        VisualElement titleArea, 
        VisualElement btnGroup,
        int lastScore, int bestScore, int coins,
        Label lblMainScore, Label lblCurrent, Label lblBest, Label lblCoin)
    {
        // Phase 1: Background Fade-In (Black Semi-transparent)
        float bgDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < bgDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / bgDuration;
            if (container != null) 
                container.style.backgroundColor = new Color(0, 0, 0, Mathf.Lerp(0f, 0.85f, t));
            yield return null;
        }
        if (container != null) container.style.backgroundColor = new Color(0, 0, 0, 0.85f);

        // Phase 2: Text & Scores Slide-Up
        StartCoroutine(FadeAndSlideUp(titleArea, 0.8f, 0.1f));
        StartCoroutine(FadeAndSlideUp(scoreHud, 0.8f, 0.3f));
        StartCoroutine(FadeAndSlideUp(coinHud, 0.8f, 0.4f));

        yield return new WaitForSecondsRealtime(0.5f);

        // Phase 3: Score Count-up Animations
        if (lblMainScore != null) StartCoroutine(AnimateScore(lblMainScore, lastScore, "SCORE: "));
        if (lblCurrent != null) StartCoroutine(AnimateScore(lblCurrent, lastScore));
        if (lblBest != null)
        {
            if (lastScore >= bestScore && lastScore > 0)
                StartCoroutine(AnimateScore(lblBest, bestScore, "BEST: "));
            else
                lblBest.text = "BEST: " + bestScore.ToString("N0");
        }
        if (lblCoin != null) StartCoroutine(AnimateScore(lblCoin, coins, "", 1.0f));

        yield return new WaitForSecondsRealtime(1.0f);

        // Phase 4: Buttons Fade-In
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

    private IEnumerator FadeAndSlideUp(VisualElement element, float duration, float delay)
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
            float easedT = 1f - Mathf.Pow(1f - t, 3f); // Ease Out Cubic

            element.style.opacity = t;
            float yPos = Mathf.Lerp(50, 0, easedT);
            element.style.translate = new Translate(0, yPos, 0);
            yield return null;
        }
        element.style.opacity = 1;
        element.style.translate = new Translate(0, 0, 0);
    }

    private IEnumerator AnimateScore(UnityEngine.UIElements.Label label, int targetValue, string prefix = "", float duration = 1.5f)
    {
        if (label == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // Ease out cubic
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            int currentValue = Mathf.FloorToInt(targetValue * easedProgress);
            label.text = prefix + currentValue.ToString("N0");
            yield return null;
        }
        label.text = prefix + targetValue.ToString("N0");
    }

    private void SetupLegacyUGUI(bool disableIfToolkitActive)
    {
        if (rankButton == null)
        {
            GameObject go = GameObject.Find("RANK_button");
            if (go != null) rankButton = go.GetComponent<UnityEngine.UI.Button>();
        }

        if (mainButton == null)
        {
            GameObject go = GameObject.Find("MAIN_button") ?? GameObject.Find("MAIN_box") ?? GameObject.Find("MAIN");
            if (go != null) mainButton = go.GetComponent<UnityEngine.UI.Button>();
        }

        if (restartButton == null)
        {
            GameObject go = GameObject.Find("RESTART_button");
            if (go != null) restartButton = go.GetComponent<UnityEngine.UI.Button>();
        }

        // If UI Toolkit is active, hide the entire legacy UI hierarchy
        if (disableIfToolkitActive)
        {
            Canvas legacyCanvas = null;
            if (rankButton != null) legacyCanvas = rankButton.GetComponentInParent<Canvas>();
            else if (mainButton != null) legacyCanvas = mainButton.GetComponentInParent<Canvas>();
            else if (restartButton != null) legacyCanvas = restartButton.GetComponentInParent<Canvas>();
            else
            {
                var canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    if (c.name.Contains("UI") || c.name.Contains("Canvas"))
                    {
                        legacyCanvas = c;
                        break;
                    }
                }
            }

            // UIDocument가 같은 Canvas에 있으므로 Canvas 전체를 끄면 안 됨
        // 레거시 버튼들만 개별적으로 숨김
        if (rankButton != null) rankButton.gameObject.SetActive(false);
        if (mainButton != null) mainButton.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);
        
        Debug.Log("SceneLoader: Legacy UGUI buttons hidden (Canvas preserved for UIDocument).");
        return;
        }

        // Add listeners
        if (rankButton != null)
        {
            rankButton.onClick.RemoveAllListeners();
            rankButton.onClick.AddListener(GoToRanking);
        }

        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.onClick.AddListener(GoToMain);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    public void GoToRanking()
    {
        Debug.Log("Loading Ranking Scene...");
        SceneManager.LoadScene("Ranking Scene");
    }

    public void GoToMain()
    {
        Debug.Log("Loading Main Scene...");
        SceneManager.LoadScene("MainScene");
    }

    public void RestartGame()
    {
        Debug.Log("Restarting Game...");
        SceneManager.LoadScene("GamePlayScene");
    }
}
