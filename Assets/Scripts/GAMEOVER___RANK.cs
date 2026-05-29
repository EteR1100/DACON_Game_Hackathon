using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void GoToRanking()
    {
        SceneManager.LoadScene("Ranking Scene");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Ranking Scene");
    }
}