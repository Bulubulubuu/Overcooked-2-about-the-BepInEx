# Custom Level Floor Shader Fix 0.4.0

新增《宜了 2.2》两个场景的定向地板修复，继续保留旧版自定义关卡地板和“宜”blue_water 水面修复。

## 原因与处理

这份关卡包按 Windows 平台打包；两个场景分别是 `s_gl_level` 和 `s_gl_level 2`。地板是名为“【哲风壁纸】云朵-卡通-可爱”（以及带编号副本）的独立 SpriteRenderer，不在 Ground/floor 节点下，旧版没有识别到。

新版在这两个场景中精确匹配该对象，优先读取 Sprite 自身贴图，并从本机已加载、受支持的着色器中选择替代项。即使导入的着色器报告受支持，也会尝试替换这一块地板。保留原贴图、颜色和 Sprite 网格/UV；不修改关卡压缩包、布局、存档或显示设置。

## 安装

完全退出游戏，将本目录 `CustomLevelFloorShaderFix.dll` 替换到游戏 `BepInEx/plugins/CustomLevelFloorShaderFix.dll`。旧文件先备份到 plugins 外，避免重复加载。不要同时安装 PearMacFloorFix。

进入“宜了 2.2”两个关卡分别检查，等待约 13 秒。`BepInEx/LogOutput.log` 应出现 `Custom Level Floor Shader Fix 0.4.0`、`ForceGlFloor=True` 和对应的 `REPAIRED` 记录。现有配置会自动增加 `[Repair] ForceGlFloor=true`，无需删除配置。

## 验证范围

已用本机 Unity 2017.4.8f1 / BepInEx 5 依赖编译；已解析两个实际场景验证对象名与材质引用，编译后的匹配函数通过 11 项真实对象及作用范围检查。旧版修复逻辑保留。

尚待游戏内验证实际画面。替代材质不保证重现原始光照或特效；若日志显示 missingTexture 或 missingShader，会保留原材质并记录原因。

## 回退

退出游戏后，用备份的旧 DLL 替换新版 DLL；不要把两份同时放进 plugins。
