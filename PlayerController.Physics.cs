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
        return new Vector3(transform.position.x, transform.position.y - (capsuleCollider.height / 2f) + 0.1f, transform.position.z);
    }
}

