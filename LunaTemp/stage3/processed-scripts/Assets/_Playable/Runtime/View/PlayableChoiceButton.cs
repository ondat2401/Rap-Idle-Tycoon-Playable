using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Mot nut lua chon. Dung chung cho ca 6 nhom (chon ban gai, thoai, xe, nha, quan ao, tha thu)
    /// - chi khac sprite icon va label gia.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PlayableChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _priceRoot;
        [SerializeField] private TMP_Text _priceLabel;
        [SerializeField] private TMP_Text _titleLabel;

        private RectTransform _rect;

        public event Action<PlayableChoiceButton> Clicked;

        public RectTransform Rect => this._rect != null ? this._rect : this._rect = (RectTransform)this.transform;

        /// <summary>Vi tri trong nhom, do <see cref="PlayableChoiceGroup"/> gan.</summary>
        public int Index { get; internal set; }

        private void Awake()
        {
            if (this._button == null)
            {
                this._button = this.GetComponent<Button>();
            }

            if (this._button != null)
            {
                this._button.onClick.AddListener(this.HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (this._button != null)
            {
                this._button.onClick.RemoveListener(this.HandleClick);
            }
        }

        /// <summary>Gia &lt;= 0 thi an cum gia (dung cho nut thoai va nut tha thu).</summary>
        public void SetPrice(long price)
        {
            bool hasPrice = price > 0L;

            if (this._priceRoot != null)
            {
                this._priceRoot.SetActive(hasPrice);
            }

            if (hasPrice && this._priceLabel != null)
            {
                this._priceLabel.text = price.ToString("N0", CultureInfo.InvariantCulture);
            }
        }

        public void SetTitle(string title)
        {
            if (this._titleLabel != null)
            {
                this._titleLabel.SetText(title);
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (this._icon == null)
            {
                return;
            }

            this._icon.sprite = sprite;
            this._icon.enabled = sprite != null;
        }

        public void SetInteractable(bool interactable)
        {
            if (this._button != null)
            {
                this._button.interactable = interactable;
            }
        }

        private void HandleClick()
        {
            this.Clicked?.Invoke(this);
        }
    }
}
