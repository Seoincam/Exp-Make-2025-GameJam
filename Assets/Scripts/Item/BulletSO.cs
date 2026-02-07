using UnityEngine;

[CreateAssetMenu(fileName = "BulletSO", menuName = "Player/Bullet SO")]
public class BulletSO : ScriptableObject
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletCount = 10;

    public GameObject BulletPrefab => bulletPrefab;
    public int BulletCount => Mathf.Max(0, bulletCount);
}
