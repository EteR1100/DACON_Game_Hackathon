using UnityEngine;

public class GrazeTextFX : MonoBehaviour
{
    private TextMesh _tm;
    private float _life = 1f;
    private float _maxLife = 1f;

    void Start()
    {
        _tm = GetComponent<TextMesh>();
    }

    void Update()
    {
        // Move upward slightly
        transform.position += Vector3.up * 1.5f * Time.deltaTime;
        
        // Decrease life
        _life -= Time.deltaTime;
        if (_life <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Fade out
        if (_tm != null)
        {
            Color c = _tm.color;
            c.a = _life / _maxLife;
            _tm.color = c;
        }
    }
}
