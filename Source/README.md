# EndfieldFramework 源码

RimWorld 1.7 起改用 Waylith 自研引擎 + .NET 10，编译方式与 1.6 完全不同：

- 目标框架：`net10.0`
- 引用：`<游戏根目录>/Assembly-CSharp.dll`、`<游戏根目录>/UnityStubs.dll`
  （UnityEngine.* 在运行时被 WaylithClr 重定向到 UnityStubs）
- 产物：`EndfieldFramework.dll` → `../Assemblies/`

```bash
dotnet build -p:OutputPath=<可写目录> -p:BaseIntermediateOutputPath=<可写目录>
```

目录：

| 路径 | 内容 |
|---|---|
| Defs/ | OperatorDef / OperatorClassDef / OperatorSeriesDef / SkillEntry / InnateHediff / 各类 modExtension |
| Generation/OperatorGenerator.cs | 造人主流程 |
| Generation/FaceApplicator.cs | 面部表情反射注入（软依赖 UNAGI） |
| Buildings/Building_RecruitConsole.cs | 招募终端建筑 |
| UI/Window_OperatorSelect.cs | 招募界面 |
| Debug/EF_DebugActions.cs | 开发者菜单测试入口 |
| _legacy/ | 旧 Patch.dll（依赖 AK_DLL，已停用，仅留档） |