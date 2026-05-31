using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GlobalButtonSoundPlayer : MonoBehaviour
{
    private static GlobalButtonSoundPlayer _instance;
    private AudioSource _audioSource;
    private AudioClip _buttonClickSound;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("GlobalButtonSoundPlayer");
            _instance = go.AddComponent<GlobalButtonSoundPlayer>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        
        // Load the sound from Resources
        _buttonClickSound = Resources.Load<AudioClip>("SFX/버튼_1");
        
        if (_buttonClickSound == null)
        {
            Debug.LogWarning("GlobalButtonSoundPlayer: Could not find sound at Resources/SFX/버튼_1");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Give a frame for UI Toolkit to initialize
        Invoke(nameof(RefreshButtonHooks), 0.1f);
    }

    public void RefreshButtonHooks()
    {
        // 1. UI Toolkit (UIDocument)
        var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (var uiDoc in uiDocuments)
        {
            if (uiDoc.rootVisualElement != null)
            {
                // Unregister first to avoid duplicates
                uiDoc.rootVisualElement.UnregisterCallback<ClickEvent>(OnToolkitButtonClicked);
                uiDoc.rootVisualElement.RegisterCallback<ClickEvent>(OnToolkitButtonClicked, TrickleDown.TrickleDown);
            }
            else
            {
                // If not ready, try again shortly for this specific document
                StartCoroutine(WaitForRootAndHook(uiDoc));
            }
        }

        // 2. Legacy UI (UnityEngine.UI.Button)
        var legacyButtons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        foreach (var btn in legacyButtons)
        {
            btn.onClick.RemoveListener(PlaySound);
            btn.onClick.AddListener(PlaySound);
        }
    }

    private System.Collections.IEnumerator WaitForRootAndHook(UIDocument uiDoc)
    {
        while (uiDoc != null && uiDoc.rootVisualElement == null)
        {
            yield return null;
        }

        if (uiDoc != null && uiDoc.rootVisualElement != null)
        {
            uiDoc.rootVisualElement.UnregisterCallback<ClickEvent>(OnToolkitButtonClicked);
            uiDoc.rootVisualElement.RegisterCallback<ClickEvent>(OnToolkitButtonClicked, TrickleDown.TrickleDown);
        }
    }

    private void OnToolkitButtonClicked(ClickEvent evt)
    {
        // Check if the target or any of its ancestors is a Button
        VisualElement target = evt.target as VisualElement;
        while (target != null)
        {
            if (target is Button)
            {
                PlaySound();
                return;
            }
            target = target.parent;
        }
    }

    private void PlaySound()
    {
        if (_audioSource != null && _buttonClickSound != null)
        {
            _audioSource.PlayOneShot(_buttonClickSound);
        }
    }

    // Public method to manually play the sound if needed
    public static void Play()
    {
        if (_instance != null)
        {
            _instance.PlaySound();
        }
    }
}
