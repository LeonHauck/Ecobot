using UnityEditor;
using UnityEngine;

namespace EcoBot.EditorTools
{
    /// <summary>
    /// Configura automaticamente a importação da arte e do áudio:
    /// sprites pixel-art (16 px por unidade, sem filtro/compressão) e músicas em streaming.
    /// </summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Art/";
        public const int PixelsPerUnit = 16;

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith(ArtRoot)) Apply((TextureImporter)assetImporter, assetPath);
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/Music")) return;
            var importer = (AudioImporter)assetImporter;
            var s = importer.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            importer.defaultSampleSettings = s;
        }

        public static bool NeedsFix(TextureImporter ti) =>
            ti.textureType != TextureImporterType.Sprite ||
            !Mathf.Approximately(ti.spritePixelsPerUnit, PixelsPerUnit) ||
            ti.filterMode != FilterMode.Point;

        public static void Apply(TextureImporter ti, string path)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;   // necessário para o modo "Tiled"
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = path.Contains("/UI/ui_") ? new Vector4(5, 5, 5, 5) : Vector4.zero;
            ti.SetTextureSettings(settings);
        }
    }
}
