using System.Collections.Generic;
using UnityEngine;

public class LucesParpadeantes : MonoBehaviour
{
    [Header("Configuración de la Luz")]
    [SerializeField] private float intensidad = 1f; // Intensidad de la luz
    [SerializeField] private float frecuencia = 1f; // Frecuencia de parpadeo en Hz
    [SerializeField] private float duracionParpadeo = 0.5f; // Duración del parpadeo en segundos
    public List<Light> luces = new List<Light>(); // Lista de luces a parpadear

    private void Update()
    {
        Parpadear();
    }
    private void Parpadear()
    {
        foreach (Light light in luces)
        {
            if (light != null)
            {
                // Alterna la intensidad de la luz entre 0 y la intensidad configurada
                light.intensity = Mathf.PingPong(Time.time * frecuencia, intensidad); // PinPong hace que la luz parpadea entre 0 e intensidad
            }
        }
    }
}