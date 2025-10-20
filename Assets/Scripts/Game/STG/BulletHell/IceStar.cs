using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [CreateAssetMenu(fileName = "IceStar", menuName = "BulletPatterns/Cirno/IceStar")]
    public class IceStar : BulletPattern
    {
        [Header("冰星 - Ice Star")]
        [Tooltip("五角星或六角星形状的弹幕")]
        public GameObject bulletPrefab;
        public int starPoints = 5;            // 星形角数（5或6）
        public int bulletsPerLine = 8;        // 每条线的子弹数
        public float lineLength = 4f;         // 线长度
        public float bulletSpeed = 3.5f;
        public int repeatCount = 3;           // 重复次数
        public float repeatDelay = 0.4f;      // 重复延迟
        public float rotationPerRepeat = 36f; // 每次旋转角度

        public override IEnumerator PlayPattern(Transform origin, Transform player = null)
        {
            var currentRotation = 0f;

            for (var repeat = 0; repeat < repeatCount; repeat++)
            {
                SoundManager.Instance.Play("bossshoot1");
                var angleStep = 360f / starPoints;

                for (var point = 0; point < starPoints; point++)
                {
                    var angle = (angleStep * point + currentRotation) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    // 沿着每个方向发射一条线
                    for (var i = 0; i < bulletsPerLine; i++)
                    {
                        var distance = (lineLength / bulletsPerLine) * (i + 1);
                        var spawnPos = origin.position + (Vector3)direction * distance;

                        var bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
                        if (bullet.TryGetComponent<Bullet>(out var b))
                        {
                            b.Init(direction, bulletSpeed);
                        }
                    }
                }

                currentRotation += rotationPerRepeat;
                yield return new WaitForSeconds(repeatDelay);
            }
        }
    }
}