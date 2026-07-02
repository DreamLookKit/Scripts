using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]


public partial class PlayerController : MonoBehaviour{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float crouchSpeed = 3.5f;
    [SerializeField] private float mouseSensivity = 2f;
    [Header("Crouch Settings")]
    [SerializeField] private float standHeight = 2f;        // Стандартная высота игрока
    [SerializeField] private float crouchHeight = 1.0f;        // Высота игрока в приседе
    [SerializeField] private float crouchSmoothTime = 8f;   // Скорость плавного опускания камеры
    [Range(0.5f, 1.0f)]
    [SerializeField] private float cameraHeightRatio = 0.85f;   // Eye level (percentage of body height)
    [Header("Jump & Physics Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [Header("Water Control Settings")]
    [SerializeField] private float waterVerticalSpeed = 5f;
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [Header("Input Actions")]
    // Мы делаем ссылки на действия публичными, чтобы скрипт меню настроек мог получить к ним доступ
    public InputAction MoveAction { get; private set; }
    public InputAction LookAction { get; private set; }
    public InputAction SprintAction { get; private set; }
    public InputAction JumpAction { get; private set; }
    public InputAction CrouchAction { get; private set; }
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;    // Ссылка для изменения высоты тела
    public CapsuleCollider PlayerCollider => capsuleCollider; // Безопасная ссылка на CapsuleCollider
    private BuoyantObject buoyantScript;
    private float cameraRotationX = 0f;
    private float currentCameraY;   // Текущая локальная высота камеры
    private bool isGrounded;
    private void Awake(){
        // Создаем действие для обзора мышью
        LookAction = new InputAction("Look", binding: "<Mouse>/delta");
        // Мы создаем составной 2D-вектор для WASD
        // Каждой кнопке жестко присвоен индекс (ID) от 1 до 4; это критически важно для будущего переназначения клавиш!
        MoveAction = new InputAction("Move");
        MoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")     // Индекс привязки: 1
            .With("Down", "<Keyboard>/s")   // Индекс привязки: 2
            .With("Left", "<Keyboard>/a")   // Индекс привязки: 3
            .With("Right", "<Keyboard>/d"); // Индекс привязки: 4
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
        capsuleCollider = GetComponent<CapsuleCollider>();      // Поиск нашего коллайдера
        buoyantScript = GetComponent<BuoyantObject>();          // Поиск плавучести
        rb.interpolation = RigidbodyInterpolation.Interpolate;  
        // Автоматически находим камеру среди дочерних объектов, если она не была перетащена в Инспектор
        if(playerCamera == null){
            Camera childCamera = GetComponentInChildren<Camera>();
            if(childCamera != null) playerCamera = childCamera.transform;   // Записываем ссылку на камеру
            // Блокируем курсор мыши в центре экрана, чтобы он не покидал окно игры
        }
        Cursor.lockState = CursorLockMode.Locked;
        // Скрываем его
        Cursor.visible = false;
    }
    // Графика и Логика (Привязана к FPS: 60, 100, 144 - неважно)
    private void Update(){
        if(Mouse.current != null){
            // Мы получаем изменение положения мыши для текущего кадра (30f - компенсация медленного движения мыши)
            Vector2 mouseDelta = LookAction.ReadValue<Vector2>() * (mouseSensivity * 30f) * Time.deltaTime;
            // Поворачиваем туловище влево и вправо
            transform.Rotate(Vector3.up * mouseDelta.x);
            // Наклоняем камеру вверх и вниз
            // Насколько наклонить камеру, отняв координапты мыши за текущий кадр, ограничив 85 градусами
            cameraRotationX = Mathf.Clamp(cameraRotationX - mouseDelta.y, -85f, 85f); // Насколько сдвинуть камеру, отняв координапты мыши за текущий кадр, ограничив 85 градусами
            playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
        }
        isGrounded = Physics.Raycast(GetObjectBottom(), Vector3.down, groundCheckDistance, groundLayer);
        // Механика прыжка на суше
        if(JumpAction.WasPressedThisFrame() && isGrounded && !IsInWater())
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        // Механика прыжка из воды
        if(JumpAction.WasPressedThisFrame() && IsInWater())
            rb.AddForce(Vector3.up * (jumpForce * 1.2f), ForceMode.VelocityChange);
        // Логика изменения высоты коллайдера и камеры (только если мы НЕ в воде)
        if(!IsInWater())
            HandleCrouch();
    }
    // Физическая сила всегда применяется в FixedUpdate (50 раз всекунду)
    private void FixedUpdate(){
        // Считываем чистый вектор WASD
        Vector2 inputVector = MoveAction.ReadValue<Vector2>();
        // Мы вычисляем физическое направление движения
        Vector3 moveDirection = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized;
        // Расчет скорости (Спринт, Присед или Ходьба)
        float currentSpeed = walkSpeed;
        float targetVelocityY = rb.linearVelocity.y;
        if(IsInWater()){
            currentSpeed = walkSpeed * 0.5f; 
            // Перенесли чтение высоты сюда, чтобы она была доступна ВСЕМ условиям ниже
            float waterSurfaceY = 0f;
            // Определяем текущую координату поверхности воды
            if(buoyantScript != null && buoyantScript.WaterScript != null)
                waterSurfaceY = buoyantScript.WaterScript.SurfaceY;
            if(CrouchAction.IsPressed()) 
                targetVelocityY = -waterVerticalSpeed; // Нажата кнопка приседа — активно погружаемся на глубину
            else if(JumpAction.IsPressed()){
                 if (transform.position.y < (waterSurfaceY - 0.2f)){
                    targetVelocityY = waterVerticalSpeed; // Нажат пробел и мы глубоко — плывем вверх (всплываем)
                 }else targetVelocityY = rb.linearVelocity.y; // Мы у самого края воды — отключаем силу, просто дрейфуем
            }
        }
        else if(CrouchAction.IsPressed())
            currentSpeed = crouchSpeed;
        else if(SprintAction.IsPressed())
            currentSpeed = sprintSpeed;
        // Применяем обновленные скорости к горизонтальным осям
        float targetVelocityX = moveDirection.x * currentSpeed;
        float targetVelocityZ = moveDirection.z * currentSpeed;
        rb.linearVelocity = new Vector3(targetVelocityX, targetVelocityY, targetVelocityZ);
    }
    private void HandleCrouch(){
        float targetHeight = CrouchAction.IsPressed() ? crouchHeight : standHeight;
        // Независимый от кадров шаг сглаживания
        float lerpFactor = 1f - Mathf.Exp(-crouchSmoothTime * Time.deltaTime);
        float currentHeight = Mathf.Lerp(capsuleCollider.height, targetHeight, lerpFactor);
        capsuleCollider.height = currentHeight;
        // 2. ФИКС: Сдвигаем центр коллайдера, чтобы ноги всегда оставались на земле!
        // Формула вычисляет половину разницы между стандартной высотой и текущей высотой.
        Vector3 colCenter = capsuleCollider.center;
        colCenter.y = currentHeight / 2f;
        capsuleCollider.center = colCenter;
        // 3. Динамическое положение камеры (теперь выровнено по настоящему верху коллайдера)
        Vector3 camPos = playerCamera.localPosition;
        camPos.y = currentHeight * cameraHeightRatio;
        playerCamera.localPosition = camPos;
    }
}
