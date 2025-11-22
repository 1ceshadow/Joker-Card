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
2. 添加场景：
   - MainMenu
   - GameRoom
   - GameScene（游戏内场景）

### 4. 网络模式说明

#### 游戏启动时
- **不启动网络**：游戏启动时不会自动连接网络
- 只有在点击"创建房间"或"加入房间"时才开始网络连接

#### 创建房间（房主）
- 点击"创建房间" → 切换到 GameRoom 场景
- 自动调用 `StartHost()` → 启动服务器并连接
- 该玩家成为服务器（Server + Client）

#### 加入房间（客户端）
- 点击"加入房间" → 切换到 GameRoom 场景
- 显示 IP 输入框
- 输入房主 IP → 调用 `StartClient()` → 连接到服务器

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
创建房间/加入房间
    ↓
GameRoom 场景（启动网络）
    ├─ 房主：Server + Client
    └─ 客户端：Client only
    ↓
开始游戏
    ↓
GameScene 场景（保持网络连接）
    ↓
游戏结束
    ↓
返回主菜单（断开网络）
```

