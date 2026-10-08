# 弹幕尖塔（DanmakuSpire v1.2.1）58 条规则：触发时机与触发内容逆向整理

> 用途：为国运尖塔（NationalSpire）弹幕层整合提供逐条依据。
> 所有结论均来自对发布包的静态分析，未运行、未注入该 DLL。

## 0. 证据来源（可复核）

| 证据 | 位置 | 内容 |
| --- | --- | --- |
| 规则 ID ↔ 触发枚举 | `DanmakuSpire.dll` → `DanmakuSpireCode.Core.DanmakuRuleIds.Get(DanmakuTrigger)` | 58 条映射，另含框架元触发 `CombatStart => "META-COMBAT-START"`（不计入 58） |
| 中文名 + 一句话说明 | `DanmakuSpireCode.Content.RuleDisplayCatalog.AllEntries` | 58 条 `RuleDisplayEntry(id, name, hint, CanSubmit)` |
| 触发时机 + 判定条件 + 钩子点 | `DanmakuSpireCode.Data.DanmakuRuleCatalog.All` | 每条 `Spec(trigger, TimingPhase, condition, hook)`，含具体阈值与补丁目标 |
| 词条与风格 | `content/danmaku/rules.json` | 58 组 / 单人 336 条 / 多人 58 条；`style ∈ {narration, character}` |
| 版本线索 | `CHANGELOG.md` | 「判定规则从 56 条扩展到 58 条，新增『卡组过小』判定与『领主阳伞』判定」，与 E14/F3 对应 |

事件族键名以国运尖塔现有词库为准：`situation/lib-a.json`～`lib-d.json` 实际含 22 族
（a: cheap danger death draw elite hurt；b: boss clean quick shop slack upgrade；c: gap mock opening praise rival target；d: achieve draw_repeat overkill potion_waste skip_reward slack）。
任务给出的 18 族里 `rest` 在词库中不存在，对应键名为 `slack`，下表统一写作 `slack`。

## 1. 58 条规则对照表

置信度判定规则：**高** = 同时存在权威判定条件文本与具体钩子点，可直接实现；**中** = 条件明确但依赖国运层尚不存在的钩子或需改写语义；**低** = 依赖原版专属实体/招式，国运层无对应触发时机。

> **关于「低置信度」的重要说明**：本次从 DLL 中直接取到了作者自带的完整规则表（`DanmakuRuleIds` + `RuleDisplayCatalog` + `DanmakuRuleCatalog`），
> 因此**没有任何一条规则的触发条件是靠词条内容反推的**——即不存在「依据不足，仅从词条推测」的行。
> 表中「低」标注的不是证据强度，而是**可整合性**：这 4 条触发条件同样有 DLL 原文证据，但依赖原版专属实体/招式，国运尖塔无对应触发时机。
> 若需要真正的「仅从词条推测」对照，只能针对 `CHANGELOG` 未覆盖的自定义投稿时机（`content/danmaku/rules-contrib.json`），不在本次 58 条范围内。

| ruleId | 中文名 | 触发时机（TimingPhase） | 判定条件 / 钩子点 | 置信度 | 依据 |
| --- | --- | --- | --- | --- | --- |
| **H1** | SL 操作 | Run | 检测到存档被加载（玩家进行了 SL）<br>`SaveManager 读取存档 / RunManager run 恢复事件；用模组内标志区分'加载'与'新开局'` | 高 | `cheap` / `mock` |
| **A1** | 强敌登场 | CombatStart | 战斗开始时检查怪物 ModelId：A类(前两层遇异蛙寄生虫/花园幽灵鳗/外骨骼虫/残杀千足虫且牌库无AOE)；B类(遇多尼斯异鸟/活雾/鬼祟珊瑚群/感染棱柱即触发)<br>`CombatManager.CombatBegan；遍历 state.Enemies 取 ModelId 比对强怪集合 + 查牌库 AOE 牌` | 高 | `opening` / `danger` |
| **A5** | 残血进精英 | CombatStart | 战斗开始时房间为精英房且玩家 HP 比例 < 30%<br>`CombatStartedFact 冻结房间类型与玩家 HP；检查 PointType==Elite 和 Hp/MaxHp` | 高 | `elite` / `danger` |
| **A6** | 地图连续选择精英 | CombatStart | 每个 Act 独立计算；排除 SL 后重复进入同一地图节点，进入第三个不同精英房时触发<br>`CombatStartedFact 冻结 Act 与地图节点 StableKey；RunContext 按 Act 对 Elite 节点去重计数` | 高 | `elite` / `mock` |
| **A3** | 旧敌重逢 | CombatStart | 战斗开始时检查敌怪 ModelId 是否在本 run 已遇过的集合中<br>`CombatBegan；持久化 HashSet<ModelId> 记录已遇怪种，命中则触发` | 高 | `opening` / `mock` |
| **B1** | 防御不足 | Combat | 仅 Act1；玩家回合抽牌及自动预打完全结束后：任意敌怪有攻击意图，且手牌无防御牌(GainsBlock)、无过牌牌<br>`Hook AfterAutoPrePlayPhaseEntered 原 Task 完成后；复查 AutoPrePlay 生命周期，再查 Act、敌怪 Intent、手牌 GainsBlock/过牌分类` | 高 | `cheap` / `danger` |
| **B2** | 输出不足 / 卡手 | Combat | 仅 Act1；玩家回合抽牌及自动预打完全结束后：敌怪均无攻击意图（没有防御压力），且手牌无攻击牌、无过牌牌<br>`Hook AfterAutoPrePlayPhaseEntered 原 Task 完成后；复查 AutoPrePlay 生命周期，再查 Act、敌怪 Intent、手牌攻击/过牌分类` | 高 | `cheap` / `draw_repeat` |
| **C1** | 裸接重击 | Combat | 本次玩家受到 ≥5 点 HP 伤害，且该次伤害没有消耗任何格挡<br>`CombatHistory.DamageReceived；限定 LocalContext.IsMe(receiver)，查 DamageResult.BlockedDamage==0 与 UnblockedDamage` | 高 | `hurt` |
| **C4** | 一回合击杀（OTK） | Combat | 战斗的第一回合内击杀敌怪（首个玩家回合内击杀即触发）<br>`CombatBegan 标记首回合；TurnStarted 首个玩家回合；怪物死亡时若仍处首回合则触发` | 高 | `quick` |
| **B3** | 有费有防不出 | Combat | 结束回合时实际弃掉至少一张当时可打出的防御牌（原版 CanPlay，含资源和禁用限制），随后受到敌怪HP伤害<br>`AboutToSwitchToEnemyTurn 用 CardModel.CanPlay 过滤；系统 flush 后确认弃牌及最低费用；DamageReceived 判定` | 高 | `cheap` / `hurt` |
| **B4** | 战斗启动 | Combat | 打出'启动牌'集合中的卡牌；{x}=本场累计打出所有启动牌的总次数（所有启动牌共同累加）<br>`PlayCardAction.ExecuteAction Postfix；卡牌 ModelId 比对启动牌集合；维护本场启动牌打出总计数(所有启动牌累加)，{x}=该计数` | 中 | `quick` / `praise` |
| **B5** | 卡动画 | Combat | 有卡牌在等待区(NCardPlayQueue)停留 ≥3 秒，或本次打出的攻击牌 CardPlay.PlayCount ≥5<br>`_Process 检查 NCardPlayQueue.Instance 队列非空持续≥3秒(空则归零)；或 CombatHistory.CardPlayStarted 查 cardPlay.PlayCount≥5 且卡牌为攻击牌` | 中 | `draw` / `mock` |
| **D1** | 对策完美匹配 | Combat | 打出牌时(任一)：A.用AOE牌且场上敌怪数目≥3；B.用多段牌且目标为外骨骼虫(EXOSKELETON)<br>`PlayCardAction Postfix；A查卡牌TargetType.AllEnemies+state.Enemies.Count>=3；B查卡牌BaseReplayCount>=3+目标ModelId==EXOSKELETON` | 中 | `overkill` / `quick` |
| **C5** | 单卡高伤害 | Combat | 单张卡单次伤害结算值（含致死溢出伤害）≥40<br>`CombatHistory.DamageReceived Postfix；检查 TotalDamage + OverkillDamage` | 高 | `overkill` |
| **B6** | 假装思考十二秒 | Combat | 本地玩家处于可打牌阶段，连续 ≥12 秒没有打牌或使用药水<br>`Combat 内 _Process 计时；打牌/药水/阶段中断/离开战斗重置计时器` | 高 | `mock` / `slack` |
| **D2** | 误弑灯 | Combat | 女王战中真实击杀火炬头<br>`CombatBegan 检查 Queen；死亡确认判断火炬头` | 低 | `boss` / `death` |
| **D3** | 无视灯 | Combat | 女王战中火炬头仍存活时，本地玩家用单体攻击牌对女王造成正伤害<br>`CombatBegan 检查 Queen；DamageReceived 检查存活火炬头、Attack 与 TargetType.AnyEnemy 后判定女王伤害` | 低 | `boss` / `danger` |
| **C3** | 盛碗虫（石）极限 | Combat | 盛碗虫（石）攻击消耗玩家格挡但未完全挡住，并恰好造成1点HP伤害<br>`仅 CombatBegan 发现 BOWLBUG_ROCK 时启用快速标志；DamageReceived 中只对该攻击者即时判定` | 中 | `hurt` / `overkill` |
| **B7** | 战中豪饮 | Combat | 本战斗药水使用次数 ≥2<br>`Hook AfterPotionUsed；本战斗药水计数，战斗开始重置` | 高 | `potion_waste` |
| **B8** | 过牌溢出 | Combat | 抽牌后手牌数量 ≥10（含溢出）<br>`Hook AfterCardDrawn；查手牌数量` | 高 | `draw` / `overkill` |
| **A4** | 旧敌再审 | Combat | 击杀敌怪时，该怪种 ModelId 曾出现在本 Run 此前已胜利的战斗中<br>`CombatBegan 冻结此前胜利历史；CombatEnded(won) 后才整体写入本场怪种` | 中 | `praise` / `mock` |
| **C6** | 爽局掉血 | Combat | 连续 ≥4 场未因敌怪攻击掉血后，本场首次被敌怪打掉 HP<br>`DamageResolvedFact 处理破功；CombatBegan 根据既有连续无伤计数冻结爽局标记` | 高 | `hurt` / `mock` |
| **C9** | 连续无伤战斗 | Combat | 战斗胜利且本场未因敌怪攻击掉血时计数+1；达到5场时触发，之后每增加2场再触发<br>`CombatEndedFact 冻结 won/DamagedByEnemy；RunContext 维护连续无伤计数并按5、7、9…里程碑判定` | 高 | `clean` |
| **C7** | 实验体刮痧 | Combat | 实验体处于无实体时累计收到 ≥5 次正数伤害结算；多段、Replay、额外打出分别计次<br>`CombatHistory.DamageReceived；识别实验体 ModelId + Intangible power + 每次 TotalDamage>0 计数` | 中 | `cheap` / `draw` |
| **A2** | 强敌倒下 | Combat | 击杀本场已触发'强敌登场'且被识别为强怪的对应敌怪<br>`CombatBegan 冻结本场强怪 ModelId 集合；死亡确认只匹配集合成员；离开战斗清空` | 高 | `praise` / `clean` |
| **D4** | 雕刻师吟唱前击杀 | Combat | 虔诚雕刻师(DEVOTED_SCULPTOR)尚未使用禁忌唱颂(FORBIDDEN_INCANTATION)招式时被击杀<br>`CombatBegan 识别 DEVOTED_SCULPTOR+记录其招式；怪物死亡时判断未使用 FORBIDDEN_INCANTATION_MOVE` | 低 | `boss` / `quick` |
| **D5** | 听到雕刻师吟唱 | Combat | 虔诚雕刻师(DEVOTED_SCULPTOR)使用了禁忌唱颂(FORBIDDEN_INCANTATION)招式<br>`怪物选招/使用招式事件；识别 DEVOTED_SCULPTOR 使用 FORBIDDEN_INCANTATION_MOVE` | 低 | `boss` / `opening` |
| **D6** | 咬人卷轴致损血上限 | Combat | 咬人卷轴战中本地玩家失去最大生命值；每场一次<br>`CombatBegan 识别 SCROLL_OF_BITING；PaperCutsPower.AfterDamageGiven 冻结该卷轴对本地玩家造成正伤害前后的 MaxHp` | 中 | `hurt` / `death` |
| **D7** | 咬人卷轴无损血上限 | Combat | 咬人卷轴战胜利结束且本地玩家整场未失去最大生命值<br>`CombatBegan 冻结咬人卷轴在场标记；CombatEnded(won) 检查 MaxHp 损失标记` | 中 | `clean` / `praise` |
| **C8** | 角色死亡 | Combat | 玩家 Creature 死亡<br>`Hook ShouldDie/BeforeDeath；玩家 Creature 死亡事件` | 高 | `death` |
| **C2** | 数值极限 | TurnEnd | 精确格挡：敌方真实攻击、玩家损失0 HP、至少消耗1格挡且最终格挡为0；精确击杀：玩家伤害使敌怪真实死亡且无溢出<br>`敌方回合冻结/完成攻击链后统一判定精确格挡；DamageReceived冻结无溢出候选并由真实死亡确认` | 高 | `overkill` / `clean` |
| **E1** | 一层抓未来牌 | Reward | Act1 选牌奖励中获得'未来牌'集合中的牌<br>`AfterRewardTaken/ModifyCardRewardOptions；查当前 Act + 卡牌未来牌集合` | 中 | `upgrade` / `draw` |
| **E2** | 重复抓牌 | Reward | 获得牌时牌库已有同名牌；{card}=卡牌名<br>`AfterRewardTaken；比对牌库与所选牌` | 高 | `draw_repeat` |
| **E3** | 获得关键牌 | Reward | 获得牌时该牌在当前角色'关键牌'集合中<br>`AfterRewardTaken；卡牌 ModelId 比对角色关键牌集合` | 高 | `upgrade` / `praise` |
| **E4** | 获得超模牌 | Reward | 获得牌或遗物时该物品在'超模牌/超模遗物'集合中<br>`AfterRewardTaken 卡牌 ModelId 比对超模牌集合；CollectibleObtainedFact 遗物 ModelId 比对超模遗物集合` | 高 | `upgrade` / `overkill` |
| **E5** | 攻防一体牌 | Reward | 获得牌时该牌同时在攻击牌集合与防御牌集合中<br>`AfterRewardTaken；查卡牌 CardType.Attack + GainsBlock` | 中 | `upgrade` |
| **E6** | 错过关键牌 | Reward+Shop | 选牌奖励界面关闭时：候选含关键牌或超模牌且未选取 ｜ 商店分支：离开商店时剩余金币 >= 关键牌或超模牌价格，且未购买之（B 分支）<br>`奖励界面关闭回调；记录候选关键牌/超模牌 + 是否选取 ｜ 商店分支：NMerchantRoom 离开回调；查剩余金币 >= 关键牌/超模牌价格 且未购` | 高 | `cheap` / `mock` |
| **E7** | 不选择未来牌 | Reward | Act1 选牌时候选只有一张'未来牌'且未选取<br>`奖励界面关闭回调；查 Act + 候选未来牌数 + 是否选取` | 高 | `skip_reward` / `cheap` |
| **E8** | 全面神化 | Reward | 获得牌后牌组所有牌均已升级，或获得'神化'牌；火堆成功升级后牌组全部升级同样触发<br>`RewardClosedFact 遍历最终牌组升级状态 / 查获得牌 ModelId；RestSiteOptionCompletedFact 升级差异后复评` | 高 | `upgrade` / `achieve` |
| **E9** | 跳过抓牌 | Reward | 跳过选牌奖励（点击跳过按钮）按动态概率触发：初始 20%，每次跳过后未触发概率 +10%，触发后回落 20%<br>`奖励界面跳过回调；RunContext 保存概率，Random.Shared 抽签` | 高 | `skip_reward` |
| **E10** | 难以抉择 | Reward | 选牌奖励生成时候选含 ≥2 张超模牌/关键牌<br>`ModifyCardRewardOptions；查候选超模/关键牌数量` | 中 | `praise` / `target` |
| **E11** | 大卡组成长 | Reward | Run级倒计数初始0；每净新增1张牌减1；添加后牌组>40且计数≤0时触发一次并重置为5<br>`RewardClosed 读取 AddedCards 和最终牌组；其他加牌来源接入统一 DeckChangedFact 后复用同一状态转移` | 高 | `draw` / `mock` |
| **E12** | 某牌三顾茅庐 | Reward | 某牌在选牌/商店累计出现达到稀有度阈值：白(Common)5 次、蓝(Uncommon)4 次、金(Rare)3 次；已在卡组中的牌不计入；{card}=卡牌名<br>`ModifyCardRewardOptions/商店生成；持久化 Dictionary<ModelId,int> 计数，升级前后同名卡按 ModelId 归并` | 高 | `draw_repeat` |
| **E13** | 强力组合 | Run | 强力组合按预设 A→B 单向登记；观察到 A 时提示 B，第20个后续节点结束仍未获得 B 则超时，之后集齐仍触发完成<br>`本地 Deck.CardAdded/Player.RelicObtained → CollectibleObtainedFact；CardCmd.Enchant → CardEnchantmentChangedFact；RoomLeavingFact 推进第20节点结束边界；RunContext保存每个方向的独立状态` | 中 | `achieve` / `praise` |
| **E14** | 卡组过小 | Run | 牌组不超过 10 张时，每减少 1 张触发一次<br>`DeckChangedFact(removed) 后读取 DeckCardIds；牌组≤10 时每个移除事实独立触发` | 高 | `draw` / `mock` |
| **F1** | 购买商店高价值商品 | Shop | 购买价格并列为所有卡牌最高的卡牌，或所有遗物最高的遗物，或特殊 RelicRarity.Shop 槽遗物<br>`ShopOpened 冻结本地库存/价格/特殊槽；ShopPurchased 后按卡牌和遗物分别比较` | 高 | `shop` |
| **F2** | 商店删牌 | Shop | 在商店执行删牌服务<br>`NMerchantRoom 删牌服务回调` | 高 | `shop` / `upgrade` |
| **F3** | 领主阳伞 | Shop | 进入商店页面（见到物品）即触发：对每个在售物品（含删牌服务）各发射一条置顶弹幕，固定按物品数、不受词条密度与强度影响<br>`ShopOpenedFact 遍历 Items；每物品一条置顶弹幕，文本从词条组随机取一条并以{item}替换物品名` | 高 | `shop` / `target` |
| **G1** | 建筑师带饮料 | Event | 进入建筑师事件且药水栏非空<br>`NEventRoom SetupLayout 捕获事件 ModelId；阶段未切换时挂起、RoomEntered 切到 Event 后补发；查药水栏` | 中 | `opening` / `potion_waste` |
| **G2** | 桥事件 | Event | 事件'桥'中失去卡牌（按失去弱牌/强牌分子条件）<br>`桥事件选项回调；查失去卡牌分类` | 中 | `upgrade` / `mock` |
| **G3** | 克隆附魔 | RestSite | 第一次获得克隆附魔时，及每次在火堆进行克隆时<br>`patch CardCmd.Enchant<Clone> 首次获得(或 PaelsGrowth.AfterObtained)；patch CloneRestSiteOption.OnSelect 火堆克隆` | 中 | `upgrade` / `achieve` |
| **G4** | 火堆睡觉 | RestSite | 本地玩家在火堆成功选择睡觉(HEAL)<br>`RestSiteSynchronizer.ChooseLocalOption 异步完成后发布 RestSiteOptionCompletedFact；比较 OptionId==HEAL` | 高 | `slack` / `rest` |
| **G5** | 满血火堆睡觉 | RestSite | 选择前 HP 等于 MaxHp 且在火堆成功选择睡觉；可与普通睡觉同时触发<br>`RestSiteSynchronizer.ChooseLocalOption Prefix 冻结玩家快照，异步完成后检查选择前 HP/MaxHp` | 高 | `slack` / `mock` |
| **G6** | 残血火堆不睡 | RestSite | 进入火堆时 HP<MaxHp 的30%，之后成功选择非睡觉选项；持有 MINIATURE_TENT 时豁免<br>`RestSiteOpenedFact 冻结进入火堆快照；选择完成后检查 OptionId!=HEAL、进入时 HP 比例和遗物集合` | 高 | `slack` / `danger` |
| **G7** | 连续升级卡牌 | RestSite | 不持有迷你帐篷时，连续3个火堆节点均成功升级卡牌；其他火堆选项会中断计数，归零后可再次触发<br>`RestSiteOptionCompletedFact 的 OptionId 与 RuleDiff.Upgrades 确认成功升级；RunContext 按节点维护连续计数` | 高 | `upgrade` |
| **I1** | 多人选牌 | Reward | 多人会话中本地玩家在奖励界面选中一张卡牌；词条只服务多人模式，固定抽取1条并转发给队友<br>`复用 RewardSelectedFact（NonCombatFactPatches+RewardSelectedPatch 已生产，不新增补丁）；requireLocalPlayer 过滤` | 中 | `draw` / `praise` |
| **I2** | 多人商店购买 | Shop | 多人会话中本地玩家在商店成功购买物品；词条只服务多人模式，固定抽取1条并转发给队友<br>`复用 ShopPurchasedFact（NonCombatFactPatches+ShopPurchasedPatch 已生产，不新增补丁）；requireLocalPlayer 过滤` | 中 | `shop` |
| **I3** | 多人火堆选择 | RestSite | 多人会话中本地玩家在火堆成功完成一次选项；词条只服务多人模式，固定抽取1条并转发给队友<br>`复用 RestSiteOptionCompletedFact（NonCombatFactPatches+RestSiteSelectedPatch 已生产，不新增补丁）；requireLocalPlayer 过滤` | 中 | `slack` / `rest` |

> 表中 `TimingPhase` 取值来自 DLL 枚举：`CombatStart / Combat / TurnEnd / Reward / Shop / Event / RestSite / Run`。
> `E6 错过关键牌` 在 DLL 中出现两次 Spec（`Reward` 与 `Shop` 两个时机），上表已合并展示。

## 2. 可整合映射建议（归入 18 个事件族）

| ruleId | 建议事件族 | 整合说明 |
| --- | --- | --- |
| H1 | `cheap` / `mock` | 读取存档（SL）＝cheap/嘲讽语感最强的一类 |
| A1 | `opening` / `danger` | 强敌登场＝开局面板/首次遭遇，opening + danger 双写；国运层需在 LiveRunBridge 的 CombatBegan 冻结敌怪 ModelId 集合 |
| A5 | `elite` / `danger` | 残血进精英＝elite 族原生场景，国运层已有 elite 话题源 |
| A6 | `elite` / `mock` | 同幕第 3 个不同精英房＝elite 连续计数，需按 Act 去重 |
| A3 | `opening` / `mock` | 旧敌重逢＝opening 的“又来了”语感，可复用生涯 ExposedPeople 已遇表 |
| B1 | `cheap` / `danger` | 第一幕手牌防御/过牌双缺＝cheap“菜”语感最贴；危险压力取 danger |
| B2 | `cheap` / `draw_repeat` | 第一幕无攻击牌卡手＝cheap + draw_repeat |
| C1 | `hurt` | 无格挡承受 ≥5 点＝hurt 族原生场景 |
| C4 | `quick` | 首个玩家回合结束战斗＝quick 族原生场景 |
| B3 | `cheap` / `hurt` | 弃掉可打防御牌后掉血＝典型的“有费有防不出”嘲讽，cheap 为主、hurt 记录伤害 |
| B4 | `quick` / `praise` | 打出启动牌＝启动回合，quick 语感 |
| B5 | `draw` / `mock` | 卡动画/多段连续结算＝节奏类，draw + mock；国运层无直接对应，需新钩子 |
| D1 | `overkill` / `quick` | 群攻命中 3 敌／多段打外骨骼虫＝overkill + quick，属对策匹配 |
| C5 | `overkill` | 单卡单次 ≥40 伤害＝overkill 族原生场景 |
| B6 | `mock` / `slack` | 12 秒不打牌＝slack（摸鱼）族原生场景 |
| D2 | `boss` / `death` | 女王战击杀火炬头＝boss 专属敌人机制，国运层无“火炬头”实体 |
| D3 | `boss` / `danger` | 火炬头存活时单体打女王＝同上，boss 专属 |
| C3 | `hurt` / `overkill` | 恰好损失 1 点生命＝hurt + overkill 边缘值 |
| B7 | `potion_waste` | 一场战斗 ≥2 瓶药水＝potion_waste 族原生场景 |
| B8 | `draw` / `overkill` | 抽牌后手牌 ≥10＝draw 溢满，可写 overkill |
| A4 | `praise` / `mock` | 旧敌再审＝战斗结束二次击败，与 A2 区别在结算时机 |
| C6 | `hurt` / `mock` | 爽局掉血＝hurt + 嘲讽破功，需维护连续无伤计数 |
| C9 | `clean` | 连续无伤胜利 5/7/9 场＝clean 族原生场景（里程碑计数） |
| C7 | `cheap` / `draw` | 实验体无实体刮痧＝cheap 语感；依赖原版实验体机制，国运层可换任意无实体敌人 |
| A2 | `praise` / `clean` | 击杀强敌＝达成成就语感，建议走 praise；若同时无伤则 clean 优先 |
| D4 | `boss` / `quick` | 雕刻师吟唱前击杀＝boss 招式的抢杀窗口，需原版招式事件 |
| D5 | `boss` / `opening` | 雕刻师使用禁忌唱颂＝boss 招式播报，依赖原版选招事件 |
| D6 | `hurt` / `death` | 咬人卷轴损失最大生命＝hurt 极端分支 |
| D7 | `clean` / `praise` | 胜咬人卷轴且未失上限＝clean 完胜 |
| C8 | `death` | 玩家角色死亡＝death 族原生场景 |
| C2 | `overkill` / `clean` | 精确格挡/无溢出精准击杀＝overkill 族原生场景 |
| E1 | `upgrade` / `draw` | 一层抓未来牌＝upgrade 族（获得成长），可归 draw |
| E2 | `draw_repeat` | 重复抓牌＝draw_repeat 族原生场景 |
| E3 | `upgrade` / `praise` | 获得关键牌＝upgrade 族原生场景（题材对应「获得」） |
| E4 | `upgrade` / `overkill` | 获得超模牌/遗物＝upgrade + overkill |
| E5 | `upgrade` | 攻防一体牌＝upgrade 子类 |
| E6 | `cheap` / `mock` | 错过关键牌（奖励未选或买得起没买）＝cheap 嘲讽，双时机（Reward+Shop） |
| E7 | `skip_reward` / `cheap` | 不选未来牌＝skip_reward 族原生场景 |
| E8 | `upgrade` / `achieve` | 全面神化＝upgrade 顶点，可升格 achieve |
| E9 | `skip_reward` | 跳过卡牌奖励＝skip_reward 族原生场景（带动态概率） |
| E10 | `praise` / `target` | 候选 ≥2 张关键/超模＝幸福的烦恼，praise 或 target |
| E11 | `draw` / `mock` | 牌组 >40 继续增长＝draw 过量，嘲讽用 mock |
| E12 | `draw_repeat` | 同一张牌三顾茅庐＝draw_repeat 加强版，按稀有度设阈值 |
| E13 | `achieve` / `praise` | 强力组合集齐＝achieve（A→B 单向登记 + 20 节点超时） |
| E14 | `draw` / `mock` | 牌组 ≤10 还继续删＝draw 极端精简，v1.2.1 新增 |
| F1 | `shop` | 购买当前商店最贵物品＝shop 族原生场景 |
| F2 | `shop` / `upgrade` | 商店删牌＝shop + upgrade（精简牌组） |
| F3 | `shop` / `target` | 领主阳伞：对每件在售商品各发一条置顶弹幕＝shop 批量，v1.2.1 新增 |
| G1 | `opening` / `potion_waste` | 带药水进建筑师事件＝事件类，opening + potion_waste |
| G2 | `upgrade` / `mock` | 桥事件失去卡牌（强/弱牌分支）＝upgrade 移除语感 |
| G3 | `upgrade` / `achieve` | 克隆附魔＝upgrade 特殊来源（首次获得 + 火堆克隆） |
| G4 | `slack` / `rest` | 火堆睡觉＝slack（摸鱼不升级）族原生场景；situation 库键名为 slack |
| G5 | `slack` / `mock` | 满血火堆睡觉＝slack 极端，浪费火堆 |
| G6 | `slack` / `danger` | 残血火堆不睡＝slack + danger |
| G7 | `upgrade` | 连续 3 火堆升级＝upgrade 连胜 |
| I1 | `draw` / `praise` | 多人奖励选牌＝需多人信号通道，单人不可用 |
| I2 | `shop` | 多人商店购买＝同上 |
| I3 | `slack` / `rest` | 多人火堆选择＝同上 |

### 2.1 按族汇总（可直接补建国运尖塔 triggers）

- **mock**（12 条）：H1、A6、A3、B5、B6、A4、C6、E6、E11、E14、G2、G5
- **upgrade**（9 条）：E1、E3、E4、E5、E8、F2、G2、G3、G7
- **praise**（8 条）：B4、A4、A2、D7、E3、E10、E13、I1
- **cheap**（7 条）：H1、B1、B2、B3、C7、E6、E7
- **draw**（7 条）：B5、B8、C7、E1、E11、E14、I1
- **overkill**（6 条）：D1、C5、C3、B8、C2、E4
- **danger**（5 条）：A1、A5、B1、D3、G6
- **hurt**（5 条）：C1、B3、C3、C6、D6
- **slack**（5 条）：B6、G4、G5、G6、I3
- **boss**（4 条）：D2、D3、D4、D5
- **clean**（4 条）：C9、A2、D7、C2
- **opening**（4 条）：A1、A3、D5、G1
- **quick**（4 条）：C4、B4、D1、D4
- **shop**（4 条）：F1、F2、F3、I2
- **achieve**（3 条）：E8、E13、G3
- **death**（3 条）：D2、D6、C8
- **draw_repeat**（3 条）：B2、E2、E12
- **elite**（2 条）：A5、A6
- **potion_waste**（2 条）：B7、G1
- **rest**（2 条）：G4、I3
- **skip_reward**（2 条）：E7、E9
- **target**（2 条）：E10、F3

### 2.2 最值得整合的 10 个规则

| # | ruleId | 名称 | 推荐理由 |
| --- | --- | --- | --- |
| 1 | **C9** | 连续无伤战斗（clean） | 国运层已有 clean 族与 CleanStreak 计数，触发条件（连胜 5/7/9）与比赛叙事直接挂钩 |
| 2 | **B7** | 战中豪饮（potion_waste） | potion_waste 族是 situation 库现有 22 族之一，国运层缺触发源，接入成本最低 |
| 3 | **B6** | 假装思考十二秒（mock/slack） | slack 族已有词条，纯计时钩子，与原版战斗阶段解耦 |
| 4 | **H1** | SL 操作（cheap/mock） | 弹幕文化与生涯叙事最契合，且国运层已有存档恢复链路（MatchRecovery） |
| 5 | **C4** | 一回合击杀 OTK（quick） | quick 族原生场景，LiveCommentary 已有 QuickWins 计数 |
| 6 | **C1** | 裸接重击（hurt） | hurt 族原生场景，LiveVoiceObservation 已在产 hurt 线索 |
| 7 | **E9** | 跳过抓牌（skip_reward） | EventFor 已把 skip_ 映射到 skip_reward，只差触发源 |
| 8 | **G4** | 火堆睡觉（slack） | EventFor 已把 rest_ 映射到 slack，只差触发源 |
| 9 | **E2** | 重复抓牌（draw_repeat） | draw_repeat 族已建库，词条“已经获得卡牌后不再触发”的修复已就绪 |
| 10 | **F2** | 商店删牌（shop/upgrade） | shop 族原生场景，可与国运层 CareerCommerce 采购叙事联动 |

## 3. 无法整合的规则清单

| ruleId | 名称 | 无法整合的原因 |
| --- | --- | --- |
| D2 | 误弑灯 | 判定对象是原版女王战专属召唤物「火炬头」(QueenTorchhead)，国运层无该实体 |
| D3 | 无视灯 | 同上：火炬头存活时单体攻击女王；依赖原版女王战目标结构 |
| D4 | 雕刻师吟唱前击杀 | 依赖原版虔诚雕刻师 DEVOTED_SCULPTOR 的招式事件 FORBIDDEN_INCANTATION_MOVE |
| D5 | 听到雕刻师吟唱 | 同上：依赖原版怪物选招/用招事件 |
| D6 | 咬人卷轴致损血上限 | 依赖原版咬人卷轴 SCROLL_OF_BITING 与 PaperCutsPower 减最大生命机制 |
| D7 | 咬人卷轴无损血上限 | 同上 |
| C7 | 实验体刮痧 | 依赖原版实验体 + Intangible（无实体）能力；可改写为「任意无实体敌人」但语义已变 |
| D1 | 对策完美匹配 | 分支 B 硬编码外骨骼虫 EXOSKELETON 的多段目标；分支 A（AOE 命中 ≥3）可移植，整体按不可整合计 |
| C3 | 盛碗虫（石）极限 | 依赖原版盛碗虫（石）BOWLBUG_ROCK 的单体攻击数值结构 |
| G1 | 建筑师带饮料 | 依赖原版建筑师事件（Architect）与药水栏；国运层无建筑师事件 |
| G2 | 桥事件 | 依赖原版「桥」事件选项回调 |
| A1 | 强敌登场（部分） | 强怪集合为模组硬编码的原版 ModelId 白名单（异蛙寄生虫/花园幽灵鳗/外骨骼虫/残杀千足虫/多尼斯异鸟/活雾/鬼祟珊瑚群/感染棱柱），国运层需自建等价集合 |
| I1 | 多人选牌 | 只在多人会话内有意义，单人生涯无触发时机 |
| I2 | 多人商店购买 | 同上 |
| I3 | 多人火堆选择 | 同上 |

> 上表 15 条中，`D2 D3 D4 D5 D6 D7 C3 C7 G1 G2`（10 条）为「依赖原版专属实体/事件/招式」，属硬性不可整合；
> `D1 A1`（2 条）为「部分可整合」——需替换模组硬编码的原版 ModelId 白名单；
> `I1 I2 I3`（3 条）为「仅在多人会话内存在触发时机」，单人生涯不适用。

## 4. 内容清单（词条条数与风格构成）

| ruleId | 中文名 | 单人词条 | narration | character | 多人词条 | 代表作（词条 id · 文本） |
| --- | --- | --- | --- | --- | --- | --- |
| H1 | SL 操作 | 5 | 5 | 0 | 0 | `h1-00001` 0.5sl了 ；`h1-00003` 问心无愧了 |
| A1 | 强敌登场 | 8 | 5 | 3 | 0 | `a1-00001` 老霸道了 ；`a1-00003` 骇死我力 ；`a1-00005` 骇死我力！ |
| A5 | 残血进精英 | 8 | 6 | 2 | 0 | `a5-00001` 死战不退！ ；`a5-00002` 死战不退了 ；`a5-00005` 我说死战不退 |
| A6 | 地图连续选择精英 | 3 | 2 | 1 | 0 | `a6-00001` 有一个角色追着精英打，你知道他是谁 ；`a6-00003` 依旧冲精英 |
| A3 | 旧敌重逢 | 6 | 5 | 1 | 0 | `a3-00001` 你怎么还在这里？ ；`a3-00002` 你怎么还在这？ ；`a3-00004` 这集我看过 |
| B1 | 防御不足 | 6 | 3 | 3 | 3 | `b1-00001` 发牌员眉目了 ；`b1-00003` 依旧敌人攻击我攻击 ；`b1-00004` 发牌员游牧了 |
| B2 | 输出不足 / 卡手 | 5 | 2 | 3 | 3 | `b2-00001` 发牌员眉目了 ；`b2-00003` 依旧敌人防御我防御 ；`b2-00004` 发牌员游牧了 |
| C1 | 裸接重击 | 14 | 9 | 5 | 5 | `c1-00001` 依旧神秘红色格挡条 ；`c1-00003` 懦夫才防 ；`c1-00008` 红色格挡条发力了 |
| C4 | 一回合击杀（OTK） | 8 | 5 | 3 | 0 | `c4-00001` 减速带来了 ；`c4-00005` 一键扫荡了 ；`c4-00006` 时间紧，任务重 |
| B3 | 有费有防不出 | 6 | 4 | 2 | 2 | `b3-00001` 细节不防骗弹幕 ；`b3-00005` 展示容错 ；`b3-00004` 有防不用算你输了 |
| B4 | 战斗启动 | 9 | 5 | 4 | 4 | `b4-00001` 那么，我们开始吧？ ；`b4-00005` 我已启动！ ；`b4-00006` 要来咯 |
| B5 | 卡动画 | 5 | 3 | 2 | 0 | `b5-00001` 我说动画卡我启动了 ；`b5-00003` 动画大于一切 ；`b5-00004` 我说动画大于一切 |
| D1 | 对策完美匹配 | 3 | 3 | 0 | 0 | `d1-00001` 专业对口 |
| C5 | 单卡高伤害 | 7 | 5 | 2 | 2 | `c5-00001` 我说{card}伤害特别高 ；`c5-00003` 大数字享受者 ；`c5-00004` 大数字爱好者 |
| B6 | 假装思考十二秒 | 8 | 6 | 2 | 2 | `b6-00001` 假装思考 ；`b6-00002` 抉择，抉择了 ；`b6-00005` 依旧假装犹豫 |
| D2 | 误弑灯 | 5 | 5 | 0 | 0 | `d2-00001` 竟敢误弑灯 ；`d2-00003` ？！竟敢误弑灯！？ |
| D3 | 无视灯 | 7 | 7 | 0 | 0 | `d3-00001` 竟敢无视灯 ；`d3-00004` 竟然敢无视灯！ |
| C3 | 盛碗虫（石）极限 | 2 | 1 | 1 | 0 | `c3-00001` 精妙的数值设计 ；`c3-00002` 我说精妙的数值设计 |
| B7 | 战中豪饮 | 5 | 3 | 2 | 2 | `b7-00001` 我不是药神 ；`b7-00002` 口渴了 ；`b7-00003` 回过神来，已是豪饮 |
| B8 | 过牌溢出 | 1 | 0 | 1 | 1 | `b8-00001` 潦草急就了 |
| A4 | 旧敌再审 | 6 | 6 | 0 | 0 | `a4-00001` 二审维持原判 ；`a4-00004` 这集结局看过了 |
| C6 | 爽局掉血 | 4 | 4 | 0 | 0 | `c6-00001` 神也会流血吗 ；`c6-00003` 神也会受伤吗 |
| C9 | 连续无伤战斗 | 5 | 4 | 1 | 0 | `c9-00001` 一直无伤算你赢了 ；`c9-00003` 神不会流血 ；`c9-00004` 目前不知道怎么输 |
| C7 | 实验体刮痧 | 6 | 6 | 0 | 0 | `c7-00001` 我说硬刮无实体算你赢了 ；`c7-00004` 折磨实验体算你赢了 |
| A2 | 强敌倒下 | 2 | 2 | 0 | 0 | `a2-00001` 老霸倒了 |
| D4 | 雕刻师吟唱前击杀 | 8 | 8 | 0 | 0 | `d4-00001` 我说没听到咔咔美声算你输了 ；`d4-00005` 咔咔美声藏好了 |
| D5 | 听到雕刻师吟唱 | 9 | 9 | 0 | 0 | `d5-00001` 我说听到咔咔美声算你输了 ；`d5-00005` ！？咔咔？！ |
| D6 | 咬人卷轴致损血上限 | 5 | 4 | 1 | 1 | `d6-00001` 我说掉最大生命值算你输了 ；`d6-00005` 保护血上限失败了 ；`d6-00003` 掉血上限算你输了 |
| D7 | 咬人卷轴无损血上限 | 5 | 4 | 1 | 1 | `d7-00001` 保护血上限算你赢了 ；`d7-00005` 卷轴没咬到 ；`d7-00003` 我说保护血上限算你赢了 |
| C8 | 角色死亡 | 8 | 6 | 2 | 2 | `c8-00001` 坠机了 ；`c8-00004` 死是凉爽的夏夜 ；`c8-00005` 你的旅途到此为止 |
| C2 | 数值极限 | 9 | 6 | 3 | 0 | `c2-00001` 精妙的数值设计 ；`c2-00002` 我说精妙的数值设计 ；`c2-00005` 精妙！ |
| E1 | 一层抓未来牌 | 6 | 3 | 3 | 3 | `e1-00001` 要不战个未来吧 ；`e1-00002` 战未来了 ；`e1-00004` 战未来！ |
| E2 | 重复抓牌 | 6 | 4 | 2 | 2 | `e2-00001` 我说{card}好牌多抓 ；`e2-00003` 我说好牌多拿 ；`e2-00004` {card}好牌多抓 |
| E3 | 获得关键牌 | 6 | 4 | 2 | 2 | `e3-00001` 我说尽孝 ；`e3-00002` 百善孝为先 ；`e3-00004` 依旧尽孝 |
| E4 | 获得超模牌 | 12 | 11 | 1 | 1 | `e4-00001` 算你赢了 ；`e4-00012` {card}特别强 ；`e4-00007` 666算你赢了 |
| E5 | 攻防一体牌 | 9 | 7 | 2 | 0 | `e5-00001` 有攻有防，是铁斩波 ；`e5-00002` 攻防一体的艺术品 ；`e5-00005` 广义铁斩波 |
| E6 | 错过关键牌 | 8 | 8 | 0 | 0 | `e6-00001` 记笔记，{char}不抓{card} ；`e6-00005` ！？不拿{card}？！ |
| E7 | 不选择未来牌 | 8 | 8 | 0 | 0 | `e7-00001` 我说不战未来算你输了 ；`e7-00005` 战个现在是吧 |
| E8 | 全面神化 | 8 | 5 | 3 | 3 | `e8-00001` 我已神化 ；`e8-00003` 我已登神！ ；`e8-00005` 我 已 神 化 |
| E9 | 跳过抓牌 | 7 | 7 | 0 | 0 | `e9-00001` ！？不拿拿？！ ；`e9-00004` ？！banana！？ |
| E10 | 难以抉择 | 5 | 5 | 0 | 0 | `e10-00001` 忠孝难两全 ；`e10-00003` 忠孝两难全了 |
| E11 | 大卡组成长 | 6 | 5 | 1 | 1 | `e11-00001` 终于看到正常大小的卡组了 ；`e11-00003` 终于玩到小卡组了 ；`e11-00004` 终于看到正常卡组了 |
| E12 | 某牌三顾茅庐 | 4 | 4 | 0 | 0 | `e12-00001` {card}真的很想加入这个家 ；`e12-00003` {card}又来了 |
| E13 | 强力组合 | 8 | 7 | 1 | 0 | `e13-00001` {item}给我藏好了 ；`e13-00003` 等一个{item} ；`e13-00005` 完美契合了 |
| E14 | 卡组过小 | 2 | 2 | 0 | 0 | `e14-00001` 东尼意思 |
| F1 | 购买商店高价值商品 | 3 | 1 | 2 | 0 | `f1-00001` 贵有贵的道理 ；`f1-00002` 我说贵有贵的道理 |
| F2 | 商店删牌 | 5 | 4 | 1 | 1 | `f2-00001` 删牌大于一切啊 ；`f2-00003` 这个，不需要了（冷漠） ；`f2-00004` 尼已急哭 |
| F3 | 领主阳伞 | 2 | 2 | 0 | 0 | `f3-00001` ？！不拿{item}？！ |
| G1 | 建筑师带饮料 | 6 | 5 | 1 | 0 | `g1-00001` 给建筑师带瓶饮料 ；`g1-00002` 我说给建筑师带瓶饮料 ；`g1-00004` 给建筑师带小甜水算你赢了 |
| G2 | 桥事件 | 9 | 5 | 4 | 4 | `g2-00001` 我爱桥 ；`g2-00004` 桥有木了 ；`g2-00005` 这桥有用 |
| G3 | 克隆附魔 | 5 | 3 | 2 | 2 | `g3-00001` 我说指数是个危险的设计 ；`g3-00003` 翻倍是危险的设计 ；`g3-00004` 翻倍是个危险的设计 |
| G4 | 火堆睡觉 | 6 | 3 | 3 | 3 | `g4-00001` 好好睡 ；`g4-00004` 我说好好睡 ；`g4-00005` 火堆起防 |
| G5 | 满血火堆睡觉 | 5 | 3 | 2 | 2 | `g5-00001` 睡个美容觉 ；`g5-00003` 依旧美容觉 ；`g5-00004` 睡美容觉算你赢了 |
| G6 | 残血火堆不睡 | 7 | 7 | 0 | 0 | `g6-00001` 有一个角色{hp}血不睡觉，你知道他是谁 ；`g6-00004` {hp}血还不睡？！ |
| G7 | 连续升级卡牌 | 5 | 3 | 2 | 2 | `g7-00001` 勤学精进了 ；`g7-00002` 我说好牌多敲 ；`g7-00003` 敲牌大于一切了 |
| I1 | 多人选牌 | 0 | 0 | 0 | 1 | （无单人词条） |
| I2 | 多人商店购买 | 0 | 0 | 0 | 2 | （无单人词条） |
| I3 | 多人火堆选择 | 0 | 0 | 0 | 1 | （无单人词条） |
| **合计** | 58 条 | **336** | 259 | 77 | **58** | — |

> 单人词条 336 条 = 普通规则组 312 条 + 多人专用组 I1/I2/I3 的 24 条；多人词条恒为每组 1 条，共 58 条。

### 4.1 含 variant 的组（同一 ruleId 内的语义变体）

| ruleId | variant 取值 |
| --- | --- |
| E13 | `completed` / `timeout` / `waiting` |
| G2 | `strong` / `weak` |

## 5. 置信度统计

| 置信度 | 条数 | 说明 |
| --- | --- | --- |
| 高 | 36 | 判定条件与钩子点均明确，可直接映射到国运尖塔现有事件族 |
| 中 | 18 | 条件明确但需新钩子、或需替换原版专属语义 |
| 低 | 4 | 依赖原版专属实体/招式，国运层无对应触发时机 |
| **合计** | **58** | 58 条（`META-COMBAT-START` 为框架元触发，不计入） |

## 6. 实现提示（整合时需注意）

- 规则由 `DanmakuTrigger` 枚举驱动，`DanmakuRuleIds.Get` 是唯一 ID 来源；新增规则须同时改枚举、`DanmakuRuleIds`、`DanmakuRuleCatalog`、`RuleDisplayCatalog` 四处。
- 判定文本里的阈值可直接复用：手牌 10 张、裸伤 5 点、单卡 40 点、12 秒未动、同场 2 瓶药水、无伤连胜 5/7/9、大牌组 40 张、小牌组 10 张、连升 3 火堆、稀有度次数阈值 白 5/蓝 4/金 3、跳过抓牌概率 20%+10%/次。
- `E9 跳过抓牌` 与 `E13 强力组合` 带状态机（概率回落、20 节点超时），移植时需一并搬运 `RunContext` 状态。
- `F3 领主阳伞` 是唯一「按在售物品数逐条发射置顶弹幕」的规则，且不受词条密度与强度影响，与国运尖塔现有 `Pick()` 的 density/tier 逻辑冲突，需单独通路。
- `I1/I2/I3` 依赖「只传信号不传文本」的多人通道（`MultiplayerRuleConsumers`），国运尖塔 Coop 层已有类似设计，可参考其 `requireLocalPlayer` 过滤。
- 未确认项：`DanmakuRuleCatalog` 部分条件文本为作者概述而非完整布尔表达式（如 `E13` 的「组合状态依赖预设目标」）；精确组合清单存放在 `DanmakuSpireCode.Data.PowerfulCombos` / `content/catalogs/collectible-sets.json`，本次未逐条展开。
