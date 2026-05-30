using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Button rankButton;
    [SerializeField] private Button mainButton;
    [SerializeField] private Button restartButton;

    private void Start()
    {
        // Try to automatically find buttons if not assigned in inspector
        if (rankButton == null)
        {
            GameObject go = GameObject.Find("RANK_button");
            if (go != null) rankButton = go.GetComponent<Button>();
        }

        if (mainButton == null)
        {
            GameObject go = GameObject.Find("MAIN_button");
            if (go == null) go = GameObject.Find("MAIN_box"); // Check common variations
            if (go == null) go = GameObject.Find("MAIN");
            if (go != null) mainButton = go.GetComponent<Button>();
        }

        if (restartButton == null)
        {
            GameObject go = GameObject.Find("RESTART_button");
            if (go != null) restartButton = go.GetComponent<Button>();
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
