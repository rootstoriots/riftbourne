using UnityEngine;

namespace Riftbourne.UI
{
    /// <summary>
    /// Generates default icons for UI elements when custom icons are not available.
    /// </summary>
    public static class IconGenerator
    {
        private static Sprite unarmedIcon;

        /// <summary>
        /// Gets the default unarmed icon (fist/hand icon).
        /// First tries to load from Resources/UnarmedAttackIcon (place your unarmed icon sprite there).
        /// If not found, generates a simple placeholder icon.
        /// 
        /// To use your own unarmed icon:
        /// 1. Place your sprite asset in a Resources folder (e.g., Assets/Resources/ or Assets/UI/Resources/)
        /// 2. Name it "UnarmedAttackIcon" (without file extension)
        /// 3. The sprite will be automatically loaded and used
        /// </summary>
        public static Sprite GetUnarmedIcon()
        {
            if (unarmedIcon == null)
            {
                // Try to load from Resources first (can be in Assets/Resources/ or Assets/UI/Resources/)
                // This allows you to place your own unarmed icon sprite in any Resources folder
                Sprite loadedIcon = Resources.Load<Sprite>("UnarmedAttackIcon");
                if (loadedIcon != null)
                {
                    unarmedIcon = loadedIcon;
                    return unarmedIcon;
                }

                // If not found in Resources, generate a simple placeholder
                Texture2D texture = CreateUnarmedIconTexture();
                unarmedIcon = Sprite.Create(
                    texture,
                    new Rect(0, 0, 64, 64),
                    new Vector2(0.5f, 0.5f)
                );
            }
            return unarmedIcon;
        }

        private static Texture2D CreateUnarmedIconTexture()
        {
            Texture2D texture = new Texture2D(64, 64);

            // Create a simple fist/hand icon pattern
            // Main color: brown/tan for skin
            Color mainColor = new Color(0.8f, 0.6f, 0.4f, 1f);
            // Border color: darker brown
            Color borderColor = new Color(0.5f, 0.4f, 0.3f, 1f);
            // Background: transparent
            Color backgroundColor = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Create a simple fist shape in the center
                    float centerX = 32f;
                    float centerY = 32f;
                    float distX = Mathf.Abs(x - centerX);
                    float distY = Mathf.Abs(y - centerY);
                    float distance = Mathf.Sqrt(distX * distX + distY * distY);

                    // Border
                    if (distance > 28f || distance < 8f)
                    {
                        texture.SetPixel(x, y, backgroundColor);
                    }
                    // Outer border
                    else if (distance > 26f || distance < 10f)
                    {
                        texture.SetPixel(x, y, borderColor);
                    }
                    // Main fist shape
                    else
                    {
                        // Create a rounded rectangle shape for fist
                        bool inFist = (distX < 20f && distY < 18f) || distance < 18f;
                        if (inFist)
                        {
                            texture.SetPixel(x, y, mainColor);
                        }
                        else
                        {
                            texture.SetPixel(x, y, backgroundColor);
                        }
                    }
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
