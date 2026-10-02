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

// 효과음 목록과 음량. 메뉴 "MonsterCamp → 효과음 만들기"가 소리 파일을 만들어 채운다 (바꾼 음량·간격은 그대로 둔다)
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
    [Tooltip("처음 실행할 때 효과음 음량 (설정에서 바꾸면 PlayerPrefs에 저장)")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultVolume = 0.8f;
    [SerializeField] private Entry[] _entries = new Entry[0];

    public int SourceCount => Mathf.Max(1, _sourceCount);
    public float DefaultVolume => _defaultVolume;

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
