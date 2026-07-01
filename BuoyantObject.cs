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
        // We are trying to quickly locate the player controller on this same object
        bool isPlayer = pc != null && pc.PlayerCollider != null;
        if (isInsideWater && rb.mass < 100f && !isPlayer){
            // 1. Dynamically calculate the displacement of the center depending on the mass of the object.
            // The heavier the object, the closer the point is to the center (0f). The lighter the closer to the bottom (-0.5f).
            // The Mathf.Clamp function limits the values ​​so that the point does not fly below the bottom or above the center.
            float massFactor = Mathf.Clamp(rb.mass * 0.1f, 0f, 0.9f);
            float dynamicOffset = -0.5f + massFactor;   //point of buoyancy (плавучесть)
            // 2. Find the world Y-coordinate of this dynamic point
            float objectY = transform.position.y + (transform.localScale.y * dynamicOffset);
            // 3. We take the real Y of the water surface from the flood script (исправлено на заглавную букву)
            float waterSurfaceY = (WaterScript != null) ? WaterScript.SurfaceY : 0f;
            // 4. We calculate the immersion depth of our dynamic point
            float immersionDepth = waterSurfaceY - objectY;
            // 5. If the point goes under water - application of the Archimedes force
            if (immersionDepth > 0){
                // Multiply the force by rb.mass so that physics takes into account the weight of the object, 
                // and add ForceMode.Acceleration for smoothness
                float dynamicBuoyancy = buoyancyForce * Mathf.Max(rb.mass, 2f) * immersionDepth;
                // Fix extremal pushed objects
                dynamicBuoyancy = Mathf.Clamp(dynamicBuoyancy, 0f, maxBuoyancyLimit);
                // Push out the object
                rb.AddForce(Vector3.up * dynamicBuoyancy, ForceMode.Acceleration);

                // 3. SIDE-ENTRY FIX: Additionally dampen vertical velocity in water (fluid viscosity effect)
                // The faster the object tries to move up or down in the water, the more strongly the water slows it down.
                float dampForce = -rb.linearVelocity.y * waterDrag;
                rb.AddForce(Vector3.up * dampForce, ForceMode.Acceleration);
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