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
    private Label _lblBestScore;
    private Label _nicknameCounter;
    private Label _lblDataPackets;
    
    private Button _btnStart;
    private Button _btnShop;
    private Button _btnRank;
    private Slider _sliderBGM;
    private Slider _sliderSFX;

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
        _lblBestScore = root.Q<Label>("LblBestScore");
        _nicknameCounter = root.Q<Label>("NicknameCounter");
        _lblDataPackets = root.Q<Label>("LblDataPackets");
        
        _btnStart = root.Q<Button>("BtnStart");
        _btnShop = root.Q<Button>("BtnShop");
        _btnRank = root.Q<Button>("BtnRank");
        _sliderBGM = root.Q<Slider>("SliderBGM");
        _sliderSFX = root.Q<Slider>("SliderSFX");

        if (_nicknamePopup == null || _inputNickname == null || _btnConfirmNickname == null || _lblNickname == null)
        {
            Debug.LogError("MainMenuController: Could not find one or more essential UI elements.");
            return;
        }
        
        if (_sliderBGM == null) Debug.LogWarning("MainMenuController: SliderBGM not found in UXML.");
        if (_sliderSFX == null) Debug.LogWarning("MainMenuController: SliderSFX not found in UXML.");

        // Register callbacks
        _inputNickname.maxLength = 12;
        _btnConfirmNickname.clicked += OnConfirmNicknameClicked;
        _inputNickname.RegisterCallback<KeyDownEvent>(OnNicknameInputKeyDown);
        _inputNickname.RegisterValueChangedCallback(evt => UpdateNicknameCounter(evt.newValue));
        UpdateNicknameCounter(_inputNickname.value);
        
        if (_btnStart != null)
        {
            _btnStart.clicked += OnStartButtonClicked;
        }

        if (_btnShop != null)
        {
            _btnShop.clicked += OnShopButtonClicked;
        }
        
        if (_btnRank != null)
        {
            _btnRank.clicked += OnRankButtonClicked;
        }

        // BGM Volume Setup
        if (_sliderBGM != null)
        {
            float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
            _sliderBGM.value = savedBGM;
            
            // Sync initial volume
            if (GlobalBGMPlayer.Instance != null) 
                GlobalBGMPlayer.Instance.SetVolume(savedBGM);

            _sliderBGM.RegisterValueChangedCallback(evt => OnBGMVolumeChanged(evt.newValue));
        }

        // SFX Volume Setup
        if (_sliderSFX != null)
        {
            float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
            _sliderSFX.value = savedSFX;
            _sliderSFX.RegisterValueChangedCallback(evt => OnSFXVolumeChanged(evt.newValue));
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

        // Display Best Score
        if (_lblBestScore != null)
        {
            int best = PlayerPrefs.GetInt("BestScore", 0);
            _lblBestScore.text = best.ToString("N0");
        }

        UpdateDataPacketsDisplay();

        // Grant 10,000 coins only once
        if (PlayerPrefs.GetInt("InitialCoinsGranted", 0) == 0)
        {
            PlayerPrefs.SetInt("Coins", 10000);
            PlayerPrefs.SetInt("InitialCoinsGranted", 1);
            PlayerPrefs.Save();
            UpdateDataPacketsDisplay();
        }

        // Register Shop Close callback
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopClosed += UpdateDataPacketsDisplay;
        }
    }

    private void UpdateDataPacketsDisplay()
    {
        if (_lblDataPackets != null)
        {
            int coins = PlayerPrefs.GetInt("Coins", 10000);
            _lblDataPackets.text = coins.ToString("N0");
        }
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnShopClosed -= UpdateDataPacketsDisplay;
        }
    }

    private void OnBGMVolumeChanged(float newValue)
    {
        if (GlobalBGMPlayer.Instance != null)
        {
            GlobalBGMPlayer.Instance.SetVolume(newValue);
            Debug.Log($"[MainMenu] BGM Volume updated to: {newValue}");
        }
        else
        {
            Debug.LogWarning("[MainMenu] GlobalBGMPlayer.Instance is null!");
        }
        PlayerPrefs.SetFloat("BGMVolume", newValue);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(float newValue)
    {
        PlayerPrefs.SetFloat("SFXVolume", newValue);
        PlayerPrefs.Save();
        Debug.Log($"[MainMenu] SFX Volume updated to: {newValue}");
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

    private void OnStartButtonClicked()
    {
        SceneManager.LoadScene("GamePlayScene");
    }

    private void OnShopButtonClicked()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenShop();
        }
        else
        {
            Debug.LogWarning("ShopManager instance not found in scene.");
        }
    }

    private void OnRankButtonClicked()
    {
        SceneManager.LoadScene("Ranking Scene");
    }
}
