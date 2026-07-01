using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyantObject : MonoBehaviour{
    // Variables
    [Header("Buoyant settings")]
    [Tooltip("Force, pushing object upward. Must be more than gravity (ex, > 9.81)")]
    [SerializeField] private float buoyancyForce = 40f;
    [Tooltip("Maximum allowed buoyancy force to prevent catapult effect")]
    [SerializeField] private float maxBuoyancyLimit = 150f;     // Fix extremal pushed objects
    [Tooltip("Water movement drag (for smoothy braking object & not jumping like a bol)")]
    [SerializeField] private float waterDrag = 4f;
    [Tooltip("Water rotate drag (stabelize boat from sudden change)")]
    [SerializeField] private float waterAngularDrag = 1f;
    [Header("References")]
    // Эта магия открывает честный доступ для игрока, но защищает ссылку от перезаписи извне
    [field: SerializeField] public WaterRising WaterScript { get; private set; }
    private Rigidbody rb;
    private float originalDrag;         //Original drag from object
    private float originalAngularDrag;  //Original angular drag from object
    private bool isInsideWater = false;
    private PlayerController pc; // Теперь эта ссылка видна ВСЕМ методам внутри этого файла!
    // Публичное свойство для проверки нахождения в воде
    public bool IsInWater => isInsideWater; 
    // Start
    private void Start(){
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<PlayerController>();
        // Memorizing the standart object drag in air (from Rigidbody settings)
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
        // AUTO-FIND WATER: Since we're a prefab and can't see the scene beforehand,
        // we find the water ourselves in the very first millisecond after spawning!
        if (WaterScript == null){
            WaterScript = Object.FindAnyObjectByType<WaterRising>();
        }
    }
    //Physical forse always apply in FixedUpdate
    private void FixedUpdate(){
        if (isInsideWater){
            // ФИКС АРХИТЕКТУРЫ: Расчет точки плавучести для бочек и игрока
            float waterSurfaceY = (WaterScript != null) ? WaterScript.SurfaceY : 0f;
            float immersionDepth = waterSurfaceY - GetObjectBottom();
            if (immersionDepth > 0){
                // Применение силы Архимеда
                float dynamicBuoyancy = Mathf.Clamp(buoyancyForce * massMultiplier * immersionDepth, 0f, maxBuoyancyLimit);
                rb.AddForce(Vector3.up * dynamicBuoyancy, ForceMode.Acceleration);
            }
        }
    }
    // This method works ONCE at the trigger water touch
    private void OnTriggerEnter(Collider other){
        // We check that the object we flew into has the tag "Water"
        if (other.CompareTag("Water")){
            isInsideWater = true;
            // Making the physics engine use WATER resistance
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
        }
    }
    // This method works ONCE at the moment of complete exit from the water
    private void OnTriggerExit(Collider other){
        if (other.CompareTag("Water")){
            isInsideWater = false;
            // We return the standard AIR resistance, which was remembered in Start()
            rb.linearDamping = originalDrag;
            rb.angularDamping = originalAngularDrag;
            
        }
    }
}