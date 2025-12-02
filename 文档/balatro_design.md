<DOCUMENT filename="balatro_design_zh.md">

# Balatro 机制拆解

## 1. 项目概述
**目标**：忠实还原《Balatro》的核心机制  
**类型**：扑克主题 Roguelike 牌堆构建游戏  
**核心循环**：选择 Blind → 打出扑克牌型 → 赚取筹码/金钱 → 在商店购买小丑/升级 → 进入下一个 Blind

---

## 2. 游戏整体结构

### 2.1 一局游戏（Run）
一次完整的游戏过程，由多个 **Ante（阶段）** 组成。  
- **失败条件**：未达到当前 Blind 要求的筹码目标分数  
- **胜利条件**：击败 Ante 8 的 Boss Blind

### 2.2 Ante 结构
每局游戏按“Ante”递增（Ante 1、Ante 2……），每个 Ante 包含 3 个 **Blind**：
1. **Small Blind（小盲）**：目标分数低，可跳过换取 Tag，奖励少量金钱  
2. **Big Blind（大盲）**：目标分数中等，可跳过换取 Tag，奖励中等金钱  
3. **Boss Blind（Boss 盲）**：目标分数高，**不可跳过**，拥有特殊 Debuff 能力，奖励最多金钱

### 2.3 回合（Round）
在单个 Blind 内的实际对局。  
**资源**：
- `Hands`：可出牌次数（默认 4 次）  
- `Discards`：可丢弃次数（默认 3 次，每次最多丢 5 张牌）  
- `Hand Size`：手牌上限（默认 8 张）  
- `Deck`：当前牌组（初始为标准 52 张扑克）

---

## 3. 实体数据结构

### 3.1 扑克牌（最小单位）
每张牌包含以下属性：
- **花色**：黑桃、红心、梅花、方块  
- **点数**：2-10、J、Q、K、A（数值：2-10 面值，J/Q/K=10，A=11）  
- **强化（槽位 1）**：
  - Bonus：+30 筹码  
  - Mult：+4 倍率  
  - Wild：可当作任意花色  
  - Glass：×2 倍率，发挥时有 1/4 概率销毁  
  - Steel：持有时 ×1.5 倍率  
  - Stone：+50 筹码，无点数与花色  
  - Gold：回合结束时若持有，赚 $3  
  - Lucky：1/5 概率 +20 倍率，1/15 概率 +$20  
- **版本（槽位 2）**：
  - Base：无效果  
  - Foil：+50 筹码  
  - Holographic：+10 倍率  
  - Polychrome：×1.5 倍率  
- **印章（槽位 3）**：
  - 红印：重触发该牌 1 次  
  - 蓝印：回合结束时若持有，生成 1 张星球卡  
  - 金印：出牌时赚 $3  
  - 紫印：丢弃时生成 1 张塔罗卡

### 3.2 小丑（Joker）——核心构建引擎
- **稀有度**：普通、罕见、稀有、传奇  
- **效果类型**：+筹码、+倍率、×倍率、功能性、经济性  
- **触发时机**：
  - `OnScore`：单张牌计分时触发  
  - `OnJokerCalc`：牌型计分完成后独立触发  
  - `OnDiscard`：丢弃牌时触发  
  - `OnEndRound`：回合结束时触发

---

## 4. 计分引擎（最核心部分）

计分必须严格遵守 **运算顺序**。  
**最终公式**：`最终分数 = 总筹码 × 总倍率`

### 详细计分流程：

1. **牌型识别**  
   分析出牌（最多 5 张计分牌）判定当前扑克牌型（如同花、葫芦等）  
   ※ 石头牌计入“出牌数量”，但不参与点数/花色判定

2. **基础数值获取**  
   根据当前牌型的 **星球卡等级** 获取基础筹码与倍率  
   示例（等级 1 的对子）：10 筹码，2 倍率

3. **逐张计分（从左到右）**  
   对每张**计分牌**进行以下操作：
   A. 加上该牌的点数筹码  
   B. 加上强化/版本的加成  
   C. 触发所有满足条件的“单张牌计分”小丑效果  
   D. 应用 Glass/Polychrome 等 ×倍率效果  
   E. 若有红印，重复 A~D 一次

4. **手牌中持有效果**  
   检查手牌中未出牌的部分，触发 Steel 等效果（如 Steel 牌 ×1.5 倍率）

5. **小丑独立效果（从左到右）**  
   - 先结算所有 “+倍率”  
   - 最后结算所有 “×倍率”（这就是小丑顺序非常重要的原因）

6. **最终计算**  
   `分数 = 总筹码 × 总倍率` → 累加到本回合总分 → 与目标分数对比

---

## 5. 经济与商店系统

### 5.1 金钱来源
- 基础奖励：小盲 $3、大盲 $4、Boss $5  
- 利息：每持有 $5 获得 $1 利息（默认最高 $5 利息），存款上限 $25  
- 剩余出牌次数：每剩 1 次出牌 +$1

### 5.2 商店
每打完一个 Blind 后刷新，包含：
- 2 张单卡：随机出现 小丑 / 塔罗 / 星球 / 幽灵卡  
- 2 个卡包：标准包、奥术包、天界包、小丑包、幽灵包  
- 1 张凭证（Voucher）：永久被动升级，仅在击败 Boss 后刷新新凭证  
- 重掷：首次 $5，每次在本商店内使用后价格上涨

---

## 6. 难度与成长曲线

### 6.1 牌型基础数值（未升级时）
- 高牌：5 筹码 / 1 倍率  
- 对子：10 / 2  
- 两对：20 / 2  
- 三条：30 / 3  
- 顺子：30 / 4  
- 同花：35 / 4  
- 葫芦：40 / 4  
- 四条：60 / 7  
- 同花顺：100 / 8  
- 皇家同花顺：100 / 8

### 6.2 Ante 难度指数增长
目标分数呈指数级上升，大致公式：`基础值 × (增长系数 ^ Ante)`  
- Ante 1 Boss：约 600 分  
- Ante 8 Boss：10万+ 分

---

## 7. 实现伪代码（类 Python）

```python
class ScoringEngine:
    def calculate_hand(self, played_cards, hand_type, jokers, held_cards):
        # 1. 从星球卡等级获取基础值
        current_chips = hand_type.base_chips + (hand_type.level * hand_type.chip_scale)
        current_mult = hand_type.base_mult + (hand_type.level * hand_type.mult_scale)

        # 2. 逐张计分（左→右）
        for card in played_cards:
            if card.is_debuffed: 
                continue
                
            # 红印重触发
            triggers = 2 if card.seal == "Red" else 1
            
            for _ in range(triggers):
                # 牌本身数值
                current_chips += card.get_chip_value()   # 点数 + Bonus + Foil
                current_mult += card.get_mult_value()    # Holo + Mult强化
                
                # 小丑单张牌触发
                for joker in jokers:
                    ret = joker.trigger("on_card_score", card)
                    current_chips += ret.chips
                    current_mult += ret.mult

                # 卡片自带 X 倍率
                if card.enhancement == "Glass": 
                    current_mult *= 2.0
                if card.edition == "Polychrome": 
                    current_mult *= 1.5

        # 3. 手牌中持有效果（如钢牌）
        for card in held_cards:
            if card.enhancement == "Steel":
                current_mult *= 1.5

        # 4. 小丑全局效果（左→右）
        for joker in jokers:
            current_mult += joker.passive_mult
            if joker.passive_x_mult > 1:
                current_mult *= joker.passive_x_mult   # X倍率最后算

        return int(current_chips) * int(current_mult)
```

## 8. AI 开发路线图
1. **阶段 1**：逻辑与数据 —— 实现 Card、Deck、手牌判定逻辑（无 UI）  
2. **阶段 2**：游戏循环 —— 实现抽牌 → 出牌 → 丢弃 → 计分的完整循环  
3. **阶段 3**：小丑引擎 —— 建立 Joker 基类与触发系统  
4. **阶段 4**：商店与经济 —— 实现金钱计算、商店生成  
5. **阶段 5**：UI 与交互 —— 将逻辑接入可视化界面

</DOCUMENT>
