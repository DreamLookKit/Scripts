using UnityEngine;

// Слово partial говорит Unity: "Это вторая половинка класса PlayerController"
public partial class PlayerController
{
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
}

