<!-- .github/copilot-instructions.md - guidance for AI coding agents working on Joker-Card -->
# Copilot Instructions for Joker-Card

Unity 6 LAN multiplayer card game combining 炸金花 (Chinese poker betting) with Balatro-style Joker mechanics.

## Architecture Overview

```
Assets/Scripts/
├── Network/           # Mirror networking (Host/Client)
│   ├── NetworkManagerCustom.cs  → Room creation, Host/Client lifecycle, singleton
│   ├── PlayerData.cs            → SyncVars for player state (money, debt, cards, jokers)
│   └── RoomState.cs             → Room player tracking
├── Game/              # Core game logic (server-authoritative)
│   ├── GameManager.cs           → Turn flow, betting, win conditions
│   ├── JokerData.cs             → 6 Joker types with mult bonuses
│   └── ShopManager.cs           → Post-round Joker shop
├── Cards/             # Poker mechanics
│   ├── Card.cs                  → Suit/rank (2-14, A=14)
│   ├── Deck.cs                  → Shuffle, deal, return cards
│   └── Scoring.cs               → Hand detection + chips×mult formula
└── UI/                # Scene-specific UIs
```

## Key Patterns

**Mirror Networking:**
- Attributes: `[SyncVar]`, `[Command]` (prefix `Cmd`), `[ClientRpc]` (prefix `Rpc`), `[Server]`
- Complex data synced via JSON strings: `handCardsJson`, `jokersJson`, `playedCardsJson`
- `NetworkManagerCustom.Instance` is singleton with `DontDestroyOnLoad`
- Transport auto-adds `TelepathyTransport` if none configured

**Server Authority (critical):**
- All money operations validated server-side via `[Server]` methods
- Debt/borrowing only allowed in MainMenu, not during active games
- `TrySubtractMoney()` returns false if insufficient funds (no mid-game debt creation)
- Winnings apply to `PayDebt()` first, then `AddMoney()`

**Scoring System (`Scoring.cs`):**
```csharp
// Formula: (baseChips + cardRankValues + jokerChips) × (baseMult + jokerMult)
// Joker bonuses applied left-to-right: all chips first, then all mult
int score = Scoring.CalculateScore(cards, jokers);
HandType type = Scoring.DetectHandType(cards);  // HighCard → StraightFlush
```

## Game Constants (from GDD)

| Parameter | Value |
|-----------|-------|
| Hand size | 8 cards |
| Max play | 5 cards |
| Discards | 3 times |
| Starting money | 20 |
| Starting ante | 5 |
| Max Jokers | 5 |
| Max debt | 200 (10× initial funds) |

## Scenes (Build Settings order)

1. `MainMenu` – No network, player setup, borrow money
2. `CreateRoom` – Used by both Host and Client after connection
3. `JoinRoom` – Client IP input before connecting
4. `GameScene` – Active gameplay

## Common Tasks

**Add new Joker type:**
1. Add enum in `JokerData.JokerType`
2. Add case in `InitializeJoker()` for name/prices
3. Add bonus logic in `CalculateBonus(List<Card>)`
4. Add description in `GetDescription()`

**Modify scoring:**
- Hand type base values: `Scoring.HandTypeData` dictionary
- Detection logic: `Scoring.DetectHandType()` checks in priority order

**Add new SyncVar:**
1. Add `[SyncVar(hook = nameof(OnXChanged))]` field in `PlayerData.cs`
2. Create hook method for client-side UI updates
3. Add `[Server]` setter method for server-side changes

## Testing

- **Local:** Create Room in Editor, inspect RoomUI player list
- **LAN:** Device A creates room, Device B joins via IP; same WiFi required
- Check `Debug.Log` in `NetworkManagerCustom.cs` for connection flow
- Console shows Mirror logs for connection state

## Documentation

Authoritative docs in `文档/`: 只是初步拟定，不一定完全正确
- `QUICK_REFERENCE.md` – Flow diagrams, parameter tables
- `NETWORK_SETUP.md` – Transport config, troubleshooting
- `PROGECT_GUIDE.md` – 项目完整结构与说明
- `balatro_design.md` – Detailed Balatro mechanics reference

## Do Not

- Modify `Assets/Mirror/` core code (vendored library)
- Create multiple NetworkManager instances
- Allow borrowing during active games

## 可以做/鼓励做
- 用中文回答我
- 有注释
- 大胆改，原代码有不合理的(即使和文档一致)、性能差的，可以告诉我，直接改
- 可以用MCP工具 unity、unity-MCP
- 可以用DOTS/ECS、Job System、Burst Compiler
- 可以用第三方插件（但要告诉我用的什么插件，最好是免费的）
- 尽量保持SOLID原则，低耦合、高内聚、易拓展，避免硬编码