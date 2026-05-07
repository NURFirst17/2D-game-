using UnityEngine;

public class WindowSimpleFlicker : MonoBehaviour
{
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float alpha = 1f - Mathf.Abs(Mathf.Sin(Time.time * 1.5f)) * 0.05f;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}