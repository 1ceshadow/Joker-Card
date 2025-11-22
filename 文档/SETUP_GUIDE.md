# 游戏设置指南

## 脚本文件清单

### 网络系统 (Assets/Scripts/Network/)
1. **NetworkManagerCustom.cs** - 自定义网络管理器
2. **PlayerData.cs** - 玩家数据（网络同步）

### 卡牌系统 (Assets/Scripts/Cards/)
3. **Card.cs** - 卡牌数据类
4. **Deck.cs** - 牌组管理类
5. **Scoring.cs** - 分数计算系统

### 游戏逻辑 (Assets/Scripts/Game/)
6. **GameManager.cs** - 游戏管理器（核心逻辑）
7. **JokerData.cs** - 小丑牌数据类
8. **ShopManager.cs** - 商店管理器
9. **PlayerDataManager.cs** - 玩家数据管理器（保存/加载）
10. **QRCodeGenerator.cs** - 二维码生成器（可选）
11. **QRCodeScanner.cs** - 二维码扫描器（可选，移动端）

### UI系统 (Assets/Scripts/UI/)
12. **MainMenu.cs** - 主菜单UI
13. **RoomUI.cs** - 房间UI
14. **GameUI.cs** - 游戏内UI
15. **ShopUI.cs** - 商店UI
16. **BettingUI.cs** - 押注UI
17. **ResultUI.cs** - 结算UI
18. **CardUI.cs** - 卡牌UI组件
19. **JokerItemUI.cs** - 小丑牌UI组件
20. **PlayerAvatarInRoom.cs** - 房间内玩家头像UI
21. **ResultItemUI.cs** - 结算结果项UI

## Prefab 配置说明

### 1. Player Prefab (Assets/Prefabs/Player.prefab)
- 必须添加 `PlayerData` 组件
- 必须添加 `NetworkIdentity` 组件
- 设置为 NetworkManager 的 Player Prefab

### 2. Card Prefab (Assets/Prefabs/Card.prefab)
- 必须添加 `CardUI` 组件
- 包含以下UI元素：
  - Image (卡牌背景)
  - TextMeshProUGUI (点数显示)
  - Image (花色显示)
  - Button (点击交互)

### 3. JokerItem Prefab (Assets/Prefabs/JokerItem.prefab)
- 必须添加 `JokerItemUI` 组件
- 包含以下UI元素：
  - Image (小丑牌图片)
  - TextMeshProUGUI (名称)
  - TextMeshProUGUI (描述)
  - TextMeshProUGUI (价格)
  - Button (购买/操作按钮)
  - Button (售卖按钮，可选)

### 4. PlayerAvatarInRoom Prefab (Assets/Prefabs/PlayerAvatarInRoom.prefab)
- 必须添加 `PlayerAvatarInRoom` 组件
- 包含以下UI元素：
  - Image (头像)
  - TextMeshProUGUI (玩家名称)

## 场景设置

### MainMenu 场景
1. 创建 Canvas
2. 添加 `MainMenu` 组件到 Canvas 或空对象
3. 配置 MainMenu 的所有引用：
   - 玩家信息UI（头像、名称、资金文本）
   - 三个按钮（创建房间、加入房间、退出）
   - ShopUI 引用
   - 玩家信息输入窗口（包含输入框、确认按钮、头像选择区域）

### GameRoom 场景
1. 创建 Canvas
2. 添加 `RoomUI` 组件
3. 配置 RoomUI 的所有引用：
   - 房间IP文本
   - 二维码图片
   - 开始游戏按钮
   - 离开房间按钮
   - 玩家列表父对象
   - 加入房间面板（IP输入框、连接按钮、扫描按钮）

### GameScene 场景（游戏内场景）
1. 创建 Canvas
2. 添加 `GameUI` 组件
3. 配置 GameUI 的所有引用：
   - 玩家信息区域
   - 手牌区域
   - 小丑牌区域
   - 其他玩家区域
   - 中心出的牌区域
   - 三个操作按钮（出牌、押注、弃牌）
   - 底池文本
   - 结算面板引用
4. 添加 `GameManager` 组件到空对象（必须添加 NetworkIdentity）
5. 添加 `BettingUI` 组件（押注窗口）
6. 添加 `ResultUI` 组件（结算面板）

## NetworkManager 设置

1. 创建空对象，添加 `NetworkManagerCustom` 组件
2. 设置 Player Prefab 为 Player.prefab
3. 配置网络传输（推荐使用 Telepathy 或 KCP）
4. 设置最大连接数（默认5人）

## 资源文件要求

### Avatars (Assets/Resources/Avatars/)
- avatar_0.png 到 avatar_9.png（至少10个头像）

### Jokers (Assets/Resources/Jokers/)
- Joker.png
- GreedyJoker.png
- LustyJoker.png
- WrathfulJoker.png
- GluttonousJoker.png
- JollyJoker.png

### PlayingCards (Assets/Resources/PlayingCards/)
- 卡牌图片资源（可选，用于显示卡牌）

## 依赖库安装

### ZXing（二维码生成，可选）
1. 打开 Package Manager
2. 点击 "+" -> "Add package from git URL"
3. 输入：`https://github.com/zxing/zxing-unity.git`
4. 或者在 Asset Store 搜索 "ZXing" 并导入

### Mirror（已包含）
- 确保 Mirror 已正确导入项目

### TextMeshPro（已包含）
- 确保 TextMeshPro 已正确导入项目

## 打包设置

### Android 打包
1. File -> Build Settings -> 选择 Android
2. Player Settings:
   - Minimum API Level: Android 5.0 (API Level 21) 或更高
   - Target API Level: 根据需求设置
   - Internet Access: Require
   - Write Permission: External (SDCard)（用于保存玩家数据）

### iOS 打包
1. File -> Build Settings -> 选择 iOS
2. Player Settings:
   - Target minimum iOS Version: 11.0 或更高
   - Camera Usage Description: 如果使用二维码扫描功能
   - 需要 Xcode 进行最终编译

## 重要注意事项

1. **所有 UI 引用必须通过 [SerializeField] 在 Inspector 中手动分配**
2. **PlayerData 必须添加到 Player Prefab 上**
3. **GameManager 必须添加 NetworkIdentity 组件**
4. **所有网络同步的数据使用 [SyncVar] 和 RPC**
5. **服务器端逻辑使用 [Server] 属性**
6. **客户端调用服务器使用 [Command] 属性**
7. **商店每局结束后会自动刷新10张小丑牌**
8. **玩家数据保存在 Application.persistentDataPath**

## 测试步骤

1. 打开 MainMenu 场景
2. 第一次运行会提示输入玩家信息
3. 创建房间或加入房间
4. 当玩家数 >= 2 时可以开始游戏
5. 测试出牌、押注、弃牌功能
6. 测试小丑牌效果
7. 测试结算和返回主菜单

## 已知限制

1. 二维码功能需要 ZXing 库支持
2. 移动端二维码扫描需要额外插件
3. 卡牌图片需要手动添加到 Resources 文件夹
4. 头像资源需要手动准备

## 调试建议

1. 使用 Unity 的 Network Manager HUD 进行调试
2. 检查 Console 中的网络连接日志
3. 使用 Mirror 的 Network Statistics 查看网络状态
4. 在 PlayerData 中添加日志输出，跟踪数据同步

