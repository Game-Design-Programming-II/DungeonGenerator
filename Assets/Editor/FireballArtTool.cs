// Creates a simple fireball sprite and projectile prefab, and assigns it to the Fireball spell.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using ClassSystem.Spells;

public static class FireballArtTool
{
    const string ArtRoot = "Assets/Art/Demo";
    const string FireballPng = ArtRoot + "/Fireball.png";
    const string FireballPrefabPath = ArtRoot + "/FireballProjectile.prefab";
    const int PPU = 32;

    [MenuItem("Tools/RPG/Art/Create Fireball Demo Prefab")] 
    public static void CreateFireballPrefab()
    {
        EnsureFolder(ArtRoot);

        // Create sprite
        var tex = MakeCircleTexture(48, new Color(1f, 0.68f, 0.1f, 1f), new Color(1f, 0.39f, 0.04f, 1f));
        WriteSpritePNG(FireballPng, tex);

        // Create prefab
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FireballPng);
        var go = new GameObject("FireballProjectile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<ClassSystem.Spells.SpellProjectile>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, FireballPrefabPath);
        Object.DestroyImmediate(go);

        // Assign to Fireball spell under ClassObjects if found
        var guids = AssetDatabase.FindAssets("t:Spell Fireball", new[] { "Assets/ClassObjects/Spells" });
        if (guids.Length > 0)
        {
            var spell = AssetDatabase.LoadAssetAtPath<Spell>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (spell)
            {
                spell.projectilePrefab = prefab;
                EditorUtility.SetDirty(spell);
                AssetDatabase.SaveAssets();
                Debug.Log("Assigned Fireball prefab to Fireball spell: " + FireballPrefabPath);
            }
        }
        else
        {
            Debug.Log("Created Fireball prefab at: " + FireballPrefabPath + ". Create sample content first (Tools/RPG/Generate Sample ClassObjects) to auto-assign.");
        }
    }

    static Texture2D MakeCircleTexture(int size, Color fill, Color border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var cx = (size - 1) * 0.5f;
        var cy = (size - 1) * 0.5f;
        var r = size * 0.45f;
        var rBorder = r * 0.86f;
        var cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color c;
                if (d <= rBorder) c = fill;
                else if (d <= r) c = border;
                else c = new Color(0, 0, 0, 0);
                cols[y * size + x] = c;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    static void WriteSpritePNG(string path, Texture2D tex)
    {
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = PPU;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif

