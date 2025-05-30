using UnityEngine;
using UnityEngine.InputSystem;

public class DroneFlycam : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;                 // Normal movement speed
    public float sprintMultiplier = 2f;          // Multiplier for movement speed when sprinting
    public float slowMotionFactor = 0.5f;        // Factor to reduce time scale when slow motion is active

    [Header("Rotation Settings")]
    public float rotationSensitivity = 2f;       // Sensitivity for mouse rotation

    private float defaultTimeScale;
    private float defaultFixedDeltaTime;
    private float rotationX = 0f;
    private float rotationY = 0f;
    public float smooth = 0.1f;

    public InputActionAsset inputActions;

    private InputAction m_moveAction;
    private InputAction m_lookAction;

    Vector2 movePlayer;
    Vector2 lookPlayer;

    private void Awake()
    {
       
        m_moveAction = inputActions.FindAction("Move");
        m_lookAction = inputActions.FindAction("Look");

     
    }

    private void OnEnable()
    {
      inputActions.FindActionMap("Player").Enable();
    }
    private void OnDisable()
    {
        inputActions.FindActionMap("Player").Disable();
    }

    void Start()
    {
        // Save default time scale and fixed delta time for resetting after slow motion
        defaultTimeScale = Time.timeScale;
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        // Lock and hide the cursor for a better camera control experience
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize the rotation values with the camera's starting rotation
        rotationX = transform.eulerAngles.y;
        rotationY = transform.eulerAngles.x;
    }

    void Update()
    {
        movePlayer = m_moveAction.ReadValue<Vector2>();
        lookPlayer = m_lookAction.ReadValue<Vector2>();

        //HandleMovement();
       // HandleRotation();

        HandleSlowMotion();

        Movement();
        Rotation();
    }

    private void Movement() // Nuevo Input System
    { 
       
        Vector3 direction = transform.forward * movePlayer.y + transform.right * movePlayer.x;

        //Subir
        if (Keyboard.current.qKey.isPressed)
        {
            direction += Vector3.down;
        }

        //Bajar
        if (Keyboard.current.eKey.isPressed)
        {
            direction += Vector3.up;
        }

        // Sprint 
        float adjustedSpeed = moveSpeed * (Keyboard.current.leftShiftKey.isPressed ? sprintMultiplier : 1f);

        
        transform.position += adjustedSpeed * Time.deltaTime * direction;
    }

    private void HandleMovement() // Viejo Input System
    {
        // Get input for forward, backward, left, and right movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D for left/right
        float vertical = Input.GetAxis("Vertical");     // W/S for forward/back

        // Calculate movement direction based on input and speed
        Vector3 direction = transform.forward * vertical + transform.right * horizontal;

        // Handle upward and downward movement with Q and E keys
        if (Input.GetKey(KeyCode.Q))
        {
            direction += Vector3.down;  // Move down
        }
        if (Input.GetKey(KeyCode.E))
        {
            direction += Vector3.up;    // Move up
        }

        // Adjust movement speed when Shift is held down for sprint
        float adjustedSpeed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        // Apply movement to the camera
        transform.position += direction * adjustedSpeed * Time.deltaTime;
    }

    private void Rotation()  // Nuevo Input System con suavizado
    {
        // Suavizado de la rotación
        float targetRotationX = rotationX + lookPlayer.x * rotationSensitivity;
        float targetRotationY = rotationY - lookPlayer.y * rotationSensitivity;

        targetRotationY = Mathf.Clamp(targetRotationY, -90f, 90f);

        // Interpolación suave
        rotationX = Mathf.Lerp(rotationX, targetRotationX, smooth);
        rotationY = Mathf.Lerp(rotationY, targetRotationY, smooth);

        // Aplicar rotación a la cámara
        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }

    private void HandleRotation() // Viejo Input System
    {
        // Get mouse movement and adjust rotation values based on sensitivity
        rotationX += Input.GetAxis("Mouse X") * rotationSensitivity;
        rotationY -= Input.GetAxis("Mouse Y") * rotationSensitivity;
        rotationY = Mathf.Clamp(rotationY, -90f, 90f); // Clamp vertical rotation to prevent flipping

        // Apply rotation to the camera
        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }

    private void HandleSlowMotion()
    {
        // Activate slow motion when Space bar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = slowMotionFactor;
            Time.fixedDeltaTime = defaultFixedDeltaTime * slowMotionFactor;
        }

        // Reset time scale when Space bar is released
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Time.timeScale = defaultTimeScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Lock and hide the cursor when the game window is focused
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
