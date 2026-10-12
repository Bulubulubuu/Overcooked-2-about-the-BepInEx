# Pear Mac Floor Fix（实验补丁）

尝试修复 Overcooked! 2 macOS 中 pear 自定义关卡的地板着色器问题。

## 验证状态

已使用本机 Unity 2017.4.8f1 和 BepInEx 组件编译通过。尚未确认游戏内地板修复有效。
测试期间曾反馈存档列表异常；同一轮排查发现 OC2DIYLevelRuntimeWLoader 前置缺失，以及框架读取 RecipeMatchList 资源的空引用错误。
用户随后反馈已恢复正常，但未确认具体恢复步骤。补丁与该问题之间的因果关系尚未确定。

## 工作方式

只检查 s_pear_* 场景中的地板 Renderer，尝试将不支持的着色器、InternalErrorShader 或 Sprites/Default 替换为已加载且受支持的着色器。
候选顺序：Sprites/Default、Unlit/Transparent、Unlit/Texture、UI/Default。
这是运行时材质替换，无法补回缺失的资源或脚本，也不等同于 macOS 原生关卡导出。

## 安装和卸载

退出游戏，将 PearMacFloorFix.dll 放入游戏目录下 BepInEx/plugins/，保留所有原有框架和前置。
重启后进入 pear 关卡，等待约 8 秒。
如遇异常，退出游戏，移走 PearMacFloorFix.dll 后重启。

## 日志

在 BepInEx/LogOutput.log 中搜索 Pear Mac Floor Fix。
Replaced floor material 仅表示执行过替换，不代表画面已恢复。
replacement=NONE 表示未找到支持的候选着色器；floors=0 表示未匹配到地板渲染器。

源码：PearMacFloorFix.cs。DLL 编译时依赖 BepInEx.dll、UnityEngine.dll、UnityEngine.CoreModule.dll 和 UnityEngine.SpriteMaskModule.dll；不随本目录分发这些依赖。
