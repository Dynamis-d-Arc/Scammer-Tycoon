using UnityEditor;
using UnityEngine;

namespace ScamDen
{
    // Texture-based atlas rendering preserves the source artwork without requiring extra packages.
    public class ScamDenSheetImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 2;
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("scamden_") || !assetPath.EndsWith("_sheet.png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.isReadable = true; // Alpha-aware picking ignores transparent atlas padding.
            importer.alphaIsTransparency = true;
        }
    }
}
