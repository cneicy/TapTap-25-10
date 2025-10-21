using System.Collections;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Cup
{
    public class MouseEnterCupEvent : EventBase
    {
        public string Name { get; }
        public string Description { get; }
        public MouseEnterCupEvent(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class MouseExitCupEvent : EventBase {}

    public abstract class CupBase : MonoBehaviour
    {
        public string Name { get; set; }
        public string Description { get; set; }

        private Coroutine _hoverAnimCoroutine;
        private Vector3 _originalScale;
        private Quaternion _originalRotation;
        private bool _isHovered;

        private float _scaleUpFactor = 1.1f;
        private float _rotationAmplitude = 10f;
        private float _rotationSpeed = 3f;
        public Vector2 power;

        protected virtual void Awake()
        {
            _originalScale = transform.localScale;
            _originalRotation = transform.localRotation;
        }

        public virtual void OnMouseEnter()
        {
            EventBus.TriggerEvent(new MouseEnterCupEvent(Name, Description));
            _isHovered = true;

            if (_hoverAnimCoroutine != null)
                StopCoroutine(_hoverAnimCoroutine);

            _hoverAnimCoroutine = StartCoroutine(nameof(HoverAnimation));
        }

        public virtual void Crash()
        {
            GetComponent<Rigidbody2D>().simulated = true;
            GetComponent<Rigidbody2D>().AddForce(power, ForceMode2D.Impulse);
        }

        public virtual void OnMouseExit()
        {
            EventBus.TriggerEvent(new MouseExitCupEvent());
            _isHovered = false;

            if (_hoverAnimCoroutine != null)
                StopCoroutine(_hoverAnimCoroutine);

            StartCoroutine(nameof(RestoreAnimation));
        }

        private IEnumerator HoverAnimation()
        {
            float t = 0;
            var targetScale = _originalScale * _scaleUpFactor;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, t);
                yield return null;
            }

            var rotationTime = 0f;
            while (_isHovered)
            {
                rotationTime += Time.deltaTime * _rotationSpeed;
                var angle = Mathf.Sin(rotationTime) * _rotationAmplitude;
                transform.localRotation = _originalRotation * Quaternion.Euler(0, 0, angle);
                yield return null;
            }
        }

        private IEnumerator RestoreAnimation()
        {
            var startScale = transform.localScale;
            var startRotation = transform.localRotation;
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                transform.localScale = Vector3.Lerp(startScale, _originalScale, t);
                transform.localRotation = Quaternion.Slerp(startRotation, _originalRotation, t);
                yield return null;
            }
        }
    }
}
