namespace Shared.Stat
{
    public class LifeTimeHandler : IEffectHandler
    {
        private float _lifeTime;
        
        public LifeTimeHandler(float lifeTime)
        {
            _lifeTime = lifeTime;    
        }
        
        public void OnStart(EffectContext context)
        {
        }

        public void Tick(EffectContext context, float deltaTime)
        {
            _lifeTime -= deltaTime;
            if (_lifeTime <= 0)
            {
                context.RequestEnd();
            }
        }

        public void OnEnd(EffectContext context)
        {
        }
    }
}