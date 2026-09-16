using UnityEngine;
using UnityEngine.UI;

namespace Amanotes.MagicTiles3
{
    public class OutroCard : MonoBehaviour
    {
        [SerializeField] RawImage _image;

        public RectTransform Rect => _image.rectTransform;

        public void SetTexture(Texture2D tex)
        {
            _image.texture = tex;
            _image.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
        }
    }
}
