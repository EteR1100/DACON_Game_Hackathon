using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
        if (_uiDocument != null)
        {
            SetupUIToolkit();
        }

        // 2. Try to automatically find UGUI buttons if not assigned
        SetupLegacyUGUI();
    }

    private void SetupUIToolkit()
    {
        var root = _uiDocument.rootVisualElement;
        if (root == null) return;

        var btnRestart = root.Q<UnityEngine.UIElements.Button>("RESTART_button");
        var btnRank = root.Q<UnityEngine.UIElements.Button>("RANK_button");
        var btnMain = root.Q<UnityEngine.UIElements.Button>("MAIN_button");

        if (btnRestart != null) btnRestart.clicked += RestartGame;
        if (btnRank != null) btnRank.clicked += GoToRanking;
        if (btnMain != null) btnMain.clicked += GoToMain;

        // Display Scores & Coins
        var lblMainScore = root.Q<UnityEngine.UIElements.Label>("MainScoreLabel");
        var lblCurrent = root.Q<UnityEngine.UIElements.Label>("CurrentScoreLabel");
        var lblBest = root.Q<UnityEngine.UIElements.Label>("BestScoreLabel");
        var lblCoin = root.Q<UnityEngine.UIElements.Label>("CoinLabel");

        int lastScore = PlayerPrefs.GetInt("LastScore", 0);
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int coins = PlayerPrefs.GetInt("Coins", 0);

        if (lblMainScore != null) lblMainScore.text = "SCORE: " + lastScore.ToString("N0");
        if (lblCurrent != null) lblCurrent.text = lastScore.ToString("N0");
        if (lblBest != null) lblBest.text = bestScore.ToString("N0");
        if (lblCoin != null) lblCoin.text = coins.ToString("N0");

        Debug.Log($"SceneLoader: UI Toolkit (Neon) UI initialized. Score: {lastScore}, Best: {bestScore}, Coins: {coins}");
    }

    private void SetupLegacyUGUI()
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
