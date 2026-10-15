// Applies the Scam Den import settings from ../README.md to scamden_<style>_sheet.png and slices
// each sheet into named sprites: <style>_<key>_<angle>, e.g. neon_desk_90.
// Runs on every import of the sheets, so change settings here rather than in the Inspector.
// Requires the 2D Sprite package (com.unity.2d.sprite).
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ScamDen
{
    public class ScamDenSheetImporter : AssetPostprocessor
    {
        const int Cell = 320, Columns = 4;
        static readonly Vector2 Pivot = new Vector2(0.5f, 0.35f);
        static readonly string[] Rows =
        {
            "desk", "monitor", "laptop", "chair", "phones", "fridge", "stash", "board",
            "bed", "pizza", "cans", "trash", "light", "rug", "floorTile", "wallSeg",
        };
        static readonly Regex SheetName = new Regex(@"^scamden_(\w+)_sheet$");

        public override uint GetVersion() => 1;

        void OnPreprocessTexture()
        {
            var sheet = Path.GetFileNameWithoutExtension(assetPath);
            var match = SheetName.Match(sheet);
            if (!match.Success) return;
            var style = match.Groups[1].Value;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 64;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Keep existing sprite IDs, including those from a manual grid slice
            // (named <sheet>_<index>), so scene and prefab references survive.
            var ids = new Dictionary<string, GUID>();
            foreach (var existing in provider.GetSpriteRects())
                ids[existing.name] = existing.spriteID;

            var height = Rows.Length * Cell;
            var rects = new List<SpriteRect>();
            var names = new List<SpriteNameFileIdPair>();
            for (var row = 0; row < Rows.Length; row++)
            for (var col = 0; col < Columns; col++)
            {
                var name = $"{style}_{Rows[row]}_{col * 90}";
                if (!ids.TryGetValue(name, out var id) && !ids.TryGetValue($"{sheet}_{row * Columns + col}", out id))
                    id = GUID.Generate();
                rects.Add(new SpriteRect
                {
                    name = name,
                    spriteID = id,
                    rect = new Rect(col * Cell, height - (row + 1) * Cell, Cell, Cell),
                    alignment = SpriteAlignment.Custom,
                    pivot = Pivot,
                });
                names.Add(new SpriteNameFileIdPair(name, id));
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(names);
            provider.Apply();
        }
    }
}
