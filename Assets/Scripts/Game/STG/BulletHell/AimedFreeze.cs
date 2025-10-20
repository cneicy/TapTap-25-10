using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "AimedFreeze", menuName = "BulletPatterns/Cirno/AimedFreeze")]
    public class AimedFreeze : BulletPattern
    {
        [Header("瞄准冰冻 - Aimed Freeze")]
        public GameObject bulletPrefab;
        public int shotCount = 8;             // 总射击次数
        public int bulletsPerShot = 5;        // 每次发射的子弹数
        public float spreadAngle = 30f;       // 扩散角度
        public float bulletSpeed = 5f;
        public float shotInterval = 0.5f;     // 射击间隔

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            for (var shot = 0; shot < shotCount; shot++)
            {
                if (player)
                {
                    Vector2 toPlayer = (player.position - origin.position).normalized;
                    var baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
                    SoundManager.Instance.Play("bossshoot1");
                    for (var i = 0; i < bulletsPerShot; i++)
                    {
                        var offset = (i - (bulletsPerShot - 1) / 2f) * (spreadAngle * Mathf.Deg2Rad / bulletsPerShot);
                        var angle = baseAngle + offset;
                        var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                        var bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
                        
                        if (bullet.TryGetComponent<Bullet>(out var b))
                        {
                            b.Init(direction, bulletSpeed);
                        }
                    }
                }

                yield return new WaitForSeconds(shotInterval);
            }
        }
    }
}