# CustomLevelFloorShaderFix v0.6.4

默认简体中文。优先保留可用的原材质；对缺少原材质的地板，按用户选择改用 test_1 的地板，不再任意选择道路材质。
不修改 OC2ModSettings.dll，不联网更新，也不改写原关卡或存档。

## 安装和使用

1. 完全退出游戏，备份并替换 BepInEx/plugins/CustomLevelFloorShaderFix.dll，只保留一份。
2. 重启，Mod Settings → 自定义关卡贴图修复。
3. 界面 → 界面语言可选“简体中文 / English”，默认简体中文。
4. 开启“缺材质地板：使用 test_1 地板”。若希望重启后应用，关闭“立即应用新规则”。
5. 进入“四季双人版”，待物体加载后，点击“检查 levels 并准备修复”。已有历史观测时，主菜单准备也会尝试将空材质地板规则改为 test_1 来源。
6. 准备完成后正常退出并重启，再进入该关验证。

扫描入口固定为 BepInEx/plugins/OC2DIYLevel/levels/。不扫描 modules、savefiles 或其他插件目录，排除隐藏缓存。

## test_1 地板的确定来源

读取本机：BepInEx/plugins/OC2DIYLevel/levels/test_level/info_test_level

- Floor 主地板：assets/levelsets/test/materials/mat_city_path_02.mat
- Floor (1)/(2)/(3) 边条：assets/levelsets/test/materials/mat_city_path_01.mat

实际核对了 test_1 场景的四个 MeshRenderer 材质引用及资源包里的对应资产：主地板材质路径ID为 610526985877103900，边条为 -7566651897043342686。
会复制完整材质，保留 test_1 的贴图、缩放、偏移、颜色和其他材质属性。
准备过程自动读取这个普通资源包，不自动进入 test_1 场景，也不启动其关卡逻辑。若资源包由补丁临时加载，读取并克隆后用 Unload(false) 释放包句柄，避免阻止关卡加载器后来正常读取。

这明确是 test_1 的地板替代，不声称是恢复原作者未找到的四季资源。原地板材质已经正常加载时不会走空材质替代。
如果 test_1 的包、贴图依赖或兼容 Shader 不可用，会记录失败，不改用其他随机道路材质。

## 修正与兼容

- 四季双人对应的 s_young_2 四个空材质槽会被明确识别。
- 原先任意本地道路来源的规则会在准备时更新为 test_1 的指定地板来源。
- 未启用粒子拖尾时，空的第二材质槽归为可选槽，不计入未解决故障；主粒子槽和已启用拖尾仍检查。
- 新观测已判定正常的历史记录不会继续作为待应用项保留。
- 保留既有 Shader 兼容、Gl/宜、雨效透明类型保护及本地规则维护接口。
- 延续 .plan 二进制格式、校验和与备份，不依赖 System.Xml 或 InvalidDataException。
- 使用游戏自带 mscorlib/System/System.Core 编译。

## 本地记录

BepInEx/config/CustomLevelTextureRepair/：

- observations.plan：观测历史。
- pending.plan：下次启动要应用的规则。
- active.plan：当前规则。
- last-report.txt：目录和材质检查报告。

报告的“待应用：test_1 地板（替代缺失材质）”表示已确定替代来源，不代表已经看到游戏内画面恢复。
可以点击“撤销待更新”取消尚未应用的规则。
卸载时退出游戏并移走补丁 DLL；原关卡、贴图及存档不受影响。

## 验证状态

- 无警告编译通过。
- 41 项流程/文件/API、12 项原有规则、25 项本地识别/语言边界、13 项 test_1 映射/粒子拖尾/规则保存检查通过。
- 所有直接引用均在本机游戏文件中可解析：119 类型、299 方法、1 字段，0 失败。
- test_1 材质的资产路径、对象ID和贴图缩放已从本机资源包静态核对。
- 尚未实机验证 AssetBundle 读取、语言按钮操作及最终地板画面。保留旧 DLL 备份，出现异常可退出游戏后恢复。

## 编译

运行 bash build.sh，需要 Mono mcs 和本机 Overcooked! 2 游戏组件。可通过 OC2_GAME_DIR 指定游戏根目录。
所有源码和检查代码在本包中，不分发游戏资源包或依赖 DLL。
