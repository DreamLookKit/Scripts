/* - От 0.1 до 0.9 — «Тонущие объекты» (Тяжелее воды)
Физически это означает, что плотность объекта выше плотности воды. 
Даже если предмет полностью уйдет на дно, силы Архимеда не хватит, чтобы его поднять. 
Он будет реалистично лежать на дне, но падать сквозь воду чуть медленнее, чем в воздухе.
- Ровно 1.0 — «Идеальный баланс» (Нейтральная плавучесть)
Объект весит ровно столько же, сколько вытесненная им вода (как подводная лодка или рыба). 
На какой глубине вы его оставите, там он и зависнет.
- От 1.1 до 1.9 — «Тяжелая плавучесть» (Металл, сырое дерево)
Объекты будут плавать, но погружаясь в воду очень глубоко. 
Например, при 1.2 бочка будет торчать из воды всего на 15–20%, а остальная её часть будет скрыта под поверхностью.
- Ровно 2.0 — «Стандарт» (Сухое дерево, пластик)
Универсальная точка равновесия. Объект погружается ровно наполовину (на 50%). 
Выглядит отлично для большинства стандартных игровых коробок и бочек.
-От 2.1 до 5.0 — «Экстремальная плавучесть» (Пенопласт, мячи, воздух)
Объекты плавают строго на поверхности, едва касаясь воды дном (погружение на 10–20%). 
Если такую бочку насильно притопить скриптом или сбросить с высоты, она очень резво выскочит обратно наверх.
 */
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyantObject : MonoBehaviour{
    // Переменные
    [Header("Buoyant settings")]
    //Коэффициент плавучести. 1.0 — баланс, 2.0 — плавает как пенопласт, меньше 1.0 — тонет
    [Tooltip("Buoyancy coefficient")]
    [Range(0.1f, 5.0f)]
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
                // Ограничиваем максимальную глубину высотой самого объекта, 
                // чтобы сила выталкивания не росла бесконечно, когда объект полностью под водой.
                // objectHeight — высота вашей бочки или персонажа
                float objectHeight = GetComponent<Collider>().bounds.size.y;
                float middleObjectHeight = Mathf.Min(immersionDepth, objectHeight);
                float gravity = Mathf.Abs(Physics.gravity.y);
                // Считаем честную силу Архимеда с учетом массы
                Vector3 buoyanctForce = Vector3.up * floatingPower * middleObjectHeight * rb.mass * gravity;
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