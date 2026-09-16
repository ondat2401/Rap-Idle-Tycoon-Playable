using System;
using System.Collections;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Ban emoji bay len moi lan nguoi choi tap. Ban goc: chon prefab ngau nhien, spawn tai diem co dinh,
    /// tween by(0.5s, x += random(-250, 250), y += 300) roi huy.
    /// </summary>
    public sealed class PlayableEmojiSpawner : MonoBehaviour
    {
        [SerializeField] private RectTransform _spawnPoint;
        [SerializeField] private RectTransform _container;
        [SerializeField] private GameObject[] _prefabs = new GameObject[0];

        [Tooltip("Prefab tu index nay tro xuong duoc thu nho con nua - giong ban goc (index < 9).")]
        [SerializeField] private int _smallPrefabCount = 9;

        [SerializeField] private float _smallScale = 0.5f;

        private void Awake()
        {
            if (this._container == null)
            {
                this._container = (RectTransform)this.transform;
            }

            if (this._spawnPoint == null)
            {
                this._spawnPoint = this._container;
            }
        }

        public void Spawn(float riseDistance, float spreadX, float duration)
        {
            if (this._prefabs == null || this._prefabs.Length == 0 || this._spawnPoint == null)
            {
                return;
            }

            int index = UnityEngine.Random.Range(0, this._prefabs.Length);
            GameObject prefab = this._prefabs[index];
            if (prefab == null)
            {
                return;
            }

            GameObject instance = Instantiate(prefab, this._container);
            var rect = instance.transform as RectTransform;
            if (rect == null)
            {
                Destroy(instance);
                return;
            }

            rect.anchoredPosition = this._spawnPoint.anchoredPosition;
            rect.localScale = index < this._smallPrefabCount
                ? Vector3.one * this._smallScale
                : Vector3.one;
            instance.SetActive(true);

            this.StartCoroutine(this.Fly(rect, riseDistance, spreadX, duration));
        }

        private IEnumerator Fly(RectTransform rect, float riseDistance, float spreadX, float duration)
        {
            float offsetX = UnityEngine.Random.Range(-spreadX, spreadX);
            var group = rect.GetComponent<CanvasGroup>();

            if (group != null)
            {
                this.StartCoroutine(PlayableTween.Fade(group, 0f, duration));
            }

            yield return PlayableTween.MoveAnchoredBy(rect, new Vector2(offsetX, riseDistance), duration,
                PlayableEase.QuadOut);

            if (rect != null)
            {
                Destroy(rect.gameObject);
            }
        }
    }
}
