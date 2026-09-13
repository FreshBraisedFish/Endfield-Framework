# BF Ability Extras 参考文档

命名空间：`BF_Library`  
源文件：`[BF]-Library\Assemblies\BF_Library\BF_Library\`

---

## 1. BF_CompAbilityEffect_Charge — 冲刺位移

**类：** `BF_CompProperties_Charge` / `BF_CompAbilityEffect_Charge`  
**基类：** `CompProperties_AbilityEffect` / `CompAbilityEffect` + `ICompAbilityEffectOnJumpCompleted`  
**功能：** 施法者沿直线向目标冲刺一段距离，落地后由实现 `ICompAbilityEffectOnJumpCompleted` 的 comp 触发落地效果。

> 旧版 `BF_Verb_Charge` / `BF_VerbProperties_Charge` 已移除。现在用原版 `Verb_CastAbility` 释放能力，冲刺逻辑由本 comp 承担。

### 可用字段（BF_CompProperties_Charge）

| 字段                       | 类型            | 默认值         | 说明                            |
| ------------------------ | ------------- | ----------- | ----------------------------- |
| `chargeToTarget`         | `bool`        | `false`     | `true`=直冲目标格子，`false`=冲刺固定距离  |
| `chargeFlyerDef`         | `ThingDef`    | `PawnFlyer` | 自定义飞行器 ThingDef（控制速度/高度/落地硬直） |
| `startEffecterDef`       | `EffecterDef` | `null`      | 起步时在施法者位置生成的 Effecter         |
| `startEffecterTicks`     | `int`         | `30`        | 起步 Effecter 维持的 tick 数        |
| `startFleckDef`          | `FleckDef`    | `null`      | 起步时在施法者位置生成的一次性粒子             |
| `startSoundDef`          | `SoundDef`    | `null`      | 起步时在施法者位置播放的音效                |
| `stunTargetDuringCharge` | `bool`        | `false`     | 冲刺途中眩晕目标（时长按飞行距离估算，仅第一段生效）     |
| `postChargeStunTicks`    | `int`         | `0`         | 最后一段落地后额外眩晕施法者的 tick 数          |
| `delayTicks`             | `int`         | `0`         | 延迟冲刺的 tick（`0`=施放后立即跳）        |
| `chargeCount`            | `int`         | `1`         | 连续冲刺段数（`1`=单段；`>1` 为多段位移） |
| `chargeInterval`         | `int`         | `0`         | 每段冲刺之间的间隔（tick）              |
| `landingOffset`          | `Vector3`     | `(0,0,0)`   | 每段落点的固定坐标偏移                 |
| `spreadRadius`           | `float`       | `0`         | 每段落点的随机散布半径（格）              |

### 继承 verbProps 原生字段

| 字段                  | 说明                                    |
| ------------------- | ------------------------------------- |
| `range`             | 冲刺距离（固定距离模式）或选敌最大距离（chargeToTarget模式） |
| `warmupTime`        | 起手前摇时间（秒）                             |
| `flightEffecterDef` | 飞行中的视觉效果（绑定在 PawnFlyer 上）             |
| `soundLanding`      | 落地音效                                  |
| `drawAimPie`        | 显示扇形指引                                |
| `targetParams`      | 目标筛选（canTargetPawns/Locations 等）      |

### 行为

- **固定距离模式**（`chargeToTarget=false`）：冲刺 `range` 格，若目标被挡住则退到最近可通行格
- **冲目标模式**（`chargeToTarget=true`）：直接冲到目标格子
- **多段冲刺**（`chargeCount>1`）：落地后按 `chargeInterval` 间隔自动接下一段，每段落点都会重新计算；最后一段落地才应用 `postChargeStunTicks`
- **落点偏移**：先算基础落点，再叠加 `landingOffset`（固定）与 `spreadRadius`（随机散布）；偏移后非法则回退基础落点
- 冲刺前会结束施法者当前 job，防止落地后 job 被恢复导致二次冲刺
- 能力由原版 `Verb_CastAbility` 在**施放瞬间**激活，本 comp 的 `Apply()` 随即启动冲刺；落地后只回调实现 `ICompAbilityEffectOnJumpCompleted` 的 comp
- 建议把本 comp 放在 `<comps>` **最后**，让其它 comp 在施法者尚未离场时先执行（见 §5）

### XML 示例

```xml
<!-- 步骤1：自定义飞行器 -->
<ThingDef ParentName="PawnFlyerBase">
  <defName>ChargeFlyer_Fast</defName>
  <pawnFlyer>
    <flightSpeed>15</flightSpeed>
    <flightDurationMin>0.2</flightDurationMin>
    <heightFactor>0.3</heightFactor>
    <stunDurationTicksRange>0~0</stunDurationTicksRange>
  </pawnFlyer>
</ThingDef>

<!-- 步骤2：能力定义 -->
<AbilityDef>
  <defName>BlitzStrike</defName>
  <label>blitz strike</label>
  <iconPath>UI/Abilities/Blitz</iconPath>
  <cooldownTicksRange>3000</cooldownTicksRange>
  <hostile>true</hostile>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>7.9</range>
    <warmupTime>0.3</warmupTime>
    <flightEffecterDef>Charge_GroundDust</flightEffecterDef>
    <soundLanding>Charge_Impact</soundLanding>
    <targetParams>
      <canTargetLocations>true</canTargetLocations>
      <canTargetPawns>true</canTargetPawns>
      <canTargetBuildings>false</canTargetBuildings>
    </targetParams>
  </verbProperties>
  <comps>
    <li Class="CompProperties_AbilityExplosion">
      <damageDef>Blunt</damageDef>
      <damageAmount>25</damageAmount>
      <explosionRadius>1.9</explosionRadius>
      <screenShakeFactor>0.5</screenShakeFactor>
    </li>
    <!-- 冲刺 comp 放最后 -->
    <li Class="BF_Library.BF_CompProperties_Charge">
      <chargeToTarget>false</chargeToTarget>
      <chargeFlyerDef>ChargeFlyer_Fast</chargeFlyerDef>
      <startSoundDef>Charge_Whoosh</startSoundDef>
      <startFleckDef>DustPuff</startFleckDef>
      <startEffecterDef>Charge_StartEffect</startEffecterDef>
      <startEffecterTicks>45</startEffecterTicks>
    </li>
  </comps>
</AbilityDef>
```

---

## 2. 必定命中弹丸 — 用 BF_CompAbilityEffect_DelayedHits 实现

> 旧版 `BF_Verb_AbilityRangedGuaranteed`（单发）与 `BF_Verb_MultiHit`（连发）已移除。
> 二者都可以用现成的 `BF_CompAbilityEffect_DelayedHits` 挂在普通 `Verb_CastAbility` 上复刻。

| 旧 Verb                          | 等价写法                                                                                              |
| ------------------------------- | ------------------------------------------------------------------------------------------------- |
| `BF_Verb_AbilityRangedGuaranteed` | `<verbClass>Verb_CastAbility</verbClass>` + `DelayedHits`：`delayTicks=0`、`hitCount=1`、`spawnPosition=Target` |
| `BF_Verb_MultiHit`               | `<verbClass>Verb_CastAbility</verbClass>` + `DelayedHits`：`delayTicks=0`、`hitCount=burstShotCount`、`hitInterval=ticksBetweenBurstShots` |

### 单发必定命中 XML 示例

```xml
<AbilityDef>
  <defName>PreciseShot</defName>
  <label>precise shot</label>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>24.9</range>
    <warmupTime>1</warmupTime>
  </verbProperties>
  <comps>
    <li Class="BF_Library.BF_CompProperties_DelayedHits">
      <delayTicks>0</delayTicks>
      <hitCount>1</hitCount>
      <projectileDef>Bullet_Frost</projectileDef>
      <spawnPosition>Target</spawnPosition>
    </li>
    <li Class="CompProperties_AbilityGiveHediff">
      <hediffDef>Stun</hediffDef>
      <severity>1</severity>
    </li>
  </comps>
</AbilityDef>
```

### 连发必定命中 XML 示例

```xml
<AbilityDef>
  <defName>BulletStorm</defName>
  <label>bullet storm</label>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>24.9</range>
    <warmupTime>0.5</warmupTime>
  </verbProperties>
  <comps>
    <li Class="BF_Library.BF_CompProperties_DelayedHits">
      <delayTicks>0</delayTicks>
      <hitCount>5</hitCount>
      <hitInterval>4</hitInterval>
      <projectileDef>Bullet_Frost</projectileDef>
      <spawnPosition>Target</spawnPosition>
    </li>
    <li Class="CompProperties_AbilityExplosion">
      <damageDef>Flame</damageDef>
      <damageAmount>30</damageAmount>
      <explosionRadius>2.9</explosionRadius>
    </li>
  </comps>
</AbilityDef>
```

### 必定命中机制

弹丸在 **目标格子** 生成并发射（`spawnPosition=Target`），飞行距离 ≈ 0，**下个 tick 即撞击目标**，无法被中途拦截。

---

## 3. 原版 Verb_CastAbility 的触发模型

用原版 Verb 释放能力时，触发顺序如下（这也是原版 Longjump 的模型）：

```
暖机完成
  └── Verb_CastAbility.TryCastShot()
        └── ability.Activate(target, dest)
              ├── PreActivate()            → 冷却/充能结算
              └── 按顺序执行每个 comp 的 Apply()
                    └── BF_CompProperties_Charge.Apply() → 启动冲刺（施法者变 flyer）
落地
  └── PawnFlyer.RespawnPawn()
        └── 对每个实现 ICompAbilityEffectOnJumpCompleted 的 comp 调用 OnJumpCompleted()
```

要点：

- 所有 comp 的 `Apply()` 都在**施放瞬间**执行，而不是落地时
- `BF_CompProperties_Charge` 一旦执行就会把施法者变成 flyer（`pawn.Spawned=false`），所以排在它后面的 comp 会看到"施法者已离场"
- 需要"落地才生效"的效果，必须让该 comp 实现原版接口 `ICompAbilityEffectOnJumpCompleted`
- 旧的 `activateOnCast` 两段触发机制已随 `BF_Verb_Charge` 一起移除，原版模型天然就是"施放段 + 落地回调"两段

---

## 4. BF_CompAbilityEffect_DelayedHits — 延迟多段命中

**类：** `BF_CompProperties_DelayedHits` / `BF_CompAbilityEffect_DelayedHits`  
**基类：** `CompProperties_AbilityEffect` / `CompAbilityEffect`  
**功能：** 施放后不立即出伤，延迟指定 tick 后在目标格子生成弹丸（飞行距离=0，不可被拦截），支持多段间隔。

### 字段（BF_CompProperties_DelayedHits）

| 字段                     | 类型                        | 默认值       | 说明                                     |
| ---------------------- | ------------------------- | --------- | -------------------------------------- |
| `delayTicks`           | `int`                     | `60`      | 施放到第一次命中的延迟（tick）                      |
| `hitCount`             | `int`                     | `1`       | 命中次数                                   |
| `hitInterval`          | `int`                     | `0`       | 每次命中之间的间隔（tick）                        |
| `projectileDef`        | `ThingDef`                | `null`    | **必填** — 弹丸 ThingDef                   |
| `spawnPosition`        | `ProjectileSpawnPosition` | `Target`  | 弹丸生成位置：`Target`（默认）／`Caster`           |
| `spawnOffset`          | `Vector3`                 | `(0,0,0)` | 相对生成位置的坐标偏移（如 `x=1, z=0` 向右一格）         |
| `spreadRadius`         | `float`                   | `0`       | 生成位置的随机散布半径（格）                          |
| `damageDefOverride`    | `DamageDef`               | `null`    | 覆盖弹丸伤害类型；留空用弹丸自身的                     |
| `damageAmountOverride` | `int`                     | `-1`      | 覆盖弹丸伤害数值；`<0` 时不覆盖，改用 `damageMultiplier` |
| `damageMultiplier`     | `float`                   | `1`       | 弹丸伤害倍率（未设 `damageAmountOverride` 时生效）   |

### 行为

```
Apply()  →  存储目标，开始计时
 └── CompTick()   ticksLeft--
      └── ticksLeft = 0  →  FireHit()
           ├── 目标格子生成弹丸（飞行距离=0）
           ├── hitsRemaining--
           └── ticksLeft = hitInterval（准备下次命中）
      └── hitsRemaining = 0  →  停止
```

> **`delayTicks <= 0` 时**：`Apply()` 内**立即同步发射**（不等 CompTick）。这样即使施法者紧接着进入 flyer（`pawn.Spawned=false`），第一发也能可靠出伤。

### 伤害覆盖

- `damageDefOverride`：直接覆盖弹丸的伤害类型，任意弹丸都生效。
- `damageAmountOverride` / `damageMultiplier`：覆盖/缩放伤害数值。生成弹丸时库会**临时**把弹丸 def 的 `thingClass` 换成库提供的子类（`BF_Projectile` / `BF_Bullet` / `BF_Projectile_Explosive`），生成后立即还原，因此对原版任意弹丸透明生效。
- 若弹丸使用自定义 `thingClass`（不是 `Projectile`/`Bullet`/`Projectile_Explosive`），无法覆盖数值，会打印警告并退回普通生成（`damageDefOverride` 仍然有效）。

优先级：`damageAmountOverride >= 0` 时直接使用该值；否则用 `基础伤害 × damageMultiplier`。

### 与其他 comps 叠加使用

`CompAbilityEffect_DelayedHits` 只负责"在延迟后发射弹丸"，不处理额外效果。它也可以用来复刻"必定命中单发/连发"（见 §2）。若要在弹丸之后追加爆炸/给 Hediff 等效果，按顺序排列 comps 即可；若要"落地才触发"，请让相关 comp 实现原版接口 `ICompAbilityEffectOnJumpCompleted`。

### XML 示例

```xml
<AbilityDef>
  <defName>TimeBomb</defName>
  <label>time bomb</label>
  <verbProperties>
    <verbClass>Verb_CastAbility</verbClass>
    <range>24.9</range>
    <warmupTime>1</warmupTime>
  </verbProperties>
  <comps>
    <!-- 延迟 120 tick 后发射 3 发弹丸，每发间隔 10 tick -->
    <li Class="BF_Library.BF_CompProperties_DelayedHits">
      <delayTicks>120</delayTicks>
      <hitCount>3</hitCount>
      <hitInterval>10</hitInterval>
      <projectileDef>Bullet_Frost</projectileDef>
    </li>
    <!-- 3 发全部打完后触发爆炸 -->
    <li Class="CompProperties_AbilityExplosion">
      <damageDef>Bomb</damageDef>
      <explosionRadius>3.9</explosionRadius>
    </li>
  </comps>
</AbilityDef>
```

---

## 5. 落地回调 — 原版 ICompAbilityEffectOnJumpCompleted

> 旧版 `BF_CompAbilityEffect_ActivateOnLanding` 与 `BF_CompAbilityEffect_DelayedJump` 已移除。

在施法者以 PawnFlyer 落地后，RimWorld 会对能力的所有 comp 调用原版接口 `ICompAbilityEffectOnJumpCompleted.OnJumpCompleted(origin, target)`。需要落地效果的 comp 直接实现该接口即可：

```csharp
public class MyLandingComp : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
{
    public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
    {
        // 落地时触发
    }
}
```

`BF_CompAbilityEffect_Charge` 本身就实现了它，用来处理 `postChargeStunTicks`。

### 与冲刺组合的触发顺序

原版模型下，`Verb_CastAbility.TryCastShot()` 会先 `ability.Activate()`（按顺序执行所有 comp 的 `Apply()`），随后冲刺才把施法者变成 flyer：

- 放在 `BF_CompProperties_Charge` **之前**的 comp：施法者尚在场时执行
- 放在**之后**的 comp：施法者已离场（flyer），`pawn.Spawned` 可能为 false
- 实现 `ICompAbilityEffectOnJumpCompleted` 的 comp：落地时执行

### 延迟冲刺（delayTicks）

`BF_CompProperties_Charge` 已内置延迟逻辑（内部用 `CompTick` 倒计时），不需要额外的 `DelayedJump` comp：

```xml
<li Class="BF_Library.BF_CompProperties_Charge">
  <chargeToTarget>true</chargeToTarget>
  <delayTicks>60</delayTicks>
</li>
```

### XML 示例

```xml
<comps>
  <!-- 施放瞬间触发：冲刺前给目标上标记 -->
  <li Class="CompProperties_AbilityGiveHediff">
    <hediffDef>Marked</hediffDef>
    <severity>1</severity>
  </li>

  <!-- 冲刺 comp 放最后 -->
  <li Class="BF_Library.BF_CompProperties_Charge">
    <chargeToTarget>true</chargeToTarget>
    <range>2.9</range>
  </li>
</comps>
```

---

## 6. 常见问题

### 6.1 冲刺后其它 comp 没反应 / 报错

**原因：** `BF_CompProperties_Charge` 在 `Apply()` 里会把施法者变成 flyer（`pawn.Spawned=false`），排在它后面的 comp 会在施法者离场状态下执行。

**解决：** 把 `BF_CompProperties_Charge` 放到 `<comps>` 最后；或让相关 comp 实现 `ICompAbilityEffectOnJumpCompleted`，把逻辑放到落地回调里。

### 6.2 冲刺成功但没有任何伤害/效果

**原因：** 能力由原版 `Verb_CastAbility` 正常激活，所有 comp 的 `Apply()` 都会在施放瞬间执行。若某个效果"看起来没触发"，通常是它被放在了冲刺 comp 之后、且依赖 `pawn.Spawned`。

**解决：** 调整 comp 顺序，或改用落地接口（见 §5）。

---

## 7. BF_CompAbilityEffect_HediffDetector — 检测/移除 Hediff + 伤害 + 施加 Hediff

**类：** `BF_CompProperties_HediffDetector` / `BF_CompAbilityEffect_HediffDetector`  
**基类：** `CompProperties_AbilityEffect` / `CompAbilityEffect`  
**功能：** 检测目标身上的"检测 Hediff"，然后可选地：**移除检测到的 Hediff**、**造成伤害**、**施加/修改结果 Hediff**。三者独立，可任意组合。

> 合并了旧的 `BF_CompAbilityEffect_HediffDetect` 与 `BF_CompAbilityEffect_RemoveAndDamage`，二者已移除。

### 字段（BF_CompProperties_HediffDetector）

**检测**

| 字段                  | 类型          | 默认值     | 说明                    |
| ------------------- | ----------- | ------- | --------------------- |
| `detectedHediffDef` | `HediffDef` | —       | 主检测 Hediff（与 extra 至少填一个） |
| `extraHediff_1/2/3` | `HediffDef` | `null`  | 额外检测 Hediff（severity 一并求和） |
| `requireDetected`   | `bool`      | `true`  | 无检测 Hediff 时跳过全部效果     |
| `validOnlyIfDetected` | `bool`    | `false` | 无检测 Hediff 时技能不可施放     |

**移除检测到的 Hediff**

| 字段                    | 类型     | 默认值     | 说明                |
| --------------------- | ------ | ------- | ----------------- |
| `removeDetectedHediff` | `bool` | `false` | 处理完后移除检测到的 Hediff |
| `removeApplyToTarget`  | `bool` | `true`  | 移除对目标生效           |
| `removeApplyToSelf`    | `bool` | `false` | 移除对施法者生效          |

**结果 Hediff**

| 字段                   | 类型          | 默认值     | 说明                         |
| -------------------- | ----------- | ------- | -------------------------- |
| `resultHediffDef`    | `HediffDef` | `null`  | 要施加/修改的 Hediff；留空 = 不施加    |
| `severityMultiplier` | `float`     | `1`     | 检测 severity 倍率             |
| `severityOffset`     | `float`     | `0`     | severity 偏移                |
| `missingSeverity`    | `float`     | `1`     | `requireDetected=false` 且无检测时用的 severity |
| `useFixedSeverity`   | `bool`      | `false` | `true` = 直接用 `fixedSeverity` |
| `fixedSeverity`      | `float`     | `1`     | 固定施加的 severity              |
| `hediffApplyToTarget` | `bool`     | `true`  | 施加 Hediff 对目标生效            |
| `hediffApplyToSelf`  | `bool`      | `false` | 施加 Hediff 对施法者生效           |

**伤害**

| 字段                         | 类型          | 默认值     | 说明              |
| -------------------------- | ----------- | ------- | --------------- |
| `damageDef`                | `DamageDef` | `null`  | 伤害类型；留空 = 不造成伤害 |
| `baseDamage`               | `int`       | `10`    | 基础伤害            |
| `damageSeverityMultiplier` | `float`     | `1`     | 每点检测 severity 追加的伤害 |
| `damageApplyToTarget`      | `bool`      | `true`  | 伤害对目标生效         |
| `damageApplyToSelf`        | `bool`      | `false` | 伤害对施法者生效        |

### 公式

```
总伤害 = baseDamage + 检测severity之和 × damageSeverityMultiplier
结果Hediff severity = (检测severity之和 或 missingSeverity) × severityMultiplier + severityOffset（或用 fixedSeverity）
```

### XML 示例

```xml
<!-- 检测 Charged/Burn：施加 Burned，造成 Frost 伤害，最后移除 -->
<li Class="BF_Library.BF_CompProperties_HediffDetector">
  <detectedHediffDef>Charged</detectedHediffDef>
  <extraHediff_1>Burn</extraHediff_1>
  <resultHediffDef>Burned</resultHediffDef>
  <severityMultiplier>2</severityMultiplier>
  <damageDef>Frost</damageDef>
  <baseDamage>5</baseDamage>
  <damageSeverityMultiplier>20</damageSeverityMultiplier>
  <removeDetectedHediff>true</removeDetectedHediff>
</li>

<!-- 纯伤害：不需要检测 -->
<li Class="BF_Library.BF_CompProperties_HediffDetector">
  <requireDetected>false</requireDetected>
  <damageDef>Blunt</damageDef>
  <baseDamage>15</baseDamage>
</li>
```

> 纯伤害/纯施加时记得 `requireDetected=false`。目标就是施法者自己时，`*ApplyToTarget` 与 `*ApplyToSelf` 合并只处理一次。

---

## 8. 特效路径速查

| 时机      | 方式                                                                                                                     | 来源                                          |
| ------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| 暖机中（蓄力） | `warmupEffecter` / `warmupMote` / `warmupSound` / `warmupStartSound`                                                   | AbilityDef 原生                               |
| 起步瞬间    | `startEffecterDef` / `startFleckDef` / `startSoundDef`                                                                 | `BF_CompProperties_Charge` 内置               |
| 飞行中     | `flightEffecterDef`                                                                                                    | verbProps 原生                                |
| 落地音效    | `soundLanding`                                                                                                         | verbProps 原生                                |
| 落地其他特效  | 实现 `ICompAbilityEffectOnJumpCompleted` 的 comp                                                                          | 原版接口                                        |
| 弹丸命中    | projectileDef 的 `projectile` 属性（`damageDef` / `explosionRadius` / `explosionEffect`）                                   | 弹丸 ThingDef                                 |
| 目标状态    | `CompProperties_AbilityGiveHediff` / `CompProperties_AbilityGiveMentalState` / `CompProperties_AbilityStun` 等          | comps                                       |
