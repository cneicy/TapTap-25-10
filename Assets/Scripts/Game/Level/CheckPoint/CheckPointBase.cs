using System.Collections;
using System.Collections.Generic;
using Data;
using Game.Meta;
using ScreenEffect;
using ShrinkEventBus;
using UnityEngine;
using PlayerController = Game.Player.PlayerController;

namespace Game.Level.CheckPoint
{
    public enum HitBy
    {
        Player, Ammo
    }

    public class TouchCheckPointEvent : EventBase
    {
        public HitBy HitBy { get; }
        public CheckPointBase CheckPointBase { get; }

        public TouchCheckPointEvent(HitBy hitBy, CheckPointBase checkPointBase)
        {
            HitBy = hitBy;
            CheckPointBase = checkPointBase;
        }
    }

    public abstract class CheckPointBase : MonoBehaviour
    {
        private static readonly int Save = Animator.StringToHash("Save");
        private static readonly int Kick = Animator.StringToHash("Kick");
        public bool IsSpecial { get; set; }

        [SerializeField] private float triggerCooldown = 2f;
        private float _lastTriggerTime = -999f;
        private Animator _animator;

        private readonly List<float> _saveTimestamps = new();
        private AudioSource _audioSource;
        private CapsuleCollider2D _capsuleCollider2D;

        public void Start()
        {
            _capsuleCollider2D = FindAnyObjectByType<PlayerController>().GetComponent<CapsuleCollider2D>();
            _audioSource = GetComponent<AudioSource>();
            _animator = GetComponent<Animator>();
        }

        public virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time - _lastTriggerTime < triggerCooldown)
                return;

            var triggered = false;

            if (other.CompareTag("Player"))
            {
                EventBus.TriggerEvent(new TouchCheckPointEvent(HitBy.Player, this));
                GetComponent<AudioSource>().Play();
                triggered = true;
            }
            else if (other.CompareTag("Ammo") || other.name.Contains("Bullet"))
            {
                if (!IsSpecial)
                {
                    EventBus.TriggerEvent(new TouchCheckPointEvent(HitBy.Ammo, this));
                    GetComponent<AudioSource>().Play();
                    triggered = true;
                }
            }

            if (!triggered)
                return;

            _lastTriggerTime = Time.time;
            _animator.SetTrigger(Save);

            DataManager.Instance.ForceSave();

            RecordSaveTime();
        }

        private void RecordSaveTime()
        {
            var now = Time.time;
            _saveTimestamps.Add(now);

            _saveTimestamps.RemoveAll(t => now - t > 15f);

            if (_saveTimestamps.Count < 5 || !IsSpecial) return;
            
            _saveTimestamps.Clear();
            StartCoroutine(KickPlayer());
        }
        public IEnumerator KickPlayer()
        {
            _animator.SetTrigger(Kick);
            _audioSource.Play();
            print("KickPlayer");
            yield return new WaitForSeconds(0.8f);
            _capsuleCollider2D.enabled = false;
            yield return new WaitForSeconds(1f);
            Camera.main.transform.parent = FindAnyObjectByType<PlayerController>().gameObject.transform;
            Camera.main.transform.localPosition = new Vector3(0, 0, -10);
            MetaAudioManager.Instance.Play("Wind");
            yield return new WaitForSeconds(2f);
            MetaAudioManager.Instance.Play("FallToSTG");
            yield return new WaitForSeconds(15f);
            RectTransitionController.Instance.StartTransition();
            yield return new WaitForSeconds(0.25f);
            LevelManager.Instance.SwitchLevel("Level_STG");
        }

        public void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
