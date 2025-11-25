# 网络设置指南

## NetworkManager 配置步骤

### 1. 添加 Transport 组件

NetworkManagerCustom 需要 Transport 组件才能工作。有两种方式：

#### 方式1：手动添加（推荐）
1. 在 Hierarchy 中找到 NetworkManagerCustom 对象
2. 在 Inspector 中点击 "Add Component"
3. 搜索并添加以下之一：
   - **Telepathy Transport**（推荐，简单可靠）
   - **KCP Transport**（性能更好，但配置复杂）
   - **Simple Web Transport**（用于 WebGL）

#### 方式2：自动添加
代码已经自动添加 TelepathyTransport，但如果手动添加了其他 Transport，代码会优先使用手动添加的。

### 2. 配置 Player Prefab

1. 确保 Player Prefab 包含以下组件：
   - `NetworkIdentity`（Mirror）
   - `PlayerData`（自定义）

2. 在 NetworkManagerCustom 的 Inspector 中：
   - 将 Player Prefab 拖拽到 "Player Prefab" 字段

### 3. 场景配置

确保以下场景已添加到 Build Settings：
1. File → Build Settings
2. 添加场景（必须按此顺序）：
   - **0. MainMenu** - 主菜单（无网络）
   - **1. CreateRoom** - 房间场景（房主和客户端都使用）
   - **2. JoinRoom** - 加入房间场景（仅客户端使用，输入 IP）
   - **3. GameScene** - 游戏内场景

### 4. 网络模式说明

#### 游戏启动时
- **不启动网络**：游戏启动时不会自动连接网络
- 只有在点击"创建房间"或"加入房间界面的 connect"时才开始网络连接

#### 创建房间（房主）
- 点击"创建房间" → 加载 CreateRoom 场景
- 自动调用 `CreateRoomAfterSceneLoad()` 协程
- 启动 `StartHost()` → 服务器启动（Server + Client 模式）
- RoomUI 显示房间 IP 和二维码
- 等待客户端加入

#### 加入房间（客户端）
- 点击"加入房间" → 加载 JoinRoom 场景
- 输入房主 IP，点击连接
- 调用 `JoinRoomAfterSceneLoad(ip)` 协程
- 启动 `StartClient()` 连接到房主（Client 模式）
- **自动加载 CreateRoom 场景**与房主同步
- `OnClientConnect()` 自动同步玩家列表到 RoomUI

#### 离开房间
- 调用 `StopHost()` 或 `StopClient()`
- 断开网络连接
- 返回主菜单

## 常见问题

### 问题1：No Transport on Network Manager
**解决方案：**
- 在 NetworkManagerCustom 对象上添加 Transport 组件
- 或在代码中会自动添加 TelepathyTransport

### 问题2：场景切换后网络断开
**原因：** Mirror 默认在场景切换时保持连接，但需要正确配置
**解决方案：**
- 确保 NetworkManagerCustom 使用 `DontDestroyOnLoad`
- 确保场景切换时不要销毁 NetworkManager

### 问题3：无法连接
**检查项：**
1. 防火墙是否阻止连接
2. 设备是否在同一局域网
3. IP 地址是否正确
4. Transport 是否正确配置

## 测试步骤

### 本地测试（单机）
1. 运行游戏
2. 点击"创建房间"
3. 检查是否显示房间 IP
4. 检查玩家列表是否显示自己

### 局域网测试（多设备）
1. 设备A：点击"创建房间"，记录 IP 地址
2. 设备B：点击"加入房间"，输入设备A的 IP
3. 检查设备A是否显示设备B加入
4. 检查是否可以开始游戏

## 网络架构

```
主菜单（无网络）
    ↓
【房主】创建房间         【客户端】加入房间
    ↓                           ↓
CreateRoom 场景          JoinRoom 场景
    ↓                           ↓
StartHost() 启动服务器   输入房主 IP 地址
    ↓                           ↓
房主加载 CreateRoom      客户端加载 CreateRoom
(Server + Client)       (Client only)
    ↓                           ↓
    └─────────→ 同步房间状态 ←─────┘
                 (RoomState.roomPlayerIds)
    ↓
显示玩家列表
    ↓
开始游戏 → GameScene
    ↓
游戏结束
    ↓
返回主菜单（断开网络）
```

## 关键流程说明

### 场景分离设计的优点

**CreateRoom 和 JoinRoom 分离的原因**：
1. **职责分离**：JoinRoom 只负责输入 IP，CreateRoom 负责房间管理和玩家列表
2. **清晰的流程**：客户端加入后会自动加载 CreateRoom 场景，而不是混合在一个场景里
3. **代码简洁**：RoomUI 和 JoinRoomUI 各自独立，逻辑更清晰
4. **易于扩展**：可以在 JoinRoom 中添加扫描二维码、房间列表等功能而不影响 CreateRoom

### 房主流程（CreateRoom 场景）
1. MainMenu → 点击"创建房间"
2. 加载 CreateRoom 场景
3. 调用 `CreateRoomAfterSceneLoad()` 协程
4. 启动 `StartHost()`（服务器+客户端模式）
5. 等待 RoomUI 加载完成
6. 调用 `OnRoomCreated(ip)` 显示房间 IP 和二维码
7. 等待客户端加入
8. 每有一个客户端加入，`OnServerAddPlayer()` 被调用
9. 更新 `RoomState.roomPlayerIds` 和 RoomUI

### 客户端流程（JoinRoom → CreateRoom 场景）
1. MainMenu → 点击"加入房间"
2. 加载 JoinRoom 场景（输入房主 IP）
3. 输入房主 IP 并点击"连接"
4. 调用 `JoinRoomAfterSceneLoad(ip)` 协程
5. 启动 `StartClient()`（纯客户端模式）连接到房主
6. 等待客户端连接建立（检查 `NetworkClient.active == true`）
7. 自动加载 CreateRoom 场景
8. `OnClientConnect()` 被调用，启动 `UpdateRoomUIClientAfterConnect()` 协程
9. 等待玩家对象从服务器 spawn 到客户端
10. 更新 RoomUI 显示所有玩家列表

### 房间状态同步机制

**RoomState.roomPlayerIds（SyncList<uint>）**
- 存储房间内所有玩家的 netId
- 服务器在 `OnServerAddPlayer()` 时添加玩家 netId
- 服务器在 `OnServerDisconnect()` 时删除玩家 netId
- 客户端自动订阅 SyncList 变化，实时更新玩家列表

**PlayerData（SyncVar）**
- 每个玩家的详细信息（名字、头像等）
- 使用 [SyncVar] 自动同步到所有客户端
- 客户端通过 playerData.OnDataChanged 事件响应变化

## 商店刷新与存档说明

- **商店刷新**：本项目为局域网游戏，商店由本地客户端维护；当一局结束并返回主菜单时，本地商店会随机刷新（10 张小丑牌）。退出房间后没有持久化的服务器状态，因此商店不会由远端服务器持久保存或跨会话同步。
- **购买行为**：玩家在本局中购买小丑牌后，当前局不会补货；下次刷新在返回主菜单后执行。
- **本地存档（JSON）**：玩家数据保存在 `Application.persistentDataPath/players/<playerId>.json`，推荐字段：`playerName`, `avatarId`, `currentMoney`, `debt`, `jokersList`, `borrowedAmount`。


