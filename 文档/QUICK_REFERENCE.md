# 快速参考文档

## 联机流程速查

### 房主操作流程
```
MainMenu → 点击"创建房间"
    ↓
CreateRoom 场景加载
    ↓
StartHost() 启动服务器
    ↓
RoomUI 显示房间 IP（如：192.168.1.100）
    ↓
等待客户端加入...
    ↓
玩家加入后自动显示在列表中
    ↓
玩家数 >= 2 时，点击"开始游戏"进入 GameScene
```

### 客户端操作流程
```
MainMenu → 点击"加入房间"
    ↓
JoinRoom 场景加载（输入 IP 界面）
    ↓
输入房主 IP（如：192.168.1.100）
    ↓
点击"连接"按钮
    ↓
StartClient() 连接到房主
    ↓
自动加载 CreateRoom 场景
    ↓
RoomUI 显示房主的房间信息和玩家列表
    ↓
等待房主点击"开始游戏"
    ↓
进入 GameScene，开始游戏
```

## 关键代码调用链

### 房主加入流程
1. `MainMenu.OnCreateRoomClicked()` → 加载 CreateRoom 场景
2. `CreateRoomAfterSceneLoad()` → 启动 Host
3. `OnServerAddPlayer()` → 玩家加入时更新 RoomState
4. `UpdateRoomUI()` → 服务器更新房主端 UI

### 客户端加入流程
1. `JoinRoomUI.OnConnectClicked()` → 调用 `JoinRoomAfterSceneLoad(ip)`
2. `JoinRoomAfterSceneLoad()` → 启动 Client，加载 CreateRoom 场景
3. `OnClientConnect()` → 连接成功后启动 `UpdateRoomUIClientAfterConnect()` 协程
4. 协程等待 RoomUI 和玩家对象加载完成
5. `RoomUI.UpdatePlayerList()` → 客户端显示玩家列表

## 核心参数（来自 GDD）

- **开始底金**: 5
- **手牌数量**: 8
- **最多出牌数**: 5
- **最多弃牌数**: 4
- **剩余出牌次数**: 1
- **剩余弃牌次数**: 3
- **起始资金**: 20
- **最多小丑牌数**: 5
- **商店显示小丑牌数**: 10

## 6种小丑牌详情

| 名称 | 效果 | 商店价 | 售卖价 |
|------|------|--------|--------|
| Joker | +4 Mult | 3 | 3 |
| Greedy Joker | 每张方片 +3 Mult | 4 | 3 |
| Lusty Joker | 每张红桃 +3 Mult | 4 | 3 |
| Wrathful Joker | 每张黑桃 +3 Mult | 4 | 3 |
| Gluttonous Joker | 每张梅花 +3 Mult | 4 | 3 |
| Jolly Joker | 有对子 +8 Mult | 6 | 4 |

## 牌型分数表

| 牌型 | 基础 Chips | 基础 Mult | 示例分数（无joker） |
|------|-----------|-----------|-------------------|
| High Card | 0 | 1 | (14+5)×1 = 19 |
| Pair | 8 | 2 | 38×2 = 76 |
| Two Pair | 20 | 2 | 58×2 = 116 |
| Three of a Kind | 30 | 3 | 52×3 = 156 |
| Straight | 30 | 4 | 62×4 = 248 |
| Flush | 35 | 4 | 67×4 = 268 |
| Full House | 40 | 4 | 78×4 = 312 |
| Four of a Kind | 60 | 7 | 94×7 = 658 |
| Straight Flush | 100 | 8 | 112×8 = 896 |

## 游戏流程

1. **游戏开始**
   - 所有人投入押金（开始底金 = 5）
   - 随机确定出牌顺序
   - 洗牌并发牌（每人8张）

2. **回合循环**
   - 按顺序轮到每个玩家
   - 玩家可选择：出牌、押注、弃牌
   - 如果只剩该玩家，必须出牌
   - 如果剩余出牌次数为0，跳过

3. **出牌**
   - 选择1-5张牌
   - 计算分数并显示
   - 剩余出牌次数 -1
   - 所有牌（出的+没出的）放回牌组

4. **押注**
   - 输入押注金额
   - 扣除资金（可欠债）
   - 增加底池

5. **弃牌**
   - 选择要弃的牌
   - 弃掉的牌放回牌组
   - 立即摸等数量的牌
   - 剩余弃牌次数 -1

6. **游戏结束**
   - 所有玩家出完牌
   - 比较分数，最高分获胜
   - 分数相同则平分底池
   - 余数归系统
   - 赢钱优先还债
   - 刷新商店

## 关键代码位置

### 网络相关
- `NetworkManagerCustom.cs` - 网络管理器
- `PlayerData.cs` - 玩家数据同步

### 游戏逻辑
- `GameManager.cs` - 游戏流程控制
- `Deck.cs` - 牌组管理
- `Scoring.cs` - 分数计算
- `ShopManager.cs` - 商店管理

### UI
- `MainMenu.cs` - 主菜单
- `RoomUI.cs` - 房间界面
- `GameUI.cs` - 游戏内界面
- `ShopUI.cs` - 商店界面
- `BettingUI.cs` - 押注界面
- `ResultUI.cs` - 结算界面

## 重要方法

### GameManager
- `CmdStartGame()` - 开始游戏（客户端调用）
- `CmdPlayCards()` - 出牌
- `CmdBet()` - 押注
- `CmdDiscardCards()` - 弃牌

### PlayerData
- `SetHandCards()` - 设置手牌（服务器）
- `SetJokers()` - 设置小丑牌（服务器）
- `AddMoney()` - 增加资金（服务器）
- `SubtractMoney()` - 扣除资金（服务器，支持欠债）
- `PayDebt()` - 还债（服务器）

### Scoring
- `CalculateScore()` - 计算分数
- `DetectHandType()` - 检测牌型

### ShopManager
- `RefreshShop()` - 刷新商店
- `BuyJoker()` - 购买小丑牌
- `SellJoker()` - 售卖小丑牌

## 数据保存

- 保存路径：`Application.persistentDataPath/playerdata.json`
- 保存内容：名称、头像ID、资金、欠债、小丑牌列表
- 商店小丑牌：每局结束后刷新，不保存

## 网络同步

- 使用 Mirror 的 `[SyncVar]` 同步基础数据
- 使用 JSON 序列化同步复杂数据（手牌、小丑牌）
- 使用 `[Command]` 客户端调用服务器
- 使用 `[ClientRpc]` 服务器通知客户端
- 使用 `[Server]` 标记服务器端方法

## 打包注意事项

### Android
- Minimum API Level: 21 (Android 5.0)
- Internet Access: Require
- Write Permission: External (SDCard)

### iOS
- Target minimum iOS Version: 11.0
- Camera Usage Description（如果使用二维码扫描）

## 调试技巧

1. 使用 Mirror 的 Network Manager HUD
2. 检查 Console 日志
3. 使用 Network Statistics 查看网络状态
4. 在关键方法中添加 Debug.Log

## 常见问题

### Q: 手牌不同步？
A: 检查 PlayerData 的 handCardsJson 是否正确序列化

### Q: 小丑牌效果不生效？
A: 检查 Scoring.CalculateScore() 中的 jokers 参数是否正确传递

### Q: 商店不刷新？
A: 检查 GameManager.EndGame() 中是否调用了 ShopManager.RefreshShop()

### Q: 欠债系统不工作？
A: 检查 PlayerData.SubtractMoney() 中的逻辑

### Q: 分数计算错误？
A: 检查 Scoring.cs 中的牌型检测逻辑

