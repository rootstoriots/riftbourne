using UnityEngine;

namespace Riftbourne.UI
{
    /// <summary>
    /// Generates simple placeholder portraits for units.
    /// Can be replaced with actual sprite assets later.
    /// </summary>
    public static class PortraitGenerator
    {
        private static Texture2D playerPortraitTexture;
        private static Texture2D enemyPortraitTexture;

        public static Sprite GetPlayerPortrait()
        {
            if (playerPortraitTexture == null)
            {
                playerPortraitTexture = CreatePortraitTexture(new Color(0.2f, 0.6f, 0.9f, 1f)); // Blue
            }
            return Sprite.Create(
                playerPortraitTexture,
                new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f)
            );
        }

        public static Sprite GetEnemyPortrait()
        {
            if (enemyPortraitTexture == null)
            {
                enemyPortraitTexture = CreatePortraitTexture(new Color(0.9f, 0.2f, 0.2f, 1f)); // Red
            }
            return Sprite.Create(
                enemyPortraitTexture,
                new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f)
            );
        }

        private static Texture2D CreatePortraitTexture(Color mainColor)
        {
            Texture2D texture = new Texture2D(64, 64);

            // Fill with main color
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Create a simple border effect
                    if (x < 4 || x >= 60 || y < 4 || y >= 60)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else
                    {
                        texture.SetPixel(x, y, mainColor);
                    }
                }
            }

            texture.Apply();
            return texture;
        }
    }
}