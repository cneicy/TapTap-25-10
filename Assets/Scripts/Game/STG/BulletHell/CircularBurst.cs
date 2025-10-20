using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "CircularBurst", menuName = "BulletPatterns/Cirno/CircularBurst")]
    public class CircularBurst : BulletPattern
    {
        [Header("环形爆发 - Circular Burst")]
        [Tooltip("规整的圆形弹幕，像冰晶一样")]
        public GameObject bulletPrefab;
        public int bulletsPerRing = 12;       // 每圈子弹数（建议12或16）
        public int ringCount = 3;             // 总圈数
        public float bulletSpeed = 3f;
        public float speedIncrement = 0.8f;   // 每圈速度递增
        public float ringInterval = 0.25f;    // 圈间隔

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            for (var ring = 0; ring < ringCount; ring++)
            {
                SoundManager.Instance.Play("bossshoot2");
                var angleStep = 360f / bulletsPerRing;
                var speed = bulletSpeed + speedIncrement * ring;

                for (var i = 0; i < bulletsPerRing; i++)
                {
                    var angle = angleStep * i * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    var bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
                    if (bullet.TryGetComponent<Bullet>(out var b))
                    {
                        b.Init(direction, speed);
                    }
                }

                yield return new WaitForSeconds(ringInterval);
            }
        }
    }
}