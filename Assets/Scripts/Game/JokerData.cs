using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 小丑牌数据类
/// 位置：Assets/Scripts/Game/JokerData.cs
/// 功能：定义小丑牌的类型、稀有度、触发时机、效果等
/// 
/// Balatro小丑系统核心:
/// 1. 稀有度决定效果强度和获取概率
/// 2. 触发时机决定何时生效
/// 3. 效果类型: +chips, +mult, ×mult, 功能性, 经济性
/// 4. 小丑从左到右结算，先+mult后×mult
/// </summary>
[System.Serializable]
public class JokerData
{
    #region 枚举定义

    /// <summary>
    /// 小丑稀有度 - 影响获取概率和售价
    /// </summary>
    public enum Rarity
    {
        Common,     // 普通 - 最常见
        Uncommon,   // 罕见 - 效果较强
        Rare,       // 稀有 - 效果很强
        Legendary   // 传奇 - 极强效果，极难获取
    }

    /// <summary>
    /// 触发时机
    /// </summary>
    public enum TriggerTiming
    {
        OnCardScore,    // 单张牌计分时触发（逐张）
        OnJokerCalc,    // 牌型计分完成后独立触发
        OnDiscard,      // 丢弃牌时触发
        OnEndRound,     // 回合结束时触发
        Passive         // 被动效果，始终生效
    }

    /// <summary>
    /// 小丑类型
    /// </summary>
    public enum JokerType
    {
        // ===== 基础小丑 (Common) =====
        Joker,              // +4 Mult
        GreedyJoker,        // 每张计分方片 +3 Mult
        LustyJoker,         // 每张计分红桃 +3 Mult
        WrathfulJoker,      // 每张计分黑桃 +3 Mult
        GluttonousJoker,    // 每张计分梅花 +3 Mult
        JollyJoker,         // 有对子 +8 Mult
        ZanyJoker,          // 有三条 +12 Mult
        MadJoker,           // 有两对 +10 Mult
        CrazyJoker,         // 有顺子 +12 Mult
        DrollJoker,         // 有同花 +10 Mult

        // ===== 筹码小丑 (Common/Uncommon) =====
        SlyJoker,           // 有对子 +50 Chips
        WilyJoker,          // 有三条 +100 Chips
        CleverJoker,        // 有两对 +80 Chips
        DeviousJoker,       // 有顺子 +100 Chips
        CraftyJoker,        // 有同花 +80 Chips
        BannerJoker,        // 每剩余弃牌次数 +30 Chips

        // ===== 乘法小丑 (Uncommon/Rare) =====
        HalfJoker,          // 手牌≤3张时 +20 Mult
        SteelJoker,         // 持有的每张Steel牌 ×0.2 Mult（叠加）
        AbstractJoker,      // 每拥有一张小丑 +3 Mult
        
        // ===== X倍率小丑 (Rare/Legendary) =====
        TheIdol,            // 计分特定牌时 ×2 Mult
        Photograph,         // 首张计分的人头牌 ×2 Mult
        Blackboard,         // 所有手牌都是黑桃或梅花时 ×3 Mult
        Bloodstone,         // 每张计分红桃 1/3概率 ×1.5 Mult
        Arrowhead,          // 每张计分黑桃 1/3概率 ×1.5 Mult
        Onyx,               // 每张计分梅花 1/3概率 ×2 Mult
        
        // ===== 经济小丑 (Uncommon) =====
        GoldenJoker,        // 回合结束 +$4
        BullJoker,          // 每持有$1 +2 Chips
        
        // ===== 功能小丑 (各稀有度) =====
        EvenSteven,         // 只计分偶数牌
        OddTodd,            // 只计分奇数牌
        Scholar,            // 计分A时 +20 Chips, +4 Mult
        FibonacciJoker,     // 计分A,2,3,5,8时 +8 Mult
    }

    #endregion

    #region 字段

    public JokerType type;
    public string name;
    public Rarity rarity;
    public TriggerTiming triggerTiming;
    public int shopPrice;   // 商店价格
    public int sellPrice;   // 售卖价格（通常是购买价格的一半向下取整）
    
    // 效果数值（根据具体小丑类型使用不同字段）
    public int addChips;    // +筹码
    public int addMult;     // +倍率
    public float xMult;     // ×倍率（默认1.0表示无乘法效果）

    #endregion

    #region 构造函数

    /// <summary>
    /// 默认构造函数（Mirror 网络序列化需要）
    /// </summary>
    public JokerData()
    {
        type = JokerType.Joker;
        name = "";
        rarity = Rarity.Common;
        triggerTiming = TriggerTiming.OnJokerCalc;
        shopPrice = 0;
        sellPrice = 0;
        addChips = 0;
        addMult = 0;
        xMult = 1.0f;
    }

    public JokerData(JokerType type)
    {
        this.type = type;
        this.xMult = 1.0f;
        InitializeJoker();
    }

    #endregion

    #region 初始化

    private void InitializeJoker()
    {
        switch (type)
        {
            // ===== 基础+Mult小丑 (Common) =====
            case JokerType.Joker:
                name = "Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 4;
                shopPrice = 2;
                break;

            case JokerType.GreedyJoker:
                name = "Greedy Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnCardScore;
                addMult = 3; // 每张方片
                shopPrice = 5;
                break;

            case JokerType.LustyJoker:
                name = "Lusty Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnCardScore;
                addMult = 3; // 每张红桃
                shopPrice = 5;
                break;

            case JokerType.WrathfulJoker:
                name = "Wrathful Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnCardScore;
                addMult = 3; // 每张黑桃
                shopPrice = 5;
                break;

            case JokerType.GluttonousJoker:
                name = "Gluttonous Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnCardScore;
                addMult = 3; // 每张梅花
                shopPrice = 5;
                break;

            case JokerType.JollyJoker:
                name = "Jolly Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 8; // 有对子时
                shopPrice = 3;
                break;

            case JokerType.ZanyJoker:
                name = "Zany Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 12; // 有三条时
                shopPrice = 4;
                break;

            case JokerType.MadJoker:
                name = "Mad Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 10; // 有两对时
                shopPrice = 4;
                break;

            case JokerType.CrazyJoker:
                name = "Crazy Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 12; // 有顺子时
                shopPrice = 4;
                break;

            case JokerType.DrollJoker:
                name = "Droll Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 10; // 有同花时
                shopPrice = 4;
                break;

            // ===== 筹码小丑 =====
            case JokerType.SlyJoker:
                name = "Sly Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 50; // 有对子时
                shopPrice = 3;
                break;

            case JokerType.WilyJoker:
                name = "Wily Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 100; // 有三条时
                shopPrice = 4;
                break;

            case JokerType.CleverJoker:
                name = "Clever Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 80; // 有两对时
                shopPrice = 4;
                break;

            case JokerType.DeviousJoker:
                name = "Devious Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 100; // 有顺子时
                shopPrice = 4;
                break;

            case JokerType.CraftyJoker:
                name = "Crafty Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 80; // 有同花时
                shopPrice = 4;
                break;

            case JokerType.BannerJoker:
                name = "Banner";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 30; // 每剩余弃牌次数
                shopPrice = 5;
                break;

            // ===== 条件+Mult小丑 (Uncommon) =====
            case JokerType.HalfJoker:
                name = "Half Joker";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 20; // 手牌≤3张时
                shopPrice = 5;
                break;

            case JokerType.AbstractJoker:
                name = "Abstract Joker";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addMult = 3; // 每拥有一张小丑
                shopPrice = 4;
                break;

            // ===== ×倍率小丑 (Rare) =====
            case JokerType.SteelJoker:
                name = "Steel Joker";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnJokerCalc;
                xMult = 0.2f; // 每张Steel牌叠加
                shopPrice = 6;
                break;

            case JokerType.TheIdol:
                name = "The Idol";
                rarity = Rarity.Rare;
                triggerTiming = TriggerTiming.OnCardScore;
                xMult = 2.0f;
                shopPrice = 6;
                break;

            case JokerType.Photograph:
                name = "Photograph";
                rarity = Rarity.Rare;
                triggerTiming = TriggerTiming.OnCardScore;
                xMult = 2.0f; // 首张人头牌
                shopPrice = 5;
                break;

            case JokerType.Blackboard:
                name = "Blackboard";
                rarity = Rarity.Rare;
                triggerTiming = TriggerTiming.OnJokerCalc;
                xMult = 3.0f; // 所有手牌都是黑桃或梅花时
                shopPrice = 6;
                break;

            case JokerType.Bloodstone:
                name = "Bloodstone";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnCardScore;
                xMult = 1.5f; // 红桃 1/3概率
                shopPrice = 5;
                break;

            case JokerType.Arrowhead:
                name = "Arrowhead";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnCardScore;
                xMult = 1.5f; // 黑桃 1/3概率
                shopPrice = 5;
                break;

            case JokerType.Onyx:
                name = "Onyx Agate";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnCardScore;
                xMult = 2.0f; // 梅花 1/3概率
                shopPrice = 5;
                break;

            // ===== 经济小丑 =====
            case JokerType.GoldenJoker:
                name = "Golden Joker";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnEndRound;
                shopPrice = 6;
                break;

            case JokerType.BullJoker:
                name = "Bull";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnJokerCalc;
                addChips = 2; // 每持有$1
                shopPrice = 6;
                break;

            // ===== 功能小丑 =====
            case JokerType.EvenSteven:
                name = "Even Steven";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.Passive;
                addMult = 4; // 偶数牌计分时
                shopPrice = 4;
                break;

            case JokerType.OddTodd:
                name = "Odd Todd";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.Passive;
                addChips = 31; // 奇数牌计分时
                shopPrice = 4;
                break;

            case JokerType.Scholar:
                name = "Scholar";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnCardScore;
                addChips = 20;
                addMult = 4; // 计分A时
                shopPrice = 4;
                break;

            case JokerType.FibonacciJoker:
                name = "Fibonacci";
                rarity = Rarity.Uncommon;
                triggerTiming = TriggerTiming.OnCardScore;
                addMult = 8; // 计分A,2,3,5,8时
                shopPrice = 7;
                break;

            default:
                name = "Unknown Joker";
                rarity = Rarity.Common;
                triggerTiming = TriggerTiming.OnJokerCalc;
                shopPrice = 1;
                break;
        }

        // 售价通常是购买价的一半（向下取整，最少1）
        sellPrice = Mathf.Max(1, shopPrice / 2);
    }

    #endregion

    #region 效果计算

    /// <summary>
    /// 计分时效果数据结构
    /// </summary>
    public struct JokerBonus
    {
        public int chips;
        public int mult;
        public float xMult;
        public int money;

        public static JokerBonus Zero => new JokerBonus { chips = 0, mult = 0, xMult = 1.0f, money = 0 };
    }

    /// <summary>
    /// 单张牌计分时触发（OnCardScore）
    /// </summary>
    /// <param name="card">当前计分的牌</param>
    /// <param name="allPlayedCards">所有出牌</param>
    /// <param name="isFirstFaceCard">是否是第一张人头牌（用于Photograph）</param>
    public JokerBonus OnCardScoreTrigger(CardData card, List<CardData> allPlayedCards, bool isFirstFaceCard = false)
    {
        var bonus = JokerBonus.Zero;

        if (triggerTiming != TriggerTiming.OnCardScore) return bonus;

        switch (type)
        {
            case JokerType.GreedyJoker:
                if (card.GetEffectiveSuit() == CardData.Suit.Diamonds)
                    bonus.mult = addMult;
                break;

            case JokerType.LustyJoker:
                if (card.GetEffectiveSuit() == CardData.Suit.Hearts)
                    bonus.mult = addMult;
                break;

            case JokerType.WrathfulJoker:
                if (card.GetEffectiveSuit() == CardData.Suit.Spades)
                    bonus.mult = addMult;
                break;

            case JokerType.GluttonousJoker:
                if (card.GetEffectiveSuit() == CardData.Suit.Clubs)
                    bonus.mult = addMult;
                break;

            case JokerType.Scholar:
                if (card.rank == 14) // A
                {
                    bonus.chips = addChips;
                    bonus.mult = addMult;
                }
                break;

            case JokerType.FibonacciJoker:
                // 斐波那契数列: A(1), 2, 3, 5, 8
                if (card.rank == 14 || card.rank == 2 || card.rank == 3 || 
                    card.rank == 5 || card.rank == 8)
                {
                    bonus.mult = addMult;
                }
                break;

            case JokerType.Photograph:
                // 首张人头牌 ×2
                if (isFirstFaceCard && card.rank >= 11 && card.rank <= 13)
                {
                    bonus.xMult = xMult;
                }
                break;

            case JokerType.TheIdol:
                // 特定牌（这里简化为计分的K）×2
                if (card.rank == 13)
                {
                    bonus.xMult = xMult;
                }
                break;

            case JokerType.Bloodstone:
                // 红桃 1/3概率 ×1.5
                if (card.GetEffectiveSuit() == CardData.Suit.Hearts)
                {
                    if (Random.value < 0.333f)
                        bonus.xMult = xMult;
                }
                break;

            case JokerType.Arrowhead:
                // 黑桃 1/3概率 ×1.5
                if (card.GetEffectiveSuit() == CardData.Suit.Spades)
                {
                    if (Random.value < 0.333f)
                        bonus.xMult = xMult;
                }
                break;

            case JokerType.Onyx:
                // 梅花 1/3概率 ×2
                if (card.GetEffectiveSuit() == CardData.Suit.Clubs)
                {
                    if (Random.value < 0.333f)
                        bonus.xMult = xMult;
                }
                break;
        }

        return bonus;
    }

    /// <summary>
    /// 牌型计分完成后独立触发（OnJokerCalc）
    /// </summary>
    /// <param name="playedCards">出的牌</param>
    /// <param name="heldCards">手牌中持有的牌</param>
    /// <param name="handType">当前牌型</param>
    /// <param name="context">上下文信息</param>
    public JokerBonus OnJokerCalcTrigger(
        List<CardData> playedCards, 
        List<CardData> heldCards,
        Scoring.HandType handType,
        JokerContext context)
    {
        var bonus = JokerBonus.Zero;

        if (triggerTiming != TriggerTiming.OnJokerCalc) return bonus;

        switch (type)
        {
            case JokerType.Joker:
                bonus.mult = addMult;
                break;

            case JokerType.JollyJoker:
                if (handType == Scoring.HandType.Pair || 
                    handType == Scoring.HandType.TwoPair ||
                    handType == Scoring.HandType.FullHouse)
                    bonus.mult = addMult;
                break;

            case JokerType.ZanyJoker:
                if (handType == Scoring.HandType.ThreeOfAKind || 
                    handType == Scoring.HandType.FullHouse ||
                    handType == Scoring.HandType.FourOfAKind)
                    bonus.mult = addMult;
                break;

            case JokerType.MadJoker:
                if (handType == Scoring.HandType.TwoPair)
                    bonus.mult = addMult;
                break;

            case JokerType.CrazyJoker:
                if (handType == Scoring.HandType.Straight || 
                    handType == Scoring.HandType.StraightFlush)
                    bonus.mult = addMult;
                break;

            case JokerType.DrollJoker:
                if (handType == Scoring.HandType.Flush || 
                    handType == Scoring.HandType.StraightFlush)
                    bonus.mult = addMult;
                break;

            case JokerType.SlyJoker:
                if (handType == Scoring.HandType.Pair || 
                    handType == Scoring.HandType.TwoPair ||
                    handType == Scoring.HandType.FullHouse)
                    bonus.chips = addChips;
                break;

            case JokerType.WilyJoker:
                if (handType == Scoring.HandType.ThreeOfAKind || 
                    handType == Scoring.HandType.FullHouse ||
                    handType == Scoring.HandType.FourOfAKind)
                    bonus.chips = addChips;
                break;

            case JokerType.CleverJoker:
                if (handType == Scoring.HandType.TwoPair)
                    bonus.chips = addChips;
                break;

            case JokerType.DeviousJoker:
                if (handType == Scoring.HandType.Straight || 
                    handType == Scoring.HandType.StraightFlush)
                    bonus.chips = addChips;
                break;

            case JokerType.CraftyJoker:
                if (handType == Scoring.HandType.Flush || 
                    handType == Scoring.HandType.StraightFlush)
                    bonus.chips = addChips;
                break;

            case JokerType.BannerJoker:
                bonus.chips = addChips * context.remainingDiscards;
                break;

            case JokerType.HalfJoker:
                if (playedCards.Count <= 3)
                    bonus.mult = addMult;
                break;

            case JokerType.AbstractJoker:
                bonus.mult = addMult * context.jokerCount;
                break;

            case JokerType.SteelJoker:
                // 每张持有的Steel牌 +0.2 xMult
                int steelCount = heldCards.Count(c => c.enhancement == CardData.Enhancement.Steel);
                steelCount += playedCards.Count(c => c.enhancement == CardData.Enhancement.Steel);
                if (steelCount > 0)
                    bonus.xMult = 1.0f + (xMult * steelCount);
                break;

            case JokerType.Blackboard:
                // 所有手牌都是黑桃或梅花时 ×3
                bool allBlack = heldCards.All(c => 
                    c.GetEffectiveSuit() == CardData.Suit.Spades || 
                    c.GetEffectiveSuit() == CardData.Suit.Clubs);
                if (allBlack && heldCards.Count > 0)
                    bonus.xMult = xMult;
                break;

            case JokerType.BullJoker:
                bonus.chips = addChips * context.currentMoney;
                break;
        }

        return bonus;
    }

    /// <summary>
    /// 弃牌时触发（OnDiscard）
    /// </summary>
    public JokerBonus OnDiscardTrigger(List<CardData> discardedCards, JokerContext context)
    {
        var bonus = JokerBonus.Zero;

        if (triggerTiming != TriggerTiming.OnDiscard) return bonus;

        // 目前没有实现弃牌触发的小丑，预留接口

        return bonus;
    }

    /// <summary>
    /// 回合结束时触发（OnEndRound）
    /// </summary>
    public JokerBonus OnEndRoundTrigger(JokerContext context)
    {
        var bonus = JokerBonus.Zero;

        if (triggerTiming != TriggerTiming.OnEndRound) return bonus;

        switch (type)
        {
            case JokerType.GoldenJoker:
                bonus.money = 4;
                break;
        }

        return bonus;
    }

    #endregion

    #region 被动效果检查

    /// <summary>
    /// 检查牌是否应该被此小丑筛选（如Even Steven只计偶数牌）
    /// 返回true表示这张牌应该正常计分
    /// </summary>
    public bool ShouldScoreCard(CardData card)
    {
        switch (type)
        {
            case JokerType.EvenSteven:
                // 只计分偶数牌 (2,4,6,8,10)
                return card.rank % 2 == 0 && card.rank <= 10;

            case JokerType.OddTodd:
                // 只计分奇数牌 (A,3,5,7,9) - A算1所以是奇数
                return card.rank % 2 == 1 || card.rank == 14;

            default:
                return true;
        }
    }

    /// <summary>
    /// 获取被动加成（如EvenSteven/OddTodd对符合条件的牌的加成）
    /// </summary>
    public JokerBonus GetPassiveBonus(CardData card)
    {
        var bonus = JokerBonus.Zero;

        if (triggerTiming != TriggerTiming.Passive) return bonus;

        switch (type)
        {
            case JokerType.EvenSteven:
                if (card.rank % 2 == 0 && card.rank <= 10)
                    bonus.mult = addMult;
                break;

            case JokerType.OddTodd:
                if (card.rank % 2 == 1 || card.rank == 14)
                    bonus.chips = addChips;
                break;
        }

        return bonus;
    }

    #endregion

    #region 描述文本

    /// <summary>
    /// 获取描述文本
    /// </summary>
    public string GetDescription()
    {
        switch (type)
        {
            case JokerType.Joker:
                return "+4 Mult";
            case JokerType.GreedyJoker:
                return "计分的每张方片 +3 Mult";
            case JokerType.LustyJoker:
                return "计分的每张红桃 +3 Mult";
            case JokerType.WrathfulJoker:
                return "计分的每张黑桃 +3 Mult";
            case JokerType.GluttonousJoker:
                return "计分的每张梅花 +3 Mult";
            case JokerType.JollyJoker:
                return "有对子时 +8 Mult";
            case JokerType.ZanyJoker:
                return "有三条时 +12 Mult";
            case JokerType.MadJoker:
                return "有两对时 +10 Mult";
            case JokerType.CrazyJoker:
                return "有顺子时 +12 Mult";
            case JokerType.DrollJoker:
                return "有同花时 +10 Mult";
            case JokerType.SlyJoker:
                return "有对子时 +50 Chips";
            case JokerType.WilyJoker:
                return "有三条时 +100 Chips";
            case JokerType.CleverJoker:
                return "有两对时 +80 Chips";
            case JokerType.DeviousJoker:
                return "有顺子时 +100 Chips";
            case JokerType.CraftyJoker:
                return "有同花时 +80 Chips";
            case JokerType.BannerJoker:
                return "每剩余弃牌次数 +30 Chips";
            case JokerType.HalfJoker:
                return "出牌≤3张时 +20 Mult";
            case JokerType.AbstractJoker:
                return "每拥有一张小丑 +3 Mult";
            case JokerType.SteelJoker:
                return "每张Steel牌 ×0.2 Mult（叠加）";
            case JokerType.TheIdol:
                return "计分K时 ×2 Mult";
            case JokerType.Photograph:
                return "首张计分的人头牌 ×2 Mult";
            case JokerType.Blackboard:
                return "所有手牌都是♠或♣时 ×3 Mult";
            case JokerType.Bloodstone:
                return "计分红桃时 1/3概率 ×1.5 Mult";
            case JokerType.Arrowhead:
                return "计分黑桃时 1/3概率 ×1.5 Mult";
            case JokerType.Onyx:
                return "计分梅花时 1/3概率 ×2 Mult";
            case JokerType.GoldenJoker:
                return "回合结束时 +$4";
            case JokerType.BullJoker:
                return "每持有$1 +2 Chips";
            case JokerType.EvenSteven:
                return "只计分偶数牌(2,4,6,8,10)，每张 +4 Mult";
            case JokerType.OddTodd:
                return "只计分奇数牌(A,3,5,7,9)，每张 +31 Chips";
            case JokerType.Scholar:
                return "计分A时 +20 Chips, +4 Mult";
            case JokerType.FibonacciJoker:
                return "计分A,2,3,5,8时 +8 Mult";
            default:
                return "";
        }
    }

    /// <summary>
    /// 获取稀有度颜色（用于UI显示）
    /// </summary>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case Rarity.Common:
                return new Color(0.6f, 0.8f, 1.0f); // 淡蓝色
            case Rarity.Uncommon:
                return new Color(0.2f, 0.8f, 0.2f); // 绿色
            case Rarity.Rare:
                return new Color(0.8f, 0.2f, 0.2f); // 红色
            case Rarity.Legendary:
                return new Color(1.0f, 0.8f, 0.0f); // 金色
            default:
                return Color.white;
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 随机生成指定稀有度的小丑
    /// </summary>
    public static JokerData GenerateRandomJoker(Rarity targetRarity)
    {
        var jokersByRarity = System.Enum.GetValues(typeof(JokerType))
            .Cast<JokerType>()
            .Select(t => new JokerData(t))
            .Where(j => j.rarity == targetRarity)
            .ToList();

        if (jokersByRarity.Count == 0)
            return new JokerData(JokerType.Joker);

        return jokersByRarity[Random.Range(0, jokersByRarity.Count)];
    }

    /// <summary>
    /// 根据权重随机生成小丑（普通更常见）
    /// </summary>
    public static JokerData GenerateRandomJokerWeighted()
    {
        // 权重：Common 70%, Uncommon 20%, Rare 8%, Legendary 2%
        float roll = Random.value;
        Rarity targetRarity;

        if (roll < 0.70f)
            targetRarity = Rarity.Common;
        else if (roll < 0.90f)
            targetRarity = Rarity.Uncommon;
        else if (roll < 0.98f)
            targetRarity = Rarity.Rare;
        else
            targetRarity = Rarity.Legendary;

        return GenerateRandomJoker(targetRarity);
    }

    #endregion
}

/// <summary>
/// 小丑效果计算的上下文信息
/// </summary>
public struct JokerContext
{
    public int remainingHands;      // 剩余出牌次数
    public int remainingDiscards;   // 剩余弃牌次数
    public int currentMoney;        // 当前金钱
    public int jokerCount;          // 小丑数量
    public int ante;                // 当前Ante
    
    public static JokerContext Default => new JokerContext
    {
        remainingHands = 4,
        remainingDiscards = 3,
        currentMoney = 0,
        jokerCount = 0,
        ante = 1
    };
}

