using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "SpiralIce", menuName = "BulletPatterns/Cirno/SpiralIce")]
    public class SpiralIce : BulletPattern
    {
        [Header("螺旋冰弹 - Spiral Ice")]
        public GameObject bulletPrefab;
        public int armsCount = 3;             // 螺旋臂数量
        public float rotationSpeed = 60f;     // 旋转速度（度/秒）
        public float bulletSpeed = 3f;
        public float duration = 4f;           // 持续时间
        public float fireInterval = 0.08f;    // 发射间隔

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            var elapsed = 0f;
            var currentAngle = 0f;

            while (elapsed < duration)
            {
                var angleStep = 360f / armsCount;
                SoundManager.Instance.Play("bossshoot2");
                for (var i = 0; i < armsCount; i++)
                {
                    var angle = (currentAngle + angleStep * i) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    var bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
                    
                    if (bullet.TryGetComponent<Bullet>(out var b))
                    {
                        b.Init(direction, bulletSpeed);
                    }
                }

                currentAngle += rotationSpeed * fireInterval;
                elapsed += fireInterval;
                yield return new WaitForSeconds(fireInterval);
            }
        }
    }
}