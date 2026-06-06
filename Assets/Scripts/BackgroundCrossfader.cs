using UnityEngine;

public class BackgroundCrossfader : MonoBehaviour
{
    public Renderer phase2Renderer;
    public float fadeDuration = 2f;
    
    public static BackgroundCrossfader Instance { get; private set; }
    public int CurrentPhase => _currentPhase;

    private Material _phase2Material;
    private int _currentPhase = 1;
    private float _fadeProgress = 1f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (phase2Renderer == null) phase2Renderer = GetComponent<Renderer>();
        _phase2Material = phase2Renderer.material; // creates instance
        SetAlpha(_phase2Material, 0f);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        
        int score = GameManager.Instance.CurrentScore;
        int phaseIndex = (score / 1000) % 2; // 0 for Phase1, 1 for Phase2
        int targetPhase = phaseIndex == 0 ? 1 : 2;
        
        if (targetPhase != _currentPhase)
        {
            _currentPhase = targetPhase;
            _fadeProgress = 0f;
        }

        if (_fadeProgress < 1f)
        {
            _fadeProgress += Time.deltaTime / fadeDuration;
            if (_fadeProgress > 1f) _fadeProgress = 1f;
            
            float p2Alpha = _currentPhase == 2 ? _fadeProgress : (1f - _fadeProgress);
            SetAlpha(_phase2Material, p2Alpha);
        }
    }

    private void SetAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty("_Color"))
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
        else if (mat.HasProperty("_BaseColor"))
        {
            Color c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
        }
    }
}
