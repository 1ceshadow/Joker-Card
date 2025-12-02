# 网络配置指南

## Transport 配置

NetworkManagerCustom 需要 Transport 组件：
- **Telepathy Transport**（推荐）- 简单可靠
- **KCP Transport** - 性能更好

代码会自动添加 TelepathyTransport，也可手动添加。

## 场景配置

Build Settings 顺序：
1. MainMenu - 主菜单（无网络）
2. CreateRoom - 房间场景（房主+客户端）
3. JoinRoom - 输入IP（仅客户端）
4. GameScene - 游戏场景

## 网络架构

```
MainMenu（无网络）
    ↓
┌─────────────┬──────────────┐
│   房主       │   客户端     │
│ CreateRoom  │  JoinRoom    │
│ StartHost() │  输入IP      │
│             │ StartClient()│
│             │     ↓        │
│             │ CreateRoom   │
└─────┬───────┴──────┬───────┘
      │              │
      └──── 同步 ────┘
           ↓
       GameScene
```

## 同步机制

- `[SyncVar]` - 自动同步变量
- `[Command]` - 客户端→服务器（Cmd前缀）
- `[ClientRpc]` - 服务器→所有客户端（Rpc前缀）
- `[Server]` - 仅服务器执行

## 常见问题

### 无法连接
1. 检查防火墙
2. 确认同一局域网
3. 验证IP地址正确
4. Transport已配置

### 场景切换断开
- 确保 NetworkManagerCustom 使用 `DontDestroyOnLoad`
- 切换时不要销毁 NetworkManager

## Player Prefab 配置

必须包含：
- `NetworkIdentity` 组件
- `PlayerData` 组件

在 NetworkManagerCustom 的 Player Prefab 字段中设置。
