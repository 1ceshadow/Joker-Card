# 🎴 Joker-Card: 局域网小丑牌狂欢+炸金花！ 🃏

[![Unity](https://img.shields.io/badge/Unity-2022%2B-blue.svg)](https://unity.com/) [![Mirror](https://img.shields.io/badge/Mirror-Networking-brightgreen.svg)](https://mirror-networking.com/) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

在局域网内和朋友们来一场刺激的扑克对决！结合了经典**炸金花**的押注紧张感和**Balatro**的小丑牌魔力，这个游戏让你在出牌、弃牌、押注间游刃有余。谁将是分数王者？谁又会欠下一屁股债？快来组建你的小丑牌军团，洗牌开局吧！🚀

游戏介绍：
一款局域网内联机扑克游戏，主要玩法为多人联机炸金花+小丑牌升得分。

![游戏截图](https://via.placeholder.com/800x450?text=Gameplay+Screenshot)  
*(截图即将更新：手牌飞舞，小丑狡笑，底池金币堆积如山！)*

## 🌟 游戏亮点
- **多人局域网对战**：最多5人，最少2人开局。创建房间或扫描二维码快速加入，零延迟局域网狂欢！
- **扑克+小丑牌系统**：标准52张牌，9种牌型分数计算（从High Card到Straight Flush）。6种搞怪小丑牌加成，如"Jolly Joker"让对子变身+8 Mult炸弹！
- **押注与欠债机制**：起始资金20，押注不够？系统借你！但赢钱先还债，真实模拟"赌徒人生"😂。
- **商店与升级**：每局结束后刷新10张小丑牌，买买买！最多持有5张，售卖回血。
- **生动UI与交互**：点击牌向上弹起，中心炫耀出牌分数，其他玩家头像逆时针环绕，沉浸感满分！
- **跨平台**：Android/iOS/Windows，支持二维码扫码加入，局域网内随时开黑。

分数计算示例：一对A + Greedy Joker（方片加成）= 可能的分数翻倍！详细牌型表见[GDD.md](docs/GDD.md)。

## 🚀 快速上手
unity版本：6000.2.9f1 或更高

1. **克隆仓库**：  
    ```bash
    git clone https://github.com/1ceshadow/Joker-Card.git
    ```
2. **打开Unity项目**：使用Unity 2022或更高版本打开项目文件夹。
3. **安装依赖**：确保已安装Mirror Networking包（通过Unity Package Manager）。
4. **添加资源**：放置扑克牌/头像/小丑图片到`Assets/Resources/`（详见[SCRIPTS_GUIDE.md](docs/SCRIPTS_GUIDE.md)）。
5. **运行测试**：在MainMenu场景启动，创建房间，手机/另一台电脑加入IP。扫描二维码？导入ZXing库就行！
6. **打包**：Android/iOS设置见[NETWORK_SETUP.md](docs/NETWORK_SETUP.md)。防火墙别挡着哦~

**测试清单**：见[TESTING_CHECKLIST.md](docs/TESTING_CHECKLIST.md) – 从资源加载到欠债系统，一步步验证你的扑克帝国！

## 🛠️ 技术栈
- **Unity Engine**：核心游戏框架。
- **Mirror Networking**：局域网多人同步，手牌保密，状态实时更新。
- **ZXing**：二维码生成/扫描，扫一扫加入房间超方便。
- **JSON Utility**：玩家数据保存（资金、小丑牌），持久化你的"债务史"。
- **uGUI & TextMeshPro**：丝滑UI交互，牌型炫耀动画。

完整脚本清单见[SCRIPTS_LIST.md](docs/SCRIPTS_LIST.md) – 21个脚本，覆盖网络、逻辑、UI全链路！

## 📊 游戏参数速览
- 手牌：8张 | 出牌上限：5张 | 弃牌次数：3
- 起始底金：5 | 小丑牌上限：5 | 商店刷新：10张
- 分数公式：Chips × Mult + 小丑加成（详见[QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)）

## 🤝 贡献指南
欢迎PR！想加新小丑牌？优化分数算法？或修复欠债Bug？
1. Fork仓库。
2. 创建分支：`git checkout -b feature/new-joker`。
3. 提交：参考[SETUP_GUIDE.md](docs/SETUP_GUIDE.md)配置Prefab/UI。
4. PR描述：解释你的魔改，让扑克世界更疯狂！

问题？在Issues吐槽，或查[NETWORK_SETUP.md](docs/NETWORK_SETUP.md)排查连接问题。

## ⚠️ 注意事项
- **仅局域网**：不适合广域网。
- **测试环境**：确保同一WiFi，防火墙允许端口。
- **债务提醒**：游戏中欠债别当真，现实中理性娱乐！😉

## 📜 许可证
MIT License – 随意修改、分发，但请保留原作者信息。

*由扑克爱好者倾情打造。如果你赢了，别忘请朋友喝咖啡！☕*  

## TODO List
- [ ] 在选头像名字的时候，不能点加入房间/别的