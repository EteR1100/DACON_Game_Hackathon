using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalBGMPlayer : MonoBehaviour
{
    private static GlobalBGMPlayer _instance;
    public static GlobalBGMPlayer Instance => _instance;

    private AudioSource _audioSource;
    private AudioClip _menuBGM;
    
    // BGM이 연주되어야 하는 씬 이름들
    private string[] _menuScenes = { "MainScene", "Ranking Scene" };
    private string _gameplayScene = "GamePlayScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("GlobalBGMPlayer");
            _instance = go.AddComponent<GlobalBGMPlayer>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        
        // 초기 볼륨 설정
        _audioSource.volume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        
        // Load the sound from Resources
        _menuBGM = Resources.Load<AudioClip>("BGM/배경음악_1");
        
        if (_menuBGM == null)
        {
            Debug.LogWarning("GlobalBGMPlayer: Could not find BGM at Resources/BGM/배경음악_1");
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
        UpdateBGMState(scene.name);
    }

    private void UpdateBGMState(string sceneName)
    {
        bool isMenuScene = false;
        foreach (string menu in _menuScenes)
        {
            if (sceneName == menu)
            {
                isMenuScene = true;
                break;
            }
        }

        if (isMenuScene)
        {
            PlayMenuBGM();
        }
        else if (sceneName == _gameplayScene)
        {
            StopBGM();
        }
    }

    private void PlayMenuBGM()
    {
        if (_audioSource != null && _menuBGM != null)
        {
            if (_audioSource.clip == _menuBGM && _audioSource.isPlaying)
            {
                // 이미 같은 곡이 재생 중이면 그대로 둠 (끊김 방지)
                return;
            }
            
            _audioSource.clip = _menuBGM;
            _audioSource.Play();
        }
    }

    private void StopBGM()
    {
        if (_audioSource != null && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    public void SetVolume(float volume)
    {
        if (_audioSource != null)
        {
            _audioSource.volume = volume;
        }
    }
}
