using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "SimpleSpread", menuName = "BulletPatterns/Cirno/SimpleSpread")]
    public class SimpleSpread : BulletPattern
    {
        [Header("简单扇形射击 - Simple Spread")]
        [Tooltip("适合第一阶段的简单弹幕")]
        public GameObject bulletPrefab;
        public int shotsCount = 5;            // 射击次数
        public int bulletsPerShot = 7;        // 每次子弹数
        public float spreadAngle = 60f;       // 扇形角度
        public float bulletSpeed = 3.5f;
        public float shotInterval = 0.6f;     // 射击间隔
        public bool aimAtPlayer;      // 是否朝向玩家

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            for (var shot = 0; shot < shotsCount; shot++)
            {
                float baseAngle;
                SoundManager.Instance.Play("bossshoot2");
                if (aimAtPlayer && player != null)
                {
                    // 朝向玩家
                    Vector2 toPlayer = (player.position - origin.position).normalized;
                    baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
                }
                else
                {
                    // 朝向正下方（270度）
                    baseAngle = 270f * Mathf.Deg2Rad;
                }

                // 计算扇形弹幕
                var startAngle = baseAngle - (spreadAngle * Mathf.Deg2Rad / 2f);
                var angleStep = (spreadAngle * Mathf.Deg2Rad) / (bulletsPerShot - 1);

                for (var i = 0; i < bulletsPerShot; i++)
                {
                    var angle = startAngle + angleStep * i;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    var bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
                    
                    if (bullet.TryGetComponent<Bullet>(out var b))
                    {
                        b.Init(direction, bulletSpeed);
                    }
                }

                yield return new WaitForSeconds(shotInterval);
            }
        }
    }
}