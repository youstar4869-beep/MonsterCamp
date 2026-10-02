using System.IO;
using UnityEditor;
using UnityEngine;

// UI 테마 에셋(Data/UITheme)과 둥근 사각형 그림(9-slice)을 만든다. 한글 글꼴(Art/Fonts)을 테마에 연결한다.
// 이미 있는 테마는 그대로 쓰고(바꾼 색 보호), 비어 있는 그림·글꼴 칸만 채운다
public static class UIThemeBuilder
{
    private const string ThemePath = "Assets/_Project/Data/UITheme.asset";
    private const string RoundedSpritePath = "Assets/_Project/Art/Placeholders/UI/RoundedRect.png";
    private const string FontPath = "Assets/_Project/Art/Fonts/NotoSansKR-Regular.otf";
    // 그림 크기와 모서리 반지름 (픽셀). 9-slice 테두리는 반지름보다 조금 크게
    private const int SpriteSize = 64;
    private const int SpriteRadius = 24;
    private const int SpriteBorder = 28;

    [MenuItem("MonsterCamp/UI 테마 그림 만들기")]
    public static void BuildMenu()
    {
        UITheme theme = GetOrCreate();
        Selection.activeObject = theme;
        Debug.Log("[UI 테마] 준비 완료: " + ThemePath);
    }

    public static UITheme GetOrCreate()
    {
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }

        var so = new SerializedObject(theme);
        SerializedProperty sprite = so.FindProperty("_roundedSprite");
        if (sprite.objectReferenceValue == null)
        {
            sprite.objectReferenceValue = GetOrCreateRoundedSprite();
        }
        SerializedProperty font = so.FindProperty("_font");
        if (font.objectReferenceValue == null)
        {
            font.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font.objectReferenceValue == null)
            {
                Debug.LogWarning($"[UI 테마] 한글 글꼴이 없습니다: {FontPath} (Unity 기본 글꼴을 씁니다)");
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return theme;
    }

    // 흰색 둥근 사각형 (가장자리는 부드럽게). 색은 Image.color로 입힌다
    private static Sprite GetOrCreateRoundedSprite()
    {
        if (!File.Exists(RoundedSpritePath))
        {
            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    // 가장 가까운 모서리 원 중심까지 거리로 안쪽이면 불투명, 경계 1픽셀은 반투명
                    float cx = Mathf.Clamp(x + 0.5f, SpriteRadius, SpriteSize - SpriteRadius);
                    float cy = Mathf.Clamp(y + 0.5f, SpriteRadius, SpriteSize - SpriteRadius);
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(SpriteRadius - distance + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(RoundedSpritePath));
            File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(RoundedSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(SpriteBorder, SpriteBorder, SpriteBorder, SpriteBorder);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
    }

    // Image를 둥근 사각형으로 (radius: 모서리 반지름, 1080x1920 기준 픽셀. 0이면 테마 기본값)
    public static void Round(UnityEngine.UI.Image image, UITheme theme, float radius = 0f)
    {
        if (theme.RoundedSprite == null)
        {
            return;
        }
        image.sprite = theme.RoundedSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        // 그림의 반지름(SpriteRadius)이 화면에서 radius 픽셀이 되도록
        image.pixelsPerUnitMultiplier = SpriteRadius / (radius > 0f ? radius : theme.CornerRadius);
    }
}
