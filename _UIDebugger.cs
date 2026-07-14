using UnityEngine;
using TMPro;

public class _UIDebugger : MonoBehaviour{
    [Header("Links")]
    private Transform playerTransform; // Убираем SerializeField, теперь ищем кодом
    [SerializeField] private TextMeshProUGUI debugText;
    private Vector3 lastPosition;
    private float horizontalSpeed;
    private float verticalSpeed;
    private bool playerFound = false;
    void Update(){
        if (debugText == null) return;
        // Если игрок еще не найден (или раунд перезапустился и старый игрок удален)
        if (playerTransform == null)
        {
            playerFound = false;
            // Ищем на сцене живой объект игрока по его тегу
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null){
                playerTransform = playerObj.transform;
                lastPosition = playerTransform.position;
                playerFound = true;
            }
        }
        // Как только игрок найден — считаем его скорость
        Vector3 currentPosition = playerTransform.position;
        Vector3 displacement = currentPosition - lastPosition;
        float deltaTime = Time.deltaTime;
        if (deltaTime > 0)
        {
            horizontalSpeed = new Vector3(displacement.x, 0f, displacement.z).magnitude / deltaTime;
            verticalSpeed = displacement.y / deltaTime;
        }
        lastPosition = currentPosition;
        // Выводим данные на UI в реальном времени
        debugText.text = $"Speed movement (X/Z): {horizontalSpeed:F2}\n" +
                         $"Speed falling (Y): {verticalSpeed:F2}";
    }
}
