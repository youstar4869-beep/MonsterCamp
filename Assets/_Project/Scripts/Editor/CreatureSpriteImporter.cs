using UnityEditor;
using UnityEngine;

// Assets/_Project/Art/Creatures·Items 의 PNG를 픽셀 그림용 Sprite로 임포트한다
// (Sprite, Filter Point, 압축 없음, Pixels Per Unit 32). 새 PNG를 넣으면 자동 적용
public class CreatureSpriteImporter : AssetPostprocessor
{
    public const string Folder = "Assets/_Project/Art/Creatures/";
    public const string ItemFolder = "Assets/_Project/Art/Items/";
    public const int PixelsPerUnit = 32;

    private void OnPreprocessTexture()
    {
        if (!(assetPath.StartsWith(Folder) || assetPath.StartsWith(ItemFolder)) || !assetPath.EndsWith(".png"))
        {
            return;
        }
        Apply((TextureImporter)assetImporter);
    }

    // 설정이 다르면 고치고 true
    public static bool Apply(TextureImporter importer)
    {
        bool same = importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single
                    && importer.filterMode == FilterMode.Point
                    && importer.textureCompression == TextureImporterCompression.Uncompressed
                    && Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
                    && !importer.mipmapEnabled
                    && importer.alphaIsTransparency;
        if (same)
        {
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        return true;
    }
}
