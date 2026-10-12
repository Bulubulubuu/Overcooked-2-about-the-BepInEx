# Custom Level Floor Shader Fix 0.1.0（实验版）

用于 Overcooked! 2 / Unity 2017.4.8f1 / BepInEx 5 的自定义关卡地板 Shader 兼容性排查和替换。
已针对本机游戏程序集编译通过；尚未验证游戏内效果和与全部插件的兼容性。

## 范围

启动时扫描 BepInEx/plugins/OC2DIYLevel/levels 中无扩展名的 s_* 关卡文件。
只在这些关卡对应场景中检查名称或父对象名称符合 floor / p_floor / _floor_ / _flooredge 的 Renderer。
只替换 shader 为 null、不受支持或 InternalErrorShader 的材质。
不会强制替换所有 Sprites/Default 材质，也不会检查所有场景中的全部物体。
关卡使用其他命名、其他安装路径或启用后才新增文件时，可能不匹配；新增关卡后请重启游戏。

## 能做什么

尝试使用当前进程已加载且 isSupported 的 Sprites/Default、Unlit/Transparent、Unlit/Texture、UI/Default 替代失效 Shader。
克隆原材质并保留其属性，然后替换 Renderer 上的引用。替代 Shader 可能导致透明度、光照或颜色与原效果不同。

## 不能做什么

不能补回缺失的模型、贴图、关卡脚本、配方或前置包。
不能将任意 Windows 资源包自动转换为原生 macOS 资源包。
如果失效材质已经被引擎替换为一个报告支持的默认 Shader，本补丁可能无法识别。

## 安装

1. 退出游戏，先移走旧 PearMacFloorFix.dll；两个插件声明为不兼容，不要同时安装。
2. 仅将 CustomLevelFloorShaderFix.dll 放到游戏 BepInEx/plugins/。
3. 保留原有 OC2DIYLevel 框架、存档和所有前置依赖，包括已安装的 RuntimeWLoader。
4. 重启后进入自定义关卡，等待约 8 秒。
5. 在 BepInEx/LogOutput.log 搜索 Custom Level Floor Shader Fix。

日志 floors 是匹配到的 Renderer 数，changed 是该次替换次数，replacement=NONE 表示没有找到支持的候选 Shader。
替换日志不能证明画面已修复。需要游戏内人工验证。

## 卸载

退出游戏，将 CustomLevelFloorShaderFix.dll 移出 plugins，再重启。
补丁不写入关卡包或存档。
