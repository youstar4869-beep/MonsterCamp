using System.Collections.Generic;

public enum BalanceBotKind
{
    // 아무거나 산다, 아무 데나 놓는다
    Random,
    // 합성 우선, 살 수 있는 가장 비싼 크리처, 강한 순으로 배치
    Basic,
    // 한 타입을 정해서 모으거나, 조합 시너지 하나를 정해서 그 타입을 1마리씩 모은다
    Synergy,
    // 6라운드까지 골드를 모아 이자를 받고 7라운드부터 크게 쓴다
    Saver,
    // 4·5골드만 사서 같은 크리처를 모아 ★★★를 노린다 (팀 칸이 비면 남은 골드로 싼 크리처를 채움)
    HighTier
}

// 밸런스 시뮬레이터의 봇: 상점 단계에서 사고·팔고·새로고침하고 팀을 놓는다.
// 크리처의 세기는 RoundDataBuilder의 세기 지수와 같은 식 (레벨 배율 × √(체력×공격력×공격 속도))
public static class BalanceBots
{
    // 시너지 봇이 고른 타입 크리처를 이만큼 더 강하게 쳐 준다
    private const float FocusWeight = 1.5f;
    private const float FocusBuyBonus = 3f;
    // 라운드당 새로고침 최대 횟수
    private const int BasicMaxRefresh = 4;
    private const int SynergyMaxRefresh = 6;
    private const int SaverSpendMaxRefresh = 10;
    private const int HighTierMaxRefresh = 10;
    // 고등급 봇이 사는 가장 낮은 등급 (등급 = 가격)
    private const int HighTierMinTier = 4;
    // 저축 봇: 이 라운드부터 크게 쓴다. 그 전에는 남은 골드를 (라운드 × SaverReservePerRound) 아래로 내리지 않는다
    public const int SaverSpendRound = 7;
    private const int SaverReservePerRound = 6;
    // 아이템은 2라운드부터 (1라운드는 크리처부터)
    private const int ItemFromRound = 2;

    private static readonly List<CreatureInstance> Owned = new List<CreatureInstance>();
    private static readonly List<CreatureInstance> TeamPick = new List<CreatureInstance>();
    private static readonly List<CreatureInstance> BenchPick = new List<CreatureInstance>();
    private static readonly bool[] Skipped = new bool[BalanceRun.ShopSlotCount];
    private static readonly HashSet<CreatureData> Seen = new HashSet<CreatureData>();
    // 시너지 단계별 마릿수 (BalanceSimulator가 SynergyConfig 값으로 채운다)
    public static int[] SynergyThresholds = { 2, 4 };
    private static readonly Dictionary<CreatureInstance, float> AdjustedScore = new Dictionary<CreatureInstance, float>();
    // 시너지 봇이 노리는 조합 시너지의 타입 (타입을 모으는 판이면 null). PlayShop에서 정한다
    private static CreatureType[] _mixedTypes;

    public static float Power(CreatureData data, int level)
    {
        return data.GetLevelMultiplier(level) * (float)System.Math.Sqrt(data.MaxHealth * data.Attack * data.AttackSpeed);
    }

    public static void PlayShop(BalanceBotKind kind, BalanceRun run, CreatureType focus, CreatureType[] mixedTypes,
        System.Random random)
    {
        _mixedTypes = kind == BalanceBotKind.Synergy ? mixedTypes : null;
        switch (kind)
        {
            case BalanceBotKind.Random:
                PlayRandom(run, random);
                break;
            case BalanceBotKind.Basic:
                PlaySmart(run, null, 0, BasicMaxRefresh);
                break;
            case BalanceBotKind.Synergy:
                PlaySmart(run, mixedTypes == null ? focus : (CreatureType?)null, 0, SynergyMaxRefresh);
                break;
            case BalanceBotKind.Saver:
                bool spend = run.Round >= SaverSpendRound;
                int reserve = spend ? 0 : run.Round * SaverReservePerRound;
                PlaySmart(run, null, reserve, spend ? SaverSpendMaxRefresh : 0);
                break;
            case BalanceBotKind.HighTier:
                PlaySmart(run, null, 0, HighTierMaxRefresh, HighTierMinTier);
                FillTeam(run);
                break;
        }
    }

    // ---------- 랜덤 봇 ----------

    private static void PlayRandom(BalanceRun run, System.Random random)
    {
        var actions = new List<int>();
        for (int guard = 0; guard < 60; guard++)
        {
            // 0~4: 상점 칸 구매, 5: 새로고침, 6: 아이템 구매, 7: 끝
            actions.Clear();
            for (int i = 0; i < run.Offers.Length; i++)
            {
                if (run.Offers[i] != null && run.Config.GetPrice(run.Offers[i]) <= run.Gold)
                {
                    actions.Add(i);
                }
            }
            if (run.Gold >= run.RefreshCost)
            {
                actions.Add(5);
            }
            if (run.ItemOffer != null && run.ItemOffer.Price <= run.Gold)
            {
                actions.Add(6);
            }
            run.CollectOwned(Owned);
            // 크리처가 하나도 없으면 끝내지 않는다 (전투 시작 불가)
            if (Owned.Count > 0 || actions.Count == 0)
            {
                actions.Add(7);
            }

            int action = actions[random.Next(actions.Count)];
            if (action == 7)
            {
                break;
            }
            if (action == 5)
            {
                run.Refresh();
            }
            else if (action == 6)
            {
                run.BuyItem();
            }
            else
            {
                run.TryBuy(action);
            }
        }

        FuseAll(run);
        // 아무 크리처나 아무 칸에
        run.CollectOwned(Owned);
        Shuffle(Owned, random);
        var team = new CreatureInstance[BalanceRun.TeamSlotCount];
        BenchPick.Clear();
        var freeSlots = new List<int> { 0, 1, 2, 3, 4 };
        Shuffle(freeSlots, random);
        int placed = 0;
        foreach (CreatureInstance creature in Owned)
        {
            if (placed < team.Length)
            {
                team[freeSlots[placed++]] = creature;
            }
            else
            {
                BenchPick.Add(creature);
            }
        }
        run.Place(team, BenchPick);
        for (int i = 0; i < run.Items.Length; i++)
        {
            if (run.Items[i] != null && placed > 0)
            {
                CreatureInstance target = team[freeSlots[random.Next(placed)]];
                run.Equip(i, target);
            }
        }
    }

    // ---------- 기본·시너지·저축 봇 ----------

    // focus: 시너지 봇의 타입 (없으면 null), reserve: 남겨 둘 골드, minTier: 이 등급 아래는 사지 않음
    private static void PlaySmart(BalanceRun run, CreatureType? focus, int reserve, int maxRefresh, int minTier = 1)
    {
        int refreshes = 0;
        System.Array.Clear(Skipped, 0, Skipped.Length);
        for (int guard = 0; guard < 200; guard++)
        {
            FuseAll(run);
            Arrange(run, focus);
            int spendable = run.Gold - reserve;

            int pick = ChooseOffer(run, focus, spendable, minTier);
            if (pick >= 0)
            {
                if (!MakeRoom(run, run.Offers[pick], focus) || !run.TryBuy(pick))
                {
                    Skipped[pick] = true;
                }
                continue;
            }

            if (run.Round >= ItemFromRound && run.ItemOffer != null && spendable >= run.ItemOffer.Price
                && System.Array.IndexOf(run.Items, null) >= 0)
            {
                run.BuyItem();
                continue;
            }

            // 새로 뽑은 뒤 가장 싼 살 등급(등급 = 가격)이라도 살 수 있을 때만
            if (refreshes < maxRefresh && spendable >= run.RefreshCost + minTier)
            {
                run.Refresh();
                refreshes++;
                System.Array.Clear(Skipped, 0, Skipped.Length);
                continue;
            }
            break;
        }
        Arrange(run, focus);
    }

    // 살 상점 칸: 합성이 되는 것(가진 것과 같은 크리처) > 시너지 타입 > 가장 비싼 것. 없으면 -1
    private static int ChooseOffer(BalanceRun run, CreatureType? focus, int spendable, int minTier)
    {
        run.CollectOwned(Owned);
        int best = -1;
        float bestScore = 0f;
        for (int i = 0; i < run.Offers.Length; i++)
        {
            CreatureData offer = run.Offers[i];
            if (offer == null || Skipped[i] || offer.Tier < minTier || run.Config.GetPrice(offer) > spendable)
            {
                continue;
            }

            float score = offer.Tier;
            if (run.CountSame(offer, 1) > 0)
            {
                score += 100f;
            }
            if (focus.HasValue && offer.Type == focus.Value)
            {
                // 고른 타입은 3골드 더 비싼 크리처만큼 먼저 산다
                score += FocusBuyBonus;
            }
            else if (IsMixedType(offer.Type) && !Owned.Exists(c => c.Data.Type == offer.Type))
            {
                // 조합 시너지: 아직 없는 타입만 먼저 산다
                score += FocusBuyBonus;
            }
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    // 고등급 봇: 가진 크리처가 팀 칸보다 적으면 남은 골드로 가장 싼 크리처부터 채운다 (나중에 MakeRoom이 먼저 판다)
    private static void FillTeam(BalanceRun run)
    {
        while (true)
        {
            run.CollectOwned(Owned);
            if (Owned.Count >= BalanceRun.TeamSlotCount)
            {
                break;
            }
            int cheapest = -1;
            for (int i = 0; i < run.Offers.Length; i++)
            {
                CreatureData offer = run.Offers[i];
                if (offer != null && run.Config.GetPrice(offer) <= run.Gold
                    && (cheapest < 0 || run.Config.GetPrice(offer) < run.Config.GetPrice(run.Offers[cheapest])))
                {
                    cheapest = i;
                }
            }
            if (cheapest < 0 || !run.TryBuy(cheapest))
            {
                break;
            }
        }
        FuseAll(run);
        Arrange(run, null);
    }

    // 대기석이 꽉 찼으면 가장 약한 크리처를 판다 (살 크리처가 더 강할 때만, 합성 짝이 있는 것은 팔지 않음)
    private static bool MakeRoom(BalanceRun run, CreatureData offer, CreatureType? focus)
    {
        if (System.Array.IndexOf(run.Bench, null) >= 0 || run.MergesOnBuy(offer))
        {
            return true;
        }

        int weakest = -1;
        float weakestScore = float.MaxValue;
        for (int i = 0; i < run.Bench.Length; i++)
        {
            CreatureInstance creature = run.Bench[i];
            if (creature == null || creature.Data == offer || run.CountSame(creature.Data, creature.Level) > 1
                || IsFusionMaterial(run, creature))
            {
                continue;
            }
            float score = Score(creature.Data, creature.Level, focus);
            if (score < weakestScore)
            {
                weakestScore = score;
                weakest = i;
            }
        }

        bool pairs = run.CountSame(offer, 1) > 0;
        if (weakest < 0 || (!pairs && weakestScore >= Score(offer, 1, focus)))
        {
            return false;
        }
        return run.TrySell(false, weakest);
    }

    // 조합 해금이면 만들 수 있는 조합은 바로 만든다 (조합 크리처가 재료 둘보다 강함)
    private static void FuseAll(BalanceRun run)
    {
        if (!run.FusionUnlocked)
        {
            return;
        }
        bool made = true;
        while (made)
        {
            made = false;
            foreach (FusionRecipe recipe in run.Config.FusionRecipes)
            {
                // 히든 조합은 봇이 쓰지 않음
                if (recipe != null && !recipe.Hidden && run.TryFuse(recipe))
                {
                    made = true;
                    break;
                }
            }
        }
    }

    // 조합 해금이면 ★★ 이상 재료는 짝 재료를 가지고 있는 동안 팔지 않는다
    private static bool IsFusionMaterial(BalanceRun run, CreatureInstance creature)
    {
        if (!run.FusionUnlocked || creature.Level < Fusion.MinLevel)
        {
            return false;
        }
        foreach (FusionRecipe recipe in run.Config.FusionRecipes)
        {
            if (recipe == null || recipe.Hidden)
            {
                continue;
            }
            CreatureData partner = recipe.MaterialA == creature.Data ? recipe.MaterialB
                : recipe.MaterialB == creature.Data ? recipe.MaterialA
                : null;
            if (partner != null && run.CountOwned(partner) > 0)
            {
                return true;
            }
        }
        return false;
    }

    private static float Score(CreatureData data, int level, CreatureType? focus)
    {
        float power = Power(data, level);
        return (focus.HasValue && data.Type == focus.Value) || IsMixedType(data.Type) ? power * FocusWeight : power;
    }

    private static bool IsMixedType(CreatureType type)
    {
        return _mixedTypes != null && System.Array.IndexOf(_mixedTypes, type) >= 0;
    }

    // 강한 순으로 5마리를 팀에: 그중 체력이 높은 2마리가 앞줄, 나머지는 뒷줄. 아이템은 팀에 끼운다
    private static void Arrange(BalanceRun run, CreatureType? focus)
    {
        run.CollectOwned(Owned);
        Owned.Sort((a, b) => Power(b.Data, b.Level).CompareTo(Power(a.Data, a.Level)));

        if (focus.HasValue)
        {
            // 시너지 봇: 고른 타입은 더 강하게 친다. 같은 크리처는 시너지에서 1번만 세므로 가장 강한 1마리만
            Seen.Clear();
            AdjustedScore.Clear();
            foreach (CreatureInstance creature in Owned)
            {
                bool counts = creature.Data.Type == focus.Value && Seen.Add(creature.Data);
                AdjustedScore[creature] = Power(creature.Data, creature.Level) * (counts ? FocusWeight : 1f);
            }
            Owned.Sort((a, b) => AdjustedScore[b].CompareTo(AdjustedScore[a]));
            PutSynergyFirst(focus.Value);
        }
        else if (_mixedTypes != null)
        {
            PutMixedFirst();
        }

        TeamPick.Clear();
        BenchPick.Clear();
        for (int i = 0; i < Owned.Count; i++)
        {
            (i < BalanceRun.TeamSlotCount ? TeamPick : BenchPick).Add(Owned[i]);
        }
        TeamPick.Sort((a, b) => b.Data.GetMaxHealth(b.Level).CompareTo(a.Data.GetMaxHealth(a.Level)));
        // 앞줄 2칸은 체력 순 그대로, 뒷줄은 강한 순
        if (TeamPick.Count > BattleSimulation.FrontRowCount)
        {
            TeamPick.Sort(BattleSimulation.FrontRowCount, TeamPick.Count - BattleSimulation.FrontRowCount,
                Comparer<CreatureInstance>.Create((a, b) =>
                    Score(b.Data, b.Level, focus).CompareTo(Score(a.Data, a.Level, focus))));
        }
        run.Place(TeamPick, BenchPick);

        for (int i = 0; i < run.Items.Length; i++)
        {
            ItemData item = run.Items[i];
            if (item == null)
            {
                continue;
            }
            CreatureInstance target = PickItemTarget(run, item);
            if (target != null)
            {
                run.Equip(i, target);
            }
        }
    }

    // 고른 타입(같은 크리처는 1마리, 5골드는 2마리로 셈)으로 마지막 시너지 단계를 채울 수 있으면
    // 그 크리처들을 Owned 맨 앞으로 (강한 순으로 단계 마릿수가 찰 때까지)
    private static void PutSynergyFirst(CreatureType focus)
    {
        Seen.Clear();
        TeamPick.Clear();
        int count = 0;
        int total = 0;
        foreach (CreatureInstance creature in Owned)
        {
            if (creature.Data.Type == focus && Seen.Add(creature.Data))
            {
                total += TypeSynergy.GetSynergyCount(creature.Data);
            }
        }
        // 가장 높은 단계만 (1단계는 보너스가 작아서 약한 크리처를 억지로 넣지 않는다)
        int top = SynergyThresholds[SynergyThresholds.Length - 1];
        if (total < top)
        {
            return;
        }

        Seen.Clear();
        foreach (CreatureInstance creature in Owned)
        {
            if (count < top && TeamPick.Count < BalanceRun.TeamSlotCount && creature.Data.Type == focus
                && Seen.Add(creature.Data))
            {
                TeamPick.Add(creature);
                count += TypeSynergy.GetSynergyCount(creature.Data);
            }
        }
        foreach (CreatureInstance creature in TeamPick)
        {
            Owned.Remove(creature);
        }
        Owned.InsertRange(0, TeamPick);
    }

    // 노리는 조합 시너지 타입을 모두 가졌으면 타입마다 가장 강한 1마리를 Owned 맨 앞으로 (Owned는 강한 순)
    private static void PutMixedFirst()
    {
        TeamPick.Clear();
        foreach (CreatureType type in _mixedTypes)
        {
            CreatureInstance best = Owned.Find(c => c.Data.Type == type);
            if (best == null)
            {
                return;
            }
            TeamPick.Add(best);
        }
        if (TeamPick.Count > BalanceRun.TeamSlotCount)
        {
            return;
        }
        foreach (CreatureInstance creature in TeamPick)
        {
            Owned.Remove(creature);
        }
        Owned.InsertRange(0, TeamPick);
    }

    // 체력·가시·회복은 앞줄 먼저, 공격력·공격 속도는 가장 센 크리처, 행운 동전은 아무나 (아이템이 없는 팀 크리처만)
    private static CreatureInstance PickItemTarget(BalanceRun run, ItemData item)
    {
        bool defensive = item.Effect == ItemEffect.HealthBonus || item.Effect == ItemEffect.Thorns
                         || item.Effect == ItemEffect.Regen;
        CreatureInstance best = null;
        float bestScore = float.MinValue;
        for (int i = 0; i < run.Team.Length; i++)
        {
            CreatureInstance creature = run.Team[i];
            if (creature == null || creature.Item != null)
            {
                continue;
            }
            float score = defensive
                ? (i < BattleSimulation.FrontRowCount ? 1e6f : 0f) + creature.Data.GetMaxHealth(creature.Level)
                : creature.Data.GetAttack(creature.Level) * creature.Data.AttackSpeed;
            if (score > bestScore)
            {
                bestScore = score;
                best = creature;
            }
        }
        return best;
    }

    private static void Shuffle<T>(IList<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
