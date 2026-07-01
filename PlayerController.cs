using UnityEngine;
using UnityEngine.InputSystem; 

public class PlayerController : MonoBehaviour{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float crouchSpeed = 3.5f;
    [SerializeField] private float mouseSensivity = 2f;
    [Header("Crouch Settings")]
    [SerializeField] private float standHeight = 2f;        // Standart player height
    [SerializeField] private float crouchHeight = 1.0f;        // Crouch player height
    [SerializeField] private float crouchSmoothTime = 8f;   // Smooth camera lowering speed
    [Header("Jump & Physics Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayer;
    [Header("Water Control Settings")]
    [SerializeField] private float waterVerticalSpeed = 5f;
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [Header("Input Actions")]
    // We make the links to the actions public so that the settings menu script can access them
    public InputAction MoveAction { get; private set; }
    public InputAction LookAction { get; private set; }
    public InputAction SprintAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction CrouchAction { get; private set; }
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;    //Link to change body height
    public CapsuleCollider PlayerCollider => capsuleCollider; //Safe link for CapsuleCollider
    private BuoyantObject buoyantScript;
    private float cameraRotationX = 0f;
    private float currentCameraY;   // Current local camera height
    private bool isGrounded;
    private void Awake(){
        // Creating a mouse look action
        LookAction = new InputAction("Look", binding: "<Mouse>/delta");
        // We create a composite 2D vector for WASD
        // Each button is hard-assigned an index (ID) from 1 to 4; this is critical for future remapping!
        MoveAction = new InputAction("Move");
        MoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")     // Index binding: 1
            .With("Down", "<Keyboard>/s")   // Index binding: 2
            .With("Left", "<Keyboard>/a")   // Index binding: 3
            .With("Right", "<Keyboard>/d"); // Index binding: 4
        SprintAction = new InputAction("Sprint", binding:"<Keyboard>/leftShift");
        JumpAction = new InputAction("Jump", binding:"<Keyboard>/space");
        CrouchAction = new InputAction("Crouch", binding:"<Keyboard>/leftCtrl");
    }
    private void OnEnable(){
        MoveAction.Enable();
        LookAction.Enable();
        SprintAction.Enable();
        JumpAction.Enable();
        CrouchAction.Enable();
    }
    private void OnDisable(){
        MoveAction.Disable();
        LookAction.Disable();
        SprintAction.Disable();
        JumpAction.Disable();
        CrouchAction.Disable();
    }
    private void Start(){
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();  //Search our collider
        buoyantScript = GetComponent<BuoyantObject>();
        // Automatically find the camera among child objects if it wasn't dragged into the Inspector
        if(playerCamera == null){
            Camera childCamera = GetComponentInChildren<Camera>();
            if(childCamera != null) playerCamera = childCamera.transform;
            // Lock the mouse cursor to the center of the screen so it doesn't leave the game window
        }
        currentCameraY = playerCamera.localPosition.y;  // Note the camera's starting height
        Cursor.lockState = CursorLockMode.Locked;
        // Hide him
        Cursor.visible = false;
    }
    private void Update(){
        if(Mouse.current != null)
        {
            // We get the mouse change for the current frame (30f - compensation slow mouse movement)
            Vector2 mouseDelta = LookAction.ReadValue<Vector2>() * (mouseSensivity * 30f) * Time.deltaTime;
            // Turn the torso to the left and right
            transform.Rotate(Vector3.up * mouseDelta.x);
            // Tilt the camera up and down
            cameraRotationX -= mouseDelta.y;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -85f, 85f);
            playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
        }
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        // The mechanics of the jump on land
        if(JumpAction.WasPressedThisFrame() && isGrounded && !IsInWater())
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        // The mechanics of the jump from water
        if(JumpAction.WasPressedThisFrame() && IsInWater())
            rb.AddForce(Vector3.up * (jumpForce * 1.2f), ForceMode.VelocityChange);
        // Logic for changing the height of the collider and camera (only if we are NOT in water)
        if(!IsInWater())
            HandleCrouch();
   }
    private void FixedUpdate(){
        // Read the raw WASD vector
        Vector2 inputVector = MoveAction.ReadValue<Vector2>();
        // We calculate the physical direction of movement
        Vector3 moveDirection = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized;
        // Calculate speed (Sprint, Crouch or Walk
        float currentSpeed = walkSpeed;
        float targetVelocityX = moveDirection.x * currentSpeed;
        float targetVelocityZ = moveDirection.z * currentSpeed;
        float targetVelocityY = rb.linearVelocity.y;
        if(IsInWater()){
            currentSpeed = walkSpeed * 0.7f; 
            // Перенесли чтение высоты сюда, чтобы она была доступна ВСЕМ условиям ниже
            float waterSurfaceY = 0f;
            // Determine the current coordinate of the water surface
            if(buoyantScript != null && buoyantScript.WaterScript != null){
                waterSurfaceY = buoyantScript.WaterScript.SurfaceY;
            }
            if(CrouchAction.IsPressed()) 
                targetVelocityY = -waterVerticalSpeed; // Активно погружаемся
            else if(JumpAction.IsPressed()){
                if (transform.position.y < (waterSurfaceY - 0.2f)){
                    targetVelocityY = waterVerticalSpeed; // Всплываем
                }else targetVelocityY = rb.linearVelocity.y; // Дрейфуем
            }
        }
        else if(CrouchAction.IsPressed())
            currentSpeed = crouchSpeed;
        else if(SprintAction.IsPressed())
            currentSpeed = sprintSpeed;
        // Apply updated speeds to horizontal axes
        if(!IsInWater()){
            targetVelocityX = moveDirection.x * currentSpeed;
            targetVelocityZ = moveDirection.z * currentSpeed;
        }
        rb.linearVelocity = new Vector3(targetVelocityX, targetVelocityY, targetVelocityZ);
    }
    private void HandleCrouch(){
        float targetHeight = CrouchAction.IsPressed() ? crouchHeight : standHeight;
        // Smoothly update collider height
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, Time.deltaTime * crouchSmoothTime);
        // Adjust camera position dynamically based on collider height
        Vector3 camPos = playerCamera.localPosition;
        float targetCamY = targetHeight * 0.8f; // Head position approximation
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * crouchSmoothTime);
        playerCamera.localPosition = camPos;
    }
    private bool IsInWater(){
        // Safe check for water script link
        return buoyantScript != null && buoyantScript.IsInWater;
    }
}
