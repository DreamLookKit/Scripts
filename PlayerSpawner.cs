using UnityEngine;

public class PlayerSpawner : MonoBehaviour{
    [Header("Spawn settings")]
    [SerializeField] private GameObject playerPrefab;  // Prefab your player
    [SerializeField] private Transform spawnPoint;     // Spawn your player
    private GameObject currentInstance;
    private void Awake(){
        SpawnPlayer();  // Spawn player when scene are appear
    }
    public void SpawnPlayer(){
        // We check whether we forgot to drag the objects in the Inspector
        if(playerPrefab == null || spawnPoint == null){
            Debug.LogError("Player: Prefab or SpawnPoint not assigned in Spawner!");
            return;
        }
        // If a copy of the player was already in the scene (for future respawning), remove the old one
        if(currentInstance != null)
            Destroy(currentInstance);
        // Create the player from the prefab at the connection point with the required rotation
        currentInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("Player has appear");
    }
}
