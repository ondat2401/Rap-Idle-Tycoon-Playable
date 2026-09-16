using UnityEngine;
using System.Collections;

namespace Amanotes.Core
{
    public class CameraShake : MonoBehaviour
    {
        private Vector3 originalPos;
        private bool originCached;

        public void Shake(float duration, float magnitude)
        {
            if (!originCached) CacheOrigin();

            StopAllCoroutines();
            transform.localPosition = originalPos;
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private void CacheOrigin()
        {
            originalPos = transform.localPosition;
            originCached = true;
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                transform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
        }

        private void OnShake(CameraShakeData evt)
        {
            Shake(evt.cameraShakeDuration, evt.cameraShakeMagnitude);
        }

        void OnEnable()
        {
            EventBus.Subscribe<CameraShakeData>(OnShake);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<CameraShakeData>(OnShake);
        }
    }

    public class CameraShakeData
    {
        public float cameraShakeDuration;
        public float cameraShakeMagnitude;
    }
}
