using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 8비트 느낌 효과음 10개를 사인파·사각파·노이즈로 만들어 Audio/Sfx/에 WAV로 저장하고, SoundConfig 에셋에 연결한다.
// 메뉴를 다시 실행하면 WAV는 새로 만들고, SoundConfig에서 바꾼 음량·간격은 그대로 둔다
public static class SfxBuilder
{
    private const string SfxFolder = "Assets/_Project/Audio/Sfx";
    private const string ConfigPath = "Assets/_Project/Data/SoundConfig.asset";
    public const int SampleRate = 22050;
    // 여러 소리가 겹쳐도 깨지지 않게 최대 크기를 낮춘다
    private const float Peak = 0.5f;

    private enum Wave
    {
        Sine,
        Square,
        // 폭 25% 사각파 (더 가늘고 날카로운 소리)
        Pulse,
        Noise,
    }

    // 한 음: 파형, 시작·끝 주파수(Hz, 노이즈는 값이 바뀌는 빠르기), 길이(초), 크기, 감쇠(1이면 끝에서 0까지 줄어듦)
    private struct Note
    {
        public Wave Wave;
        public float From;
        public float To;
        public float Seconds;
        public float Volume;
        public float Decay;

        public Note(Wave wave, float from, float to, float seconds, float volume = 1f, float decay = 1f)
        {
            Wave = wave;
            From = from;
            To = to;
            Seconds = seconds;
            Volume = volume;
            Decay = decay;
        }
    }

    // 기본 음량·간격 (SoundConfig를 처음 만들 때만 쓴다). 공격·피격은 2배속에서 시끄럽지 않게 간격을 둔다
    private static readonly Dictionary<Sfx, (float volume, float interval)> Defaults = new Dictionary<Sfx, (float, float)>
    {
        { Sfx.Button, (0.6f, 0.05f) },
        { Sfx.Buy, (0.8f, 0f) },
        { Sfx.Sell, (0.8f, 0f) },
        { Sfx.Merge, (0.9f, 0f) },
        { Sfx.Attack, (0.35f, 0.15f) },
        { Sfx.Hit, (0.45f, 0.12f) },
        { Sfx.Skill, (0.6f, 0.2f) },
        { Sfx.Faint, (0.7f, 0.1f) },
        { Sfx.Win, (0.9f, 0f) },
        { Sfx.Lose, (0.9f, 0f) },
    };

    [MenuItem("MonsterCamp/효과음 만들기")]
    public static void BuildMenu()
    {
        BuildClips();
        SoundConfig config = GetOrCreateConfig();
        Selection.activeObject = config;
        Debug.Log("[효과음] 만들기 완료: " + SfxFolder);
    }

    // SoundConfig가 없으면 효과음까지 만든다 (Build Main Scene에서 사용)
    public static SoundConfig GetOrCreateConfig()
    {
        if (!File.Exists(ClipPath(Sfx.Button)))
        {
            BuildClips();
        }

        var config = AssetDatabase.LoadAssetAtPath<SoundConfig>(ConfigPath);
        bool created = config == null;
        if (created)
        {
            config = ScriptableObject.CreateInstance<SoundConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        // 빠진 효과음 줄을 채우고, 소리 파일을 다시 연결한다
        var so = new SerializedObject(config);
        SerializedProperty entries = so.FindProperty("_entries");
        foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
        {
            SerializedProperty entry = FindEntry(entries, sfx);
            if (entry == null)
            {
                entries.arraySize++;
                entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("id").enumValueIndex = (int)sfx;
                entry.FindPropertyRelative("volume").floatValue = Defaults[sfx].volume;
                entry.FindPropertyRelative("minInterval").floatValue = Defaults[sfx].interval;
            }
            entry.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(sfx));
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return config;
    }

    private static SerializedProperty FindEntry(SerializedProperty entries, Sfx sfx)
    {
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("id").enumValueIndex == (int)sfx)
            {
                return entry;
            }
        }
        return null;
    }

    private static string ClipPath(Sfx sfx) => $"{SfxFolder}/{sfx}.wav";

    private static void BuildClips()
    {
        Directory.CreateDirectory(SfxFolder);
        foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
        {
            WriteWav(ClipPath(sfx), Render(Notes(sfx)));
        }
        AssetDatabase.Refresh();

        foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
        {
            string path = ClipPath(sfx);
            // 짧은 소리라 바로 낼 수 있게 메모리에 풀어 둔다
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }

    // ---------- 소리 모양 (모두 0.5초 이하) ----------

    private static Note[] Notes(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Button:
                // 짧은 딸깍
                return new[] { new Note(Wave.Pulse, 1200f, 900f, 0.05f, 0.7f) };
            case Sfx.Buy:
                // 동전: 낮은음 → 높은음
                return new[]
                {
                    new Note(Wave.Square, 988f, 988f, 0.06f, 0.8f, 0.2f),
                    new Note(Wave.Square, 1319f, 1319f, 0.2f, 0.8f),
                };
            case Sfx.Sell:
                // 동전 반대로: 높은음 → 낮은음 (사인파로 부드럽게)
                return new[]
                {
                    new Note(Wave.Sine, 1319f, 1319f, 0.07f, 1f, 0.2f),
                    new Note(Wave.Sine, 880f, 880f, 0.18f, 1f),
                };
            case Sfx.Merge:
                // 올라가는 화음 + 끝에 반짝
                return new[]
                {
                    new Note(Wave.Pulse, 523f, 523f, 0.06f, 0.8f, 0.3f),
                    new Note(Wave.Pulse, 659f, 659f, 0.06f, 0.8f, 0.3f),
                    new Note(Wave.Pulse, 784f, 784f, 0.06f, 0.8f, 0.3f),
                    new Note(Wave.Pulse, 1047f, 1568f, 0.22f, 0.8f),
                };
            case Sfx.Attack:
                // 휙: 높은 노이즈가 빠르게 사라짐
                return new[] { new Note(Wave.Noise, 9000f, 3000f, 0.07f, 0.6f) };
            case Sfx.Hit:
                // 퍽: 낮은 노이즈 + 아래로 떨어지는 사각파
                return new[]
                {
                    new Note(Wave.Noise, 2500f, 800f, 0.04f, 0.8f, 0.5f),
                    new Note(Wave.Square, 220f, 70f, 0.07f, 0.7f),
                };
            case Sfx.Skill:
                // 슝: 위로 올라가는 사인파
                return new[]
                {
                    new Note(Wave.Sine, 400f, 1400f, 0.18f, 1f, 0.2f),
                    new Note(Wave.Pulse, 1400f, 1800f, 0.1f, 0.5f),
                };
            case Sfx.Faint:
                // 아래로 길게 떨어짐
                return new[] { new Note(Wave.Square, 660f, 90f, 0.4f, 0.7f, 0.8f) };
            case Sfx.Win:
                // 도-미-솔-높은 도 (마지막 길게)
                return new[]
                {
                    new Note(Wave.Square, 523f, 523f, 0.08f, 0.7f, 0.3f),
                    new Note(Wave.Square, 659f, 659f, 0.08f, 0.7f, 0.3f),
                    new Note(Wave.Square, 784f, 784f, 0.08f, 0.7f, 0.3f),
                    new Note(Wave.Square, 1047f, 1047f, 0.26f, 0.7f),
                };
            case Sfx.Lose:
                // 솔-미-도-낮은 도 (내려감)
                return new[]
                {
                    new Note(Wave.Square, 392f, 392f, 0.1f, 0.7f, 0.3f),
                    new Note(Wave.Square, 330f, 330f, 0.1f, 0.7f, 0.3f),
                    new Note(Wave.Square, 262f, 262f, 0.1f, 0.7f, 0.3f),
                    new Note(Wave.Square, 196f, 150f, 0.2f, 0.7f),
                };
            default:
                return new[] { new Note(Wave.Square, 440f, 440f, 0.1f) };
        }
    }

    // 음을 차례로 이어 붙인다. 음마다 시작 2ms는 서서히 커지고 이후 감쇠 (딸깍 소리 방지)
    private static float[] Render(Note[] notes)
    {
        var samples = new List<float>();
        // 같은 결과가 나오게 시드 고정
        var random = new System.Random(1);
        float noiseValue = 0f;
        float noisePhase = 0f;
        float phase = 0f;

        foreach (Note note in notes)
        {
            int count = Mathf.RoundToInt(note.Seconds * SampleRate);
            int attack = Mathf.Min(count, SampleRate / 500);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float frequency = Mathf.Lerp(note.From, note.To, t);
                phase = (phase + frequency / SampleRate) % 1f;

                float value;
                switch (note.Wave)
                {
                    case Wave.Sine:
                        value = Mathf.Sin(phase * 2f * Mathf.PI);
                        break;
                    case Wave.Square:
                        value = phase < 0.5f ? 1f : -1f;
                        break;
                    case Wave.Pulse:
                        value = phase < 0.25f ? 1f : -1f;
                        break;
                    default:
                        // 정해진 빠르기로만 값이 바뀌는 노이즈 (옛날 게임기 느낌)
                        noisePhase += frequency / SampleRate;
                        if (noisePhase >= 1f)
                        {
                            noisePhase -= 1f;
                            noiseValue = (float)random.NextDouble() * 2f - 1f;
                        }
                        value = noiseValue;
                        break;
                }

                float envelope = (i < attack ? (float)i / attack : 1f) * (1f - note.Decay * t);
                samples.Add(value * envelope * note.Volume * Peak);
            }
        }
        return samples.ToArray();
    }

    // 16비트 모노 PCM WAV
    public static void WriteWav(string path, float[] samples)
    {
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            int dataSize = samples.Length * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            foreach (float sample in samples)
            {
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }
    }
}
