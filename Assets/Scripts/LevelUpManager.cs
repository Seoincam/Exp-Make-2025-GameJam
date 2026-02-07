using Cysharp.Threading.Tasks;
using Player;
using Shared.Stat;
using UnityEngine;

namespace DefaultNamespace
{
    public class LevelUpManager : MonoBehaviour
    {
        private PlayerCharacter Player => PlayerCharacter.Current;
        
        private void Awake()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => Player);

            Player.Stat.StatChanged += OnStatChanged;
        }

        private void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type == StatType.Exp)
            {
                if ((int)args.NewFinalValue % 10 == 0)
                {
                    Debug.Log("10ÀÇ ¹è¼ö");
                    ShowUI();
                }
            }
        }

        private void ShowUI()
        {
            gameObject.SetActive(true);
        }

        private void HideUI()
        {
            gameObject.SetActive(false);
        }
    }
}