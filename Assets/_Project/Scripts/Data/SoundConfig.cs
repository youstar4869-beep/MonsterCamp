using System;
using UnityEngine;

// 효과음 종류 (SoundConfig 목록과 SfxBuilder가 만드는 파일 이름에 같은 이름을 쓴다)
public enum Sfx
{
    Button,
    Buy,
    Sell,
    Merge,
    Attack,
    Hit,
    Skill,
    Faint,
    Win,
    Lose,
}

// 배경음 (None이면 끔)
public enum Bgm
{
    None,
    Shop,
    Battle,
}

// 효과음·배경음 목록과 음량. 메뉴 "MonsterCamp → 효과음 만들기"·"배경음 만들기"가 소리 파일을 만들어 채운다 (바꾼 음량·간격은 그대로 둔다)
[CreateAssetMenu(fileName = "SoundConfig", menuName = "MonsterCamp/Sound Config")]
public class SoundConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("같은 소리를 다시 내기까지 최소 간격 (초, 실제 시간). 0이면 한 프레임에 한 번만")]
        public float minInterval;
    }

    [Tooltip("효과음을 동시에 낼 수 있는 수 (AudioSource 개수)")]
    [SerializeField] private int _sourceCount = 8;
    [Tooltip("처음 실행할 때 효과음·배경음 설정 음량 (타이틀 슬라이더로 바꾸면 PlayerPrefs에 저장)")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultVolume = 0.7f;
    [SerializeField] private Entry[] _entries = new Entry[0];

    [Header("배경음")]
    [Tooltip("상점·타이틀 등 전투가 아닌 화면")]
    [SerializeField] private AudioClip _shopBgm;
    [SerializeField] private AudioClip _battleBgm;
    [Tooltip("효과음(구매·판매 0.45)의 60% 정도. 실제 크기 = 이 값 × 설정 음량")]
    [Range(0f, 1f)]
    [SerializeField] private float _bgmVolume = 0.27f;
    [Tooltip("곡이 바뀔 때 페이드 시간 (초)")]
    [SerializeField] private float _bgmFadeSeconds = 0.5f;

    public int SourceCount => Mathf.Max(1, _sourceCount);
    public float DefaultVolume => _defaultVolume;
    public float BgmVolume => _bgmVolume;
    public float BgmFadeSeconds => Mathf.Max(0.01f, _bgmFadeSeconds);

    public AudioClip GetBgm(Bgm bgm)
    {
        switch (bgm)
        {
            case Bgm.Shop:
                return _shopBgm;
            case Bgm.Battle:
                return _battleBgm;
            default:
                return null;
        }
    }

    public Entry Find(Sfx id)
    {
        foreach (Entry entry in _entries)
        {
            if (entry.id == id)
            {
                return entry;
            }
        }
        return null;
    }
}
