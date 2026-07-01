using UnityEngine;

public class GameManager : MonoBehaviour{
    // Singleton instance
    public static GameManager Instance {get; private set;}
    // List of Game Phases
    public enum GameState{
        Building, // Phase 1: Build a boat (Ex: 2 min)
        Flood,    // Phase 2: Flood the area
        Battle,   // Phase 3: Battle with bots
        RoundEnd  // Phase 4: Add up score and cleanup the area
    }
    // Current game state
    public GameState currentState = GameState.Building;
    public float timer = 10f; // 10 sec (for test) timer for building boat
    void Awake(){
        //Custom singlton: that object will be avaliable for all
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else
            Destroy(gameObject);
    }
    void Update(){
        // Method Update call by each frame. Decrease timer
        if (timer > 0){
            timer -= Time.deltaTime; // Time.deltaTime - its a delay between frames
        }
        else
            AdvancePhase(); // Timer was ended, move to next phase
    }

    void AdvancePhase(){
        switch (currentState){
            case GameState.Building:
                currentState = GameState.Flood;
                timer = 30f; // 30 (for test) sec to flood
                StartFlooding();
                break;
            case GameState.Flood:
                currentState = GameState.Battle;
                timer = 99999f; // 99999 (for test) sec to battle with bots
                StartBattle();
                break;
            case GameState.Battle:
                currentState = GameState.RoundEnd;
                timer = 20f; // 20 sec to add up score and cleanup the area
                EndRound();
                break;      
        }
    }
    void StartFlooding(){
        Debug.Log("Water is rising!"); 
    }
    void StartBattle(){
        Debug.Log("Battle started!"); 
    }
    void EndRound(){
        Debug.Log("Round ended! Cleaning up..."); 
    }
}
