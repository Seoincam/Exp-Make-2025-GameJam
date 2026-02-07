using System.Linq;
using UnityEngine;

public sealed class MonsterAnimationHub : MonoBehaviour
{
    [Tooltip("명시하지 않으면 자식들의 AnimationPlayerBase를 자동 수집합니다.")]
    [SerializeField] private AnimationPlayerBase[] players;

    void Awake()
    {
        if (players == null || players.Length == 0)
            players = GetComponentsInChildren<AnimationPlayerBase>(true);
    }

    /// 모든 상태 Enter에서 이 한 줄만 호출하면 됩니다.
    public void SetTag(MonsterStateTag tag, MonsterContext ctx)
    {
        if (players == null) return;

        // 활성 & 호환되는 플레이어들에게 전달
        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (!p) continue;
            if (!p.isActiveAndEnabled) continue;
            if (!p.IsActiveFor(ctx)) continue;

            p.SetTag(tag, ctx);
        }
    }
}