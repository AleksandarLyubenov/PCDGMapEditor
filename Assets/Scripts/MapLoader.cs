using UnityEngine;
using System.IO;

[RequireComponent(typeof(SpriteRenderer))]
public class MapLoader : MonoBehaviour
{
    public string imagePath = "map.png"; // relative to project or absolute

    private void Start()
    {
        LoadImage();
    }

    public void LoadImage()
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogWarning("Map image not found: " + imagePath);
            return;
        }

        byte[] bytes = File.ReadAllBytes(imagePath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);

        Sprite sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Optional: scale so width ~ certain world units
        float targetWidth = 200f;
        float worldWidth = sprite.bounds.size.x;
        float scale = targetWidth / worldWidth;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
