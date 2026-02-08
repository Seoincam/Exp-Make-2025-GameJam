using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Project-level singleton
    public static GameManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public int GetMonsters = 0;
    [SerializeField] private int toNext = 5;
    [SerializeField, Min(0)] private int maxStage = 0; // 0 = unlimited
    public int CurrentStage = 1;

    public void CatchMonster()
    {
        GetMonsters++;
        Debug.Log($"Monsters defeated: {GetMonsters}");
        if (GetMonsters >= toNext)
        {
            if (maxStage > 0 && CurrentStage >= maxStage)
            {
                EndGame();
                return;
            }

            Debug.Log("Stage up.");
            GetMonsters = 0;
            CurrentStage++;
        }
    }

    private void EndGame()
    {
    }
}
