using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>
    /// 干员生成器：完全按 OperatorDef 复现一个固定 pawn，不依赖任何外部框架。
    ///
    /// 流程：生成裸人 → 身份 → 外观 → 背景 → 技能 → 特性 → 清装备 → 穿装备 → hediff
    ///       → 落地 → 面部/英文名/额外能力。
    /// </summary>
    public static class OperatorGenerator
    {
        public static Pawn Generate(OperatorDef def, Map map, IntVec3 cell, Faction faction = null)
        {
            if (def == null)
            {
                Log.Error("[Endfield] Generation failed: OperatorDef is null");
                return null;
            }
            if (map == null)
            {
                Log.Error("[Endfield] Generation failed: no map available for " + def.defName);
                return null;
            }

            Pawn pawn = null;
            try
            {
                pawn = PawnGenerator.GeneratePawn(BuildRequest(def, faction ?? Faction.OfPlayer));

                ApplyIdentity(pawn, def);
                ApplyLooks(pawn, def);
                ApplyStory(pawn, def);
                ApplySkills(pawn, def);
                ApplyTraits(pawn, def);
                StripGear(pawn);
                ApplyGear(pawn, def);
                ApplyInnateHediffs(pawn, def);

                GenSpawn.Spawn(pawn, FindSpawnCell(map, cell), map, WipeMode.Vanish);

                ApplyModExtensions(pawn, def);
            }
            catch (Exception e)
            {
                Log.Error("[Endfield] Error while generating " + def.defName + ": " + e);
            }
            return pawn;
        }

        private static PawnGenerationRequest BuildRequest(OperatorDef def, Faction faction)
        {
            return new PawnGenerationRequest(
                kind: PawnKindDefOf.Colonist,
                faction: faction,
                context: PawnGenerationContext.PlayerStarter,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                fixedGender: def.isMale ? Gender.Male : Gender.Female,
                forcedXenotype: def.xenoType,
                forceNoGear: true);
        }

        // ---------------------------------------------------------------- 身份

        private static void ApplyIdentity(Pawn pawn, OperatorDef def)
        {
            string given = def.GivenName;
            string nick = def.DisplayName;
            string family = def.FamilyName;
            pawn.Name = new NameTriple(given, nick, family);

            pawn.gender = def.isMale ? Gender.Male : Gender.Female;

            int bio = def.age > 0 ? def.age : 20;
            int chrono = def.realAge > 0 ? def.realAge : bio;
            if (chrono < bio)
            {
                chrono = bio;
            }
            pawn.ageTracker.AgeBiologicalTicks = (long)bio * 3600000L;
            pawn.ageTracker.AgeChronologicalTicks = (long)chrono * 3600000L;
        }

        // ---------------------------------------------------------------- 外观

        private static void ApplyLooks(Pawn pawn, OperatorDef def)
        {
            Pawn_StoryTracker story = pawn.story;
            if (story == null)
            {
                return;
            }

            if (def.bodyTypeDef != null)
            {
                story.bodyType = def.bodyTypeDef;
            }
            else if (!def.isMale)
            {
                story.bodyType = BodyTypeDefOf.Female;
            }

            if (def.headTypeDef != null)
            {
                story.headType = def.headTypeDef;
            }

            story.hairDef = def.hair ?? HairDefOf.Bald;
            story.skinColorOverride = def.skinColor;
            story.HairColor = def.hairColor;

            if (pawn.style != null)
            {
                pawn.style.beardDef = def.beard ?? BeardDefOf.NoBeard;
            }
        }

        // ---------------------------------------------------------------- 背景

        private static void ApplyStory(Pawn pawn, OperatorDef def)
        {
            if (def.childHood != null)
            {
                pawn.story.Childhood = def.childHood;
            }
            if (pawn.ageTracker.AgeBiologicalYears >= 20)
            {
                pawn.story.Adulthood = def.adultHood;
            }
            else
            {
                pawn.story.Adulthood = null;
            }
            pawn.Notify_DisabledWorkTypesChanged();
        }

        // ---------------------------------------------------------------- 技能

        private static void ApplySkills(Pawn pawn, OperatorDef def)
        {
            if (pawn.skills == null)
            {
                return;
            }

            // 不能清空 skills 列表：原版任何 GetSkill(未登记技能) 都会报
            // "Did not find skill of def ..."。这里保留全部记录，只把数值归零后重写。
            foreach (SkillRecord record in pawn.skills.skills)
            {
                record.Level = 0;
                record.passion = Passion.None;
            }

            if (def.skills == null)
            {
                return;
            }

            foreach (SkillEntry entry in def.skills)
            {
                if (entry == null || entry.skill == null)
                {
                    continue;
                }
                SkillRecord record = pawn.skills.GetSkill(entry.skill);
                if (record == null)
                {
                    continue;
                }
                record.passion = entry.EffectivePassion;
                record.Level = entry.level;
            }
        }

        // ---------------------------------------------------------------- 特性

        private static void ApplyTraits(Pawn pawn, OperatorDef def)
        {
            if (def.traits == null || def.traits.Count == 0 || pawn.story?.traits == null)
            {
                return;
            }

            pawn.story.traits.allTraits.Clear();
            foreach (TraitEntry entry in def.traits)
            {
                if (entry == null || entry.def == null)
                {
                    continue;
                }
                pawn.story.traits.GainTrait(new Trait(entry.def, entry.degree, false), false);
            }
        }

        // ---------------------------------------------------------------- 装备

        private static void StripGear(Pawn pawn)
        {
            pawn.apparel?.DestroyAll(DestroyMode.Vanish);
            pawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
            pawn.inventory?.innerContainer?.ClearAndDestroyContents(DestroyMode.Vanish);
        }

        private static void ApplyGear(Pawn pawn, OperatorDef def)
        {
            if (def.apparels != null && pawn.apparel != null)
            {
                foreach (ThingDef apDef in def.apparels)
                {
                    if (apDef == null)
                    {
                        continue;
                    }
                    Apparel apparel = (Apparel)ThingMaker.MakeThing(apDef, GenStuff.DefaultStuffFor(apDef));
                    CompBiocodable bio = apparel.TryGetComp<CompBiocodable>();
                    if (bio != null)
                    {
                        bio.CodeFor(pawn);
                    }
                    pawn.apparel.Wear(apparel, dropReplacedApparel: true, locked: false);
                    pawn.outfits?.forcedHandler.SetForced(apparel, true);
                }
            }

            if (def.weapon != null && pawn.equipment != null)
            {
                ThingWithComps weapon = (ThingWithComps)ThingMaker.MakeThing(def.weapon, GenStuff.DefaultStuffFor(def.weapon));
                pawn.equipment.AddEquipment(weapon);
            }

            if (def.items != null && pawn.inventory != null)
            {
                foreach (ItemEntry item in def.items)
                {
                    if (item?.thingDef == null)
                    {
                        continue;
                    }
                    Thing thing = ThingMaker.MakeThing(item.thingDef, GenStuff.DefaultStuffFor(item.thingDef));
                    thing.stackCount = Mathf.Max(1, item.count);
                    if (!pawn.inventory.innerContainer.TryAdd(thing))
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- hediff

        private static void ApplyInnateHediffs(Pawn pawn, OperatorDef def)
        {
            if (pawn.health?.hediffSet == null)
            {
                return;
            }

            foreach (Hediff h in pawn.health.hediffSet.hediffs.ToList())
            {
                if (h.def.isBad)
                {
                    pawn.health.RemoveHediff(h);
                }
            }

            if (def.hediffInate == null)
            {
                return;
            }

            foreach (InnateHediff entry in def.hediffInate)
            {
                if (entry == null || entry.hediff == null)
                {
                    continue;
                }

                BodyPartRecord part = null;
                if (entry.part != null)
                {
                    part = pawn.RaceProps.body.AllParts.FirstOrDefault(p => p.def == entry.part);
                    if (part == null)
                    {
                        Log.Warning("[Endfield] " + def.defName + ": body part " + entry.part.defName + " not found, applying hediff " + entry.hediff.defName + " to the whole body");
                    }
                }

                Hediff added = pawn.health.AddHediff(entry.hediff, part, null, null);
                if (added != null)
                {
                    added.Severity = entry.DefSeverity;
                }
            }
        }

        // ---------------------------------------------------------------- 落点

        private static IntVec3 FindSpawnCell(Map map, IntVec3 cell)
        {
            if (cell.IsValid && cell.InBounds(map) && cell.Standable(map))
            {
                return cell;
            }
            IntVec3 root = cell.IsValid && cell.InBounds(map) ? cell : map.Center;
            IntVec3 found = CellFinder.RandomClosewalkCellNear(root, map, 6);
            return found.IsValid ? found : CellFinder.RandomCell(map);
        }

        // ---------------------------------------------------------------- modExtensions

        private static void ApplyModExtensions(Pawn pawn, OperatorDef def)
        {
            FA_Extension face = def.GetModExtension<FA_Extension>();
            if (face != null)
            {
                FaceApplicator.Apply(pawn, face);
            }

            EF_AbilityExtension abilities = def.GetModExtension<EF_AbilityExtension>();
            if (abilities?.extraAbilities == null || pawn.abilities == null)
            {
                return;
            }

            foreach (string abilityName in abilities.extraAbilities)
            {
                AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityName);
                if (abilityDef == null)
                {
                    Log.Warning("[Endfield] " + def.defName + ": AbilityDef not found: " + abilityName);
                    continue;
                }
                try
                {
                    pawn.abilities.GainAbility(abilityDef);
                }
                catch (Exception e)
                {
                    Log.Error("[Endfield] Failed to grant ability " + abilityName + ": " + e.Message);
                }
            }
        }

    }
}