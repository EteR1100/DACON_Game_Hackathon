using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BarrierSpriteChanger : MonoBehaviour
{
    public Sprite darkSprite;
    public Sprite brightSprite;
    private SpriteRenderer _sr;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    void Update()
    {
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (BackgroundCrossfader.Instance != null && _sr != null)
        {
            _sr.sprite = BackgroundCrossfader.Instance.CurrentPhase == 1 ? darkSprite : brightSprite;
        }
    }
}
