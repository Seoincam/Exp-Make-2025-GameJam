namespace Shared.Stat
{
    public enum EffectHandlerType
    {
        /// <summary>
        /// 영구적 적용.
        /// Base Value를 수정합니다.
        /// </summary>
        Permanent,
        
        /// <summary>
        /// 일시적 적용.
        /// Final Value를 수정하고, 이펙트 종료 시 적용 해제됩니다.
        /// </summary>
        Temporary
    }
    
    public interface IEffectHandler
    {
        void OnStart(EffectContext context);
        void Tick(EffectContext context, float deltaTime);
        void OnEnd(EffectContext context);
    }
}