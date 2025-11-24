# 商店 UI 配置指南

## 问题症状
- ✅ 商店小丑牌正确创建（日志显示"共 10 个"）
- ✅ 每个卡牌都有 Image 和文本组件
- ❌ 10 个卡牌重叠在一起（无法看清）
- ❌ 显示为灰色圆形（没有图片）

## 根本原因

**UI 布局配置不正确**：
1. GridLayoutGroup 的 cellSize（单元格大小）没有设置，导致默认为 (0,0)
2. shopJokersParent 的 RectTransform 没有正确的大小设置
3. 缺少 ContentSizeFitter 让容器自动调整大小
4. 卡牌 Prefab 可能没有正确的大小和布局设置

## ✅ 解决方案（已自动修复）

### 代码修改

ShopUI.cs 中已添加以下改进：

```csharp
// 1. 设置 GridLayoutGroup 的单元格大小和间距
gridLayout.cellSize = new Vector2(150, 150); // 每个卡牌 150x150
gridLayout.spacing = new Vector2(10, 10);   // 间距 10px

// 2. 正确设置 shopJokersParent 的 RectTransform
rectTransform.anchorMin = new Vector2(0, 1);
rectTransform.anchorMax = new Vector2(1, 1); // 横向拉伸
rectTransform.sizeDelta = new Vector2(0, 350); // 高度固定

// 3. 添加 ContentSizeFitter 让高度自动调整
sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

// 4. 设置每个卡牌的大小
jokerRect.sizeDelta = new Vector2(140, 140);

// 5. 强制刷新布局
Canvas.ForceUpdateCanvases();
LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
```

### 需要在 Unity 中手动检查的地方

#### 1. **shopJokersParent（Container）的配置**

在 Hierarchy 中找到 shopJokersParent（MainMenu Canvas 下的商店容器），确保：

```
shopJokersParent (RectTransform)
├─ Anchor Preset: Top Stretch（顶部拉伸）
├─ Width: 设置为具体值（如 800）
├─ Height: 350 或更大
├─ Pos X: 0, Pos Y: 0
└─ Component: GridLayoutGroup ✅
   ├─ Constraint: Fixed Column Count
   ├─ Constraint Count: 5
   ├─ Cell Size: (150, 150)
   ├─ Spacing: (10, 10)
   └─ Child Alignment: Upper Left
```

#### 2. **jokerItemPrefab（卡牌）的配置**

打开 Assets/Prefabs/JokerItem.prefab，检查结构：

```
JokerItem (Button)
├─ RectTransform
│  ├─ Width: 140, Height: 140 ✅
│  └─ Layout Element（可选，用于固定大小）
│
├─ Image（卡牌背景）
│  ├─ Color: White
│  └─ Sprite: 某个卡牌图片
│
├─ jokerImage (Image)
│  ├─ 用于显示小丑牌图片
│  └─ Sprite: 从 AvatarSpriteLoader 动态设置
│
├─ nameText (TextMeshProUGUI)
│  └─ 显示卡牌名称
│
├─ descriptionText (TextMeshProUGUI)
│  └─ 显示卡牌描述
│
├─ priceText (TextMeshProUGUI)
│  └─ 显示价格
│
├─ actionButton (Button)
│  └─ 显示"购买"
│
└─ sellButton (Button)
   └─ 显示"售卖"
```

#### 3. **关键 Prefab 设置检查清单**

- [ ] JokerItem 有 Button 组件
- [ ] Button 下至少有一个 Image 子对象（作为背景）
- [ ] RectTransform 的 Width 和 Height 都设置为正数（至少 100）
- [ ] jokerImage 存在且有 Image 组件
- [ ] nameText, descriptionText, priceText 都存在
- [ ] actionButton 存在（购买按钮）
- [ ] Layout Element 组件（可选，但推荐）：
  - Preferred Width: 140
  - Preferred Height: 140

## 快速测试步骤

1. **重启 Unity**
   - 关闭游戏（Ctrl+Shift+Esc 或点击 Stop）
   - 重新点击 Play

2. **查看 Hierarchy**
   - 展开 shopJokersParent
   - 应该看到 10 个 JokerItem，排成 2 行 5 列
   - 每个都不应该重叠

3. **查看 Scene View**
   - 点击其中一个 JokerItem
   - 应该看到清晰的卡牌界面（不是灰色圆形）
   - 应该能看到小丑牌的图片

4. **查看 Game View**
   - 应该能在主菜单右侧看到完整的商店面板
   - 10 个卡牌整齐排列，有清晰的分割

## 如果还是不显示

### 检查点 1: Image 图片是否加载成功
```csharp
// 在 JokerItemUI.cs 中应该有：
Sprite jokerSprite = JokerSpriteLoader.GetJokerSprite(jokerData.type);
if (jokerSprite != null)
    jokerImage.sprite = jokerSprite;
else
    Debug.LogWarning($"小丑牌图片未找到: {jokerData.type}");
```

### 检查点 2: 检查控制台错误
运行时查看 Console，如果有 `NullReferenceException`，表示：
- Prefab 中缺少某个组件
- shopJokersParent 未分配
- jokerItemPrefab 未分配

### 检查点 3: 调整单元格大小
如果卡牌太大或太小，在代码中修改：
```csharp
gridLayout.cellSize = new Vector2(150, 150); // 改这个
gridLayout.spacing = new Vector2(10, 10);   // 或改这个
jokerRect.sizeDelta = new Vector2(140, 140); // 或改这个
```

## 参考值

| 参数 | 推荐值 | 说明 |
|------|--------|------|
| cellSize.x | 150 | 每个卡牌的宽度 |
| cellSize.y | 150 | 每个卡牌的高度 |
| spacing.x | 10 | 卡牌之间的水平间距 |
| spacing.y | 10 | 卡牌之间的垂直间距 |
| Container Width | 800 | 容器宽度（5 列 × 150 + 4 × 10 = 790） |
| Container Height | 350 | 容器高度（2 行 × 150 + 1 × 10 + 40 留白 = 350） |
| jokerRect.sizeDelta | 140 | 卡牌内容的实际大小（小于 cellSize 以避免重叠） |

## 常见问题

### Q: 为什么卡牌显示为灰色圆形？
A: 这是 Button 的默认样式。确保 Prefab 中有正确的 Image 组件和图片。

### Q: 为什么卡牌重叠在一起？
A: GridLayoutGroup 的 cellSize 没有正确设置。代码已修复，cellSize 现在是 (150, 150)。

### Q: 为什么看不到小丑牌的图片？
A: 检查 JokerSpriteLoader 是否正确加载了图片。在 Console 中查找 "小丑牌图片未找到" 的警告。

### Q: 重新运行后还是重叠？
A: Unity 可能缓存了旧的布局。试试：
1. 删除 Library/ScriptAssemblies 文件夹
2. 重新打开项目
3. 或者重启 Unity
