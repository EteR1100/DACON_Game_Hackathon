using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;

    private SpriteRenderer _sr;
    private float _timer;
    private int _currentIndex;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= 1f / fps)
        {
            _timer -= 1f / fps;
            _currentIndex = (_currentIndex + 1) % frames.Length;
            _sr.sprite = frames[_currentIndex];
        }
    }
}
