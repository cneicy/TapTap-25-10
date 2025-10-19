using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "PerfectFreeze", menuName = "BulletPatterns/Cirno/PerfectFreeze")]
    public class PerfectFreeze : BulletPattern
    {
        [Header("完美冻结 - Perfect Freeze")]
        [Tooltip("琪露诺的招牌符卡！")]
        public GameObject bulletPrefab;
        public int ringLayers = 6;            // 冻结环层数
        public int bulletsPerLayer = 16;      // 每层子弹数
        public float layerInterval = 0.25f;   // 层间隔
        public float initialSpeed = 1f;       // 初始速度
        public float speedIncrement = 0.5f;   // 每层速度增量
        public float rotationPerLayer = 22.5f; // 每层旋转角度

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            // 显示符卡名称（可选）
            Debug.Log("★ 冷符「Perfect Freeze」★");

            var currentRotation = 0f;

            for (var layer = 0; layer < ringLayers; layer++)
            {
                var angleStep = 360f / bulletsPerLayer;
                var speed = initialSpeed + speedIncrement * layer;

                // 创建一个冻结环
                for (var i = 0; i < bulletsPerLayer; i++)
                {
                    var angle = (angleStep * i + currentRotation) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    var bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
                    
                    if (bullet.TryGetComponent<Bullet>(out var b))
                    {
                        b.Init(direction, speed);
                    }
                }

                currentRotation += rotationPerLayer;
                yield return new WaitForSeconds(layerInterval);
            }

            // 最后一击 - 密集弹幕
            yield return new WaitForSeconds(0.3f);

            for (var i = 0; i < bulletsPerLayer * 2; i++)
            {
                var angle = (360f / (bulletsPerLayer * 2)) * i * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                var bullet = Instantiate(bulletPrefab, origin.position, Quaternion.identity);
                
                if (bullet.TryGetComponent<Bullet>(out var b))
                {
                    b.Init(direction, initialSpeed + speedIncrement * ringLayers);
                }
            }
        }
    }
}