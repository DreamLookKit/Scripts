using UnityEngine;

public class WaterRising : MonoBehaviour{
    [SerializeField] private float modificationSpeed = 0.5f; // How fast the volume grows
    [SerializeField] private float maxScaleY = 5.0f; // Max water thickness/height
    [SerializeField] private float minScaleY = 1.0f; // Min water thickness/height
    [SerializeField] private float declineSpeedMultiplier = 3f; // Multiplier decline water level
    // This property returns the exact top point of the water object in the world.
    public float SurfaceY => transform.position.y + (transform.localScale.y / 2f);
    // Public property for safety change maxScaleY from other scripts
    public float MaxScaleY 
    {
        get => maxScaleY;
        set => maxScaleY = Mathf.Max(minScaleY, value); // Blocking change max less min
    }
    void FixedUpdate(){
        // Calculate how much the scale should changed in this frame
        float scaleChange = modificationSpeed * Time.fixedDeltaTime;
        // Safe game phase check via a singleton
        if (GameManager.Instance != null){
            switch (GameManager.Instance.currentState){
                case GameManager.GameState.Flood:
                    GrowWaterVolume(scaleChange);
                    break;
                case GameManager.GameState.RoundEnd:
                    DeclineWaterVolume(scaleChange);
                    break;
            }
        }
        
    }
    private void GrowWaterVolume(float scaleChange){
        // Checking if the water thickness is still below the max limit
        if(transform.localScale.y < maxScaleY){
            // 1. Calculate how much the scale shoulkd increase in this frame
            // float scaleChange = modificationSpeed * Time.deltaTime;
            // 2. Increase the scale along the Y axis (streaching the volume)
            transform.localScale += new Vector3(0, scaleChange, 0);
            // 3. Move the object up by half of the scale change.
            // This anchors the bottom of the cube and makes it grow only upwards.
            transform.position += new Vector3(0, scaleChange / 2f, 0);
        }
    }
    private void DeclineWaterVolume(float scaleChange){
        // Checking if the water thickness is still upward the min limit
        if(transform.localScale.y > minScaleY){
            // 1. Calculate how much the scale shoulkd decrease in this frame
            // float scaleChange = modificationSpeed * Time.deltaTime;
            // 2. Decrease the scale along the Y axis (narrowing the volume)
            transform.localScale -= new Vector3(0, scaleChange * declineSpeedMultiplier, 0);
            // 3. Move the object up by half of the scale change.
            // This anchors the bottom of the cube and makes it decline only downwards.
            transform.position -= new Vector3(0, (scaleChange * declineSpeedMultiplier) / 2f, 0);
            // If water scale accidentaly fall down below min level, fixate him tough
            if(transform.localScale.y < minScaleY)
            transform.localScale = new Vector3(transform.localScale.x, minScaleY, transform.localScale.z);
        }
    }
}

