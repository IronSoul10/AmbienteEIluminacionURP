using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform; // Referencia al transform de la cámara

    [Header("Configuración del Shake")]
    [SerializeField] private float shakeDuration = 0.5f; // Duración de cada shake en segundos
    [SerializeField] private float shakeIntensity = 0.1f; // Intensidad del shake
    [SerializeField] private float shakeFrequency = 20f; // Frecuencia del shake (vibraciones por segundo)

    [Header("Configuración del Timer")]
    [SerializeField] private float shakeInterval = 4f; // Intervalo entre shakes (4 segundos)
    [SerializeField] private bool autoStart = true; // Si debe empezar automáticamente

    [Header("Configuración Avanzada")]
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // Curva de intensidad del shake
    [SerializeField] private bool useRandomDirection = true; // Si usar direcciones aleatorias
    [SerializeField] private Vector3 preferredDirection = Vector3.one; // Dirección preferida si no es aleatoria

    // Variables privadas
    private Vector3 originalPosition; // Posición original de la cámara
    private Coroutine shakeRoutine; // Referencia a la corrutina del shake
    private Coroutine timerRoutine; // Referencia a la corrutina del timer
    private bool isShaking = false; // Estado actual del shake

    void Start()
    {
        // Si no se asigna cámara, usa el transform de este objeto
        if (cameraTransform == null)
            cameraTransform = transform;

        // Guarda la posición original
        originalPosition = cameraTransform.localPosition;

        // Inicia el timer automáticamente si está habilitado
        if (autoStart)
            StartShakeTimer();
    }

    void Update()
    {
        // Teclas de prueba (opcional - puedes remover esto)
        if (Input.GetKeyDown(KeyCode.Space))
            TriggerShake();

        if (Input.GetKeyDown(KeyCode.P))
            ToggleShakeTimer();
    }

    // Inicia el timer que activa el shake cada X segundos
    public void StartShakeTimer()
    {
        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        timerRoutine = StartCoroutine(ShakeTimerRoutine());
    }

    // Detiene el timer del shake
    public void StopShakeTimer()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }
    }

    // Alterna el timer (encender/apagar)
    public void ToggleShakeTimer()
    {
        if (timerRoutine != null)
            StopShakeTimer();
        else
            StartShakeTimer();
    }

    // Corrutina del timer que ejecuta el shake cada intervalo
    IEnumerator ShakeTimerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(shakeInterval);
            TriggerShake();
        }
    }

    // Activa un shake manualmente
    public void TriggerShake()
    {
        if (!isShaking)
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeRoutine());
        }
    }

    // Activa un shake con parámetros personalizados
    public void TriggerShake(float duration, float intensity)
    {
        if (!isShaking)
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }
    }

    // Corrutina principal del shake
    IEnumerator ShakeRoutine(float? customDuration = null, float? customIntensity = null)
    {
        isShaking = true;

        float duration = customDuration ?? shakeDuration;
        float intensity = customIntensity ?? shakeIntensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Calcula el progreso normalizado (0 a 1)
            float progress = elapsed / duration;

            // Aplica la curva de intensidad
            float currentIntensity = intensity * shakeCurve.Evaluate(progress);

            // Genera el offset del shake
            Vector3 shakeOffset;

            if (useRandomDirection)
            {
                // Shake en dirección aleatoria
                shakeOffset = Random.insideUnitSphere * currentIntensity;
            }
            else
            {
                // Shake en dirección específica con variación sinusoidal
                float shakeX = Mathf.Sin(elapsed * shakeFrequency) * preferredDirection.x * currentIntensity;
                float shakeY = Mathf.Sin(elapsed * shakeFrequency * 1.1f) * preferredDirection.y * currentIntensity;
                float shakeZ = Mathf.Sin(elapsed * shakeFrequency * 0.9f) * preferredDirection.z * currentIntensity;

                shakeOffset = new Vector3(shakeX, shakeY, shakeZ);
            }

            // Aplica el shake a la posición
            cameraTransform.localPosition = originalPosition + shakeOffset;

            yield return null;
        }

        // Restaura la posición original
        cameraTransform.localPosition = originalPosition;
        isShaking = false;
    }

    // Actualiza la posición original (útil si la cámara se mueve)
    public void UpdateOriginalPosition()
    {
        if (!isShaking)
            originalPosition = cameraTransform.localPosition;
    }

    // Detiene el shake inmediatamente
    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        cameraTransform.localPosition = originalPosition;
        isShaking = false;
    }

    // Cambia el intervalo del shake en tiempo real
    public void SetShakeInterval(float newInterval)
    {
        shakeInterval = newInterval;

        // Reinicia el timer si está activo
        if (timerRoutine != null)
        {
            StopShakeTimer();
            StartShakeTimer();
        }
    }

    // Propiedades públicas para acceder desde otros scripts
    public bool IsShaking => isShaking;
    public bool IsTimerActive => timerRoutine != null;

    // Limpia las corrutinas al desactivar
    void OnDisable()
    {
        StopShake();
        StopShakeTimer();
    }

    // Visualización en el inspector
    void OnDrawGizmos()
    {
        if (cameraTransform != null)
        {
            // Dibuja un cubo que representa el área de shake
            Gizmos.color = isShaking ? Color.red : Color.yellow;
            Gizmos.DrawWireCube(cameraTransform.position, Vector3.one * shakeIntensity * 2);

            // Dibuja una línea hacia la posición original
            if (isShaking)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(cameraTransform.position, transform.TransformPoint(originalPosition));
            }
        }
    }
}