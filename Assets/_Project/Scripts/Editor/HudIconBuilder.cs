using System.IO;
using UnityEditor;
using UnityEngine;

// 상단 정보바 아이콘 (32x32 픽셀 그림, Art/Creatures와 같은 임포트 설정): 하트, 빈 하트, 동전, 별, 깃발.
// 모양은 "안쪽인지"로 정하고, 테두리 1픽셀·왼쪽 위 밝은 면·오른쪽 아래 어두운 면을 칠한다.
// 이미 있는 그림은 다시 만들지 않는다 (직접 그린 그림으로 바꿔도 보호)
public static class HudIconBuilder
{
    private const string Folder = "Assets/_Project/Art/UI";
    private const int Size = 32;

    public static Sprite Heart => GetOrCreate("Heart", InsideHeart,
        new Color(0.93f, 0.25f, 0.34f), new Color(0.42f, 0.07f, 0.14f), new Color(1f, 0.68f, 0.72f), new Color(0.72f, 0.15f, 0.24f));

    public static Sprite EmptyHeart => GetOrCreate("HeartEmpty", InsideHeart,
        new Color(0.22f, 0.23f, 0.29f), new Color(0.58f, 0.6f, 0.68f), new Color(0.3f, 0.31f, 0.38f), new Color(0.18f, 0.19f, 0.24f));

    public static Sprite Coin => GetOrCreate("Coin", InsideCoin,
        new Color(1f, 0.8f, 0.22f), new Color(0.52f, 0.33f, 0.04f), new Color(1f, 0.95f, 0.65f), new Color(0.85f, 0.58f, 0.1f),
        (x, y) => CoinMark(x, y) ? new Color(0.78f, 0.52f, 0.08f) : (Color?)null);

    public static Sprite Star => GetOrCreate("Star", InsideStar,
        new Color(1f, 0.86f, 0.22f), new Color(0.58f, 0.38f, 0.04f), new Color(1f, 0.97f, 0.72f), new Color(0.9f, 0.62f, 0.1f));

    public static Sprite Flag => GetOrCreate("Flag", InsideFlag,
        new Color(0.92f, 0.32f, 0.3f), new Color(0.32f, 0.12f, 0.1f), new Color(1f, 0.62f, 0.55f), new Color(0.7f, 0.2f, 0.2f),
        (x, y) => FlagPole(x, y) ? new Color(0.62f, 0.45f, 0.3f) : (Color?)null);

    private static Sprite GetOrCreate(string name, System.Func<float, float, bool> inside,
        Color fill, Color outline, Color light, Color shade, System.Func<int, int, Color?> decorate = null)
    {
        string path = $"{Folder}/{name}.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    texture.SetPixel(x, y, PixelColor(x, y, inside, fill, outline, light, shade, decorate));
                }
            }
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (CreatureSpriteImporter.Apply(importer))
            {
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Color PixelColor(int x, int y, System.Func<float, float, bool> inside,
        Color fill, Color outline, Color light, Color shade, System.Func<int, int, Color?> decorate)
    {
        bool In(int px, int py) => px >= 0 && py >= 0 && px < Size && py < Size && inside(px + 0.5f, py + 0.5f);

        if (!In(x, y))
        {
            return Color.clear;
        }
        if (!In(x - 1, y) || !In(x + 1, y) || !In(x, y - 1) || !In(x, y + 1))
        {
            return outline;
        }
        Color? decoration = decorate?.Invoke(x, y);
        if (decoration.HasValue)
        {
            return decoration.Value;
        }
        // 테두리 바로 안쪽: 왼쪽 위는 밝게, 오른쪽 아래는 어둡게
        if (!In(x - 2, y) || !In(x, y + 2))
        {
            return light;
        }
        if (!In(x + 2, y) || !In(x, y - 2))
        {
            return shade;
        }
        return fill;
    }

    // ---------- 모양 (x 오른쪽, y 위쪽, 0~32) ----------

    private static bool InsideHeart(float x, float y)
    {
        float u = (x - 16f) / 13f;
        float v = (y - 14.5f) / 13f;
        float a = u * u + v * v - 1f;
        return a * a * a - u * u * v * v * v <= 0f;
    }

    private static bool InsideCoin(float x, float y)
    {
        return (x - 16f) * (x - 16f) + (y - 16f) * (y - 16f) <= 13.5f * 13.5f;
    }

    // 동전 안쪽 고리와 가운데 세로 홈
    private static bool CoinMark(int x, int y)
    {
        float d = Mathf.Sqrt((x + 0.5f - 16f) * (x + 0.5f - 16f) + (y + 0.5f - 16f) * (y + 0.5f - 16f));
        bool ring = d >= 9f && d < 10f;
        bool slot = (x == 15 || x == 16) && y >= 11 && y <= 20;
        return ring || slot;
    }

    // 위를 향한 별: 바깥 반지름 15, 안쪽 6.5인 꼭짓점 10개 다각형 안쪽인지
    private static readonly Vector2[] StarPoints = MakeStarPoints(new Vector2(16f, 15f), 15f, 6.5f);

    private static Vector2[] MakeStarPoints(Vector2 center, float outer, float inner)
    {
        var points = new Vector2[10];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float radius = i % 2 == 0 ? outer : inner;
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    private static bool InsideStar(float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = StarPoints.Length - 1; i < StarPoints.Length; j = i++)
        {
            Vector2 a = StarPoints[i];
            Vector2 b = StarPoints[j];
            if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private static bool InsideFlag(float x, float y)
    {
        return FlagPole((int)x, (int)y) || InsideBanner(x, y);
    }

    private static bool FlagPole(int x, int y)
    {
        return x >= 6 && x <= 8 && y >= 2 && y <= 29;
    }

    // 깃발 천: 오른쪽 끝이 제비 꼬리 모양으로 파인 사각형
    private static bool InsideBanner(float x, float y)
    {
        if (x < 8f || x > 28f || y < 15f || y > 29f)
        {
            return false;
        }
        float notch = 22f + Mathf.Abs(y - 22f) * 0.85f;
        return x <= notch;
    }
}
