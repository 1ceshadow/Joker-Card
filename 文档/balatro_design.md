# Balatro Clone: Technical Design Document (TDD)

## 1. Project Overview
**Objective:** Create a faithful recreation of the core mechanics of the game "Balatro".
**Genre:** Poker-themed Roguelike Deck-builder.
**Core Loop:** Select Blind -> Play Poker Hands -> Earn Chips/Money -> Buy Jokers/Upgrades in Shop -> Next Blind.

---

## 2. Core Game Hierarchy

### 2.1 The Run
A single game session consisting of multiple **Antes**.
* **Game Over Condition:** Failing to reach the Target Score (Chips) for a Blind.
* **Win Condition:** Beating Ante 8 Boss Blind.

### 2.2 Ante Structure
The game progresses through "Antes" (Level 1, 2, 3...). Each Ante consists of 3 **Blinds**:
1.  **Small Blind:** Low target score. Reward: $ (Skippable for Tag).
2.  **Big Blind:** Medium target score. Reward: $$ (Skippable for Tag).
3.  **Boss Blind:** High target score. Reward: $$$. **Has a Special Ability (Debuff).** (Not Skippable).

### 2.3 The Round
The actual gameplay within a Blind.
* **Resources:**
    * `Hands`: Number of times player can play cards (Default: 4).
    * `Discards`: Number of times player can discard up to 5 cards (Default: 3).
    * `Hand Size`: Max cards held (Default: 8).
    * `Deck`: The current deck of cards (starts with standard 52).

---

## 3. Entity Data Structures

### 3.1 Playing Card (Atomic Unit)
Each card in the deck must have:
* **Suit:** Spades, Hearts, Clubs, Diamonds.
* **Rank:** 2-10, J, Q, K, A. (Value: 2-10=Face, JQK=10, A=11).
* **Enhancement (Slot 1):**
    * *Bonus:* +30 Chips.
    * *Mult:* +4 Mult.
    * *Wild:* Counts as any suit.
    * *Glass:* X2 Mult, 1/4 chance to destroy on play.
    * *Steel:* X1.5 Mult while held in hand.
    * *Stone:* +50 Chips, no Rank/Suit.
    * *Gold:* Earn $3 if held at end of round.
    * *Lucky:* 1/5 chance +20 Mult, 1/15 chance +$20.
* **Edition (Slot 2):**
    * *Base:* No effect.
    * *Foil:* +50 Chips.
    * *Holographic:* +10 Mult.
    * *Polychrome:* X1.5 Mult.
* **Seal (Slot 3):**
    * *Red:* Retrigger card 1 time.
    * *Blue:* Create Planet card if held at end of round.
    * *Gold:* Earn $3 on play.
    * *Purple:* Create Tarot card on discard.

### 3.2 Joker (Core Modifier)
Jokers are the engine of the build.
* **Rarity:** Common, Uncommon, Rare, Legendary.
* **Effect Type:** `+Chips`, `+Mult`, `X Mult`, `Utility`, `Economy`.
* **Trigger Contexts:**
    * `OnScore`: Trigger when an individual card is scored.
    * `OnJokerCalc`: Trigger after cards are scored (Independent).
    * `OnDiscard`: Trigger when cards are discarded.
    * `OnEndRound`: Trigger after round clears.

---

## 4. The Scoring Engine (CRITICAL)

The scoring algorithm must follow a strict **Order of Operations**.
**Formula:** `Final Score = Total Chips * Total Mult`

### Step-by-Step Pipeline:

1.  **Hand Identification:**
    * Analyze played cards (max 5 scoring) to determine the Poker Hand (e.g., Flush, Pair).
    * *Note:* Stone cards count towards "played" count but not towards Rank/Suit logic.

2.  **Base Calculation:**
    * Fetch `Base Chips` and `Base Mult` from the Hand Type's current **Planet Level**.
    * *Example (Lvl 1 Pair):* 10 Chips, 2 Mult.

3.  **Card Scoring Loop (Left to Right):**
    * Iterate through each **Scoring Card** (Non-scoring cards are ignored unless "Splash" Joker exists).
    * **A. Card Base:** Add Card Rank Value to `Total Chips`.
    * **B. Card Modifiers:**
        * Add Enhancement/Edition bonuses to `Total Chips` / `Total Mult`.
    * **C. Joker Triggers (OnScore):**
        * Check all Jokers. If condition met (e.g., "Played a Heart"), apply Joker effect.
    * **D. Card Multipliers:**
        * Apply Glass/Polychrome (X Mult) to `Total Mult`.
    * **E. Red Seal:**
        * If card has Red Seal, repeat steps A-D.

4.  **Hand-Based Triggers:**
    * Check cards **Held in Hand**. Apply Steel/King/Queen effects (e.g., Steel adds X1.5 Mult).

5.  **Joker Independent Calculation (Left to Right):**
    * Iterate through Jokers one last time for standalone effects.
    * Add `+Mult` first.
    * Apply `X Mult` last (This is why Joker order matters).

6.  **Finalize:**
    * `Score = Total Chips * Total Mult`.
    * Add to `Round Score`. Check against `Target Score`.

---

## 5. Economy & Shop System

### 5.1 Money
* **Base Reward:** Small ($3), Big ($4), Boss ($5).
* **Interest:** Earn $1 per $5 saved (Max $5 interest by default). Cap at $25 savings.
* **Hands Remaining:** $1 per remaining hand.

### 5.2 The Shop
Generated after every round. Contains:
* **2 Cards:** Random selection of Joker, Tarot, Planet, or Spectral cards.
* **2 Packs:** Booster packs (Standard, Arcana, Celestial, Buffoon, Spectral).
* **1 Voucher:** Passive permanent upgrade (restocks only after Ante boss).
* **Reroll:** Cost starts at $5, increases per use in same shop.

---

## 6. Scaling & Difficulty

### 6.1 Hand Types (Base Values)
* **High Card:** 5 Chips / 1 Mult
* **Pair:** 10 / 2
* **Two Pair:** 20 / 2
* **Three of a Kind:** 30 / 3
* **Straight:** 30 / 4
* **Flush:** 35 / 4
* **Full House:** 40 / 4
* **Four of a Kind:** 60 / 7
* **Straight Flush:** 100 / 8
* *Royal Flush:* 100 / 8

### 6.2 Ante Scaling (Exponential)
Target scores increase significantly per Ante.
* *Rough curve:* `Base * (GrowthFactor ^ Ante)`.
* Ante 1 Boss: ~600
* Ante 8 Boss: ~100,000+

---

## 7. Implementation Pseudo-Code (Python-like)

```python
class ScoringEngine:
    def calculate_hand(self, played_cards, hand_type, jokers, held_cards):
        # 1. Init from Planet Level
        current_chips = hand_type.base_chips + (hand_type.level * hand_type.chip_scale)
        current_mult = hand_type.base_mult + (hand_type.level * hand_type.mult_scale)

        # 2. Score Cards (Left -> Right)
        for card in played_cards:
            if card.is_debuffed: continue
            
            # Repetitions for Red Seal
            triggers = 2 if card.seal == "Red" else 1
            
            for _ in range(triggers):
                # Card Values
                current_chips += card.get_chip_value() # Rank + Bonus + Foil
                current_mult += card.get_mult_value()  # Holo + Enhancement
                
                # Joker triggers specific to card scoring
                for joker in jokers:
                    ret = joker.trigger("on_card_score", card)
                    current_chips += ret.chips
                    current_mult += ret.mult

                # X Mults on cards
                if card.enhancement == "Glass": current_mult *= 2.0
                if card.edition == "Polychrome": current_mult *= 1.5

        # 3. Held in Hand Effects
        for card in held_cards:
            if card.enhancement == "Steel":
                current_mult *= 1.5

        # 4. Global Joker Effects (Left -> Right)
        for joker in jokers:
            current_mult += joker.passive_mult
            if joker.passive_x_mult > 1:
                current_mult *= joker.passive_x_mult

        return int(current_chips) * int(current_mult)
```
## 8. Development Roadmap for AI
1. Phase 1: Logic & Data: Implement Card, Deck, HandEvaluator logic without UI.

2. Phase 2: Loop: Implement the GameLoop (Draw -> Play -> Discard -> Score).

3. Phase 3: Joker Engine: Create the Joker base class and implement the trigger system.

4. Phase 4: Shop & Economy: Implement money calculation and shop generation.

5. Phase 5: UI/Interaction: Connect logic to a visual interface.