using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// 최적화 2단계 (에셋·설정): 크리처 그림 Sprite Atlas, Android 텍스처 압축 ASTC, Android Quality 단계 하나만.
// 여러 번 실행해도 같은 결과
public static class OptimizationSettingsMenu
{
    private const string AtlasPath = "Assets/_Project/Art/CreatureAtlas.spriteatlasv2";
    private const string CreatureFolder = "Assets/_Project/Art/Creatures";
    private const string ItemFolder = "Assets/_Project/Art/Items";
    private const string AndroidPlatform = "Android";
    // 2D 픽셀 그림이라 그림자·안티앨리어싱이 필요 없다. Android는 이 단계 하나만 쓴다
    private const string AndroidQualityName = "Medium";

    [MenuItem("MonsterCamp/최적화 설정 적용 (Atlas·Android)")]
    public static void Apply()
    {
        CreateCreatureAtlas();
        PlayerSettings.Android.textureCompressionFormats = new[] { TextureCompressionFormat.ASTC };
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        string quality = KeepOneAndroidQuality();
        AssetDatabase.SaveAssets();
        Debug.Log($"[최적화] Sprite Atlas {AtlasPath}, Android 텍스처 ASTC, Android Quality: {quality}");
    }

    // 크리처·아이템 폴더 전체를 한 장에. 픽셀 그림이므로 Point, 압축 없음, 회전·타이트 패킹 끔 (UI Image용)
    private static void CreateCreatureAtlas()
    {
        if (AssetDatabase.LoadMainAssetAtPath(AtlasPath) == null)
        {
            var atlas = new SpriteAtlasAsset();
            atlas.Add(new Object[]
            {
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(CreatureFolder),
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(ItemFolder)
            });
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath);
        }

        var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.includeInBuild = true;
        importer.packingSettings = new SpriteAtlasPackingSettings
        {
            enableRotation = false,
            enableTightPacking = false,
            enableAlphaDilation = false,
            padding = 2
        };
        importer.textureSettings = new SpriteAtlasTextureSettings
        {
            filterMode = FilterMode.Point,
            generateMipMaps = false,
            readable = false,
            sRGB = true,
            anisoLevel = 0
        };

        TextureImporterPlatformSettings defaults = importer.GetPlatformSettings("DefaultTexturePlatform");
        defaults.textureCompression = TextureImporterCompression.Uncompressed;
        defaults.crunchedCompression = false;
        importer.SetPlatformSettings(defaults);

        // 프로젝트 기본이 ASTC여도 이 아틀라스는 압축하지 않는다 (픽셀이 뭉개지지 않게)
        TextureImporterPlatformSettings android = importer.GetPlatformSettings(AndroidPlatform);
        android.overridden = true;
        android.format = TextureImporterFormat.RGBA32;
        android.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformSettings(android);

        importer.SaveAndReimport();
    }

    // Android 기본 Quality 하나만 남기고 나머지 단계에서 Android를 뺀다 (PC 에디터용 단계는 그대로)
    private static string KeepOneAndroidQuality()
    {
        Object asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
        var settings = new SerializedObject(asset);
        SerializedProperty levels = settings.FindProperty("m_QualitySettings");
        int keep = -1;
        for (int i = 0; i < levels.arraySize; i++)
        {
            if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == AndroidQualityName)
            {
                keep = i;
            }
        }
        if (keep < 0)
        {
            Debug.LogError($"[최적화] Quality 단계 '{AndroidQualityName}'를 찾지 못했어요");
            return "변경 없음";
        }

        for (int i = 0; i < levels.arraySize; i++)
        {
            SerializedProperty excluded = levels.GetArrayElementAtIndex(i).FindPropertyRelative("excludedTargetPlatforms");
            int found = -1;
            for (int j = 0; j < excluded.arraySize; j++)
            {
                if (excluded.GetArrayElementAtIndex(j).stringValue == AndroidPlatform)
                {
                    found = j;
                }
            }
            if (i == keep && found >= 0)
            {
                excluded.DeleteArrayElementAtIndex(found);
            }
            else if (i != keep && found < 0)
            {
                excluded.InsertArrayElementAtIndex(excluded.arraySize);
                excluded.GetArrayElementAtIndex(excluded.arraySize - 1).stringValue = AndroidPlatform;
            }
        }

        SerializedProperty perPlatform = settings.FindProperty("m_PerPlatformDefaultQuality");
        for (int i = 0; i < perPlatform.arraySize; i++)
        {
            SerializedProperty pair = perPlatform.GetArrayElementAtIndex(i);
            if (pair.FindPropertyRelative("first").stringValue == AndroidPlatform)
            {
                pair.FindPropertyRelative("second").intValue = keep;
            }
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        return AndroidQualityName;
    }
}
