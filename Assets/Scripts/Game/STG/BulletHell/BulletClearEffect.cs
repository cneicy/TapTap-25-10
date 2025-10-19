using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    public class BulletClearEffect : MonoBehaviour
    {
        [Header("清除效果设置")]
        [Tooltip("清除时子弹是否向上飘")]
        public bool floatUp = true;
        
        [Tooltip("飘起速度")]
        public float floatSpeed = 2f;
        
        [Tooltip("淡出时间")]
        public float fadeOutDuration = 0.3f;
        
        [Tooltip("清除时的颜色")]
        public Color clearColor = new(1f, 1f, 0.5f);

        public static void ClearBullet(GameObject bullet, bool withEffect = true)
        {
            if (!bullet) return;

            if (withEffect)
            {
                var effect = bullet.AddComponent<BulletClearEffect>();
                effect.StartClear();
            }
            else
            {
                Destroy(bullet);
            }
        }

        public static int ClearAllBullets(bool withEffect = true)
        {
            var bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
            
            foreach (var bullet in bullets)
            {
                ClearBullet(bullet.gameObject, withEffect);
            }
            
            return bullets.Length;
        }

        private void StartClear()
        {
            StartCoroutine(ClearAnimation());
        }

        private IEnumerator ClearAnimation()
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb)
            {
                if (floatUp)
                {
                    rb.linearVelocity = Vector2.up * floatSpeed;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }

            var sr = GetComponent<SpriteRenderer>();
            if (sr)
            {
                var startColor = sr.color;
                var targetColor = clearColor;
                targetColor.a = 0f;

                var elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.deltaTime;
                    var t = elapsed / fadeOutDuration;
                    
                    sr.color = Color.Lerp(startColor, targetColor, t);
                    
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(fadeOutDuration);
            }

            Destroy(gameObject);
        }
    }
}