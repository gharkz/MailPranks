using UnityEngine;

namespace MailPranks
{
    internal static class SpriteAsset
    {
        // Fit the supplied high-resolution art to the game's 32px inventory tile at runtime.
        // Keep the original asset untouched and preserve its alpha.
        internal static Sprite CreateInventorySprite(Texture2D source)
        {
            int left = source.width, right = -1, bottom = source.height, top = -1;
            var pixels = source.GetPixels32();
            for (int y = 0; y < source.height; y++) for (int x = 0; x < source.width; x++)
                if (pixels[y * source.width + x].a > 32) { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
            if (right < left) throw new System.InvalidOperationException("Empty ledger sprite.");
            var tile = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var output = new Color32[32 * 32];
            float scale = 28f / Mathf.Max(right - left + 1, top - bottom + 1);
            int width = Mathf.Max(1, Mathf.RoundToInt((right - left + 1) * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt((top - bottom + 1) * scale));
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int sx = Mathf.Min(right, left + Mathf.FloorToInt((x + 0.5f) / scale));
                int sy = Mathf.Min(top, bottom + Mathf.FloorToInt((y + 0.5f) / scale));
                output[(y + (32 - height) / 2) * 32 + x + (32 - width) / 2] = pixels[sy * source.width + sx];
            }
            tile.SetPixels32(output); tile.Apply();
            // Native inventory sprites use 100 pixels per unit; 32 makes UI.SetNativeSize
            // enlarge the same 32px tile by 3.125x and overlap adjacent shop slots.
            return Sprite.Create(tile, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
