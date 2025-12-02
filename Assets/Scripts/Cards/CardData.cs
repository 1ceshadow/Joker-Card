using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 卡牌数据类
/// 位置：Assets/Scripts/Cards/CardData.cs
/// 功能：定义卡牌的完整属性（花色、点数、强化、版本、印章）
/// </summary>
[System.Serializable]
public class CardData : IComparable<CardData>
{
    #region 枚举定义

    /// <summary>
    /// 花色
    /// </summary>
    public enum Suit
    {
        Spades = 0,    // 黑桃 ♠
        Hearts = 1,    // 红桃 ♥
        Diamonds = 2,  // 方片 ♦
        Clubs = 3      // 梅花 ♣
    }

    /// <summary>
    /// 强化类型（槽位1）- 影响卡牌基础效果
    /// </summary>
    public enum Enhancement
    {
        None = 0,       // 无强化
        Bonus,          // +30 筹码
        Mult,           // +4 倍率
        Wild,           // 可当作任意花色
        Glass,          // ×2 倍率，发挥时有 1/4 概率销毁
        Steel,          // 持有时 ×1.5 倍率（不需要打出）
        Stone,          // +50 筹码，无点数与花色
        Gold,           // 回合结束时若持有，赚 $3
        Lucky           // 1/5 概率 +20 倍率，1/15 概率 +$20
    }

    /// <summary>
    /// 版本类型（槽位2）- 额外加成
    /// </summary>
    public enum Edition
    {
        Base = 0,       // 无效果
        Foil,           // +50 筹码
        Holographic,    // +10 倍率
        Polychrome      // ×1.5 倍率
    }

    /// <summary>
    /// 印章类型（槽位3）- 特殊触发效果
    /// </summary>
    public enum Seal
    {
        None = 0,       // 无印章
        Red,            // 红印：重触发该牌 1 次
        Blue,           // 蓝印：回合结束时若持有，生成 1 张星球卡
        Gold,           // 金印：出牌时赚 $3
        Purple          // 紫印：丢弃时生成 1 张塔罗卡
    }

    #endregion

    #region 基础属性

    public Suit suit;
    public int rank; // 2-14 (2-10, J=11, Q=12, K=13, A=14)

    #endregion

    // Balatro 三槽位系统

    public Enhancement enhancement = Enhancement.None;
    public Edition edition = Edition.Base;
    public Seal seal = Seal.None;



    // 状态标记

    /// <summary>
    /// 是否被Debuff（Boss Blind效果等）
    /// </summary>
    public bool isDebuffed = false;





    /// <summary>
    /// 默认构造函数（用于序列化）
    /// </summary>
    public CardData()
    {
        suit = Suit.Spades;
        rank = 2;
        enhancement = Enhancement.None;
        edition = Edition.Base;
        seal = Seal.None;
        isDebuffed = false;
    }

    /// <summary>
    /// 基础构造函数（仅花色和点数）
    /// </summary>
    public CardData(Suit suit, int rank)
    {
        this.suit = suit;
        this.rank = rank;
        enhancement = Enhancement.None;
        edition = Edition.Base;
        seal = Seal.None;
        isDebuffed = false;
    }

    /// <summary>
    /// 完整构造函数
    /// </summary>
    public CardData(Suit suit, int rank, Enhancement enhancement, Edition edition, Seal seal)
    {
        this.suit = suit;
        this.rank = rank;
        this.enhancement = enhancement;
        this.edition = edition;
        this.seal = seal;
        isDebuffed = false;
    }



    #region 筹码计算

    /// <summary>
    /// 获取基础点数筹码值
    /// ~~Balatro规则：2-10=面值, J/Q/K=10, A=11~~
    /// 使用卡牌点数作为基础筹码值
    /// </summary>
    public int GetBaseChipValue()
    {
        // Stone牌没有点数
        if (enhancement == Enhancement.Stone)
            return 0;

        switch (rank)
        {
            // case 14: return 11; // A = 11
            // case 13: return 10; // K = 10
            // case 12: return 10; // Q = 10
            // case 11: return 10; // J = 10
            default: return rank; // 2-10 = 面值
        }
    }

    /// <summary>
    /// 获取强化带来的额外筹码
    /// </summary>
    public int GetEnhancementChips()
    {
        switch (enhancement)
        {
            case Enhancement.Bonus: return 30;
            case Enhancement.Stone: return 50;
            default: return 0;
        }
    }

    /// <summary>
    /// 获取版本带来的额外筹码
    /// </summary>
    public int GetEditionChips()
    {
        switch (edition)
        {
            case Edition.Foil: return 50;
            default: return 0;
        }
    }

    /// <summary>
    /// 获取该牌的总筹码值（点数 + 强化 + 版本）
    /// </summary>
    public int GetTotalChips()
    {
        return GetBaseChipValue() + GetEnhancementChips() + GetEditionChips();
    }

    #endregion

    #region 倍率计算

    /// <summary>
    /// 获取强化带来的加法倍率
    /// </summary>
    public int GetEnhancementAddMult()
    {
        switch (enhancement)
        {
            case Enhancement.Mult: return 4;
            default: return 0;
        }
    }

    /// <summary>
    /// 获取版本带来的加法倍率
    /// </summary>
    public int GetEditionAddMult()
    {
        switch (edition)
        {
            case Edition.Holographic: return 10;
            default: return 0;
        }
    }


    /// <summary>
    /// 获取强化带来的乘法倍率
    /// </summary>
    public float GetEnhancementXMult()
    {
        switch (enhancement)
        {
            case Enhancement.Glass: return 2.0f;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// 获取版本带来的乘法倍率
    /// </summary>
    public float GetEditionXMult()
    {
        switch (edition)
        {
            case Edition.Polychrome: return 1.5f;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// 获取Steel牌持有时的乘法倍率
    /// </summary>
    public float GetSteelHeldXMult()
    {
        if (enhancement == Enhancement.Steel)
            return 1.5f;
        return 1.0f;
    }

    #endregion

    #region Lucky牌

    /// <summary>
    /// 检查Lucky牌是否触发 +20 Mult（1/5概率）
    /// 返回触发的mult值，未触发返回0
    /// </summary>
    public int CheckLuckyMult()
    {
        if (enhancement != Enhancement.Lucky)
            return 0;
        if (UnityEngine.Random.value < 0.2f) // 1/5 = 20%
            return 20;
        return 0;
    }

    /// <summary>
    /// 检查Lucky牌是否触发 +$20（1/15概率）
    /// 返回触发的金钱值，未触发返回0
    /// </summary>
    public int CheckLuckyMoney()
    {
        if (enhancement != Enhancement.Lucky)
            return 0;
        if (UnityEngine.Random.value < (1f / 15f)) // 1/15 ≈ 6.67%
            return 20;
        return 0;
    }

    #endregion

    #region Glass牌效果

    /// <summary>
    /// 检查Glass牌是否销毁（1/4概率）
    /// </summary>
    public bool CheckGlassDestroy()
    {
        if (enhancement != Enhancement.Glass)
            return false;
        return UnityEngine.Random.value < 0.25f; // 1/4 = 25%
    }

    #endregion

    #region 红印重触发

    /// <summary>
    /// 获取触发次数（红印=2次，其他=1次）
    /// </summary>
    public int GetTriggerCount()
    {
        return seal == Seal.Red ? 2 : 1;
    }

    #endregion

    #region Wild牌花色

    /// <summary>
    /// 检查是否匹配指定花色（Wild牌匹配所有花色）
    /// </summary>
    public bool MatchesSuit(Suit targetSuit)
    {
        if (enhancement == Enhancement.Wild)
            return true;
        return suit == targetSuit;
    }

    /// <summary>
    /// 获取有效花色（用于计分和牌型判定）
    /// 注意：Wild牌返回原花色，由调用方处理Wild逻辑
    /// </summary>
    public Suit GetEffectiveSuit()
    {
        // Stone牌没有花色，但仍返回原花色
        // Wild牌可当作任意花色，也返回原花色，实际判定时由调用方处理
        return suit;
    }

    /// <summary>
    /// 检查Stone牌（无花色无点数，不参与牌型判定）
    /// </summary>
    public bool IsStone()
    {
        return enhancement == Enhancement.Stone;
    }

    /// <summary>
    /// 是否是Wild牌
    /// </summary>
    public bool IsWild()
    {
        return enhancement == Enhancement.Wild;
    }

    #endregion

    #region 显示相关

    /// <summary>
    /// 获取花色名称（中文）
    /// </summary>
    public string GetSuitName()
    {
        if (enhancement == Enhancement.Stone)
            return "无";
        switch (suit)
        {
            case Suit.Spades: return "黑桃";
            case Suit.Hearts: return "红桃";
            case Suit.Diamonds: return "方片";
            case Suit.Clubs: return "梅花";
            default: return "";
        }
    }

    /// <summary>
    /// 获取花色符号
    /// </summary>
    public string GetSuitSymbol()
    {
        if (enhancement == Enhancement.Stone)
            return "◆";
        if (enhancement == Enhancement.Wild)
            return "★";
        switch (suit)
        {
            case Suit.Spades: return "♠";
            case Suit.Hearts: return "♥";
            case Suit.Diamonds: return "♦";
            case Suit.Clubs: return "♣";
            default: return "";
        }
    }

    /// <summary>
    /// 获取点数名称
    /// </summary>
    public string GetRankName()
    {
        if (enhancement == Enhancement.Stone)
            return "石";
        switch (rank)
        {
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            case 14: return "A";
            default: return rank.ToString();
        }
    }

    /// <summary>
    /// 获取完整牌名（如："红桃A"）
    /// </summary>
    public string GetFullName()
    {
        return $"{GetSuitName()}{GetRankName()}";
    }

    /// <summary>
    /// 获取显示名称（别名，兼容Scoring）
    /// </summary>
    public string GetDisplayName() => GetShortName();

    /// <summary>
    /// 获取简短牌名（如："♥A"）
    /// </summary>
    public string GetShortName()
    {
        return $"{GetSuitSymbol()}{GetRankName()}";
    }

    /// <summary>
    /// 获取强化名称
    /// </summary>
    public string GetEnhancementName()
    {
        switch (enhancement)
        {
            case Enhancement.Bonus: return "奖励";
            case Enhancement.Mult: return "倍率";
            case Enhancement.Wild: return "百搭";
            case Enhancement.Glass: return "玻璃";
            case Enhancement.Steel: return "钢铁";
            case Enhancement.Stone: return "石头";
            case Enhancement.Gold: return "黄金";
            case Enhancement.Lucky: return "幸运";
            default: return "";
        }
    }

    /// <summary>
    /// 获取版本名称
    /// </summary>
    public string GetEditionName()
    {
        switch (edition)
        {
            case Edition.Foil: return "箔片";
            case Edition.Holographic: return "全息";
            case Edition.Polychrome: return "多彩";
            default: return "";
        }
    }

    /// <summary>
    /// 获取印章名称
    /// </summary>
    public string GetSealName()
    {
        switch (seal)
        {
            case Seal.Red: return "红印";
            case Seal.Blue: return "蓝印";
            case Seal.Gold: return "金印";
            case Seal.Purple: return "紫印";
            default: return "";
        }
    }

    #endregion

    #region 比较与克隆

    /// <summary>
    /// 用于排序（先按点数，再按花色）
    /// </summary>
    public int CompareTo(CardData other)
    {
        if (other == null) return 1;
        if (rank != other.rank)
            return rank.CompareTo(other.rank);
        return suit.CompareTo(other.suit);
    }

    /// <summary>
    /// 深拷贝
    /// </summary>
    public CardData Clone()
    {
        return new CardData(suit, rank, enhancement, edition, seal)
        {
            isDebuffed = this.isDebuffed
        };
    }

    /// <summary>
    /// 判断两张牌是否相等（仅比较花色和点数）
    /// </summary>
    public bool Equals(CardData other)
    {
        if (other == null) return false;
        return suit == other.suit && rank == other.rank;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as CardData);
    }

    public override int GetHashCode()
    {
        return ((int)suit << 16) | rank;
    }

    public override string ToString()
    {
        string result = GetFullName();
        if (enhancement != Enhancement.None)
            result += $" [{GetEnhancementName()}]";
        if (edition != Edition.Base)
            result += $" ({GetEditionName()})";
        if (seal != Seal.None)
            result += $" <{GetSealName()}>";
        return result;
    }

    #endregion
}

#region 序列化包装器

/// <summary>
/// 卡牌列表包装器（用于JSON序列化）
/// </summary>
[System.Serializable]
public class CardDataListWrapper
{
    public CardData[] cards;

    public CardDataListWrapper()
    {
        cards = new CardData[0];
    }

    public CardDataListWrapper(CardData[] cards)
    {
        this.cards = cards;
    }
}

#endregion
