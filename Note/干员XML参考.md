# 干员 XML 参考（Endfield Framework · 1.7 独立版）

一个 `Endfield.OperatorDef` = 一个固定人物。招募终端里点一次，就按这份定义精确生成一次，**没有随机**。

## 最小示例

```xml
<Endfield.OperatorDef ParentName="EF_RecruitBase_Striker">
  <defName>EF_Recruit_MyOperator</defName>
  <name>名字</name>
  <nickname>代号</nickname>
  <surname>姓</surname>
  <description>简介。</description>
  <isMale>false</isMale>
  <age>21</age>
  <bodyTypeDef>Female</bodyTypeDef>
  <headTypeDef>Female_AverageNormal</headTypeDef>
  <hair>Bob</hair>
  <hairColor>(0.44, 0.74, 1.0, 1.0)</hairColor>
  <skinColor>(1.0, 0.9, 0.878, 1.0)</skinColor>
  <xenoType>Baseliner</xenoType>
  <apparels><li>Apparel_BasicShirt</li></apparels>
  <weapon>Gun_BoltActionRifle</weapon>
  <skills><li><skill>Shooting</skill><level>6</level><fireLevel>2</fireLevel></li></skills>
  <traits><li><def>Tough</def></li><li><def>Beauty</def><degree>2</degree></li></traits>
</Endfield.OperatorDef>
```

## 字段总表

| 字段 | 类型 | 说明 |
|---|---|---|
| `name` / `nickname` / `surname` | string | 名 / 显示代号 / 姓 |
| `isMale` | bool | 性别，默认女 |
| `age` / `realAge` | int | 生理年龄 / 历法年龄，realAge 不填则跟随 age |
| `bodyTypeDef` `headTypeDef` `hair` `beard` | Def | 体型、头部、发型、胡须 |
| `skinColor` `hairColor` | Color | 写 `(r,g,b,a)`，0~1 |
| `xenoType` | XenotypeDef | 异种类型，可空 |
| `apparels` | List<ThingDef> | 逐件穿上，会强制保留（outfit forced） |
| `weapon` | ThingDef | 主手武器 |
| `items` | List<ItemEntry> | 背包物品：`<li><thingDef>MedicineIndustrial</thingDef><count>2</count></li>` |
| `childHood` `adultHood` | BackstoryDef | 童年 / 成年背景，成年背景仅在年龄 ≥ 20 时生效 |
| `skills` | List<SkillEntry> | `<skill>` + `<level>` + `<fireLevel>`(0~2 热情) 或 `<passion>` |
| `traits` | List<TraitRequirement> | `<def>` + 可选 `<degree>`；会用这份列表**替换**生成出来的特性 |
| `hediffInate` | List<InnateHediff> | `<hediff>` + `<part>`(BodyPartDef，可空=全身) + `<severity>` |
| `operatorType` | OperatorClassDef | 职业，决定招募界面的图标与排序 |
| `portraitPath` | string | 头像贴图路径（相对 Textures/），填了就优先显示它 |
| `standPath` | string | 立绘贴图路径，预留 |
| `series` | OperatorSeriesDef | 阵营 / 作品系列 |

## modExtensions

```xml
<modExtensions>
  <!-- 面部表情（UNAGI FacialAnimation），没装会自动跳过 -->
  <li Class="Endfield.FA_Extension">
    <headTypeDef>HeadMini</headTypeDef>
    <eyeTypeDef>EyeSlippery</eyeTypeDef>
    <browTypeDef>BrowKomari</browTypeDef>
    <lidTypeDef>LidCloseUwame</lidTypeDef>
    <mouthTypeDef>MouthNormal</mouthTypeDef>
    <skinTypeDef>SkinBlush</skinTypeDef>
    <eyeballColorR>0.8</eyeballColorR>
    <eyeballColorG>0.345</eyeballColorG>
    <eyeballColorB>0.345</eyeballColorB>
  </li>
  <!-- 招募后直接授予的能力 -->
  <li Class="Endfield.EF_AbilityExtension">
    <extraAbilities><li>EF_Ability_Dash</li></extraAbilities>
  </li>
</modExtensions>
```

## 本地化（XML 写英文，中文进 Languages）

和其他原版 Def 一致：**XML 里写英文（源语言）**，中文写在 `Languages/ChineseSimplified/DefInjected/` 下，键名是 `DefName.字段名`。

```
Languages/English/DefInjected/Endfield.OperatorDef/Recruit_LastRite.xml
```

```xml
<LanguageData>
  <EF_Recruit_LastRite.name>Adelheid</EF_Recruit_LastRite.name>
  <EF_Recruit_LastRite.nickname>LastRite</EF_Recruit_LastRite.nickname>
  <EF_Recruit_LastRite.description>...</EF_Recruit_LastRite.description>
</LanguageData>
```

## 招募终端参数

```xml
<ThingDef ParentName="BuildingBase">
  ...
  <modExtensions>
    <li Class="Endfield.RecruitConsoleExtension">
      <silverCost>0</silverCost>        <!-- 每次招募消耗银币 -->
      <cooldownTicks>600</cooldownTicks> <!-- 冷却 tick，600 = 10 秒 -->
    </li>
  </modExtensions>
</ThingDef>
```

## 从 MIS 版迁移

| 旧 | 新 |
|---|---|
| `<AK_DLL.OperatorDef>` | `<Endfield.OperatorDef>` |
| `<AK_DLL.OperatorClassDef>` | `<Endfield.OperatorClassDef>` |
| `<AK_DLL.OperatorSeriesDef>` | `<Endfield.OperatorSeriesDef>` |
| `operatorID` / `voicePackDef` / `stand` / `headPortrait` / `staticStands` / `standOffsets` | 删除（立绘与语音系统已移除） |
| `<serverity>` | 可用 `<severity>`，旧拼写仍兼容 |