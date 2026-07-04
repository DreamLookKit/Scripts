using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyantObject : MonoBehaviour{
    // Переменные
    [Header("Buoyant settings")]
    //[Tooltip("Force, pushing object upward. Must be more than gravity (ex, > 9.81)")]
    //[SerializeField] private float buoyancyForce = 40f;
    //[Tooltip("Maximum allowed buoyancy force to prevent catapult effect")]
    //[SerializeField] private float maxBuoyancyLimit = 150f;     // Фикс экстремального выталкивания объектов
    
    //Коэффициент плавучести. 1.0 — баланс, 2.0 — плавает как пенопласт, меньше 1.0 — тонет
    [Tooltip("Buoyancy coefficient")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float floatingPower = 2.0f; // Возвращаем её в контекст кода!
    [Tooltip("Water movement drag (for smoothy braking object & not jumping like a bol)")]
    [SerializeField] private float waterDrag = 4f;
    [Tooltip("Water rotate drag (stabelize boat from sudden change)")]
    [SerializeField] private float waterAngularDrag = 1f;
    [Header("References")]
    // Эта магия открывает честный доступ для игрока, но защищает ссылку от перезаписи извне
    [field: SerializeField] public WaterRising WaterScript { get; private set; }
    private Rigidbody rb;
    private float originalDrag;         // Исходное сопротивление движения объекта
    private float originalAngularDrag;  // Исходное сопротивление вращения объекта
    private bool isInsideWater = false;
    private PlayerController pc; // Теперь эта ссылка видна ВСЕМ методам внутри этого файла!
    // Публичное свойство для проверки нахождения в воде
    public bool IsInWater => isInsideWater; 
    // Start
    private void Start(){
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerController>();
        // Запоминаем стандартное сопротивление объекта в воздухе (из настроек Rigidbody)
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        // АВТОПОИСК ВОДЫ: Так как мы префаб и не можем видеть сцену заранее,
        // мы находим воду сами в самую первую миллисекунду после спавна!
        if (WaterScript == null)
            WaterScript = Object.FindAnyObjectByType<WaterRising>();
    }
    // Физическая сила всегда применяется в FixedUpdate (50 раз всекунду)
    private void FixedUpdate(){
        if (isInsideWater){
            float waterSurfaceY = (WaterScript != null) ? WaterScript.SurfaceY : 0f;
            float immersionDepth = waterSurfaceY - GetObjectBottom();
            if (immersionDepth > 0){
                //float massMultiplier = (pc != null) ? 1f : Mathf.Max(rb.mass, 2f);
                //float dynamicBuoyancy = Mathf.Clamp(buoyancyForce * massMultiplier * immersionDepth, 0f, maxBuoyancyLimit);
                //rb.AddForce(Vector3.up * dynamicBuoyancy, ForceMode.Acceleration);
                
                // Ограничиваем максимальную глубину высотой самого объекта, 
                // чтобы сила выталкивания не росла бесконечно, когда объект полностью под водой.
                // objectHeight — высота вашей бочки или персонажа
                float objectHeight = GetComponent<Collider>().bounds.size.y;
                float middleObjectHeight = Mathf.Min(immersionDepth, objectHeight);
                float massMultiplier = (pc != null) ? 1f : Mathf.Max(rb.mass, 2f);
                // Считаем честную силу Архимеда с учетом массы
                Vector3 buoyanctForce = Vector3.up * floatingPower * middleObjectHeight * massMultiplier;
                rb.AddForce(buoyanctForce, ForceMode.Force);
          }
        }
    }
    // Этот метод срабатывает ОДИН РАЗ в момент касания триггера воды
    private void OnTriggerEnter(Collider other){
        // Мы проверяем, что объект, в который мы влетели, имеет тег "Water"
        if (other.CompareTag("Water")){
            isInsideWater = true;
            // Заставляем физический движок использовать сопротивление ВОДЫ
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
    }
    // Этот метод срабатывает ОДИН РАЗ в момент полного выхода из воды
    private void OnTriggerExit(Collider other){
        if (other.CompareTag("Water")){
            isInsideWater = false;
            // Мы возвращаем стандартное сопротивление ВОЗДУХА, которое запомнили в Start()
            rb.linearDamping = originalDrag;
            rb.angularDamping = originalAngularDrag;
            
        }
    }
    //Этот метод находит самую нижнюю точку хитбокса (коллайдера) объекта в игровом мире
    private float GetObjectBottom(){
        if (pc != null && pc.PlayerCollider != null){   // Это коллайдер игрока?
            return transform.position.y - (pc.PlayerCollider.height / 2f);  // Рассчитываем низ игрока (ноги)
        }
        return transform.position.y - (transform.localScale.y / 2f);    // Рассчитываем низ предмета (например, бочки)
    }
}