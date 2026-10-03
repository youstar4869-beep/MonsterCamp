using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 8비트 느낌 배경음 2곡(상점, 전투)을 사각파·사인파·노이즈로 만들어 Audio/Bgm/에 WAV로 저장하고, SoundConfig에 연결한다.
// 곡 길이를 8분음표 칸 수 × 칸 길이(샘플)로 딱 맞추고 모든 음이 곡 안에서 끝나게 해서 이음새 없이 반복된다
public static class BgmBuilder
{
    private const string BgmFolder = "Assets/_Project/Audio/Bgm";
    private const int SampleRate = SfxBuilder.SampleRate;
    // 섞은 뒤 가장 큰 소리를 이 크기로 맞춘다
    private const float Peak = 0.7f;
    // 한 마디 = 8분음표 8칸
    private const int StepsPerBar = 8;

    private enum Wave
    {
        Sine,
        Square,
        // 폭 25% 사각파 (밝고 가는 소리)
        Pulse,
        Noise,
    }

    private class Song
    {
        public int Bpm;
        // 마디마다 화음 (예: "C", "Am", "E")
        public string[] Chords;
        // 마디마다 멜로디 8칸: 음 이름(C5, G#4) = 새 음, "-" = 앞 음 늘이기, "." = 쉼
        public string[] Lead;
        public int LeadTranspose;
        public Wave LeadWave;
        public float LeadDecay;
        // 전투: 베이스 8분음표 연타, 하이햇 매 칸, 킥 추가
        public bool Driving;
    }

    // 상점: 밝고 느긋하게 (C장조, 112 BPM, 12마디 ≈ 25.7초)
    private static readonly Song ShopSong = new Song
    {
        Bpm = 112,
        Chords = new[] { "C", "Am", "F", "G", "C", "Am", "Dm", "G", "F", "Em", "Dm", "G" },
        Lead = new[]
        {
            "E5 - G5 E5 C6 - G5 -",
            "A5 - G5 E5 C5 - . .",
            "F5 - A5 F5 C6 - A5 -",
            "G5 - - B5 D6 - B5 G5",
            "E5 - G5 E5 C6 - E6 -",
            "D6 C6 A5 - E5 - . .",
            "F5 - A5 - D6 - C6 A5",
            "B5 - G5 - D5 - . .",
            "A5 - C6 - A5 F5 G5 A5",
            "G5 - E5 - B4 - E5 G5",
            "F5 E5 D5 - F5 - A5 -",
            "G5 - - - . . D5 -",
        },
        LeadWave = Wave.Pulse,
        LeadDecay = 0.5f,
    };

    // 전투: 빠르고 긴장감 있게 (A단조, 160 BPM, 16마디 = 24초)
    private static readonly Song BattleSong = new Song
    {
        Bpm = 160,
        Chords = new[] { "Am", "Am", "F", "G", "Am", "Am", "F", "E", "Dm", "Dm", "Am", "Am", "F", "G", "E", "E" },
        Lead = new[]
        {
            "A5 - - E5 A5 - C6 -",
            "B5 - A5 - E5 - - -",
            "F5 - A5 - C6 - A5 -",
            "B5 - D6 - B5 - G5 -",
            "A5 - - E5 A5 - C6 -",
            "D6 - C6 - B5 - A5 -",
            "C6 - A5 - F5 - A5 -",
            "G#5 - - - B5 - E5 -",
            "D6 - - F6 E6 - D6 -",
            "A5 - - D6 C6 - A5 -",
            "C6 - - E6 D6 - C6 -",
            "E5 - A5 - C6 - E6 -",
            "F6 - E6 - C6 - A5 -",
            "D6 - B5 - G5 - B5 -",
            "G#5 - B5 - E6 - D6 -",
            "B5 - G#5 - E5 - - -",
        },
        // 한 옥타브 낮춰 귀가 덜 아프게
        LeadTranspose = -12,
        LeadWave = Wave.Square,
        LeadDecay = 0.2f,
        Driving = true,
    };

    [MenuItem("MonsterCamp/배경음 만들기")]
    public static void BuildMenu()
    {
        BuildClips();
        SoundConfig config = GetOrCreateConfig();
        Selection.activeObject = config;
        Debug.Log("[배경음] 만들기 완료: " + BgmFolder);
    }

    // SoundConfig(효과음 포함)를 준비하고, 배경음이 없으면 만들어 연결한다 (Build Main Scene에서 사용)
    public static SoundConfig GetOrCreateConfig()
    {
        SoundConfig config = SfxBuilder.GetOrCreateConfig();
        if (!File.Exists(ClipPath(Bgm.Shop)) || !File.Exists(ClipPath(Bgm.Battle)))
        {
            BuildClips();
        }

        var so = new SerializedObject(config);
        so.FindProperty("_shopBgm").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(Bgm.Shop));
        so.FindProperty("_battleBgm").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath(Bgm.Battle));
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return config;
    }

    private static string ClipPath(Bgm bgm) => $"{BgmFolder}/{bgm}.wav";

    private static void BuildClips()
    {
        Directory.CreateDirectory(BgmFolder);
        SfxBuilder.WriteWav(ClipPath(Bgm.Shop), Render(ShopSong));
        SfxBuilder.WriteWav(ClipPath(Bgm.Battle), Render(BattleSong));
        AssetDatabase.Refresh();

        // WAV가 곡마다 1MB 넘게 커서 Vorbis로 줄이고, 메모리에 다 풀지 않고 재생하면서 읽는다
        foreach (Bgm bgm in new[] { Bgm.Shop, Bgm.Battle })
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath(bgm));
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.5f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }

    // ---------- 곡 만들기 ----------

    private static float[] Render(Song song)
    {
        int stepSamples = Mathf.RoundToInt(SampleRate * 60f / song.Bpm / 2f);
        int bars = song.Chords.Length;
        var buffer = new float[bars * StepsPerBar * stepSamples];
        var random = new System.Random(1);

        // 멜로디
        int step = 0;
        int noteStart = -1;
        int noteMidi = 0;
        foreach (string bar in song.Lead)
        {
            string[] tokens = bar.Split(' ');
            if (tokens.Length != StepsPerBar)
            {
                Debug.LogError($"[배경음] 멜로디 마디가 {StepsPerBar}칸이 아닙니다: \"{bar}\"");
            }
            foreach (string token in tokens)
            {
                if (token != "-")
                {
                    if (noteStart >= 0)
                    {
                        AddLead(buffer, song, noteMidi, noteStart, step - noteStart, stepSamples, random);
                        noteStart = -1;
                    }
                    if (token != ".")
                    {
                        noteStart = step;
                        noteMidi = ParseNote(token) + song.LeadTranspose;
                    }
                }
                step++;
            }
        }
        if (noteStart >= 0)
        {
            AddLead(buffer, song, noteMidi, noteStart, step - noteStart, stepSamples, random);
        }

        for (int b = 0; b < bars; b++)
        {
            ParseChord(song.Chords[b], out int root, out bool minor);
            int third = minor ? 3 : 4;
            int barStep = b * StepsPerBar;

            for (int s = 0; s < StepsPerBar; s++)
            {
                int start = (barStep + s) * stepSamples;

                // 베이스 (사각파, 낮은 음)
                int bass = 36 + root;
                if (song.Driving)
                {
                    // 근음 ↔ 한 옥타브 위를 8분음표로 연타
                    AddTone(buffer, start, stepSamples * 0.6f, Wave.Square, MidiToHz(bass + (s % 2) * 12), 0f, 0.22f, 0.3f, random);
                }
                else if (s == 0 || s == 2 || s == 4 || s == 6)
                {
                    // 근음 - 옥타브 - 5도 - 옥타브 (통통 튀게)
                    int offset = s == 0 ? 0 : s == 4 ? 7 : 12;
                    AddTone(buffer, start, stepSamples * 1.6f, Wave.Square, MidiToHz(bass + offset), 0f, 0.2f, 0.6f, random);
                }

                // 화음 분산 (사인파, 작게): 근음 - 3도 - 5도 - 3도
                int arpRoot = 60 + root > 66 ? 48 + root : 60 + root;
                int[] arp = { 0, third, 7, third };
                AddTone(buffer, start, stepSamples * 0.9f, Wave.Sine, MidiToHz(arpRoot + arp[s % 4]), 0f, 0.1f, 0.7f, random);

                // 북: 킥 1·3박 (전투는 2박 뒤 반박에도), 스네어 2·4박, 하이햇 엇박 (전투는 매 칸)
                if (s == 0 || s == 4 || (song.Driving && s == 3))
                {
                    AddTone(buffer, start, SampleRate * 0.12f, Wave.Sine, 150f, 45f, 0.45f, 1f, random);
                }
                if (s == 2 || s == 6)
                {
                    AddTone(buffer, start, SampleRate * 0.1f, Wave.Noise, 6000f, 0f, song.Driving ? 0.22f : 0.14f, 1f, random);
                }
                if (song.Driving || s % 2 == 1)
                {
                    AddTone(buffer, start, SampleRate * 0.03f, Wave.Noise, 14000f, 0f, 0.07f, 1f, random);
                }
            }
        }

        Normalize(buffer);
        return buffer;
    }

    private static void AddLead(float[] buffer, Song song, int midi, int startStep, int steps, int stepSamples, System.Random random)
    {
        // 음 사이가 살짝 끊어지게 90%만 울린다
        AddTone(buffer, startStep * stepSamples, steps * stepSamples * 0.9f, song.LeadWave, MidiToHz(midi), 0f, 0.3f,
            song.LeadDecay, random);
    }

    // 한 음을 buffer에 더한다. to가 0이면 높이 그대로, 아니면 from → to로 미끄러짐 (노이즈는 값이 바뀌는 빠르기).
    // 시작 2ms는 서서히 커지고, 끝은 음 길이의 25%(노이즈 북은 5ms) 동안 부드럽게 0까지 줄인다. 곡 끝을 넘는 부분은 자른다.
    // 사각파는 효과음과 같이 진폭을 줄이고 높은 배음을 깎아 덜 날카롭게
    private static void AddTone(float[] buffer, int start, float lengthSamples, Wave wave, float from, float to,
        float volume, float decay, System.Random random)
    {
        int length = Mathf.Min(Mathf.RoundToInt(lengthSamples), buffer.Length - start);
        int attack = Mathf.Min(length, SampleRate / 500);
        int release = Mathf.Min(length, wave == Wave.Noise ? SampleRate / 200 : Mathf.Max(SampleRate / 200, length / 4));
        float lowPass = 0f;
        float phase = 0f;
        float noisePhase = 1f;
        float noiseValue = 0f;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / length;
            float frequency = to > 0f ? Mathf.Lerp(from, to, t) : from;
            phase = (phase + frequency / SampleRate) % 1f;

            float value;
            switch (wave)
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
                    noisePhase += frequency / SampleRate;
                    if (noisePhase >= 1f)
                    {
                        noisePhase -= 1f;
                        noiseValue = (float)random.NextDouble() * 2f - 1f;
                    }
                    value = noiseValue;
                    break;
            }
            if (wave == Wave.Square || wave == Wave.Pulse)
            {
                lowPass += (value * SfxBuilder.SquareGain - lowPass) * SfxBuilder.SquareSmoothing;
                value = lowPass;
            }

            float envelope = (1f - decay * t) * SfxBuilder.FadeOut(length - i, release);
            if (i < attack)
            {
                envelope *= (float)i / attack;
            }
            buffer[start + i] += value * envelope * volume;
        }
    }

    private static void Normalize(float[] buffer)
    {
        float max = 0f;
        foreach (float sample in buffer)
        {
            max = Mathf.Max(max, Mathf.Abs(sample));
        }
        if (max <= 0f)
        {
            return;
        }
        float scale = Peak / max;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] *= scale;
        }
    }

    private static readonly Dictionary<char, int> NoteOffsets = new Dictionary<char, int>
    {
        { 'C', 0 }, { 'D', 2 }, { 'E', 4 }, { 'F', 5 }, { 'G', 7 }, { 'A', 9 }, { 'B', 11 },
    };

    // "G#5" → MIDI 번호 (C4 = 60)
    private static int ParseNote(string token)
    {
        int semitone = NoteOffsets[token[0]];
        int index = 1;
        if (token[index] == '#')
        {
            semitone++;
            index++;
        }
        int octave = int.Parse(token.Substring(index));
        return (octave + 1) * 12 + semitone;
    }

    // "Am" → 근음(0~11), 단조 여부
    private static void ParseChord(string chord, out int root, out bool minor)
    {
        root = NoteOffsets[chord[0]];
        int index = 1;
        if (chord.Length > index && chord[index] == '#')
        {
            root++;
            index++;
        }
        minor = chord.Length > index && chord[index] == 'm';
    }

    private static float MidiToHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
}
