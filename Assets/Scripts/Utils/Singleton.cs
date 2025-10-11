//------------------------------------------------------------
// Shrink Framework
// Author Eicy.
// Homepage: https://github.com/cneicy/ShrinkFramework
// Feedback: mailto:im@crash.work
//------------------------------------------------------------

using UnityEngine;

namespace Utils
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;

                if (_instance) return _instance;
                _instance = FindAnyObjectByType<T>();
                
#if UNITY_EDITOR
                if (!Application.isPlaying && !_instance)
                    return null;
#endif

                if (_instance) return _instance;
                var go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = (T)this;

            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}