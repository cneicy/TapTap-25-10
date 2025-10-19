using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    public class BossEntranceController : MonoBehaviour
    {
        [Header("进场设置")]
        [Tooltip("进场起始位置（相对于最终位置）")]
        public Vector2 entranceOffset = new(0, 5f);

        [Tooltip("进场时间")]
        public float entranceDuration = 1.5f;

        [Header("摇晃设置")]
        [Tooltip("摇晃幅度")]
        public float shakeAmplitude = 0.3f;
        
        [Tooltip("摇晃速度")]
        public float shakeSpeed = 8f;
        
        [Tooltip("摇晃次数")]
        public int shakeCount = 2;

        [Header("引用")]
        [Tooltip("Boss战斗控制器")]
        public CirnoBossController bossController;
        
        [Tooltip("弹幕控制器")]
        public CirnoEnemyShooter shooter;

        [Header("特效")]
        public ParticleSystem entranceParticle;
        public bool playParticleOnArrival = true;

        private Vector3 _finalPosition;
        private bool _isEntering = true;

        private void Start()
        {
            _finalPosition = transform.position;
            transform.position = _finalPosition + (Vector3)entranceOffset;

            if (bossController)
                bossController.enabled = false;

            if (shooter)
                shooter.StopShooting();

            StartCoroutine(EntranceSequence());
        }

        private IEnumerator EntranceSequence()
        {
            Debug.Log("[Boss进场] 开始进场动画");

            yield return StartCoroutine(FlyIn());
            yield return StartCoroutine(Shake());

            if (playParticleOnArrival && entranceParticle)
            {
                entranceParticle.Play();
            }

            yield return new WaitForSeconds(0.5f);

            StartBattle();

            Debug.Log("[Boss进场] 战斗开始！");
        }

        private IEnumerator FlyIn()
        {
            var elapsed = 0f;
            var startPos = transform.position;

            while (elapsed < entranceDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / entranceDuration;
                
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                transform.position = Vector3.Lerp(startPos, _finalPosition, t);
                yield return null;
            }

            transform.position = _finalPosition;
        }

        private IEnumerator Shake()
        {
            var shakeDuration = (1f / shakeSpeed) * shakeCount;
            var elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                
                var shake = Mathf.Sin(elapsed * shakeSpeed * Mathf.PI * 2f) * shakeAmplitude;
                
                var dampening = 1f - elapsed / shakeDuration;
                shake *= dampening;
                
                transform.position = _finalPosition + new Vector3(shake, 0, 0);
                yield return null;
            }

            transform.position = _finalPosition;
        }

        private void StartBattle()
        {
            _isEntering = false;

            if (bossController)
            {
                bossController.enabled = true;
            }

            if (shooter)
            {
                shooter.ResumeShooting();
            }

            Destroy(this);
        }
    }
}