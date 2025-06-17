using UnityEngine;

public class MoveRGB : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float emissionIntensity = 1f;
    [SerializeField] private float duration = 5f;

    private Material mat;
    private float timer = 0f;

    void Start()
    {
        mat = targetRenderer.material;
        mat.EnableKeyword("_EMISSION");
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Normaliza tiempo a rango 0-1
        float t = Mathf.Clamp01(timer / duration);

        // Recorre el espectro de colores de 0° a 360° en HSV (hue)
        float hue = t;

        // Convierte HSV a RGB (Color.HSVToRGB espera hue entre 0 y 1)
        Color color = Color.HSVToRGB(hue, 1f, 1f);

        // Ajusta la intensidad del color multiplicándolo
        mat.SetColor("_EmissionColor", color * emissionIntensity);
    }
}

