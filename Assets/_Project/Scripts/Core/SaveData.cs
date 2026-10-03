using System;
using System.Collections.Generic;

// 저장 파일 형식 (JsonUtility). 크리처·트레이너·아이템은 에셋 이름으로 저장하고, 빈 칸은 ""

// meta.json: 판이 끝나도 남는 정보 (13번 메타 성장에서 사용)
[Serializable]
public class MetaSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int stars;
    public List<string> unlockedTrainers = new List<string>();
    public List<string> unlockedCreatures = new List<string>();
    // 튜토리얼을 끝냈거나 건너뛰었으면 true (첫 판에서만 튜토리얼)
    public bool tutorialCompleted;
    // 5라운드 보스 전 "앞줄에는 체력 높은 크리처" 도움말을 봤으면 true (한 번만 보여 줌)
    public bool frontRowTipShown;
    // 클리어한 난이도 (에셋 이름). 다음 난이도를 여는 데 쓴다
    public List<string> clearedDifficulties = new List<string>();
}

// run.json: 진행 중인 판 (상점 단계 상태)
[Serializable]
public class RunSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string trainer;
    // 난이도 에셋 이름과 지금 층 (1부터)
    public string difficulty;
    public int floor;
    // 예전 저장(층 구조 전)의 라운드. floor가 0이면 보통 난이도 같은 층으로 이어한다
    public int round;
    public int lives;
    public int gold;
    // 팀 칸 순서 (0~1 앞줄, 2~4 뒷줄)
    public CreatureSaveData[] team;
    public CreatureSaveData[] bench;
    public string[] items;
    public string[] shopCreatures;
    public string shopItem;
    // 이번 판에서 받은 별 (판이 끝날 때 meta.json에 더함)
    public int starsEarned;
}

[Serializable]
public class CreatureSaveData
{
    public string creature;
    public int level;
    public string item;
}
