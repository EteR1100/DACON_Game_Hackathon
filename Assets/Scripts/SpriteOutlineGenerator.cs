using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutlineGenerator : MonoBehaviour
{
    public Color outlineColor = new Color(1f, 1f, 1f, 0.7f); // White with some transparency
    [Range(0.01f, 0.5f)]
    public float outlineThickness = 0.05f;

    private SpriteRenderer _sr;
    private SpriteRenderer[] _outlineRenderers = new SpriteRenderer[4];
    private Sprite _lastSprite;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();

        // Calculate offset based on local scale to keep outline consistent
        float tX = outlineThickness / transform.localScale.x;
        float tY = outlineThickness / transform.localScale.y;

        Vector3[] offsets = new Vector3[] {
            new Vector3(tX, 0, 0),
            new Vector3(-tX, 0, 0),
            new Vector3(0, tY, 0),
            new Vector3(0, -tY, 0)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject child = new GameObject($"Outline_{i}");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = offsets[i];
            child.transform.localScale = Vector3.one; 

            var childSr = child.AddComponent<SpriteRenderer>();
            childSr.color = outlineColor;
            childSr.sortingLayerID = _sr.sortingLayerID;
            childSr.sortingOrder = _sr.sortingOrder - 1; // Render behind
            
            _outlineRenderers[i] = childSr;
        }
    }

    void Update()
    {
        if (_sr.sprite != _lastSprite)
        {
            _lastSprite = _sr.sprite;
            for (int i = 0; i < 4; i++)
            {
                if (_outlineRenderers[i] != null)
                {
                    _outlineRenderers[i].sprite = _sr.sprite;
                }
            }
        }
    }
}
