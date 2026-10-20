using UnityEditor;

// 3성 일러스트 폴더(Art/Illustrations 아래 전부)의 그림 임포트 설정을 자동으로 맞춘다
// Sprite(2D and UI), 최대 2048, 밉맵 끔, Android는 ASTC 압축
public class IllustrationImporter : AssetPostprocessor
{
    public const string Folder = "Assets/_Project/Art/Illustrations/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;

        TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = 2048;
        android.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(android);
    }
}
