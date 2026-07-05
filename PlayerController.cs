using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]


public class PlayerController : MonoBehaviour{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float crouchSpeed = 3.5f;
    [SerializeField] private float mouseSensivity = 2f;
    [Header("Movement Physics: On ground")]
    [Tooltip("Character acceleration")]
    [SerializeField] private float acceleration = 16f;
    [Tooltip("Character stopping speed")]
    [SerializeField] private float deceleration = 14f;
    [Header("Movement Physics: On air")]
    [Tooltip("Momentum retention coefficient in air")]
    [SerializeField] [Range(0f, 1f)] private float airControlFacotr = 0.15f;
    [Header("Crouch Settings")]
    [SerializeField] private float standHeight = 2f;        // Стандартная высота игрока
    [SerializeField] private float crouchHeight = 1.0f;        // Высота игрока в приседе
    [SerializeField] private float crouchSmoothTime = 8f;   // Скорость плавного опускания камеры
    [SerializeField] [Range(0.5f, 1.0f)] private float cameraHeightRatio = 0.85f;   // Eye level (percentage of body height)
    [Header("Jump & Physics Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float groundCheckDistance = 0.2f;
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
        isGrounded = Physics.Raycast(GetObjectBottom(), Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
        //Debug.DrawRay(GetObjectBottom(), Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        // Механика прыжка на суше
        if(JumpAction.WasPressedThisFrame() && isGrounded && !IsInWater()){
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        // Пока уберу (Механика прыжка из воды)
        /* if(JumpAction.WasPressedThisFrame() && IsInWater())
            rb.AddForce(Vector3.up * (jumpForce * 1.2f), ForceMode.VelocityChange); */
        // Логика изменения высоты коллайдера и камеры (только если мы НЕ в воде)
        if(!IsInWater())
            HandleCrouch(); 
    }
    // Физическая сила всегда применяется в FixedUpdate (50 раз всекунду)
    private void FixedUpdate(){
        // Считываем чистый вектор WASD
        Vector2 inputVector = MoveAction.ReadValue<Vector2>();
        // Вычисляем физическое направление движения
        Vector3 moveDirection = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized;
        // Определяем, что мы сейчас делаем: разгоняемся (WASD зажат) или тормозим (WASD отпущен)
        // ...inputVector.magnitude > 0 означает, что игрок жмет клавиши движения
        float currentRate = (inputVector.magnitude > 0)? acceleration : deceleration;
        // Если мы НЕ на земле и НЕ в воде — срезаем силу торможения/разгона (для более длительного прыжка)
        if(!isGrounded && !IsInWater()) currentRate *= airControlFacotr;
        // Устанавливаем стандартную скорость хотьбы, тк пока не знаем, какие клавиши нажаты
        float currentSpeed = walkSpeed;
        // Важно: по умолчанию оставляем ту скорость погружения/всплытия, которую посчитал сам PhysX
        float targetVelocityY = rb.linearVelocity.y;
        // Если мы находимся в воде, инерция должна быть более «вязкой» (тормозим медленнее, разгоняемся тяжелее)
        if (IsInWater()){
            // В воде снижаем скорость изменения импульса, например, в 2.5 раза
            currentRate /= 2.5f; 
            currentSpeed = walkSpeed * 0.5f; // Обычная скорость в воде  
            if (CrouchAction.IsPressed()){
                // Нажата кнопка приседа — активно погружаемся на глубину
                //Умножение на 0.4f - снизили скорость погружения
                targetVelocityY = -waterVerticalSpeed*0.3f;
            }else if (SprintAction.IsPressed())
                currentSpeed = walkSpeed;        // Спринет в воде - сравнима с со скоростью хотьбы на суше
            else
                currentSpeed = walkSpeed * 0.5f; // Обычная скорость в воде 
            /* float waterSurfaceY = 0f;
            if (buoyantScript != null && buoyantScript.WaterScript != null)
                waterSurfaceY = buoyantScript.WaterScript.SurfaceY; */
            // Пока уберы всплытие на пробел
            /* else if (JumpAction.IsPressed())
            {
                if (transform.position.y < (waterSurfaceY - 0.2f))
                    targetVelocityY = waterVerticalSpeed; // Нажат пробел и мы глубоко — плывем вверх (всплываем)
                else 
                    targetVelocityY = rb.linearVelocity.y; // Мы у самого края воды — отключаем силу, просто дрейфуем
            } */
        }
        else if(CrouchAction.IsPressed())
            currentSpeed = crouchSpeed;
        else if (SprintAction.IsPressed())
            currentSpeed = sprintSpeed;
        // Идеальная горизонтальная скорость, которую хочет получить игрок прямо сейчас
        Vector3 normalVelocity = moveDirection * currentSpeed;
        // ПЛАВНО подтягиваем ТЕКУЩУЮ скорость Rigidbody к ИДЕАЛЬНОЙ скорости через MoveTowards
        float targetVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, normalVelocity.x, currentRate * Time.fixedDeltaTime);
        float targetVelocityZ = Mathf.MoveTowards(rb.linearVelocity.z, normalVelocity.z, currentRate * Time.fixedDeltaTime);   
        // Применяем обновленные скорости СТРОГО к горизонтальным осям
        rb.linearVelocity = new Vector3(targetVelocityX, targetVelocityY, targetVelocityZ);
    }
    private void HandleCrouch(){
        float targetHeight = CrouchAction.IsPressed() ? crouchHeight : standHeight;
        // Независимый от кадров шаг сглаживания
        float lerpFactor = 1f - Mathf.Exp(-crouchSmoothTime * Time.deltaTime);
        float currentHeight = Mathf.Lerp(capsuleCollider.height, targetHeight, lerpFactor);
        capsuleCollider.height = currentHeight;
        // 2. Динамическое положение камеры (теперь выровнено по настоящему верху коллайдера)
        Vector3 camPos = playerCamera.localPosition;
        camPos.y = currentHeight * cameraHeightRatio;
        playerCamera.localPosition = camPos;
    }
    
    #region  Water & Object Bottom
    private bool IsInWater()
    {
        return buoyantScript != null && buoyantScript.IsInWater;
    }
    private Vector3 GetObjectBottom()
    {
        // Берем честную МИРОВУЮ позицию центра игрока
        Vector3 worldPos = transform.position;
        // Считаем половину высоты его капсулы
        float halfHeight = capsuleCollider.height / 2f;
        // Возвращаем точку: строго по центру X и Z, а по вертикали Y опускаемся на уровень ног 
        // и приподнимаем старт на 5 сантиметров вверх (внутрь тела) для защиты от застревания
        return new Vector3(worldPos.x, worldPos.y - halfHeight + 0.05f, worldPos.z);
    }
    #endregion
}
