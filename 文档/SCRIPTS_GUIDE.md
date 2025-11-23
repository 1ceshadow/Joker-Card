# Unity 场景配置完整指南

## TextMeshPro 组件选择

### 推荐使用：**TextMeshPro - Text (UI)**
- 这是用于 UI Canvas 的文本组件
- 如果和 Image 无法共存，说明它们在同一层级，需要调整布局：
  - **方案1**：将 TextMeshPro 和 Image 放在不同的子对象上
  - **方案2**：使用 RectTransform 调整位置，让它们不重叠
  - **方案3**：使用 Canvas Group 或 Mask 来控制显示

### 组件说明
- **TextMeshPro - Text (UI)**：用于 UI Canvas，推荐使用 ✅
- **TextMeshPro - Text**：用于 3D 场景中的文本
- **TextMeshPro - Input Field**：用于输入框

## 资源文件处理

### 1. 卡牌图片切分（PlayingCards.png）

需要创建一个脚本来动态切分大图片：

```csharp
// Assets/Scripts/Utils/CardSpriteLoader.cs
using UnityEngine;

public static class CardSpriteLoader
{
    private static Sprite[] cardSprites;
    private static Sprite cardBackSprite;
    
    // 卡牌排列：从左到右 2-10-JQKA (13列)，从上到下 红桃-梅花-方块-黑桃 (4行)
    // 每张卡牌大小：923/13 = 71px, 380/4 = 95px
    
    public static void LoadCardSprites()
    {
        Texture2D cardTexture = Resources.Load<Texture2D>("PlayingCards/PlayingCards");
        if (cardTexture == null)
        {
            Debug.LogError("卡牌图片未找到！");
            return;
        }
        
        cardSprites = new Sprite[52];
        int cardWidth = 71;  // 923 / 13
        int cardHeight = 95; // 380 / 4
        
        // 花色顺序：红桃(0), 梅花(1), 方块(2), 黑桃(3)
        // 点数顺序：2(0), 3(1), 4(2), 5(3), 6(4), 7(5), 8(6), 9(7), 10(8), J(9), Q(10), K(11), A(12)
        int index = 0;
        for (int suit = 0; suit < 4; suit++)
        {
            for (int rank = 0; rank < 13; rank++)
            {
                int x = rank * cardWidth;
                int y = (3 - suit) * cardHeight; // 从下往上，所以是 3-suit
                
                Rect rect = new Rect(x, y, cardWidth, cardHeight);
                cardSprites[index] = Sprite.Create(cardTexture, rect, new Vector2(0.5f, 0.5f), 100);
                index++;
            }
        }
    }
    
    public static Sprite GetCardSprite(Card.Suit suit, int rank)
    {
        if (cardSprites == null)
            LoadCardSprites();
            
        // 计算索引：suit * 13 + (rank - 2)
        int suitIndex = (int)suit;
        int rankIndex = rank - 2; // rank: 2-14, index: 0-12
        
        int spriteIndex = suitIndex * 13 + rankIndex;
        if (spriteIndex >= 0 && spriteIndex < cardSprites.Length)
            return cardSprites[spriteIndex];
            
        return null;
    }
    
    public static void LoadCardBack()
    {
        cardBackSprite = Resources.Load<Sprite>("PlayingCards/Cardsback");
        if (cardBackSprite == null)
        {
            // 如果有多张背景，随机选择
            Sprite[] backs = Resources.LoadAll<Sprite>("PlayingCards/Cardsback");
            if (backs != null && backs.Length > 0)
            {
                cardBackSprite = backs[Random.Range(0, backs.Length)];
            }
        }
    }
    
    public static Sprite GetCardBackSprite()
    {
        if (cardBackSprite == null)
            LoadCardBack();
        return cardBackSprite;
    }
}
```

### 2. 小丑牌图片切分（Jokers.png）

```csharp
// Assets/Scripts/Utils/JokerSpriteLoader.cs
using UnityEngine;

public static class JokerSpriteLoader
{
    private static Sprite[] jokerSprites;
    
    public static void LoadJokerSprites()
    {
        Texture2D jokerTexture = Resources.Load<Texture2D>("Jokers/Jokers");
        if (jokerTexture == null)
        {
            Debug.LogError("小丑牌图片未找到！");
            return;
        }
        
        // 需要根据实际图片布局来切分
        // 这里假设是网格布局，需要根据实际情况调整
        // 暂时使用整个图片作为单个 Sprite
        jokerSprites = new Sprite[6];
        
        // 如果图片是网格布局，需要计算每个小丑牌的位置和大小
        // 示例：假设是 2x3 或 3x2 的网格
        // 这里需要根据实际图片调整
    }
    
    public static Sprite GetJokerSprite(JokerData.JokerType type)
    {
        // 暂时返回整个图片，需要根据实际布局切分
        Texture2D jokerTexture = Resources.Load<Texture2D>("Jokers/Jokers");
        if (jokerTexture != null)
        {
            return Sprite.Create(jokerTexture, new Rect(0, 0, jokerTexture.width, jokerTexture.height), 
                new Vector2(0.5f, 0.5f), 100);
        }
        return null;
    }
}
```

### 3. 头像图片切分（Avatars.png）

```csharp
// Assets/Scripts/Utils/AvatarSpriteLoader.cs
using UnityEngine;

public static class AvatarSpriteLoader
{
    private static Sprite[] avatarSprites;
    
    public static void LoadAvatarSprites()
    {
        Texture2D avatarTexture = Resources.Load<Texture2D>("Avatars/Avatars");
        if (avatarTexture == null)
        {
            Debug.LogError("头像图片未找到！");
            return;
        }
        
        // 需要根据实际图片布局来切分
        // 假设是网格布局，需要计算每个头像的位置和大小
        // 这里需要根据实际图片调整
    }
    
    public static Sprite GetAvatarSprite(int avatarId)
    {
        if (avatarSprites == null)
            LoadAvatarSprites();
            
        if (avatarSprites != null && avatarId >= 0 && avatarId < avatarSprites.Length)
            return avatarSprites[avatarId];
            
        return null;
    }
}
```

## 场景配置详细步骤

### 场景1：MainMenu（主菜单场景）

#### 1. 创建 Canvas
- 右键 Hierarchy → UI → Canvas
- 设置 Canvas Scaler：
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1920x1080

#### 2. 创建主菜单根对象
- 右键 Canvas → Create Empty，命名为 "MainMenu"
- 添加组件：`MainMenu` (Assets/Scripts/UI/MainMenu.cs)

#### 3. 配置左侧玩家信息区域
```
MainMenu
└── LeftPanel (空对象，用于布局)
    ├── PlayerInfoPanel
    │   ├── AvatarImage (Image 组件)
    │   ├── PlayerNameText (TextMeshPro - Text (UI))
    │   └── MoneyText (TextMeshPro - Text (UI))
    └── ButtonPanel
        ├── CreateRoomButton (Button)
        ├── JoinRoomButton (Button)
        └── ExitButton (Button)
```

**配置 MainMenu 组件引用：**
- `avatarImage` → PlayerInfoPanel/AvatarImage
- `playerNameText` → PlayerInfoPanel/PlayerNameText
- `moneyText` → PlayerInfoPanel/MoneyText
- `createRoomButton` → ButtonPanel/CreateRoomButton
- `joinRoomButton` → ButtonPanel/JoinRoomButton
- `exitButton` → ButtonPanel/ExitButton

#### 4. 配置右侧商店区域
```
MainMenu
└── RightPanel
    ├── ShopUI (空对象，添加 ShopUI 组件)
    │   ├── ShopJokersParent (空对象，用于放置商店小丑牌)
    │   └── PlayerJokersParent (空对象，用于放置玩家小丑牌)
    └── ShopTitle (TextMeshPro - Text (UI))
```

**配置 ShopUI 组件引用：**
- `shopJokersParent` → ShopUI/ShopJokersParent
- `playerJokersParent` → ShopUI/PlayerJokersParent

**配置 MainMenu 组件引用：**
- `shopUI` → RightPanel/ShopUI

#### 5. 配置玩家信息输入窗口
```
MainMenu
└── PlayerInfoWindow (GameObject，默认隐藏)
    ├── Background (Image，半透明黑色背景)
    ├── WindowPanel (Image，白色背景)
    │   ├── TitleText (TextMeshPro - Text (UI))
    │   ├── NameInputField (TMP_InputField)
    │   ├── AvatarSelectionParent (空对象，用于放置头像选择按钮)
    │   └── ConfirmButton (Button)
    └── CloseButton (Button，可选)
```

**配置 MainMenu 组件引用：**
- `playerInfoWindow` → PlayerInfoWindow
- `nameInputField` → PlayerInfoWindow/WindowPanel/NameInputField
- `confirmButton` → PlayerInfoWindow/WindowPanel/ConfirmButton
- `avatarSelectionParent` → PlayerInfoWindow/WindowPanel/AvatarSelectionParent

#### 6. 创建头像选择按钮 Prefab
- 创建 Button，命名为 "AvatarButtonPrefab"
- 添加 Image 组件显示头像
- 保存为 Prefab：Assets/Prefabs/AvatarButtonPrefab.prefab

**配置 MainMenu 组件引用：**
- `avatarButtonPrefab` → Assets/Prefabs/AvatarButtonPrefab.prefab

### 场景2：GameRoom（房间场景）
TODO待修改

#### 1. 创建 Canvas（同上）

#### 2. 创建 RoomUI 根对象
- 添加组件：`RoomUI` (Assets/Scripts/UI/RoomUI.cs)

#### 3. 配置房主UI（显示IP和二维码）
```
RoomUI
├── HostPanel (空对象，房主时显示)
│   ├── RoomIPText (TextMeshPro - Text (UI))
│   ├── QRCodeImage (Image)
│   ├── PlayerListParent (空对象，用于放置玩家头像)
│   ├── StartGameButton (Button)
│   └── LeaveRoomButton (Button)
└── JoinPanel (空对象，加入者时显示)
    ├── IPInputField (TMP_InputField)
    ├── ConnectButton (Button)
    └── ScanQRButton (Button)
```

**配置 RoomUI 组件引用：**
- `roomIPText` → HostPanel/RoomIPText
- `qrCodeImage` → HostPanel/QRCodeImage
- `startGameButton` → HostPanel/StartGameButton
- `leaveRoomButton` → HostPanel/LeaveRoomButton
- `playerListParent` → HostPanel/PlayerListParent
- `joinRoomPanel` → JoinPanel
- `ipInputField` → JoinPanel/IPInputField
- `connectButton` → **JoinPanel/**ConnectButton
- `scanQRButton` → JoinPanel/ScanQRButton

#### 4. 配置玩家头像 Prefab
- 使用现有的：Assets/Prefabs/PlayerAvatarInRoom.prefab
- 确保包含：
  - Image (头像)
  - TextMeshPro - Text (UI) (玩家名称)

**配置 RoomUI 组件引用：**
- `playerAvatarPrefab` → Assets/Prefabs/PlayerAvatarInRoom.prefab

### 场景3：GameScene（游戏场景）

#### 1. 创建 Canvas（同上）

#### 2. 创建 GameManager
- 创建空对象 "GameManager"
- 添加组件：
  - `NetworkIdentity` (Mirror)
  - `GameManager` (Assets/Scripts/Game/GameManager.cs)

**配置 GameManager 组件引用：**
- `gameUI` → 需要找到 GameUI 对象（见下方）

#### 3. 创建 GameUI 根对象
- 创建空对象 "GameUI"
- 添加组件：`GameUI` (Assets/Scripts/UI/GameUI.cs)

#### 4. 配置左上角玩家信息
```
GameUI
└── PlayerInfoPanel (空对象)
    ├── AvatarImage (Image)
    ├── PlayerNameText (TextMeshPro - Text (UI))
    └── MoneyText (TextMeshPro - Text (UI))
```

**配置 GameUI 组件引用：**
- `playerAvatarImage` → PlayerInfoPanel/AvatarImage
- `playerNameText` → PlayerInfoPanel/PlayerNameText
- `playerMoneyText` → PlayerInfoPanel/MoneyText

#### 5. 配置小丑牌区域（中间偏右上方）
```
GameUI
└── JokersPanel (空对象)
    └── JokersParent (空对象，Horizontal Layout Group)
```

**配置 GameUI 组件引用：**
- `jokersParent` → JokersPanel/JokersParent

#### 6. 配置手牌区域（底部）
```
GameUI
└── HandCardsPanel (空对象)
    ├── HandCardsParent (空对象，Horizontal Layout Group)
    └── ActionButtonsPanel
        ├── PlayCardButton (Button)
        ├── BetButton (Button)
        └── DiscardButton (Button)
```

**配置 GameUI 组件引用：**
- `handCardsParent` → HandCardsPanel/HandCardsParent
- `playCardButton` → HandCardsPanel/ActionButtonsPanel/PlayCardButton
- `betButton` → HandCardsPanel/ActionButtonsPanel/BetButton
- `discardButton` → HandCardsPanel/ActionButtonsPanel/DiscardButton

#### 7. 配置其他玩家区域（四周，逆时针）
```
GameUI
└── OtherPlayersPanel (空对象)
    └── OtherPlayersParent (空对象，用于动态创建玩家头像)
```

**配置 GameUI 组件引用：**
- `otherPlayersParent` → OtherPlayersPanel/OtherPlayersParent
- `playerAvatarPrefab` → Assets/Prefabs/PlayerAvatarInRoom.prefab

#### 8. 配置中心出的牌区域
```
GameUI
└── CenterCardsPanel (空对象)
    ├── CenterCardsParent (空对象，Horizontal Layout Group)
    └── CurrentScoreText (TextMeshPro - Text (UI))
```

**配置 GameUI 组件引用：**
- `centerCardsParent` → CenterCardsPanel/CenterCardsParent
- `currentScoreText` → CenterCardsPanel/CurrentScoreText

#### 9. 配置底池显示
```
GameUI
└── PotText (TextMeshPro - Text (UI))
```

**配置 GameUI 组件引用：**
- `potText` → PotText

#### 10. 配置结算面板
```
GameUI
└── ResultPanel (GameObject，默认隐藏)
    ├── Background (Image，半透明黑色)
    ├── ResultWindow (Image，白色背景)
    │   ├── TitleText (TextMeshPro - Text (UI))
    │   ├── ResultListParent (空对象，Vertical Layout Group)
    │   ├── WinnerText (TextMeshPro - Text (UI))
    │   ├── PotText (TextMeshPro - Text (UI))
    │   └── ReturnButton (Button)
    └── CloseButton (Button，可选)
```

**配置 ResultUI 组件引用：**
- 创建空对象 "ResultUI"，添加 `ResultUI` 组件
- `resultPanel` → ResultPanel
- `resultListParent` → ResultPanel/ResultWindow/ResultListParent
- `returnToMenuButton` → ResultPanel/ResultWindow/ReturnButton
- `potText` → ResultPanel/ResultWindow/PotText
- `winnerText` → ResultPanel/ResultWindow/WinnerText

**配置 GameUI 组件引用：**
- `resultUI` → ResultUI 对象

#### 11. 配置押注窗口
```
GameUI
└── BettingWindow (GameObject，默认隐藏)
    ├── Background (Image，半透明黑色)
    ├── BettingPanel (Image，白色背景)
    │   ├── TitleText (TextMeshPro - Text (UI))
    │   ├── AmountInputField (TMP_InputField)
    │   ├── MaxAmountText (TextMeshPro - Text (UI))
    │   ├── ConfirmButton (Button)
    │   └── CancelButton (Button)
    └── CloseButton (Button，可选)
```

**配置 BettingUI 组件引用：**
- 创建空对象 "BettingUI"，添加 `BettingUI` 组件
- `bettingWindow` → BettingWindow
- `amountInputField` → BettingWindow/BettingPanel/AmountInputField
- `confirmButton` → BettingWindow/BettingPanel/ConfirmButton
- `cancelButton` → BettingWindow/BettingPanel/CancelButton
- `maxAmountText` → BettingWindow/BettingPanel/MaxAmountText

#### 12. 配置卡牌 Prefab
- 使用现有的：Assets/Prefabs/Card.prefab
- 确保包含：
  - Image (卡牌背景，使用 CardSpriteLoader.GetCardBackSprite())
  - Image (卡牌正面，使用 CardSpriteLoader.GetCardSprite())
  - TextMeshPro - Text (UI) (点数显示，可选，如果图片已包含)
  - Button (点击交互)

**配置 GameUI 组件引用：**
- `cardPrefab` → Assets/Prefabs/Card.prefab

#### 13. 配置小丑牌 Prefab
- 使用现有的：Assets/Prefabs/JokerItem.prefab
- 确保包含：
  - Image (小丑牌图片)
  - TextMeshPro - Text (UI) (名称)
  - TextMeshPro - Text (UI) (描述)
  - TextMeshPro - Text (UI) (价格)
  - Button (购买/操作按钮)
  - Button (售卖按钮)

**配置 GameUI 组件引用：**
- `jokerItemPrefab` → Assets/Prefabs/JokerItem.prefab

## 重要提示

### TextMeshPro 和 Image 共存问题
如果 TextMeshPro 和 Image 无法共存，解决方案：

1. **使用子对象分离**：
```
CardObject
├── CardImage (Image，显示卡牌图片)
└── CardText (TextMeshPro - Text (UI)，显示点数)
```

2. **使用 RectTransform 调整位置**：
- 让它们不重叠
- 使用 Anchor 和 Pivot 精确定位

3. **使用 Mask 或 Canvas Group**：
- 控制显示区域
- 实现遮罩效果

### 资源加载顺序
1. 在游戏开始时调用 `CardSpriteLoader.LoadCardSprites()`
2. 在需要时调用 `CardSpriteLoader.GetCardSprite()`
3. 同样处理小丑牌和头像

### 网络对象设置
- GameManager 必须添加 `NetworkIdentity` 组件
- Player Prefab 必须添加 `NetworkIdentity` 和 `PlayerData` 组件
- 在 NetworkManager 中设置 Player Prefab

### 测试建议
1. 先配置单个场景，测试 UI 显示
2. 测试资源加载和切分
3. 测试网络连接
4. 测试完整游戏流程

