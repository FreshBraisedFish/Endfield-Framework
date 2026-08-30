# BF Ability Extras 参考文档

命名空间：`BF_Library`  
源文件：`[BF]-Library\Assemblies\BF_Library\BF_Library\`

---

# 

## 1. BF_Verb_Charge — 冲刺位移

**类：** `BF_Verb_Charge` / `BF_VerbProperties_Charge`  
**基类：** `Verb_CastAbility`  
**功能：** 施法者沿直线向目标冲刺一段距离，落地后触发 `ICompAbilityEffectOnJumpCompleted` comps。

### 可用字段（BF_VerbProperties_Charge）

| 字段                   | 类型            | 默认值         | 说明                            |
| -------------------- | ------------- | ----------- | ----------------------------- |
| `chargeToTarget`     | `bool`        | `false`     | `true`=直冲目标格子，`false`=冲刺固定距离  |
| `chargeFlyerDef`     | `ThingDef`    | `PawnFlyer` | 自定义飞行器 ThingDef（控制速度/高度/落地硬直） |
| `startEffecterDef`   | `EffecterDef` | `null`      | 起步时在施法者位置生成的 Effecter         |
| `startEffecterTicks` | `int`         | `30`        | 起步 Effecter 维持的 tick 数        |
| `startFleckDef`      | `FleckDef`    | `null`      | 起步时在施法者位置生成的一次性粒子             |
| `startSoundDef`      | `SoundDef`    | `null`      | 起步时在施法者位置播放的音效                |

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
- `TryCastShot()` **不调用** `ability.Activate()`，而是**结束当前 job** 再启动冲刺，防止落地后 job 被恢复导致二次冲刺
- 落地后需要 `BF_CompAbilityEffect_ActivateOnLanding` 或其它实现 `ICompAbilityEffectOnJumpCompleted` 的 comp 来触发效果

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
  <verbProperties Class="BF_Library.BF_VerbProperties_Charge">
    <verbClass>BF_Library.BF_Verb_Charge</verbClass>
    <range>7.9</range>
    <warmupTime>0.3</warmupTime>
    <chargeToTarget>false</chargeToTarget>
    <chargeFlyerDef>ChargeFlyer_Fast</chargeFlyerDef>
    <flightEffecterDef>Charge_GroundDust</flightEffecterDef>
    <soundLanding>Charge_Impact</soundLanding>
    <startSoundDef>Charge_Whoosh</startSoundDef>
    <startFleckDef>DustPuff</startFleckDef>
    <startEffecterDef>Charge_StartEffect</startEffecterDef>
    <startEffecterTicks>45</startEffecterTicks>
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
  </comps>
</AbilityDef>
```

---

## 2. BF_Verb_AbilityRangedGuaranteed — 必定命中远程

**类：** `BF_Verb_AbilityRangedGuaranteed`  
**基类：** `Verb_CastAbility`  
**功能：** 暖机完成后，在目标格子生成一发弹丸（飞行距离=0，不可被拦截），然后触发所有 comps 的效果。

### 可用字段（继承 verbProps）

| 字段                  | 说明                   |
| ------------------- | -------------------- |
| `defaultProjectile` | **必填** — 弹丸 ThingDef |
| `warmupTime`        | 前摇时间                 |
| `range`             | 射程                   |
| `targetParams`      | 目标筛选                 |

### 行为

```
TryCastShot()
  ├── FireProjectile()           ← 目标格子生成弹丸，飞行距离=0，不可拦截
  └── ability.Activate()          ← 触发所有 comps
```

### XML 示例

```xml
<AbilityDef>
  <defName>PreciseShot</defName>
  <label>precise shot</label>
  <verbProperties>
    <verbClass>BF_Library.BF_Verb_AbilityRangedGuaranteed</verbClass>
    <defaultProjectile>Bullet_Frost</defaultProjectile>
    <range>24.9</range>
    <warmupTime>1</warmupTime>
  </verbProperties>
  <comps>
    <li Class="CompProperties_AbilityGiveHediff">
      <hediffDef>Stun</hediffDef>
      <severity>1</severity>
    </li>
  </comps>
</AbilityDef>
```

---

## 3. BF_Verb_MultiHit — 必定命中连发

**类：** `BF_Verb_MultiHit`  
**基类：** `Verb_CastAbility`  
**功能：** 利用 burst 系统在目标格子连续生成多发弹丸（飞行距离=0，不可被拦截），最后一发射完后触发 comps。

### 可用字段（继承 verbProps）

| 字段                       | 说明                        |
| ------------------------ | ------------------------- |
| `defaultProjectile`      | **必填** — 弹丸 ThingDef      |
| `burstShotCount`         | 连击次数                      |
| `ticksBetweenBurstShots` | 每发间隔 tick（1 tick = 1/60秒） |
| `warmupTime`             | 前摇时间                      |
| `range`                  | 射程                        |
| `targetParams`           | 目标筛选                      |

### 行为

```
WarmupComplete()
  └── TryCastShot() #1     目标格子生成弹丸, shotsFired=1
  └── 等待 ticksBetweenBurstShots
  └── TryCastShot() #2     目标格子生成弹丸, shotsFired=2
  └── 等待 ticksBetweenBurstShots
  └── TryCastShot() #3     目标格子生成弹丸, shotsFired=3
  └── ...
  └── TryCastShot() #N     目标格子生成弹丸 + ability.Activate()  ← 最后一发触发 comps
```

### XML 示例

```xml
<AbilityDef>
  <defName>BulletStorm</defName>
  <label>bullet storm</label>
  <verbProperties>
    <verbClass>BF_Library.BF_Verb_MultiHit</verbClass>
    <defaultProjectile>Bullet_Frost</defaultProjectile>
    <burstShotCount>5</burstShotCount>
    <ticksBetweenBurstShots>4</ticksBetweenBurstShots>
    <warmupTime>0.5</warmupTime>
    <range>24.9</range>
  </verbProperties>
  <comps>
    <li Class="CompProperties_AbilityExplosion">
      <damageDef>Flame</damageDef>
      <damageAmount>30</damageAmount>
      <explosionRadius>2.9</explosionRadius>
    </li>
  </comps>
</AbilityDef>
```

### 三者的必定命中机制

弹丸均在 **目标格子** 生成并发射，飞行距离 ≈ 0，**下个 tick 即撞击目标**，无法被中途拦截。

### Verb_MultiHit + Verb_AbilityRangedGuaranteed 对比

|                      | `Verb_AbilityRangedGuaranteed` | `Verb_MultiHit`          |
| -------------------- | ------------------------------ | ------------------------ |
| 发射数量                 | 1 发                            | N 发（`burstShotCount`）    |
| 发射间隔                 | 无                              | `ticksBetweenBurstShots` |
| comps 触发时机           | 发射后立即触发                        | 最后一发射完后触发                |
| `burstShotCount=1` 时 | —                              | 行为等同于 Guaranteed         |

---

## 4. BF_CompAbilityEffect_DelayedHits — 延迟多段命中

**类：** `BF_CompProperties_DelayedHits` / `BF_CompAbilityEffect_DelayedHits`  
**基类：** `CompProperties_AbilityEffect` / `CompAbilityEffect`  
**功能：** 施放后不立即出伤，延迟指定 tick 后在目标格子生成弹丸（飞行距离=0，不可被拦截），支持多段间隔。

### 字段（BF_CompProperties_DelayedHits）

| 字段              | 类型                        | 默认值       | 说明                             |
| --------------- | ------------------------- | --------- | ------------------------------ |
| `delayTicks`    | `int`                     | `60`      | 施放到第一次命中的延迟（tick）              |
| `hitCount`      | `int`                     | `1`       | 命中次数                           |
| `hitInterval`   | `int`                     | `0`       | 每次命中之间的间隔（tick）                |
| `projectileDef` | `ThingDef`                | `null`    | **必填** — 弹丸 ThingDef           |
| `spawnPosition` | `ProjectileSpawnPosition` | `Target`  | 弹丸生成位置：`Target`（默认）／`Caster`   |
| `spawnOffset`   | `Vector3`                 | `(0,0,0)` | 相对生成位置的坐标偏移（如 `x=1, z=0` 向右一格） |

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

### 与其他 comps 叠加使用

`CompAbilityEffect_DelayedHits` 只负责"在延迟后发射弹丸"，不处理额外效果。如果你需要在延迟+多段命中**之后**再触发爆炸/给Hediff等效果，用 `ICompAbilityEffectOnJumpCompleted` 配合 CompProperties_AbilityExplosion／GiveHediff 等。

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

## 5. BF_CompAbilityEffect_ActivateOnLanding — 落地触发 Comp

**类：** `BF_CompProperties_ActivateOnLanding` / `BF_CompAbilityEffect_ActivateOnLanding`  
**基类：** `CompProperties_AbilityEffect` / `CompAbilityEffect` + `ICompAbilityEffectOnJumpCompleted`  
**功能：** 冲刺落地后自动调用 `ability.Activate()`，触发所有其他 comps 的 `Apply()`。  
**必须作为 `BF_Verb_Charge` 能力的第一个 comp 使用。**

### 字段

无额外字段。

### 行为

```
OnJumpCompleted()  ← 落地回调
  └── ability.Activate(target, dest)
        ├── PreActivate()     → 冷卻計時
        ├── DelayedHits.Apply()   → 开始延迟计时
        ├── GiveHediff.Apply()   → 立即添加Hediff
        └── ...
```

### XML 示例

```xml
<comps>
  <!-- 必须第一个放 -->
  <li Class="BF_Library.BF_CompProperties_ActivateOnLanding" />

  <li Class="BF_Library.BF_CompProperties_DelayedHits">
    <delayTicks>10</delayTicks>
    <hitCount>2</hitCount>
    <hitInterval>30</hitInterval>
    <projectileDef>Bullet_Revolver</projectileDef>
  </li>
  <li Class="CompProperties_AbilityGiveHediff">
    <hediffDef>Hypothermia</hediffDef>
    <severity>0.2</severity>
  </li>
</comps>
```

---

## 6. 常见问题

### Comps 无效果

**现象：** 冲刺成功但没有任何伤害/效果。

**原因：** `BF_Verb_Charge.TryCastShot()` 不调用 `ability.Activate()`，需要落地后由 `BF_CompAbilityEffect_ActivateOnLanding`（或其他 `ICompAbilityEffectOnJumpCompleted` comp）来触发。缺少这个 comp 时所有效果都不会执行。

---

## 7. BF_CompAbilityEffect_RemoveAndDamage — 移除Hediff造成伤害

**类：** `BF_CompProperties_RemoveAndDamage` / `BF_CompAbilityEffect_RemoveAndDamage`  
**基类：** `CompProperties_AbilityEffect` / `CompAbilityEffect`  
**功能：** 移除目标身上的指定 Hediff，根据其 Severity 倍率追加伤害。

### 字段（BF_CompProperties_RemoveAndDamage）

| 字段                   | 类型          | 默认值     | 说明                   |
| -------------------- | ----------- | ------- | -------------------- |
| `hediffDef`          | `HediffDef` | —       | **必填** — 要移除的 Hediff |
| `damageDef`          | `DamageDef` | —       | **必填** — 伤害类型        |
| `baseDamage`         | `int`       | `10`    | 基础伤害                 |
| `severityMultiplier` | `float`     | `1`     | 每点 severity 追加的伤害倍率  |
| `applyToTarget`      | `bool`      | `true`  | 是否对目标生效              |
| `applyToSelf`        | `bool`      | `false` | 是否对施法者生效             |

### 伤害公式

```
总伤害 = baseDamage + severity × severityMultiplier
```

### XML 示例

```xml
<li Class="BF_Library.BF_CompProperties_RemoveAndDamage">
  <hediffDef>Hypothermia</hediffDef>
  <damageDef>Frost</damageDef>
  <baseDamage>5</baseDamage>
  <severityMultiplier>20</severityMultiplier>
  <applyToTarget>true</applyToTarget>
</li>
```

若目标 Hypothermia severity = 0.8，则造成 `5 + 0.8 × 20 = 21` 点 Frost 伤害。

---

## 8. 特效路径速查

| 时机      | 方式                                                                                                                     | 来源                                          |
| ------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| 暖机中（蓄力） | `warmupEffecter` / `warmupMote` / `warmupSound` / `warmupStartSound`                                                   | AbilityDef 原生                               |
| 起步瞬间    | `startEffecterDef` / `startFleckDef` / `startSoundDef`                                                                 | `BF_VerbProperties_Charge` 内置               |
| 飞行中     | `flightEffecterDef`                                                                                                    | verbProps 原生                                |
| 落地音效    | `soundLanding`                                                                                                         | verbProps 原生                                |
| 落地其他特效  | `CompProperties_AbilityExplosion` / `CompProperties_AbilityFleckOnTarget` / `CompProperties_AbilityEffecterOnTarget` 等 | comps + `ICompAbilityEffectOnJumpCompleted` |
| 弹丸命中    | projectileDef 的 `projectile` 属性（`damageDef` / `explosionRadius` / `explosionEffect`）                                   | 弹丸 ThingDef                                 |
| 目标状态    | `CompProperties_AbilityGiveHediff` / `CompProperties_AbilityGiveMentalState` / `CompProperties_AbilityStun` 等          | comps                                       |
