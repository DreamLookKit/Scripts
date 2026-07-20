using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]


public class PlayerController : MonoBehaviour{
    [Header("Breath Settings")]
    [SerializeField] private float breathSpeed = 5f; // Скорость покачивания при дыхании
    [SerializeField] private float breathAmplitude = 0.02f; // Амплитуда (насколько сильно качается взгляд)
    [Header("Movement Speed Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float sprintSpeed = 5f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float waterVerticalSpeed = 5f;
    [SerializeField] private float mouseSensivity = 2f;
    [Header("Movement Physics Settings")]
    [SerializeField] private float acceleration = 16f;
    [SerializeField] private float deceleration = 14f;
    [SerializeField] [Range(0f, 1f)] private float airControlFacotr = 0.15f;    //Для контроля прыжка
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [Header("Camera Settings")]
    [SerializeField] private float standHeight = 2f;        // Стандартная высота игрока
    [SerializeField] private float crouchHeight = 1.0f;     // Высота игрока в приседе
    [SerializeField] private float crouchSmoothTime = 8f;   // Скорость плавного опускания камеры
    [SerializeField] [Range(0.5f, 1.0f)] private float cameraHeightRatio = 0.85f;   // Eye level (percentage of body height)
    [SerializeField] private float standartFOV = 60f;       // Стандартный FOV игрока
    [SerializeField] private float durationChangeFOV = 8f;  // Скорость изменения FOV игрока при прыжке
    [Header("Animation Settings")]
    [SerializeField] private Animator anim;
    [SerializeField] private float landingAheadDistance = 1.7f;
    [SerializeField] private LayerMask groundLayer;
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    private float defaultY = 0f;
    private float timer = 0f;
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
    private bool isLanded = false;    // Флаг: был ли вызван триггер Landing
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
        isLanded = false;                                       // По умолчанию ставим false - приземления не было
        // Ищем аниматор на дочерней 3D-модели
        anim = GetComponentInChildren<Animator>();
        // Автоматически находим камеру среди дочерних объектов, если она не была перетащена в Инспектор
        if(playerCamera == null){
            playerCamera = GetComponentInChildren<Camera>();
            defaultY = playerCamera.transform.localPosition.y;            // Записываем ссылку на камеру
        }
        // Блокируем курсор мыши в центре экрана, чтобы он не покидал окно игры
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
            playerCamera.transform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
        }
        isGrounded = Physics.Raycast(GetObjectBottom(), Vector3.down, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(GetObjectBottom(), Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        // Механика прыжка на суше (если на суше, не в воде и не в присяде)
        if(JumpAction.WasPressedThisFrame() && isGrounded && !IsInWater() && !CrouchAction.IsPressed()){
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            if(anim != null){
                anim.SetTrigger("Jump");
                isLanded = false;
                Debug.LogWarning("Jump trigger/isLanded reset to 'false'");
            }
        }
        // Пока уберу (Механика прыжка из воды)
        /* if(JumpAction.WasPressedThisFrame() && IsInWater())
            rb.AddForce(Vector3.up * (jumpForce * 1.2f), ForceMode.VelocityChange); */
        // Логика изменения высоты коллайдера и камеры (только если мы НЕ в воде)
        // ВАЖНО! Вызывать функцию определения присяда ТОЛЬКО ДО настраивания положения камеры
        if(!IsInWater())
            HandleCrouch(); 
        if(anim != null){
            // Считаем чисто горизонтальную скорость (без учета прыжков/падения по Y)
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            
            
            // 1. Считаем направление движения относительно взгляда игрока.
            // Vector3.Dot вернет от 1 (идем строго вперед) до -1 (идем строго назад)
            float directionSign = Vector3.Dot(horizontalVelocity.normalized, transform.forward);

            // 2. Умножаем чистую скорость на знак направления. 
            // Если directionSign отрицательный, то и итоговая скорость в аниматор уйдет со знаком минус.
            float signedSpeed = horizontalVelocity.magnitude * directionSign;
            
            
            // Передаем скорость и флаг воды в параметры аниматора
            if(CrouchAction.IsPressed())
                anim.SetFloat("Speed", signedSpeed, 0.1f, Time.deltaTime); //Снижаем изменение Speed для плавного вставания модельки
            else
                anim.SetFloat("Speed", signedSpeed);
            // Если в воде
            if(IsInWater()){
                anim.SetBool("IsInWater", true);    // Значит в воде
                anim.SetBool("IsGrounded", false);  // А землю автоматом игнорируем
            }else{  // Иначе
                anim.SetBool("IsGrounded", isGrounded); // Проверяем, на земле или над землей
                anim.SetBool("IsInWater", false);       // Воду автоматом игнорируем
            }
            // Передаем положение стоит/присяд
            anim.SetBool("IsCrouched", CrouchAction.IsPressed());
            // Передаем состояние нахождения на земле (везде кроме воды)
            //if(!IsInWater()) anim.SetBool("IsGrounded", isGrounded);
            if(CheckLandingAhead() && rb.linearVelocity.y < -3f && !IsInWater() && !isLanded){
                anim.SetTrigger("Landing");
                isLanded = true;
                Debug.Log("-Landing Trigger/isLanded is 'true', double Landing is denied");
            }
        }
        // Настраиваем положение камеры, учитывая эффект дыхангия
        if(playerCamera != null){
            // Считаем скорость движения
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f,  rb.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;
            // Если двигаемся, качаем камеру быстрее. Если стоим - это плавное дыхание
            float currentSpeed = speed > 0.1f ? breathSpeed * 1.5f : breathSpeed;
            float currentAmount = speed > 0.1f ? breathAmplitude * 2f : breathAmplitude;
            timer += Time.deltaTime * currentSpeed;
            // Сдвигаем камеру по синусоиде вверх-вниз относительно дефолтной высоты
            Vector3 newPos = playerCamera.transform.localPosition;
            newPos.y = defaultY + Mathf.Sin(timer) * currentAmount;
            playerCamera.transform.localPosition = newPos;
        }
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
            if (CrouchAction.IsPressed()){
                // Нажата кнопка приседа — активно погружаемся на глубину
                //Умножение на 0.4f - снизили скорость погружения
                targetVelocityY = -waterVerticalSpeed*0.3f;
                currentSpeed = walkSpeed * 0.5f;
                currentRate /= 2.5f;
            }else if (SprintAction.IsPressed()){
                currentSpeed = sprintSpeed * 0.5f;         // Спринет в воде - сравнима с со скоростью хотьбы на суше
                currentRate /= 1.2f; 
           }else{
                currentSpeed = walkSpeed * 0.5f; // Обычная скорость в воде 
                currentRate /= 2.5f;
           }
            /* float waterSurfaceY = 0f;
            if (buoyantScript != null && buoyantScript.WaterScript != null)
                waterSurfaceY = buoyantScript.WaterScript.SurfaceY; */
            // Пока уберу всплытие на пробел
            /* else if (JumpAction.IsPressed())
            {
                if (transform.position.y < (waterSurfaceY - 0.2f))
                    targetVelocityY = waterVerticalSpeed; // Нажат пробел и мы глубоко — плывем вверх (всплываем)
                else 
                    targetVelocityY = rb.linearVelocity.y; // Мы у самого края воды — отключаем силу, просто дрейфуем
            } */
        }
        // Если мы на суше
        else{ 
            if(CrouchAction.IsPressed())
                currentSpeed = crouchSpeed;
            else if (SprintAction.IsPressed())
                currentSpeed = sprintSpeed;
            else
                currentSpeed = walkSpeed;
        }
        // Идеальная горизонтальная скорость, которую хочет получить игрок прямо сейчас
        Vector3 normalVelocity = moveDirection * currentSpeed;
        // ПЛАВНО подтягиваем ТЕКУЩУЮ скорость Rigidbody к ИДЕАЛЬНОЙ скорости через MoveTowards
        float targetVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, normalVelocity.x, currentRate * Time.fixedDeltaTime);
        float targetVelocityZ = Mathf.MoveTowards(rb.linearVelocity.z, normalVelocity.z, currentRate * Time.fixedDeltaTime);   
        // Применяем обновленные скорости СТРОГО к горизонтальным осям
        rb.linearVelocity = new Vector3(targetVelocityX, targetVelocityY, targetVelocityZ);
        if(anim != null){
            // Намертво центрируем модель внутри капсулы, блокируя любые инерционные сдвиги
            anim.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        }
    }
    private void HandleCrouch(){
        float targetHeight = CrouchAction.IsPressed() ? crouchHeight : standHeight;
        // Независимый от кадров шаг сглаживания
        float lerpFactor = 1f - Mathf.Exp(-crouchSmoothTime * Time.deltaTime);
        float currentHeight = Mathf.Lerp(capsuleCollider.height, targetHeight, lerpFactor);
        capsuleCollider.height = currentHeight;
        // Динамически сдвигаем центр капсулы, чтобы подошва всегда оставалась в нуле
        // Формула (высота / 2) идеально держит низ коллайдера прижатым к полу
        capsuleCollider.center = new Vector3(0f, currentHeight / 2f, 0f);
        // 3. Обновляем базовую высоту камеры
        // Высота глаз теперь всегда рассчитывается строго от пола
        defaultY = currentHeight * cameraHeightRatio;
    }
    #region Water & Object Bottom & CheckLandingAhead for Jump
    private bool IsInWater(){
        return buoyantScript != null && buoyantScript.IsInWater;
    }
    private Vector3 GetObjectBottom(){
        // Берем честную МИРОВУЮ позицию центра игрока
        Vector3 worldPos = transform.position;
        // Просто приподнимаем старт луча на 5 сантиметров вверх внутрь тела, как и раньше
        return new Vector3(worldPos.x, worldPos.y + 0.05f, worldPos.z);
    }
    // Проверка земли перед приземлением (если падаем вниз и до земли остается расстояние, допустимое для включение анимации landing)
    private bool CheckLandingAhead(){
        if(isGrounded) return true;
        return Physics.Raycast(transform.position, Vector3.down, landingAheadDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }
    #endregion
}
