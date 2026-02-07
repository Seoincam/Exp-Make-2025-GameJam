using UnityEngine;

public static class IDamagableFeedbackExtensions
{
    private static bool _warnedMissingPool;

    public static void ShowDamagePopup(this IDamagable damageable, int appliedDamage)
    {
        if (appliedDamage <= 0)
        {
            return;
        }

        if (damageable is not Component component)
        {
            return;
        }

        bool spawned = DamagePopupPool.TrySpawn(appliedDamage, component.transform.position);
        if (!spawned && !_warnedMissingPool)
        {
            _warnedMissingPool = true;
            Debug.LogWarning("DamagePopupPool is missing or popupPrefab is not assigned. Add DamagePopupPool to a scene object and assign a popup prefab.");
        }
    }
}
