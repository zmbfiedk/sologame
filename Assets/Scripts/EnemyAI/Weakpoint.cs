using System.Collections;
using UnityEngine;

public class Weakpoint : MonoBehaviour
{
    private Renderer wpRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        wpRenderer = GetComponent<Renderer>();
        if (wpRenderer != null)
            originalColor = wpRenderer.material.color;

        gameObject.SetActive(false);
    }

    public void Show(Color flashColor)
    {
        gameObject.SetActive(true);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(Flash(flashColor));
    }

    public void Hide()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (wpRenderer != null)
            wpRenderer.material.color = originalColor;

        gameObject.SetActive(false);
    }

    private IEnumerator Flash(Color flashColor)
    {
        while (true)
        {
            if (wpRenderer != null)
                wpRenderer.material.color = flashColor;
            yield return new WaitForSeconds(0.3f);

            if (wpRenderer != null)
                wpRenderer.material.color = originalColor;
            yield return new WaitForSeconds(0.3f);
        }
    }
}
