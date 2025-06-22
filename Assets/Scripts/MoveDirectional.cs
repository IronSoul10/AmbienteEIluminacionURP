using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveDirectional : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>(); // Lista de emptys/waypoints

    [Header("Configuración de NavMesh")]
    [SerializeField] private float moveSpeed = 3.5f; // Velocidad de movimiento del NavMeshAgent
    [SerializeField] private float angularSpeed = 120f; // Velocidad de rotación
    [SerializeField] private float acceleration = 8f; // Aceleración del agente
    [SerializeField] private float stoppingDistance = 0.5f; // Distancia de parada

    [Header("Comportamiento")]
    [SerializeField] private MovementMode movementMode = MovementMode.Loop; // Modo de movimiento
    [SerializeField] private bool moveOnStart = true; // Si debe empezar a moverse automáticamente
    [SerializeField] private float waitTime = 0f; // Tiempo de espera en cada waypoint
    [SerializeField] private bool pauseOnObstacle = true; // Si debe pausar cuando encuentra obstáculos

    [Header("Configuración Aleatoria")]
    [SerializeField] private bool randomizeWaypoints = false; // Si debe elegir waypoints aleatoriamente
    [SerializeField] private float randomWaitTime = 2f; // Tiempo de espera aleatorio máximo

    [Header("Configuración Avanzada")]
    [SerializeField] private float pathRecalculationTime = 0.5f; // Tiempo entre recálculos de ruta
    [SerializeField] private bool autoRepath = true; // Recalcular ruta automáticamente
    [SerializeField] private float maxPathDistance = 100f; // Distancia máxima de ruta válida

    // Enumeración para los modos de movimiento
    public enum MovementMode
    {
        Loop,           // Va del primero al último y vuelve al primero
        PingPong,       // Va del primero al último y regresa
        Random,         // Elige waypoints aleatoriamente
        Once,           // Va del primero al último y se detiene
        Custom          // Movimiento controlado externamente
    }

    // Variables privadas
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private bool isWaiting = false;
    private bool movingForward = true; // Para el modo PingPong
    private bool isPaused = false;

    private Coroutine movementCoroutine;
    private Coroutine waitCoroutine;
    private Coroutine pathCheckCoroutine;

    // Componentes
    private NavMeshAgent navAgent;

    // Eventos
    public System.Action<int> OnWaypointReached; // Evento cuando llega a un waypoint
    public System.Action OnMovementComplete; // Evento cuando completa el recorrido (modo Once)
    public System.Action OnPathBlocked; // Evento cuando la ruta está bloqueada
    public System.Action OnPathFound; // Evento cuando encuentra una ruta válida

    void Start()
    {
        // Obtiene el NavMeshAgent
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            Debug.LogError("NavMeshAgent no encontrado en " + gameObject.name + ". Añade un NavMeshAgent component.");
            return;
        }

        // Configura el NavMeshAgent
        SetupNavMeshAgent();

        // Valida waypoints
        ValidateWaypoints();

        // Inicia movimiento si está configurado
        if (moveOnStart && waypoints.Count > 0)
        {
            StartMovement();
        }

        // Inicia verificación de rutas si está habilitada
        if (autoRepath)
        {
            StartPathChecking();
        }
    }

    void SetupNavMeshAgent()
    {
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = angularSpeed;
        navAgent.acceleration = acceleration;
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.autoBraking = true;
        navAgent.autoRepath = autoRepath;
    }

    void ValidateWaypoints()
    {
        // Remueve waypoints nulos
        waypoints.RemoveAll(wp => wp == null);

        if (waypoints.Count == 0)
        {
            Debug.LogWarning("No hay waypoints asignados en " + gameObject.name);
            return;
        }

        // Verifica que los waypoints están en el NavMesh
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (!IsPositionOnNavMesh(waypoints[i].position))
            {
                Debug.LogWarning($"Waypoint {i} ({waypoints[i].name}) no está en el NavMesh o muy cerca de él.");
            }
        }
    }

    bool IsPositionOnNavMesh(Vector3 position)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas);
    }

    public void StartMovement()
    {
        if (navAgent == null || waypoints.Count == 0)
        {
            Debug.LogWarning("No se puede iniciar movimiento sin NavMeshAgent o waypoints");
            return;
        }

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }

        isMoving = false;
        isWaiting = false;
        isPaused = false;
    }

    public void PauseMovement()
    {
        isPaused = true;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }
    }

    public void ResumeMovement()
    {
        isPaused = false;
    }

    IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (waypoints.Count == 0 || navAgent == null) yield break;

            // Pausa si está pausado
            while (isPaused)
            {
                yield return null;
            }

            // Obtiene el siguiente waypoint
            Transform targetWaypoint = GetNextWaypoint();
            if (targetWaypoint == null) yield break;

            // Se mueve hacia el waypoint
            yield return StartCoroutine(MoveToWaypoint(targetWaypoint));

            // Invoca evento de waypoint alcanzado
            OnWaypointReached?.Invoke(currentWaypointIndex);

            // Espera si es necesario
            if (waitTime > 0f || (randomizeWaypoints && randomWaitTime > 0f))
            {
                float currentWaitTime = randomizeWaypoints ? Random.Range(0f, randomWaitTime) : waitTime;
                if (currentWaitTime > 0f)
                {
                    yield return StartCoroutine(WaitAtWaypoint(currentWaitTime));
                }
            }

            // Actualiza índice según el modo
            UpdateWaypointIndex();

            // Verifica si debe terminar el movimiento
            if (ShouldStopMovement())
            {
                OnMovementComplete?.Invoke();
                break;
            }
        }

        isMoving = false;
    }

    Transform GetNextWaypoint()
    {
        if (waypoints.Count == 0) return null;

        if (movementMode == MovementMode.Random)
        {
            // Elige un waypoint aleatorio diferente al actual
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, waypoints.Count);
            } while (randomIndex == currentWaypointIndex && waypoints.Count > 1);

            currentWaypointIndex = randomIndex;
        }

        return waypoints[currentWaypointIndex];
    }

    IEnumerator MoveToWaypoint(Transform target)
    {
        isMoving = true;

        // Verifica que el waypoint esté en el NavMesh
        Vector3 targetPosition = GetValidNavMeshPosition(target.position);

        // Calcula la ruta
        NavMeshPath path = new NavMeshPath();
        if (!navAgent.CalculatePath(targetPosition, path))
        {
            Debug.LogWarning($"No se puede calcular ruta hacia {target.name}");
            OnPathBlocked?.Invoke();
            isMoving = false;
            yield break;
        }

        // Verifica que la ruta no sea demasiado larga
        float pathLength = GetPathLength(path);
        if (pathLength > maxPathDistance)
        {
            Debug.LogWarning($"Ruta hacia {target.name} es demasiado larga: {pathLength}m");
            OnPathBlocked?.Invoke();
            isMoving = false;
            yield break;
        }

        // Establece el destino
        navAgent.SetDestination(targetPosition);
        OnPathFound?.Invoke();

        // Espera hasta llegar al destino
        while (navAgent.pathPending || navAgent.remainingDistance > navAgent.stoppingDistance)
        {
            // Pausa si está pausado
            while (isPaused)
            {
                if (navAgent.enabled)
                {
                    navAgent.ResetPath();
                    navAgent.velocity = Vector3.zero;
                }
                yield return null;
                if (!isPaused && navAgent.enabled)
                {
                    navAgent.SetDestination(targetPosition);
                }
            }

            // Verifica si el agente está atascado
            if (pauseOnObstacle && navAgent.velocity.magnitude < 0.1f && navAgent.remainingDistance > navAgent.stoppingDistance + 1f)
            {
                yield return new WaitForSeconds(0.5f);
                if (navAgent.velocity.magnitude < 0.1f)
                {
                    Debug.LogWarning("Agente posiblemente atascado, recalculando ruta...");
                    navAgent.ResetPath();
                    yield return new WaitForSeconds(0.2f);
                    navAgent.SetDestination(targetPosition);
                }
            }

            yield return null;
        }

        isMoving = false;
    }

    Vector3 GetValidNavMeshPosition(Vector3 originalPosition)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(originalPosition, out hit, 5f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return originalPosition;
    }

    float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return length;
    }

    IEnumerator WaitAtWaypoint(float duration)
    {
        isWaiting = true;

        // Detiene el agente durante la espera
        if (navAgent.enabled)
        {
            navAgent.ResetPath();
            navAgent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(duration);
        isWaiting = false;
    }

    void StartPathChecking()
    {
        if (pathCheckCoroutine != null)
            StopCoroutine(pathCheckCoroutine);

        pathCheckCoroutine = StartCoroutine(PathCheckingRoutine());
    }

    IEnumerator PathCheckingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(pathRecalculationTime);

            if (isMoving && navAgent.enabled && !navAgent.pathPending && navAgent.hasPath)
            {
                // Verifica si la ruta actual sigue siendo válida
                if (navAgent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    Debug.LogWarning("Ruta parcialmente bloqueada, recalculando...");
                    Vector3 currentDestination = navAgent.destination;
                    navAgent.ResetPath();
                    yield return new WaitForSeconds(0.1f);
                    navAgent.SetDestination(currentDestination);
                }
            }
        }
    }

    void UpdateWaypointIndex()
    {
        switch (movementMode)
        {
            case MovementMode.Loop:
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                break;

            case MovementMode.PingPong:
                if (movingForward)
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count - 1)
                    {
                        movingForward = false;
                    }
                }
                else
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex <= 0)
                    {
                        movingForward = true;
                    }
                }
                break;

            case MovementMode.Once:
                currentWaypointIndex++;
                break;

            case MovementMode.Random:
                // Ya se maneja en GetNextWaypoint()
                break;
        }
    }

    bool ShouldStopMovement()
    {
        return movementMode == MovementMode.Once && currentWaypointIndex >= waypoints.Count;
    }

    // Métodos públicos para control externo
    public void SetWaypoints(List<Transform> newWaypoints)
    {
        waypoints = newWaypoints;
        ValidateWaypoints();
    }

    public void AddWaypoint(Transform waypoint)
    {
        if (waypoint != null && !waypoints.Contains(waypoint))
        {
            waypoints.Add(waypoint);
        }
    }

    public void RemoveWaypoint(Transform waypoint)
    {
        waypoints.Remove(waypoint);
    }

    public void GoToWaypoint(int index)
    {
        if (index >= 0 && index < waypoints.Count)
        {
            currentWaypointIndex = index;
            if (!isMoving)
            {
                StartMovement();
            }
        }
    }

    public void GoToNearestWaypoint()
    {
        if (waypoints.Count == 0) return;

        float minDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        GoToWaypoint(nearestIndex);
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (navAgent != null)
            navAgent.speed = newSpeed;
    }

    public void SetStoppingDistance(float newDistance)
    {
        stoppingDistance = newDistance;
        if (navAgent != null)
            navAgent.stoppingDistance = newDistance;
    }

    // Propiedades públicas
    public bool IsMoving => isMoving;
    public bool IsWaiting => isWaiting;
    public bool IsPaused => isPaused;
    public int CurrentWaypointIndex => currentWaypointIndex;
    public Transform CurrentWaypoint => waypoints.Count > 0 ? waypoints[currentWaypointIndex] : null;
    public int WaypointCount => waypoints.Count;
    public NavMeshAgent Agent => navAgent;
    public float RemainingDistance => navAgent != null ? navAgent.remainingDistance : 0f;
    public bool HasPath => navAgent != null && navAgent.hasPath;
    public NavMeshPathStatus PathStatus => navAgent != null ? navAgent.pathStatus : NavMeshPathStatus.PathInvalid;

    void OnDisable()
    {
        StopMovement();
        if (pathCheckCoroutine != null)
        {
            StopCoroutine(pathCheckCoroutine);
            pathCheckCoroutine = null;
        }
    }

    // Visualización en el editor
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // Dibuja waypoints
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            // Color del waypoint
            Gizmos.color = (i == currentWaypointIndex) ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);

            // Indica si está en NavMesh
            if (!IsPositionOnNavMesh(waypoints[i].position))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(waypoints[i].position, Vector3.one * 0.5f);
            }
        }

        // Dibuja líneas entre waypoints
        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }

        // Línea de cierre para modo Loop
        if (movementMode == MovementMode.Loop && waypoints.Count > 2)
        {
            if (waypoints[waypoints.Count - 1] != null && waypoints[0] != null)
            {
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
            }
        }

        // Dibuja ruta actual del NavMesh
        if (Application.isPlaying && navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] pathCorners = navAgent.path.corners;
            for (int i = 1; i < pathCorners.Length; i++)
            {
                Gizmos.DrawLine(pathCorners[i - 1], pathCorners[i]);
            }
        }

        // Dibuja línea hacia el waypoint actual
        if (Application.isPlaying && CurrentWaypoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, CurrentWaypoint.position);
        }
    }
}