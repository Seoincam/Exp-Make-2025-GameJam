using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Player;
using Shared.Stat;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class LevelUpManager : MonoBehaviour
    {
        [SerializeField] private List<Button> buttons = new();
        
        private PlayerCharacter Player => PlayerCharacter.Current;

        private void Awake()
        {
            InitializeAsync().Forget();

            if (buttons.Count != 3)
            {
                Debug.LogWarning("3개의 버튼이 존재하지 않습니다.");
                return;
            }

            buttons[0].onClick.AddListener(OnClickAttackUp);
            buttons[1].onClick.AddListener(OnClickAttackSpeedUp);
            buttons[2].onClick.AddListener(OnClickMoveSpeedUp);
            
            gameObject.SetActive(false);
        }

        private async UniTask InitializeAsync()
        {
            await UniTask.WaitUntil(() => Player);

            Player.Stat.StatChanged += OnStatChanged;
        }

        private void OnStatChanged(in Stat.StatChangedEventArgs args)
        {
            if (args.Type == StatType.Exp && !Mathf.Approximately(args.OldFinalValue, args.NewFinalValue))
            {
                if ((int)args.NewFinalValue % 5 == 0)
                {
                    ShowUI();
                }
            }
        }

        private void ShowUI()
        {
            gameObject.SetActive(true);
            Time.timeScale = 0;
        }

        private void HideUI()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1;
        }

        private void OnClickAttackUp()
        {
            var effectSpec = Effect.CreateSpec(EffectType.Test)
                .AddHandler(new InstantStatHandler(StatType.Damage, 1f));
            Player.EffectManager.AddEffect(effectSpec);
            HideUI();
        }

        private void OnClickAttackSpeedUp()
        {
            var effectSpec = Effect.CreateSpec(EffectType.Test)
                .AddHandler(new InstantStatHandler(StatType.FireInterval, -0.25f));
            Player.EffectManager.AddEffect(effectSpec);
            HideUI();
        }

        private void OnClickMoveSpeedUp()
        {
            var effectSpec = Effect.CreateSpec(EffectType.Test)
                .AddHandler(new InstantStatHandler(StatType.MoveSpeed, 1f));
            Player.EffectManager.AddEffect(effectSpec);
            HideUI();
        }
    }
}