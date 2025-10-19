using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    public class CirnoEnemyShooter : MonoBehaviour
    {
        [Header("琪露诺弹幕阶段设置")]
        public BulletPattern[] phase1Patterns;
        public BulletPattern[] phase2Patterns;
        public BulletPattern[] phase3Patterns;

        [Header("玩家引用")]
        public Transform player;

        [Header("弹幕参数")]
        public float restTimeBetweenPatterns = 1.5f;
        public bool randomizePatterns = true;

        private int _currentPhase = 1;
        private Coroutine _shootRoutine;
        private Coroutine _currentPatternRoutine;
        private bool _isShooting;

        private void Start()
        {
            ResumeShooting();
        }

        public void SwitchPhase(int phase)
        {
            if (phase < 1 || phase > 3)
            {
                Debug.LogWarning($"[CirnoShooter] 无效的阶段: {phase}");
                return;
            }

            _currentPhase = phase;
            Debug.Log($"[CirnoShooter] 切换至第 {phase} 阶段！");
        }

        public void StopShooting()
        {
            if (_shootRoutine != null)
            {
                StopCoroutine(_shootRoutine);
                _shootRoutine = null;
            }
            
            if (_currentPatternRoutine != null)
            {
                StopCoroutine(_currentPatternRoutine);
                _currentPatternRoutine = null;
            }
            
            _isShooting = false;
            Debug.Log("[CirnoShooter] 弹幕停止（包括当前播放的弹幕）。");
        }

        public void ResumeShooting()
        {
            if (!_isShooting)
            {
                _isShooting = true;
                _shootRoutine = StartCoroutine(RunPatterns());
                Debug.Log("[CirnoShooter] 弹幕恢复。");
            }
        }

        private IEnumerator RunPatterns()
        {
            while (_isShooting)
            {
                var currentList = GetCurrentPhasePatterns();

                if (currentList == null || currentList.Length == 0)
                {
                    Debug.LogWarning($"[CirnoShooter] 第 {_currentPhase} 阶段没有弹幕！");
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                var pattern = randomizePatterns 
                    ? currentList[Random.Range(0, currentList.Length)]
                    : currentList[(int)(Time.time / 5f) % currentList.Length];

                Debug.Log($"[CirnoShooter] ▶ 播放弹幕：{pattern.name}（阶段 {_currentPhase}）");
                
                _currentPatternRoutine = StartCoroutine(pattern.PlayPattern(transform, player));
                yield return _currentPatternRoutine;
                _currentPatternRoutine = null;
                
                yield return new WaitForSeconds(restTimeBetweenPatterns);
            }
        }

        private BulletPattern[] GetCurrentPhasePatterns()
        {
            return _currentPhase switch
            {
                1 => phase1Patterns,
                2 => phase2Patterns,
                3 => phase3Patterns,
                _ => phase1Patterns
            };
        }
    }
}