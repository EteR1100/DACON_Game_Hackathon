using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private UIDocument _uiDocument;
    
    // UI Elements
    private VisualElement _nicknamePopup;
    private TextField _inputNickname;
    private Button _btnConfirmNickname;
    private Label _lblNickname;
    private Label _nicknameCounter;
    
    private Button _btnRank;

    void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("MainMenuController: UIDocument not found on " + gameObject.name);
            return;
        }

        var root = _uiDocument.rootVisualElement;

        // Find elements
        _nicknamePopup = root.Q<VisualElement>("NicknamePopup");
        _inputNickname = root.Q<TextField>("InputNickname");
        _btnConfirmNickname = root.Q<Button>("BtnConfirmNickname");
        _lblNickname = root.Q<Label>("LblNickname");
        _nicknameCounter = root.Q<Label>("NicknameCounter");
        _btnRank = root.Q<Button>("BtnRank");

        if (_nicknamePopup == null || _inputNickname == null || _btnConfirmNickname == null || _lblNickname == null)
        {
            Debug.LogError("MainMenuController: Could not find one or more UI elements.");
            return;
        }

        // Register callbacks
        _inputNickname.maxLength = 12;
        _btnConfirmNickname.clicked += OnConfirmNicknameClicked;
        _inputNickname.RegisterCallback<KeyDownEvent>(OnNicknameInputKeyDown);
        _inputNickname.RegisterValueChangedCallback(evt => UpdateNicknameCounter(evt.newValue));
        UpdateNicknameCounter(_inputNickname.value);
        
        if (_btnRank != null)
        {
            _btnRank.clicked += OnRankButtonClicked;
        }

        // Check if nickname exists
        string savedNickname = PlayerPrefs.GetString("Nickname", "");
        if (string.IsNullOrEmpty(savedNickname))
        {
            // Show popup
            _nicknamePopup.style.display = DisplayStyle.Flex;
            _lblNickname.text = "UNKNOWN";
        }
        else
        {
            // Hide popup, update label
            _nicknamePopup.style.display = DisplayStyle.None;
            _lblNickname.text = savedNickname;
        }
    }

    private void OnConfirmNicknameClicked()
    {
        string newNickname = _inputNickname.value.Trim();
        if (!string.IsNullOrEmpty(newNickname))
        {
            // Save and apply
            PlayerPrefs.SetString("Nickname", newNickname);
            PlayerPrefs.Save();
            
            _lblNickname.text = newNickname;
            _nicknamePopup.style.display = DisplayStyle.None;
            
            Debug.Log("Nickname saved: " + newNickname);
        }
        else
        {
            Debug.LogWarning("Nickname cannot be empty.");
        }
    }

    private void OnNicknameInputKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            OnConfirmNicknameClicked();
        }
    }

    private void UpdateNicknameCounter(string value)
    {
        if (_nicknameCounter == null)
        {
            return;
        }

        int length = string.IsNullOrEmpty(value) ? 0 : value.Length;
        _nicknameCounter.text = $"2-12자 / {length}/12";
    }

    private void OnRankButtonClicked()
    {
        SceneManager.LoadScene("Ranking Scene");
    }
}
