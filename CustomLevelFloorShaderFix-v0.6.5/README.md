# CustomLevelFloorShaderFix v0.6.5

新增 star“海豹小店”杯子、茶壶、污渍和蒸汽的材质兼容规则；修复不带 s_ 前缀的关卡漏扫。
默认简体中文，不改 OC2ModSettings.dll，不联网、不修改关卡包或存档。

## 安装和使用

1. 完全退出游戏，备份并替换 BepInEx/plugins/CustomLevelFloorShaderFix.dll，只保留一份。
2. 重启，在 Mod Settings → 自定义关卡贴图修复 → 修复选项中，保持“star 海豹小店：杯子、茶壶和蒸汽兼容”开启。
3. 进入 star 海豹小店（当前记录场景名为 star_8），让杯子和茶壶出现。
4. 点击“检查 levels 并准备修复”，查看待应用规则，再退出并重启应用。若开启“立即应用新规则”，则按该设置即时尝试。

保留上一版四季地板的 test_1 后备、语言选择和规则保存方式。

## 此次实际检查到的内容

- 安装包里包含 star_8，但旧扫描器仅索引 s_* 文件，未观察 star 场景。
- 新版读取 UnityFS 包目录中的 BuildPlayer 场景名，兼容 star_8、star_1_1、muxixiao12 的无前缀关卡以及原有 s_* 关卡。
- 当前 levels 目录实测发现 130 个场景名，目录解析无警告。不把 info 资源包误当成场景，隐藏缓存不扫描。
- 从 commonW5 的序列化材质中核对到：
  - mat_web_teacup：_ColourAlpha 原贴图存在。
  - mat_Kettle_Anniversary_01：_ColourAlpha 原贴图存在。
  - mat_web_glassdirt：_MainTex 原贴图存在。
  - mat_Birthday_TeaCup_01_6：原本无贴图，使用 _Color=(0.5,0.5,0.5,1) 的纯色材质。
  - mat_web_pfx_steam：有原贴图，日志报告 Custom/WebPfxAlpha 不受支持。

因此本版只在 star 场景对上述指定材质尝试兼容 Shader，保留原贴图、颜色及杯子/茶壶模型，不用地板贴图替代杯子。
纯色勺子用内置白色纹理承载原颜色，并非生成新的原图。蒸汽使用透明粒子候选，避免变成不透明方块。
效果仍需在游戏中验证，兼容 Shader 可能改变金属反光或高光表现。

## 规则和报告

扫描入口：BepInEx/plugins/OC2DIYLevel/levels/
记录目录：BepInEx/config/CustomLevelTextureRepair/

- observations.plan：材质观测历史。
- pending.plan：下一次启动应用的规则。
- active.plan：当前规则，替换时保留 .bak。
- last-report.txt：检查报告，未观测场景不表示已验证正常。

缺原材质的 Floor 地板仍按上一版读取 test_level/info_test_level 的 test_1 主地板/边条；可关闭“缺材质地板：使用 test_1 地板”。
“撤销待更新”只取消未应用规则；如需卸载，退出后移走 DLL 即可，原游戏资源和存档保留。

## 验证

- 游戏自带框架无警告编译通过。
- 22 项 star 材质识别/场景目录检查通过，包括本机 star_8、star_1_1、压缩目录、重名场景及缓存排除检查。
- 既有 41 项保存/API、12 项原有规则、25 项本地识别/语言、13 项 test_1/拖尾规则检查通过。
- 游戏文件引用解析：122 类型、319 方法、1 字段，0 项失败。
- 尚未实机验证杯子画面、资源加载和面板交互，保留旧 DLL 备份便于回退。

## 编译

bash build.sh；需本机 Mono mcs 和游戏依赖。可通过 OC2_GAME_DIR 指定游戏路径。
所有源码和检查代码随附，不分发游戏素材或依赖 DLL。
