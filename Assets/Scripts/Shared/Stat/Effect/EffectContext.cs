using UnityEngine;

namespace Shared.Stat
{
    public class EffectContext
    {
        public EffectManager Manager { get; }
        public Stat Stat { get; }
        public bool EndRequested { get; private set; }
        
        public EffectContext(EffectManager manager, Stat stat)
        {
            Manager = manager;
            Stat = stat;
        }

        public void RequestEnd()
        {
            Debug.Log("RequestEnd");
            EndRequested = true;
        }
    }
}