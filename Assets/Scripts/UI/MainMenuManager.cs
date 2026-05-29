using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private VisualElement _root;
    private Button _btnStart;
    private Button _btnShop;
    private Button _btnRank;

    void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        // 버튼 찾기
        _btnStart = _root.Q<Button>("BtnStart");
        _btnShop = _root.Q<Button>("BtnShop");
        _btnRank = _root.Q<Button>("BtnRank");

        // 이벤트 연결
        if (_btnStart != null) _btnStart.clicked += OnStartClicked;
        if (_btnShop != null) _btnShop.clicked += OnShopClicked;
        if (_btnRank != null) _btnRank.clicked += OnRankClicked;
    }

    private void OnStartClicked()
    {
        Debug.Log("Starting Game...");
        SceneManager.LoadScene("GamePlayScene");
    }

    private void OnShopClicked()
    {
        Debug.Log("Opening Shop...");
        // TODO: 상점 씬이 구현되면 연결 (예: SceneManager.LoadScene("ShopScene"))
    }

    private void OnRankClicked()
    {
        Debug.Log("Opening Ranking...");
        SceneManager.LoadScene("Ranking Scene");
    }
}
