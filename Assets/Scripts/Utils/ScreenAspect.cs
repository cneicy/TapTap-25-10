using UnityEngine;

namespace Utils
{
    public class ScreenAspect : Singleton<ScreenAspect>
    {
        public static float TargetAspect = 16f / 9f;
        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;

            var windowAspect = Screen.width / (float)Screen.height;

            var scaleHeight = windowAspect / TargetAspect;

            if (scaleHeight < 1f)
            {
                var rect = _mainCamera.rect;

                rect.width = 1f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1f - scaleHeight) / 2f;

                _mainCamera.rect = rect;
            }
            else
            {
                var scaleWidth = 1f / scaleHeight;

                var rect = _mainCamera.rect;

                rect.width = scaleWidth;
                rect.height = 1f;
                rect.x = (1f - scaleWidth) / 2f;
                rect.y = 0;

                _mainCamera.rect = rect;
            }
        }
    }
}