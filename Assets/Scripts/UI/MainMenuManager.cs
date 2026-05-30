using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private VisualElement _root;
    private Button _btnStart;
    private Button _btnShop;
    private Button _btnRank;

    void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            // 다른 오브젝트에 있을 경우를 대비해 찾아보기
            uiDocument = FindFirstObjectByType<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("MainMenuManager: UIDocument를 찾을 수 없습니다! 인스펙터에서 UIDocument를 할당하거나, 오브젝트에 추가해주세요.");
            return;
        }

        _root = uiDocument.rootVisualElement;
        if (_root == null)
        {
            Debug.LogError("MainMenuManager: rootVisualElement가 null입니다.");
            return;
        }

        // 버튼 찾기
        _btnStart = _root.Q<Button>("BtnStart");
        _btnShop = _root.Q<Button>("BtnShop");
        _btnRank = _root.Q<Button>("BtnRank");

        // 이벤트 연결
        if (_btnStart != null) _btnStart.clicked += OnStartClicked;
        else Debug.LogWarning("MainMenuManager: 'BtnStart' 버튼을 찾을 수 없습니다.");

        if (_btnShop != null) _btnShop.clicked += OnShopClicked;
        else Debug.LogWarning("MainMenuManager: 'BtnShop' 버튼을 찾을 수 없습니다.");

        if (_btnRank != null) _btnRank.clicked += OnRankClicked;
        else Debug.LogWarning("MainMenuManager: 'BtnRank' 버튼을 찾을 수 없습니다.");
    }

    private void OnStartClicked()
    {
        Debug.Log("Starting Game...");
        SceneManager.LoadScene("GamePlayScene");
    }

    private void OnShopClicked()
    {
        Debug.Log("Opening Shop...");
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenShop();
        }
        else
        {
            Debug.LogError("ShopManager Instance not found!");
        }
    }

    private void OnRankClicked()
    {
        Debug.Log("Opening Ranking...");
        SceneManager.LoadScene("Ranking Scene");
    }
}
