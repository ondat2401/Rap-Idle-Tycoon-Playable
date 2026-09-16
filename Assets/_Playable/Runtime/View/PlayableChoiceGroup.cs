using System;
using System.Collections;
using System.Collections.Generic;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Mot nhom nut lua chon. Lo phan hien ra lech nhau (stagger pop-in) va cho nguoi choi chon.
    /// Khi khong du tien thi giu nguyen nhom va bao ra ngoai de HUD nhap nhay do - dung nhu ban goc.
    /// </summary>
    public sealed class PlayableChoiceGroup : MonoBehaviour
    {
        [SerializeField] private PlayableChoiceButton[] _buttons = new PlayableChoiceButton[0];
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private float _defaultStagger = 0.08f;

        private readonly List<Vector3> _baseScales = new List<Vector3>();
        private int _pendingIndex = -1;

        public IReadOnlyList<PlayableChoiceButton> Buttons => this._buttons;

        /// <summary>Index cua nut duoc chon o lan <see cref="WaitForChoice"/> gan nhat.</summary>
        public int SelectedIndex { get; private set; } = -1;

        private bool _cached;

        private void Awake()
        {
            this.EnsureCached();
        }

        /// <summary>
        /// Gan Index va nho scale goc cua tung nut. Goi lai duoc nhieu lan.
        /// Phai goi tay tu <see cref="Show"/> vi nhom thuong bi tat san trong prefab - luc do Awake
        /// chua chay.
        /// </summary>
        private void EnsureCached()
        {
            if (this._cached)
            {
                return;
            }

            this._cached = true;
            this._baseScales.Clear();

            for (int i = 0; i < this._buttons.Length; i++)
            {
                PlayableChoiceButton button = this._buttons[i];
                if (button == null)
                {
                    this._baseScales.Add(Vector3.one);
                    continue;
                }

                button.Index = i;
                this._baseScales.Add(button.transform.localScale);
            }
        }

        /// <summary>Gan gia cho tung nut. Tra ve 0 de an cum gia.</summary>
        public void Configure(Func<int, long> priceOf)
        {
            if (priceOf == null)
            {
                return;
            }

            for (int i = 0; i < this._buttons.Length; i++)
            {
                this._buttons[i]?.SetPrice(priceOf(i));
            }
        }

        /// <summary>Gan tieu de cho tung nut theo thu tu. Phan tu thieu/null thi giu nguyen.</summary>
        public void SetTitles(string[] titles)
        {
            if (titles == null)
            {
                return;
            }

            int n = Mathf.Min(titles.Length, this._buttons.Length);
            for (int i = 0; i < n; i++)
            {
                if (titles[i] != null)
                {
                    this._buttons[i]?.SetTitle(titles[i]);
                }
            }
        }

        public IEnumerator Show(float stagger = -1f)
        {
            if (stagger < 0f)
            {
                stagger = this._defaultStagger;
            }

            this.gameObject.SetActive(true);
            this.EnsureCached();

            for (int i = 0; i < this._buttons.Length; i++)
            {
                PlayableChoiceButton button = this._buttons[i];
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(true);
                button.SetInteractable(false);
                this.StartCoroutine(PlayableTween.PopIn(button.transform, this._baseScales[i], this._popDuration));
                yield return PlayableTween.Delay(stagger);
            }

            yield return PlayableTween.Delay(this._popDuration);
        }

        public void Hide()
        {
            this.SetInteractable(false);
            this.gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            foreach (PlayableChoiceButton button in this._buttons)
            {
                button?.SetInteractable(interactable);
            }
        }

        /// <summary>Cac nut dang hien - guide hand dung danh sach nay lam muc tieu.</summary>
        public IEnumerable<RectTransform> ActiveTargets()
        {
            foreach (PlayableChoiceButton button in this._buttons)
            {
                if (button != null && button.gameObject.activeInHierarchy)
                {
                    yield return button.Rect;
                }
            }
        }

        /// <summary>
        /// Cho nguoi choi chon. <paramref name="canAfford"/> tra ve false thi goi
        /// <paramref name="onRejected"/> roi tiep tuc cho.
        /// </summary>
        public IEnumerator WaitForChoice(Func<int, bool> canAfford, Action onTouched, Action<int> onRejected)
        {
            this._pendingIndex = -1;
            this.SelectedIndex = -1;
            this.Subscribe(true);
            this.SetInteractable(true);

            while (this.SelectedIndex < 0)
            {
                if (this._pendingIndex < 0)
                {
                    yield return null;
                    continue;
                }

                int candidate = this._pendingIndex;
                this._pendingIndex = -1;

                onTouched?.Invoke();

                if (canAfford == null || canAfford(candidate))
                {
                    this.SelectedIndex = candidate;
                }
                else
                {
                    onRejected?.Invoke(candidate);
                }
            }

            this.SetInteractable(false);
            this.Subscribe(false);
        }

        private void Subscribe(bool subscribe)
        {
            foreach (PlayableChoiceButton button in this._buttons)
            {
                if (button == null)
                {
                    continue;
                }

                button.Clicked -= this.HandleClicked;
                if (subscribe)
                {
                    button.Clicked += this.HandleClicked;
                }
            }
        }

        private void HandleClicked(PlayableChoiceButton button)
        {
            if (button != null)
            {
                this._pendingIndex = button.Index;
            }
        }
    }
}
