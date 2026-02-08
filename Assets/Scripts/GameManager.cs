using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글턴 아닌데 싱글턴임
    public static GameManager Instance;

    private void Awake()
    {
        Instance = this;
    }
    
    public int GetMonsters = 0;
    private int ToNext = 5;
    public int CurrentStage = 1;

    public void CatchMonster()
    {
        GetMonsters++;
        Debug.Log($"현재 몬스터{GetMonsters} 마리 잡음");
        if (GetMonsters >= ToNext)
        {
            if (CurrentStage == 3)
            {
                EndGame();
            }

            Debug.Log($"다음 레벨로..");
            GetMonsters = 0;
            CurrentStage++;
        }
    }

    private void EndGame()
    {
        
    }
}
