# Joker-Card 项目完整讲解文档

## 📋 目录
1. [项目概述](#项目概述)
2. [架构设计](#架构设计)
3. [Balatro核心系统](#balatro核心系统)
4. [网络同步机制](#网络同步机制)
5. [游戏流程](#游戏流程)
6. [关键类说明](#关键类说明)
7. [计分引擎详解](#计分引擎详解)
8. [使用指南](#使用指南)

---

## 🎮 项目概述

**Joker-Card** 是一款基于 Unity 6 的局域网多人卡牌游戏，结合了中国炸金花的玩法和 **Balatro** 风格的小丑牌机制。

### 核心特性
- **网络模式**：基于 Mirror 的 LAN 局域网多人游戏
- **游戏类型**：炸金花（押注、比牌）+ Balatro 小丑牌增强
- **玩家数量**：2-6人
- **核心玩法**：回合制出牌、押注、弃牌、小丑牌加成、商店购买

### 技术栈
- **引擎**：Unity 6
- **网络**：Mirror Networking
- **传输层**：Telepathy Transport
- **架构模式**：Server-Authority（服务器权威）

---

## 🏗️ 架构设计

### 整体架构图
```
┌─────────────────────────────────────────────────────────────┐
│                     Unity Scene 层                          │
├─────────────┬──────────────┬──────────────┬─────────────────┤
│  MainMenu   │  CreateRoom  │  JoinRoom    │  GameScene      │
│  (单机)     │ (房主/客户端) │   (客户端)    │  (联网游戏)     │
└─────────────┴──────────────┴──────────────┴─────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
            ┌───────▼────────┐ ┌───▼────────┐ ┌───▼────────┐
            │  Network Layer │ │  Game Core │ │  UI Layer  │
            │  (Mirror)      │ │  (Logic)   │ │  (Display) │
            └────────────────┘ └────────────┘ └────────────┘
```

### 分层设计

#### 1. **数据层 (Data Layer)**
- `CardData` - 卡牌数据（花色、点数、强化、版本、印章）
- `JokerData` - 小丑牌数据（类型、稀有度、触发时机、效果）
- `PlayerSaveData` - 玩家存档数据
- `Deck` - 牌组管理

#### 2. **网络层 (Network Layer)**
- `NetworkManagerCustom` - 网络管理器（连接、房间）
- `PlayerData` - 玩家网络对象（SyncVar同步）
- `Transport` - 底层传输（Telepathy）

#### 3. **游戏逻辑层 (Game Logic Layer)**
- `GameManager` - 游戏流程管理（回合、出牌、结算）
- `Scoring` - Balatro风格计分引擎
- `ShopManager` - 商店系统（购买/售卖小丑牌）

#### 4. **UI层 (UI Layer)**
- `GameUI` - 游戏内主UI
- `CardUI` - 卡牌显示组件
- `Card` - 可交互卡牌组件（带拖拽）
- `JokerItemUI` - 小丑牌显示

---

## 🃏 Balatro核心系统

### 1. 卡牌三槽位系统 (CardData)

每张扑克牌拥有三个可升级的槽位：

```csharp
public class CardData
{
    public Suit suit;           // 花色: Spades, Hearts, Diamonds, Clubs
    public int rank;            // 点数: 2-14 (A=14)
    
    // ====== Balatro三槽位系统 ======
    public Enhancement enhancement;  // 槽位1: 强化
    public Edition edition;          // 槽位2: 版本
    public Seal seal;                // 槽位3: 印章
}
```

#### 槽位1: 强化 (Enhancement)
| 强化类型 | 效果 |
|---------|------|
| None | 无效果 |
| Bonus | +30 Chips |
| Mult | +4 Mult |
| Wild | 可当作任意花色 |
| Glass | ×2 Mult，计分时有1/4概率销毁 |
| Steel | 持有时 ×1.5 Mult |
| Stone | +50 Chips，无点数无花色（不参与牌型判定） |
| Gold | 回合结束时若持有，赚 $3 |
| Lucky | 1/5概率 +20 Mult，1/15概率 +$20 |

#### 槽位2: 版本 (Edition)
| 版本类型 | 效果 |
|---------|------|
| Base | 无效果 |
| Foil | +50 Chips |
| Holographic | +10 Mult |
| Polychrome | ×1.5 Mult |

#### 槽位3: 印章 (Seal)
| 印章类型 | 效果 |
|---------|------|
| None | 无效果 |
| Red | 计分时重触发该牌1次 |
| Blue | 回合结束时若持有，生成1张星球卡 |
| Gold | 出牌时赚 $3 |
| Purple | 弃牌时生成1张塔罗卡 |

#### 点数对应chips值
```
2-10: 面值 (2=2, 3=3, ..., 10=10)
J/Q/K: 11、12、13
A: 14
```

---

### 2. 小丑牌系统 (JokerData)

小丑牌是Balatro的核心构建引擎，每张小丑牌有：

```csharp
public class JokerData
{
    public JokerType type;           // 小丑类型
    public Rarity rarity;            // 稀有度
    public TriggerTiming triggerTiming;  // 触发时机
    
    public int addChips;    // +筹码
    public int addMult;     // +倍率
    public float xMult;     // ×倍率
}
```

#### 稀有度
| 稀有度 | 获取概率 | 说明 |
|--------|---------|------|
| Common | 70% | 普通效果 |
| Uncommon | 20% | 较强效果 |
| Rare | 8% | 强力效果 |
| Legendary | 2% | 极强效果 |

#### 触发时机
| 时机 | 触发点 |
|------|-------|
| OnCardScore | 单张牌计分时（逐张触发） |
| OnJokerCalc | 牌型计分完成后独立触发 |
| OnDiscard | 弃牌时触发 |
| OnEndRound | 回合结束时触发 |
| Passive | 被动效果，始终生效 |

#### 小丑牌列表

**基础+Mult小丑 (Common)**
| 小丑牌 | 效果 | 价格 |
|--------|------|------|
| Joker | +4 Mult | 2 |
| Greedy Joker | 计分的每张方片 +3 Mult | 5 |
| Lusty Joker | 计分的每张红桃 +3 Mult | 5 |
| Wrathful Joker | 计分的每张黑桃 +3 Mult | 5 |
| Gluttonous Joker | 计分的每张梅花 +3 Mult | 5 |
| Jolly Joker | 有对子时 +8 Mult | 3 |
| Zany Joker | 有三条时 +12 Mult | 4 |
| Mad Joker | 有两对时 +10 Mult | 4 |
| Crazy Joker | 有顺子时 +12 Mult | 4 |
| Droll Joker | 有同花时 +10 Mult | 4 |

**筹码小丑 (Common)**
| 小丑牌 | 效果 | 价格 |
|--------|------|------|
| Sly Joker | 有对子时 +50 Chips | 3 |
| Wily Joker | 有三条时 +100 Chips | 4 |
| Clever Joker | 有两对时 +80 Chips | 4 |
| Devious Joker | 有顺子时 +100 Chips | 4 |
| Crafty Joker | 有同花时 +80 Chips | 4 |
| Banner | 每剩余弃牌次数 +30 Chips | 5 |

**×倍率小丑 (Uncommon/Rare)**
| 小丑牌 | 效果 | 稀有度 | 价格 |
|--------|------|--------|------|
| Steel Joker | 每张Steel牌 ×0.2 Mult（叠加） | Uncommon | 6 |
| Blackboard | 所有手牌都是♠或♣时 ×3 Mult | Rare | 6 |
| Bloodstone | 计分红桃时 1/3概率 ×1.5 Mult | Uncommon | 5 |
| Photograph | 首张计分的人头牌 ×2 Mult | Rare | 5 |

**功能小丑**
| 小丑牌 | 效果 | 价格 |
|--------|------|------|
| Even Steven | 只计分偶数牌(2,4,6,8,10)，每张 +4 Mult | 4 |
| Odd Todd | 只计分奇数牌(A,3,5,7,9)，每张 +31 Chips | 4 |
| Scholar | 计分A时 +20 Chips, +4 Mult | 4 |
| Fibonacci | 计分A,2,3,5,8时 +8 Mult | 7 |

---

## 📊 计分引擎详解

### 牌型基础数值 (Balatro标准)

| 牌型 | 基础Chips | 基础Mult | 每级+Chips | 每级+Mult |
|------|----------|----------|-----------|----------|
| 高牌 | 5 | 1 | 10 | 1 |
| 对子 | 10 | 2 | 15 | 1 |
| 两对 | 20 | 2 | 20 | 1 |
| 三条 | 30 | 3 | 20 | 2 |
| 顺子 | 30 | 4 | 30 | 3 |
| 同花 | 35 | 4 | 15 | 2 |
| 葫芦 | 40 | 4 | 25 | 2 |
| 四条 | 60 | 7 | 30 | 3 |
| 同花顺 | 100 | 8 | 40 | 4 |
| 皇家同花顺 | 100 | 8 | 40 | 4 |

### 计分流程（严格按顺序）

```
┌────────────────────────────────────────────────────────────┐
│                    Balatro 计分流程                         │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  步骤1: 牌型识别                                            │
│  ├── 分析出牌（最多5张）                                    │
│  ├── Stone牌不参与点数/花色判定                              │
│  └── Wild牌可充当任意花色                                   │
│                                                            │
│  步骤2: 获取基础数值                                        │
│  ├── 根据牌型等级获取 base_chips                            │
│  └── 根据牌型等级获取 base_mult                             │
│                                                             │
│  步骤3: 逐张计分（从左到右）                                  │
│  │                                                          │
│  │  对每张计分牌循环：                                       │
│  │  ├── A. 加上该牌的点数chips                               │
│  │  ├── B. 加上强化(Enhancement)的chips/mult                │
│  │  ├── C. 加上版本(Edition)的chips/mult                    │
│  │  ├── D. 触发所有OnCardScore小丑效果                       │
│  │  ├── E. 应用Glass/Polychrome等×mult                      │
│  │  └── F. 若有红印(Red Seal)，重复A~E一次                   │
│  │                                                         │
│  步骤4: 手牌持有效果                                         │
│  └── Steel牌：×1.5 mult                                     │
│                                                            │
│  步骤5: 小丑独立效果（从左到右）                              │
│  ├── 5a. 先结算所有 +chips                                  │
│  ├── 5b. 再结算所有 +mult                                   │
│  └── 5c. 最后结算所有 ×mult                                 │
│                                                            │
│  步骤6: 最终计算                                            │
│  └── 分数 = 总chips × 总mult                                │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 计分示例

**出牌**：红桃A, 红桃K, 红桃Q, 红桃J, 红桃10 (皇家同花顺)  
**小丑**：[Joker, Lusty Joker, Steel Joker]  
**持有手牌**：3张Steel强化的牌  

```
步骤1: 牌型识别 → 皇家同花顺

步骤2: 基础数值
  base_chips = 100
  base_mult = 8

步骤3: 逐张计分（从左到右）
  红桃A: +14 chips → chips = 114
  红桃K: +13 chips → chips = 127
  红桃Q: +12 chips → chips = 139
  红桃J: +11 chips → chips = 150
  红桃10: +10 chips → chips = 160
  
  [小丑触发OnCardScore]
  Lusty Joker: 红桃A +3 mult → mult = 11
  Lusty Joker: 红桃K +3 mult → mult = 14
  Lusty Joker: 红桃Q +3 mult → mult = 17
  Lusty Joker: 红桃J +3 mult → mult = 20
  Lusty Joker: 红桃10 +3 mult → mult = 23

步骤4: 持有效果
  Steel牌1: ×1.5 mult → mult = 34.5
  Steel牌2: ×1.5 mult → mult = 51.75
  Steel牌3: ×1.5 mult → mult = 77.625

步骤5: 小丑独立效果
  Joker: +4 mult → mult = 81.625
  Steel Joker: 3张Steel牌 × 0.2 = ×1.6 mult → mult = 130.6

步骤6: 最终计算
  分数 = 160 × 130.6 ≈ 20,896
```

### 小丑顺序的重要性

因为 ×mult 是最后计算的，所以小丑的摆放顺序会影响最终分数：

```
情况A：[+4 mult小丑, ×2 mult小丑]
  mult = 1 → +4 → 5 → ×2 → 10

情况B：[×2 mult小丑, +4 mult小丑]
  mult = 1 → ×2 → 2 → +4 → 6  ← 更低！

结论：×mult小丑应该放在右边（后计算）以最大化收益
```

---

## 🔄 网络同步机制

### Mirror网络架构
```
Host (主机)                    Client (客户端)
    │                              │
    ├── NetworkServer              ├── NetworkClient
    │   ├── 管理玩家列表            │   └── 连接到Host
    │   ├── 执行游戏逻辑            │
    │   └── 广播状态更新            │
    │                              │
    └──── [Command] ◄──────────────┘
          请求操作（客户端→服务器）
                │
                ▼
          [Server] 方法
          验证并执行
                │
                ▼
          [ClientRpc] ──────────────►  所有客户端
          广播结果
```

### PlayerData同步对象
```csharp
public class PlayerData : NetworkBehaviour
{
    // 基础信息
    [SyncVar] public string playerName;
    [SyncVar] public int money;
    [SyncVar] public int debt;
    
    // 复杂数据（JSON序列化）
    [SyncVar(hook = nameof(OnHandCardsChanged))]
    public string handCardsJson;
    
    [SyncVar(hook = nameof(OnJokersChanged))]
    public string jokersJson;
}
```

### 防作弊机制
- 所有操作通过 `[Command]` 发送到服务器
- 服务器通过 `[Server]` 方法验证后执行
- 分数计算完全在服务器端进行

---

## 🎯 游戏流程

### 完整流程
```
[启动游戏]
    ↓
[MainMenu]
- 输入玩家名、选头像
- 借钱（最多200）
- 商店购买小丑牌
    ↓
┌──选择──┐
│        │
Host    Join
│        │
↓        ↓
CreateRoom  JoinRoom
    ↓        ↓
    [等待玩家加入]
         ↓
    [房主点击开始]
         ↓
    [GameScene]
         ↓
    ┌─────────────────┐
    │    游戏循环      │
    │                 │
    │  1. 发牌        │
    │  2. 出牌计分    │
    │  3. 弃牌        │
    │  4. 比较分数     │
    │  5. 结算奖励     │
    │  6. 下一回合     │
    └─────────────────┘
```

### 单回合流程
```
[发牌阶段]
- 每人发8张牌
- 显示手牌（只有自己可见）
    ↓
[出牌阶段]
- 选择最多5张牌出牌
- 服务器计算分数
- 可弃牌3次（每次最多5张）
    ↓
[结算阶段]
- 比较所有玩家分数
- 分数最高者获胜
- 发放奖励
```

---

## 📁 关键类说明

### 数据类

| 类名 | 位置 | 职责 |
|-----|------|------|
| CardData | Cards/CardData.cs | 扑克牌数据（三槽位系统） |
| JokerData | Game/JokerData.cs | 小丑牌数据（触发系统） |
| Deck | Cards/Deck.cs | 牌组管理 |

### 网络类

| 类名 | 位置 | 职责 |
|-----|------|------|
| NetworkManagerCustom | Network/NetworkManagerCustom.cs | 网络连接管理 |
| PlayerData | Network/PlayerData.cs | 玩家网络状态同步 |

### 游戏逻辑类

| 类名 | 位置 | 职责 |
|-----|------|------|
| GameManager | Game/GameManager.cs | 游戏流程控制 |
| Scoring | Cards/Scoring.cs | Balatro风格计分引擎 |
| ShopManager | Game/ShopManager.cs | 商店系统 |

### UI类

| 类名 | 位置 | 职责 |
|-----|------|------|
| GameUI | UI/GameUI.cs | 游戏主界面 |
| CardUI | UI/CardUI.cs | 卡牌显示 |
| Card | Cards/Card.cs | 可交互卡牌（拖拽） |

---

## 🛠️ 使用指南

### 添加新的强化类型

1. 在 `CardData.Enhancement` 枚举中添加新类型
2. 在 `CardData.GetEnhancementChips/Mult/XMult()` 方法中添加对应数值
3. 在 `Scoring.CalculateScoreDetailed()` 中处理特殊逻辑

### 添加新的小丑牌

1. 在 `JokerData.JokerType` 枚举中添加新类型
2. 在 `JokerData.InitializeJoker()` 中初始化属性
3. 根据触发时机，在对应方法中添加效果逻辑：
   - `OnCardScoreTrigger()` - 单张牌计分时
   - `OnJokerCalcTrigger()` - 牌型计分后
   - `OnDiscardTrigger()` - 弃牌时
   - `OnEndRoundTrigger()` - 回合结束时
4. 在 `GetDescription()` 中添加描述文本

### 修改牌型基础数值

修改 `Scoring.HandTypeData` 字典中对应牌型的 `HandTypeInfo`：
```csharp
{ HandType.Pair, new HandTypeInfo(baseChips: 10, baseMult: 2, chipsPerLevel: 15, multPerLevel: 1) }
```

---

## 📝 重要设计说明

### 为什么CardData和Card分离？

- **CardData**：纯数据类，可序列化，用于网络传输和逻辑计算
- **Card**：MonoBehaviour组件，用于UI交互（拖拽、选择）
- 分离后 `new CardData()` 可以正常工作，不会有Unity组件限制

### 为什么计分那么复杂？

Balatro的计分系统是游戏深度的核心来源：
1. **三槽位系统**让每张牌都有独特性
2. **逐张计分**让牌的顺序有意义
3. **触发时机**让小丑牌组合有策略深度
4. **×mult在最后计算**创造了数值爆发的快感

### 服务器权威设计的意义

- 所有游戏逻辑都在服务器执行
- 客户端只负责发送请求和显示结果
- 防止客户端作弊（伪造分数、金钱等）

---

