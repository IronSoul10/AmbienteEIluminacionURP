using UnityEngine;
using System.Collections.Generic;

public class MoveRGB : MonoBehaviour
{
    [Header("Configuraci�n de Objetos")]
    [SerializeField] private List<Renderer> targetRenderers = new List<Renderer>(); // Lista de todos los objetos que tendr�n el efecto

    [Header("Configuraci�n del Efecto")]
    [SerializeField] private float emissionIntensity = 1f; // Intensidad del brillo/emisi�n del color
    [SerializeField] private float duration = 5f; // Duraci�n en segundos para completar un ciclo completo de colores

    // Variables privadas
    private List<Material> materials = new List<Material>(); // Lista de materiales correspondientes a cada renderer
    private float timer = 0f; // Contador de tiempo para el ciclo de colores

    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor en el centro de la pantalla
        // Inicializar materiales para cada renderer en la lista
        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer != null)
            {
                Material mat = renderer.material; // Obtiene el material del objeto
                mat.EnableKeyword("_EMISSION"); // Activa la emisi�n de luz en el material (necesario para que brille)
                materials.Add(mat); // A�ade el material a la lista
            }
        }
    }

    void Update()
    {
        // Incrementa el timer con el tiempo transcurrido desde el �ltimo frame
        timer += Time.deltaTime;

        // Normaliza el tiempo a un rango de 0 a 1 basado en la duraci�n configurada
        // Esto crea un ciclo que se repite cada 'duration' segundos
        float t = Mathf.Clamp01(timer / duration);

        // Convierte el tiempo normalizado a un valor de matiz (hue) para el espectro de colores
        // t va de 0 a 1, que representa de 0� a 360� en el c�rculo de colores HSV
        float hue = t;

        // Convierte el color HSV a RGB
        // hue: matiz (0-1 = 0�-360�), saturation: saturaci�n (1 = color puro), value: brillo (1 = m�ximo brillo)
        Color color = Color.HSVToRGB(hue, 1f, 1f);

        // Aplica el color con la intensidad configurada a todos los materiales
        foreach (Material mat in materials)
        {
            if (mat != null)
            {
                // Multiplica el color por la intensidad para controlar qu� tan brillante se ve
                mat.SetColor("_EmissionColor", color * emissionIntensity);
            }
        }

        // Reinicia el timer cuando completa un ciclo completo
        if (timer >= duration)
        {
            timer = 0f; // Reinicia para crear un loop infinito del efecto
        }
    }

    // M�todo p�blico para a�adir objetos din�micamente durante el juego
    public void AddRenderer(Renderer newRenderer)
    {
        if (newRenderer != null && !targetRenderers.Contains(newRenderer))
        {
            targetRenderers.Add(newRenderer);
            Material mat = newRenderer.material;
            mat.EnableKeyword("_EMISSION");
            materials.Add(mat);
        }
    }

    // M�todo p�blico para remover objetos de la lista
    public void RemoveRenderer(Renderer rendererToRemove)
    {
        int index = targetRenderers.IndexOf(rendererToRemove);
        if (index >= 0)
        {
            targetRenderers.RemoveAt(index);
            materials.RemoveAt(index);
        }
    }
}