using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DamageFlashFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = new(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float scalePunch = 0.08f;

    private Coroutine _feedbackRoutine;
    private Color _defaultColor;
    private Vector3 _defaultScale;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        _defaultColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        _defaultScale = transform.localScale;
    }

    public void Play()
    {
        if (!isActiveAndEnabled || spriteRenderer == null)
        {
            return;
        }

        if (_feedbackRoutine != null)
        {
            StopCoroutine(_feedbackRoutine);
        }

        _feedbackRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        spriteRenderer.color = flashColor;
        transform.localScale = _defaultScale * (1f + scalePunch);

        var elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = _defaultColor;
        transform.localScale = _defaultScale;
        _feedbackRoutine = null;
    }
}
