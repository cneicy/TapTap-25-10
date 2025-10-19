using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "IcicleFall", menuName = "BulletPatterns/Cirno/IcicleFall")]
    public class IcicleFall : BulletPattern
    {
        [Header("冰柱落下 - Icicle Fall")]
        [Tooltip("经典的三排冰柱弹幕")]
        public GameObject bulletPrefab;
        public int columns = 8;               // 列数
        public int rows = 3;                  // 排数
        public float columnSpacing = 1.2f;    // 列间距
        public float rowDelay = 0.15f;        // 排延迟
        public float bulletSpeed = 4f;
        public float waveCount = 2;           // 波数

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            for (var wave = 0; wave < waveCount; wave++)
            {
                // 每波稍微偏移一下位置
                var offset = (wave % 2 == 0) ? 0f : columnSpacing / 2f;
                
                for (var row = 0; row < rows; row++)
                {
                    var startX = origin.position.x - (columns * columnSpacing / 2f) + offset;
                    
                    for (var col = 0; col < columns; col++)
                    {
                        var spawnPos = new Vector3(
                            startX + col * columnSpacing,
                            origin.position.y,
                            0
                        );
                        
                        var bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                        if (bullet.TryGetComponent<Bullet>(out var b))
                        {
                            b.Init(Vector2.down, bulletSpeed);
                        }
                    }
                    
                    yield return new WaitForSeconds(rowDelay);
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }
    }
}