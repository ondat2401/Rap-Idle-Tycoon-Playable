using UnityEngine;
using UnityEngine.UI;

namespace Amanotes.Core
{
    public static class ImageRectHelper
    {
        public static void ApplyAspectFitToPanel(RectTransform canvasRect, RawImage rawImage)
        {
            if (rawImage?.texture == null || canvasRect == null)
                return;

            // Get canvas dimensions
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            // Get texture dimensions
            float textureWidth = rawImage.texture.width;
            float textureHeight = rawImage.texture.height;

            // Calculate aspect ratios
            float canvasRatio = canvasWidth / canvasHeight;
            float textureRatio = textureWidth / textureHeight;

            // Calculate final dimensions
            float finalWidth, finalHeight;
            if (textureRatio > canvasRatio)
            {
                // Width-constrained
                finalWidth = canvasWidth;
                finalHeight = canvasWidth / textureRatio;
            }
            else
            {
                // Height-constrained
                finalHeight = canvasHeight;
                finalWidth = canvasHeight * textureRatio;
            }

            // Apply size correctly (works with any anchor/stretch mode!)
            RectTransform rect = rawImage.rectTransform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
        }

        /// <summary>
        /// Applies aspect fill to panel.
        /// Simple calculation using current texture and screen dimensions.
        /// </summary>
        public static void ApplyAspectFillToPanel(RawImage rawImage)
        {
            if (rawImage.texture == null)
                return;

            // Read current dimensions fresh every time
            float textureWidth = rawImage.texture.width;
            float textureHeight = rawImage.texture.height;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // Calculate aspect fill UV rect
            Rect uvRect = CalculateAspectFillRect(
                textureWidth,
                textureHeight,
                screenWidth,
                screenHeight
            );

            // Apply directly
            rawImage.uvRect = uvRect;
        }

        /// <summary>
        /// Calculates aspect fill UV rect from dimensions.
        /// Pure calculation function - no state, no side effects.
        /// </summary>
        private static Rect CalculateAspectFillRect(
            float textureWidth,
            float textureHeight,
            float screenWidth,
            float screenHeight
        )
        {
            // Safety check
            if (textureHeight <= 0f || screenHeight <= 0f)
                return new Rect(0, 0, 1, 1);

            // Calculate aspect ratios
            float screenAspect = screenWidth / screenHeight;
            float textureAspect = textureWidth / textureHeight;

            if (screenAspect > textureAspect)
            {
                // Screen is wider than texture - crop height
                float yOffset = (1f - (textureAspect / screenAspect)) * 0.5f;
                return new Rect(0f, yOffset, 1f, 1f - yOffset - yOffset);
            }
            else
            {
                // Screen is taller than texture - crop width
                float xOffset = (1f - (screenAspect / textureAspect)) * 0.5f;
                return new Rect(xOffset, 0f, 1f - xOffset - xOffset, 1f);
            }
        }
    }
}
