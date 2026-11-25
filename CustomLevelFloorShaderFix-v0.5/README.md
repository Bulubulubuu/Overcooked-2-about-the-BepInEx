# Custom Level Floor Shader Fix v0.5.0

针对 Overcooked! 2 macOS / Unity 2017.4.8f1 / BepInEx 5 的运行时材质兼容试验补丁。
沿用旧插件 GUID 和文件名，以读取已有配置。由 v0.4 源码的已知行为扩展而来，保留“宜”和 Gl 云朵壁纸规则。

## 更新范围

- 默认检查自定义关卡中的全部 Renderer、SpriteRenderer、粒子材质、粒子拖尾和 Canvas Graphic，覆盖地板、大炮物体、墙上图示及雨效。
- 新增实际资源里出现的 _ColourAlpha、_DiffuseTexture 贴图字段。
- 对不受支持或错误 Shader 优先尝试另一份受支持的同名 Shader；找不到时才选择替代 Shader。
- 对大炮、告示牌/图示和雨等名称匹配的对象，即使导入的 Standard/Sprites/Particles/Legacy Shader 报告支持，也默认尝试替换。此规则不强制替换仍报告支持的 Overcooked_2 Shader。
- 雨和拖尾使用粒子/透明候选，不使用不透明地板材质作后备；保留粒子组件和游戏逻辑。
- 替换材质尽量保留主贴图、缩放、偏移和颜色；UI 尝试保留 stencil 参数。同名 Shader 替换会复制原材质的全部属性。
- 默认观察场景约 60 秒，覆盖延迟生成的物体；暂停时仍运行。卸载场景后释放补丁材质，停用插件时尝试恢复原引用。
- 缓存按原材质、渲染类型和贴图区分，减少重复实例；日志默认只记录替换、失败和摘要。

## 安装

1. 完全退出游戏。
2. 备份并移走 BepInEx/plugins/ 中旧版 CustomLevelFloorShaderFix.dll；也不要同时加载 PearMacFloorFix.dll。
3. 将本目录的 CustomLevelFloorShaderFix.dll 放到 BepInEx/plugins/，只保留一份。
4. 保留 OC2DIYLevel、RuntimeWLoader、FastInit、关卡包和存档等原有文件。本补丁不替换这些依赖。
5. 重启，确认 BepInEx/LogOutput.log 出现 `v0.5.0 active`，再进入关卡观察。

本次仅生成交付文件，没有修改游戏安装目录。

## 配置

路径：BepInEx/config/local.customlevel.floorshaderfix.cfg；旧配置会自动补入新键。

- Repair.ScanAllRenderers=true：扩大检查范围。false 恢复为命名地板范围。
- Repair.RepairKnownVisualAssets=true：对命名匹配的大炮、图示、雨的通用导入 Shader 尝试替换；如效果变差可设 false。
- Repair.ForceYiWater / ForceGlFloor：保留之前的特例。
- Repair.ForceShaderNames：可选精确 Shader 名，以分号分隔，默认空。只作用于发现的自定义场景。
- Discovery.AdditionalScenes：额外场景名，保留名称内部空格；“宜”和 Gl 两关仍作为内置特例。
- Discovery.WatchSeconds=60：动态物体观察窗口，实际限制 12.5–300 秒。
- Logging.VerboseMaterials=false：启用后增加每次扫描的摘要输出。

## 日志与限制

AUDIT：场景检查摘要。invalid / forced / changed 分别为当前轮待检查的异常、规则触发和替换槽位数量；已修复材质后续扫描会跳过，因此后续计数可为 0。
REPAIRED：已经替换引用，不能代替画面验证。
NO_COMPATIBLE_SHADER：当前进程没有可用的候选 Shader。
NO_SOURCE_TEXTURE：无法读取待转换材质的主贴图。

本版编译通过，12 项规则/回归检查通过；并静态解析了当前目录的 100 个场景。尚未逐关实机验证，不能宣称 100 关画面全部正常。
通用替代 Shader 可能改变光照、透明度、水面/雨效或文字边缘；UI 替代也不能保证所有遮罩行为一致。
无法补回缺失资源或脚本，不能修复配方/存档/联机逻辑，也不会自动修改 FastInit 的游戏版本兼容问题。

发生异常：退出游戏，移走本 DLL，恢复备份的旧 DLL，再启动。

## 本机检查报告

见 适配检查报告.txt 和 audit.json。young 第2、9关有一个 CAB 引用未在已索引包中匹配，仅作为待核查线索，不直接判定缺文件。

## 编译

使用游戏随附的 Unity 2017.4 DLL 和 BepInEx.dll；执行随附 build.sh。
不分发游戏依赖 DLL 或关卡包。
