# Custom Level Floor Shader Fix 0.3.0（实验版）

用于 Overcooked! 2 / Unity 2017.4.8f1 / BepInEx 5。
已使用本机游戏程序集编译通过，尚未实机确认画面或完整插件兼容性。

## 本次修改

- 保留对 Ground、floor、blue_water 及地板材质名的识别。
- MeshRenderer 优先使用受支持的 Unlit/Texture；SpriteRenderer 优先使用受支持的 Sprites/Default。
- 遍历同名 Shader 的各个已加载副本，避免仅使用 Shader.Find 命中不兼容副本。
- 如果没有支持的内置候选 Shader，尝试复制已加载且受支持的 Overcooked_2 游戏材质。
- 显式转移原主贴图、贴图缩放、偏移和 _Color；也尝试读取 SpriteRenderer 的 sprite.texture。
- 没有找到贴图或支持的候选 Shader 时，保留原材质并记录原因。
- 使用真实时间延时，在暂停及预备画面也能运行；约 12.5 秒内检查五次。
- 对场景 s_yi _level 中的 blue_water 默认尝试替换，即使导入的原 Shader 报告 isSupported=true。
- 单个 Renderer 检查失败只记录警告，不中断其他对象的处理。

## 安装

1. 完全退出游戏。
2. 移走 PearMacFloorFix.dll 和所有旧版本 CustomLevelFloorShaderFix.dll。
3. 只将本目录的 CustomLevelFloorShaderFix.dll 放入游戏 BepInEx/plugins/。
4. 保留游戏框架、关卡文件、存档和全部前置依赖，不要替换它们。
5. 重启，进入“宜”等自定义关卡，等待约 13 秒。

## 范围与限制

自动扫描 BepInEx/plugins/OC2DIYLevel/levels 下无扩展名的 s_* 场景包；额外包含 s_yi _level。
只修改匹配地板名称的 Renderer 的运行时材质；不写入关卡文件或存档。
无法修复缺失的模型、贴图、脚本、配方或前置依赖。
通用材质可能改变原有光照、透明度或水面效果；原贴图及贴图坐标变换会尽量保留。
若问题来自 UV、场景布局或贴图内容本身，换 Shader 不会修正它。

## 配置

首次启动会生成 BepInEx/config/local.customlevel.floorshaderfix.cfg。
[Repair] ForceYiWater=true：限定“宜”的 blue_water 即使报告支持也尝试替换。
[Discovery] AdditionalScenes=s_yi _level：额外场景名，可用分号分隔，保留场景内部空格。
修改配置后重启。

## 日志

在 BepInEx/LogOutput.log 中搜索 Custom Level Floor Shader Fix。
v0.3.0 active：确认新版已加载。
Inspecting custom scene：确认场景被识别。
REPAIRED：执行了材质替换，不代表画面已经验证正常。
floors=0：没有匹配到地板 Renderer。
missingTexture>0：某些待修复地板没有可获取的主贴图。
missingShader>0：某些待修复地板没有找到支持的替代 Shader。
日志会记录原 Shader、材质、主贴图及替换目标。

## 卸载

退出游戏，移走 CustomLevelFloorShaderFix.dll，重启即可。

## 编译依赖

使用 Mono mcs -target:library -sdk:2，引用本机游戏的 BepInEx.dll、UnityEngine.dll、UnityEngine.CoreModule.dll、UnityEngine.SpriteMaskModule.dll。
源码随附；不分发游戏依赖 DLL。
