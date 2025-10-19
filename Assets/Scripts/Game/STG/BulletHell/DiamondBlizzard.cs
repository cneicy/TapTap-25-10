using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "DiamondBlizzard", menuName = "BulletPatterns/Cirno/DiamondBlizzard")]
    public class DiamondBlizzard : BulletPattern
    {
        [Header("钻石暴风雪 - Diamond Blizzard")]
        [Tooltip("生成真正的钻石/菱形图案的弹幕")]
        public GameObject bulletPrefab;
        
        [Header("钻石参数")]
        public int diamondCount = 3;          // 钻石数量
        public float diamondWidth = 3f;       // 钻石宽度
        public float diamondHeight = 4f;      // 钻石高度
        public int bulletsPerEdge = 8;        // 每条边的子弹数
        
        [Header("发射参数")]
        public float bulletSpeed = 2.5f;
        public float spawnInterval = 0.6f;    // 每个钻石生成间隔
        public Vector2 moveDirection = new(0, -1); // 钻石移动方向
        public float rotationSpeed = 30f;     // 钻石旋转速度（度/秒）

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            for (var d = 0; d < diamondCount; d++)
            {
                // 每个钻石稍微偏移位置
                var offsetX = (d % 2 == 0) ? -1f : 1f;
                var spawnPos = origin.position + new Vector3(offsetX, 0, 0);
                
                // 计算旋转角度
                var rotation = d * rotationSpeed;
                
                // 生成钻石形状
                SpawnDiamond(spawnPos, rotation);
                
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        /// <summary>
        /// 生成一个钻石形状的弹幕
        /// </summary>
        private void SpawnDiamond(Vector3 center, float rotationDegrees)
        {
            // 钻石的四个顶点（菱形）
            var vertices = new Vector2[4]
            {
                new(0, diamondHeight / 2f),           // 上顶点
                new(diamondWidth / 2f, 0),            // 右顶点
                new(0, -diamondHeight / 2f),          // 下顶点
                new(-diamondWidth / 2f, 0)            // 左顶点
            };

            // 应用旋转
            var rad = rotationDegrees * Mathf.Deg2Rad;
            for (var i = 0; i < vertices.Length; i++)
            {
                var x = vertices[i].x * Mathf.Cos(rad) - vertices[i].y * Mathf.Sin(rad);
                var y = vertices[i].x * Mathf.Sin(rad) + vertices[i].y * Mathf.Cos(rad);
                vertices[i] = new Vector2(x, y);
            }

            // 沿着钻石的四条边生成子弹
            for (var edge = 0; edge < 4; edge++)
            {
                var start = vertices[edge];
                var end = vertices[(edge + 1) % 4]; // 下一个顶点

                // 在这条边上均匀分布子弹
                for (var i = 0; i < bulletsPerEdge; i++)
                {
                    var t = i / (float)(bulletsPerEdge - 1);
                    var position = Vector2.Lerp(start, end, t);
                    var worldPos = center + (Vector3)position;

                    var bullet = Instantiate(bulletPrefab, worldPos, Quaternion.identity);
                    if (bullet.TryGetComponent<Bullet>(out var b))
                    {
                        b.Init(moveDirection, bulletSpeed);
                    }
                }
            }
        }
    }
}