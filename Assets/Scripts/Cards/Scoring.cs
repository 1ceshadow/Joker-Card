using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 计分引擎
/// 位置：Assets/Scripts/Cards/Scoring.cs
/// 
/// 计分流程（严格按顺序）：
/// 1. 牌型识别 - 判定当前扑克牌型
/// 2. 基础数值获取 - 根据牌型等级获取基础chips和mult
/// 3. 逐张计分（从左到右）：
///    A. 加上该牌的点数chips
///    B. 加上强化(Enhancement)/版本(Edition)的chips/mult加成
///    C. 触发所有"单张牌计分"小丑效果 (OnCardScore)
///    D. 应用Glass/Polychrome等×mult效果
///    E. 若有红印(Red Seal)，重复A~D一次
/// 4. 手牌中持有效果 - Steel牌×1.5 mult
/// 5. 小丑独立效果（从左到右）：
///    - 先结算所有 +chips 和 +mult
///    - 最后结算所有 ×mult（这就是小丑顺序重要的原因）
/// 6. 最终计算：分数 = 总chips × 总mult
/// </summary>
public static class Scoring
{
    #region 牌型定义

    /// <summary>
    /// 牌型枚举
    /// </summary>
    public enum HandType
    {
        HighCard,        // 高牌/散牌
        Pair,            // 对子
        TwoPair,         // 两对
        ThreeOfAKind,    // 三条
        Straight,        // 顺子
        Flush,           // 同花
        FullHouse,       // 葫芦
        FourOfAKind,     // 四条
        StraightFlush,   // 同花顺
        RoyalFlush       // 皇家同花顺 (10-J-Q-K-A同花)
    }

    /// <summary>
    /// 牌型数据：基础chips, 基础mult, 每级增加chips, 每级增加mult
    /// </summary>
    public class HandTypeInfo
    {
        public int baseChips;
        public int baseMult;
        public int chipsPerLevel;  // 每级增加的chips
        public int multPerLevel;   // 每级增加的mult
        public int level;          // 当前等级（星球卡升级）

        public HandTypeInfo(int baseChips, int baseMult, int chipsPerLevel = 10, int multPerLevel = 1)
        {
            this.baseChips = baseChips;
            this.baseMult = baseMult;
            this.chipsPerLevel = chipsPerLevel;
            this.multPerLevel = multPerLevel;
            this.level = 1;
        }

        public int GetChips() => baseChips + (level - 1) * chipsPerLevel;
        public int GetMult() => baseMult + (level - 1) * multPerLevel;
    }

    /// <summary>
    /// 牌型基础数据
    /// </summary>
    private static Dictionary<HandType, HandTypeInfo> _handTypeData;
    public static Dictionary<HandType, HandTypeInfo> HandTypeData
    {
        get
        {
            if (_handTypeData == null)
            {
                _handTypeData = new Dictionary<HandType, HandTypeInfo>
                {
                    { HandType.HighCard,      new HandTypeInfo(5, 1, 10, 1) },
                    { HandType.Pair,          new HandTypeInfo(10, 2, 15, 1) },
                    { HandType.TwoPair,       new HandTypeInfo(20, 2, 20, 1) },
                    { HandType.ThreeOfAKind,  new HandTypeInfo(30, 3, 20, 2) },
                    { HandType.Straight,      new HandTypeInfo(30, 4, 30, 3) },
                    { HandType.Flush,         new HandTypeInfo(35, 4, 15, 2) },
                    { HandType.FullHouse,     new HandTypeInfo(40, 4, 25, 2) },
                    { HandType.FourOfAKind,   new HandTypeInfo(60, 7, 30, 3) },
                    { HandType.StraightFlush, new HandTypeInfo(100, 8, 40, 4) },
                    { HandType.RoyalFlush,    new HandTypeInfo(100, 8, 40, 4) }
                };
            }
            return _handTypeData;
        }
    }

    #endregion

    #region 计分结果

    /// <summary>
    /// 计分详情（用于UI显示和调试）
    /// </summary>
    public class ScoreResult
    {
        public HandType handType;
        public int baseChips;
        public int baseMult;
        public int finalChips;
        public float finalMult;  // 使用float因为有×mult
        public int finalScore;
        public List<string> breakdown;  // 计分过程详情
        public List<CardData> destroyedCards;  // 被销毁的牌（Glass效果）

        public ScoreResult()
        {
            breakdown = new List<string>();
            destroyedCards = new List<CardData>();
        }
    }

    #endregion

    #region 主计分方法

    /// <summary>
    /// 计算分数（简化版，向后兼容）
    /// </summary>
    public static int CalculateScore(List<CardData> cards, List<JokerData> jokers)
    {
        var result = CalculateScoreDetailed(cards, jokers, new List<CardData>(), JokerContext.Default);
        return result.finalScore;
    }

    /// <summary>
    /// 计算分数（详细版）
    /// </summary>
    /// <param name="playedCards">出的牌（最多5张）</param>
    /// <param name="jokers">小丑牌列表</param>
    /// <param name="heldCards">手牌中持有的牌</param>
    /// <param name="context">上下文信息</param>
    /// <returns>详细计分结果</returns>
    public static ScoreResult CalculateScoreDetailed(
        List<CardData> playedCards, 
        List<JokerData> jokers, 
        List<CardData> heldCards,
        JokerContext context)
    {
        var result = new ScoreResult();

        if (playedCards == null || playedCards.Count == 0)
        {
            result.finalScore = 0;
            return result;
        }

        // 更新上下文的小丑数量
        context.jokerCount = jokers?.Count ?? 0;

        // ========== 步骤1: 牌型识别 ==========
        result.handType = DetectHandType(playedCards);
        var handInfo = HandTypeData[result.handType];
        
        result.baseChips = handInfo.GetChips();
        result.baseMult = handInfo.GetMult();
        
        float currentChips = result.baseChips;
        float currentMult = result.baseMult;
        
        result.breakdown.Add($"牌型: {GetHandTypeName(result.handType)} - 基础 {result.baseChips} chips, {result.baseMult} mult");

        // ========== 步骤2: 确定计分牌 ==========
        // Stone牌计入出牌数量，但不参与点数/花色判定，这里我们让所有牌都参与计分
        List<CardData> scoringCards = GetScoringCards(playedCards, jokers);

        // 跟踪是否已有人头牌计分（用于Photograph小丑）
        bool hasFirstFaceCardScored = false;

        // ========== 步骤3: 逐张计分（从左到右） ==========
        foreach (CardData card in scoringCards)
        {
            // 检查是否被debuff（被禁用），如果是则跳过
            if (card.isDebuffed) continue;

            // 确定触发次数（红印重触发）
            int triggerCount = card.GetTriggerCount();

            for (int trigger = 0; trigger < triggerCount; trigger++)
            {
                // A. 加上该牌的点数chips
                int cardChips = card.GetBaseChipValue();
                currentChips += cardChips;

                // B. 加上强化(Enhancement)的chips/mult加成
                int enhancementChips = card.GetEnhancementChips();
                int enhancementMult = card.GetEnhancementAddMult();
                currentChips += enhancementChips;
                currentMult += enhancementMult;

                // C. 加上版本(Edition)的chips/mult加成
                int editionChips = card.GetEditionChips();
                int editionMult = card.GetEditionAddMult();
                currentChips += editionChips;
                currentMult += editionMult;

                // 记录breakdown
                if (trigger == 0)
                {
                    string cardInfo = $"  {card.GetDisplayName()}: +{cardChips} chips";
                    if (enhancementChips > 0) cardInfo += $", 强化 +{enhancementChips} chips";
                    if (enhancementMult > 0) cardInfo += $", 强化 +{enhancementMult} mult";
                    if (editionChips > 0) cardInfo += $", 版本 +{editionChips} chips";
                    if (editionMult > 0) cardInfo += $", 版本 +{editionMult} mult";
                    result.breakdown.Add(cardInfo);
                }
                else
                {
                    result.breakdown.Add($"  [红印重触发] {card.GetDisplayName()}");
                }

                // C2. 触发所有"单张牌计分"小丑效果 (OnCardScore)
                if (jokers != null)
                {
                    bool isFirstFaceCard = !hasFirstFaceCardScored && card.rank >= 11 && card.rank <= 13;
                    
                    foreach (var joker in jokers)
                    {
                        if (joker == null) continue;

                        // 被动效果（如EvenSteven/OddTodd）
                        var passiveBonus = joker.GetPassiveBonus(card);
                        currentChips += passiveBonus.chips;
                        currentMult += passiveBonus.mult;

                        // OnCardScore触发
                        var bonus = joker.OnCardScoreTrigger(card, playedCards, isFirstFaceCard);
                        currentChips += bonus.chips;
                        currentMult += bonus.mult;

                        // ×mult效果需要乘法
                        if (bonus.xMult > 1.0f)
                        {
                            currentMult *= bonus.xMult;
                            result.breakdown.Add($"    {joker.name}: ×{bonus.xMult} mult");
                        }
                        else if (bonus.chips > 0 || bonus.mult > 0)
                        {
                            result.breakdown.Add($"    {joker.name}: +{bonus.chips} chips, +{bonus.mult} mult");
                        }
                    }

                    if (isFirstFaceCard) hasFirstFaceCardScored = true;
                }

                // D. 应用卡牌自带的×mult效果
                // Glass强化: ×2 mult
                float cardXMult = card.GetEnhancementXMult();
                if (cardXMult > 1.0f)
                {
                    currentMult *= cardXMult;
                    result.breakdown.Add($"    Glass: ×{cardXMult} mult");

                    // Glass有1/4概率销毁
                    if (card.enhancement == CardData.Enhancement.Glass && card.CheckGlassDestroy())
                    {
                        result.destroyedCards.Add(card);
                        result.breakdown.Add($"    [Glass破碎] {card.GetDisplayName()}被销毁");
                    }
                }

                // Lucky检查: 1/5概率+20 mult
                if (card.enhancement == CardData.Enhancement.Lucky)
                {
                    int luckyMult = card.CheckLuckyMult();
                    if (luckyMult > 0)
                    {
                        currentMult += luckyMult;
                        result.breakdown.Add($"    Lucky: +{luckyMult} mult");
                    }
                    
                    int luckyMoney = card.CheckLuckyMoney();
                    if (luckyMoney > 0)
                    {
                        result.breakdown.Add($"    Lucky: +${luckyMoney}");
                        // 金钱需要通过其他方式处理
                    }
                }

                // Edition Polychrome: ×1.5 mult
                float editionXMult = card.GetEditionXMult();
                if (editionXMult > 1.0f)
                {
                    currentMult *= editionXMult;
                    result.breakdown.Add($"    Polychrome: ×{editionXMult} mult");
                }
            }
        }

        // ========== 步骤4: 手牌中持有效果 ==========
        if (heldCards != null && heldCards.Count > 0)
        {
            foreach (var card in heldCards)
            {
                if (card.enhancement == CardData.Enhancement.Steel)
                {
                    float steelXMult = card.GetSteelHeldXMult();
                    currentMult *= steelXMult;
                    result.breakdown.Add($"  [持有] Steel {card.GetDisplayName()}: ×{steelXMult} mult");
                }
            }
        }

        // ========== 步骤5: 小丑独立效果（从左到右） ==========
        if (jokers != null && jokers.Count > 0)
        {
            // 收集所有小丑的独立效果
            List<JokerData.JokerBonus> jokerBonuses = new List<JokerData.JokerBonus>();
            
            foreach (var joker in jokers)
            {
                if (joker == null) continue;
                var bonus = joker.OnJokerCalcTrigger(playedCards, heldCards ?? new List<CardData>(), result.handType, context);
                jokerBonuses.Add(bonus);
            }

            // 5a. 先结算所有 +chips（从左到右）
            for (int i = 0; i < jokerBonuses.Count; i++)
            {
                if (jokerBonuses[i].chips > 0)
                {
                    currentChips += jokerBonuses[i].chips;
                    result.breakdown.Add($"  {jokers[i].name}: +{jokerBonuses[i].chips} chips");
                }
            }

            // 5b. 再结算所有 +mult（从左到右）
            for (int i = 0; i < jokerBonuses.Count; i++)
            {
                if (jokerBonuses[i].mult > 0)
                {
                    currentMult += jokerBonuses[i].mult;
                    result.breakdown.Add($"  {jokers[i].name}: +{jokerBonuses[i].mult} mult");
                }
            }

            // 5c. 最后结算所有 ×mult（从左到右）- 这就是小丑顺序重要的原因
            for (int i = 0; i < jokerBonuses.Count; i++)
            {
                if (jokerBonuses[i].xMult > 1.0f)
                {
                    currentMult *= jokerBonuses[i].xMult;
                    result.breakdown.Add($"  {jokers[i].name}: ×{jokerBonuses[i].xMult} mult");
                }
            }
        }

        // ========== 步骤6: 最终计算 ==========
        result.finalChips = Mathf.RoundToInt(currentChips);
        result.finalMult = currentMult;
        result.finalScore = Mathf.RoundToInt(currentChips * currentMult);
        
        result.breakdown.Add($"最终: {result.finalChips} × {result.finalMult:F1} = {result.finalScore}");

        return result;
    }

    /// <summary>
    /// 获取参与计分的牌（排除被小丑过滤的牌）
    /// </summary>
    private static List<CardData> GetScoringCards(List<CardData> playedCards, List<JokerData> jokers)
    {
        if (jokers == null || jokers.Count == 0)
            return new List<CardData>(playedCards);

        var scoringCards = new List<CardData>();

        foreach (var card in playedCards)
        {
            bool shouldScore = true;

            // 检查是否有小丑会过滤这张牌
            foreach (var joker in jokers)
            {
                if (joker == null) continue;

                // EvenSteven/OddTodd等会过滤不符合条件的牌
                if (joker.type == JokerData.JokerType.EvenSteven || 
                    joker.type == JokerData.JokerType.OddTodd)
                {
                    if (!joker.ShouldScoreCard(card))
                    {
                        shouldScore = false;
                        break;
                    }
                }
            }

            if (shouldScore)
                scoringCards.Add(card);
        }

        return scoringCards;
    }

    #endregion

    #region 牌型检测

    /// <summary>
    /// 检测牌型（按优先级从高到低）
    /// 注意：Stone牌不参与点数/花色判定
    /// </summary>
    public static HandType DetectHandType(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0)
            return HandType.HighCard;

        // 过滤掉Stone牌（Stone牌无点数无花色，不参与牌型判定）
        List<CardData> validCards = cards.Where(c => c.enhancement != CardData.Enhancement.Stone).ToList();

        if (validCards.Count == 0)
            return HandType.HighCard;

        // 考虑Wild牌可以充当任意花色
        bool hasWild = validCards.Any(c => c.enhancement == CardData.Enhancement.Wild);

        // 按点数排序
        List<CardData> sortedCards = new List<CardData>(validCards);
        sortedCards.Sort((a, b) => a.rank.CompareTo(b.rank));

        bool isFlush = CheckFlush(sortedCards, hasWild);
        bool isStraight = CheckStraight(sortedCards);

        // 皇家同花顺 (10-J-Q-K-A同花)
        if (isStraight && isFlush && IsRoyalFlush(sortedCards))
            return HandType.RoyalFlush;

        // 同花顺
        if (isStraight && isFlush)
            return HandType.StraightFlush;

        // 四条
        if (GetNOfAKind(sortedCards, 4) > 0)
            return HandType.FourOfAKind;

        // 葫芦
        if (CheckFullHouse(sortedCards))
            return HandType.FullHouse;

        // 同花
        if (isFlush)
            return HandType.Flush;

        // 顺子
        if (isStraight)
            return HandType.Straight;

        // 三条
        if (GetNOfAKind(sortedCards, 3) > 0)
            return HandType.ThreeOfAKind;

        // 两对
        if (CheckTwoPair(sortedCards))
            return HandType.TwoPair;

        // 对子
        if (CheckPair(sortedCards))
            return HandType.Pair;

        // 高牌
        return HandType.HighCard;
    }

    /// <summary>
    /// 检查是否为皇家同花顺 (10-J-Q-K-A)
    /// </summary>
    private static bool IsRoyalFlush(List<CardData> cards)
    {
        if (cards.Count < 5) return false;
        
        var ranks = cards.Select(c => c.rank).OrderBy(r => r).ToList();
        // 需要包含10, J(11), Q(12), K(13), A(14)
        return ranks.Contains(10) && ranks.Contains(11) && ranks.Contains(12) 
            && ranks.Contains(13) && ranks.Contains(14);
    }

    /// <summary>
    /// 检查是否为同花（考虑Wild牌）
    /// </summary>
    private static bool CheckFlush(List<CardData> cards, bool hasWild)
    {
        if (cards.Count < 5)
            return false;

        // 分别统计每种花色的数量（Wild牌可以算任意花色）
        var nonWildCards = cards.Where(c => c.enhancement != CardData.Enhancement.Wild).ToList();
        int wildCount = cards.Count - nonWildCards.Count;

        if (nonWildCards.Count == 0)
            return true; // 全是Wild

        var suitGroups = nonWildCards.GroupBy(c => c.suit);
        foreach (var group in suitGroups)
        {
            if (group.Count() + wildCount >= 5)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否为顺子
    /// </summary>
    private static bool CheckStraight(List<CardData> cards)
    {
        if (cards.Count < 5)
            return false;

        List<int> ranks = cards.Select(c => c.rank).Distinct().OrderBy(r => r).ToList();

        if (ranks.Count < 5)
            return false;

        // 检查连续5张
        for (int i = 0; i <= ranks.Count - 5; i++)
        {
            bool isStraight = true;
            for (int j = 1; j < 5; j++)
            {
                if (ranks[i + j] != ranks[i] + j)
                {
                    isStraight = false;
                    break;
                }
            }
            if (isStraight) return true;
        }

        // 检查A-2-3-4-5顺子（A作为1）
        if (ranks.Contains(14))
        {
            List<int> lowAceRanks = ranks.Select(r => r == 14 ? 1 : r).Distinct().OrderBy(r => r).ToList();
            for (int i = 0; i <= lowAceRanks.Count - 5; i++)
            {
                bool isStraight = true;
                for (int j = 1; j < 5; j++)
                {
                    if (lowAceRanks[i + j] != lowAceRanks[i] + j)
                    {
                        isStraight = false;
                        break;
                    }
                }
                if (isStraight) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取N条的点数（返回0表示没有）
    /// </summary>
    private static int GetNOfAKind(List<CardData> cards, int n)
    {
        var rankGroups = cards.GroupBy(c => c.rank);
        foreach (var group in rankGroups)
        {
            if (group.Count() >= n)
                return group.Key;
        }
        return 0;
    }

    /// <summary>
    /// 检查是否为葫芦（三条+对子）
    /// </summary>
    private static bool CheckFullHouse(List<CardData> cards)
    {
        if (cards.Count < 5)
            return false;
        var rankGroups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count()).ToList();
        return rankGroups.Count >= 2 && rankGroups[0].Count() >= 3 && rankGroups[1].Count() >= 2;
    }

    /// <summary>
    /// 检查是否为两对
    /// </summary>
    private static bool CheckTwoPair(List<CardData> cards)
    {
        var rankGroups = cards.GroupBy(c => c.rank).Where(g => g.Count() >= 2).ToList();
        return rankGroups.Count >= 2;
    }

    /// <summary>
    /// 检查是否为对子
    /// </summary>
    public static bool CheckPair(List<CardData> cards)
    {
        var rankGroups = cards.GroupBy(c => c.rank);
        return rankGroups.Any(g => g.Count() >= 2);
    }

    /// <summary>
    /// 旧版兼容：IsPair
    /// </summary>
    public static bool IsPair(List<CardData> cards) => CheckPair(cards);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取牌型中文名称
    /// </summary>
    public static string GetHandTypeName(HandType handType)
    {
        switch (handType)
        {
            case HandType.HighCard: return "高牌";
            case HandType.Pair: return "对子";
            case HandType.TwoPair: return "两对";
            case HandType.ThreeOfAKind: return "三条";
            case HandType.Straight: return "顺子";
            case HandType.Flush: return "同花";
            case HandType.FullHouse: return "葫芦";
            case HandType.FourOfAKind: return "四条";
            case HandType.StraightFlush: return "同花顺";
            case HandType.RoyalFlush: return "皇家同花顺";
            default: return "未知";
        }
    }

    /// <summary>
    /// 获取牌型英文名称
    /// </summary>
    public static string GetHandTypeNameEn(HandType handType)
    {
        switch (handType)
        {
            case HandType.HighCard: return "High Card";
            case HandType.Pair: return "Pair";
            case HandType.TwoPair: return "Two Pair";
            case HandType.ThreeOfAKind: return "Three of a Kind";
            case HandType.Straight: return "Straight";
            case HandType.Flush: return "Flush";
            case HandType.FullHouse: return "Full House";
            case HandType.FourOfAKind: return "Four of a Kind";
            case HandType.StraightFlush: return "Straight Flush";
            case HandType.RoyalFlush: return "Royal Flush";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// 升级牌型等级（使用星球卡时调用）
    /// </summary>
    public static void UpgradeHandType(HandType handType, int levels = 1)
    {
        if (HandTypeData.ContainsKey(handType))
        {
            HandTypeData[handType].level += levels;
        }
    }

    /// <summary>
    /// 重置所有牌型等级（新游戏开始时）
    /// </summary>
    public static void ResetAllHandTypeLevels()
    {
        foreach (var kvp in HandTypeData)
        {
            kvp.Value.level = 1;
        }
    }

    /// <summary>
    /// 获取牌型当前等级
    /// </summary>
    public static int GetHandTypeLevel(HandType handType)
    {
        if (HandTypeData.ContainsKey(handType))
        {
            return HandTypeData[handType].level;
        }
        return 1;
    }

    #endregion
}

