using UnityEngine;

// 효과음 재생: AudioSource 몇 개를 돌려 쓴다. 같은 소리는 한 프레임에 한 번, SoundConfig의 최소 간격 안에는 다시 내지 않는다.
// 켜기/끄기와 음량은 PlayerPrefs에 저장. 어디서나 AudioManager.Play(Sfx.Buy)로 부른다 (씬에 없으면 조용히 무시)
public class AudioManager : MonoBehaviour
{
    private const string EnabledKey = "sfxEnabled";
    private const string VolumeKey = "sfxVolume";

    [SerializeField] private SoundConfig _config;

    private static AudioManager _instance;

    private AudioSource[] _sources;
    private int _nextSource;
    // Sfx 순서대로 미리 찾아 둔다
    private SoundConfig.Entry[] _entries;
    private int[] _lastFrame;
    private float[] _lastTime;

    private bool? _sfxEnabled;
    private float? _sfxVolume;

    // 다른 화면의 OnEnable이 먼저 불려도 쓸 수 있게 처음 쓸 때 읽는다
    public bool SfxEnabled => _sfxEnabled ??= PlayerPrefs.GetInt(EnabledKey, 1) == 1;
    public float SfxVolume => _sfxVolume ??= PlayerPrefs.GetFloat(VolumeKey, _config.DefaultVolume);

    private void Awake()
    {
        _instance = this;

        _sources = new AudioSource[_config.SourceCount];
        for (int i = 0; i < _sources.Length; i++)
        {
            _sources[i] = gameObject.AddComponent<AudioSource>();
            _sources[i].playOnAwake = false;
        }

        int count = System.Enum.GetValues(typeof(Sfx)).Length;
        _entries = new SoundConfig.Entry[count];
        _lastFrame = new int[count];
        _lastTime = new float[count];
        for (int i = 0; i < count; i++)
        {
            _entries[i] = _config.Find((Sfx)i);
            _lastFrame[i] = -1;
            _lastTime[i] = float.NegativeInfinity;
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public static void Play(Sfx sfx)
    {
        if (_instance != null)
        {
            _instance.PlaySfx(sfx);
        }
    }

    public void SetSfxEnabled(bool enabled)
    {
        _sfxEnabled = enabled;
        PlayerPrefs.SetInt(EnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, _sfxVolume.Value);
        PlayerPrefs.Save();
    }

    private void PlaySfx(Sfx sfx)
    {
        int index = (int)sfx;
        SoundConfig.Entry entry = _entries[index];
        if (!SfxEnabled || entry == null || entry.clip == null)
        {
            return;
        }
        // 2배속 전투에서도 실제 시간으로 간격을 둔다
        float now = Time.unscaledTime;
        if (_lastFrame[index] == Time.frameCount || now - _lastTime[index] < entry.minInterval)
        {
            return;
        }
        _lastFrame[index] = Time.frameCount;
        _lastTime[index] = now;

        AudioSource source = NextSource();
        source.clip = entry.clip;
        source.volume = entry.volume * SfxVolume;
        source.Play();
    }

    // 쉬고 있는 AudioSource를 먼저 쓰고, 모두 재생 중이면 가장 오래전에 시작한 것을 끊고 쓴다
    private AudioSource NextSource()
    {
        for (int i = 0; i < _sources.Length; i++)
        {
            AudioSource source = _sources[(_nextSource + i) % _sources.Length];
            if (!source.isPlaying)
            {
                _nextSource = (_nextSource + i + 1) % _sources.Length;
                return source;
            }
        }
        AudioSource oldest = _sources[_nextSource];
        _nextSource = (_nextSource + 1) % _sources.Length;
        return oldest;
    }
}
