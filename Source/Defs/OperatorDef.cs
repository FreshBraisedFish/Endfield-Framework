using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>
    /// 干员模板。一份 XML = 一个固定人物：身份、外观、装备、成长、数值全部写死在 Def 里，
    /// 招募时由 OperatorGenerator 按模板精确复现，不走随机生成。
    /// </summary>
    public class OperatorDef : Def
    {
        // ---------- 身份 ----------
        public string name;          // 名
        public string nickname;      // 昵称（显示名）
        public string surname;       // 姓
        public bool isMale;
        public int age = 16;         // 生理年龄
        public int realAge = -1;     // 历法年龄，<0 时跟随 age

        // ---------- 外观 ----------
        public BodyTypeDef bodyTypeDef;
        public HeadTypeDef headTypeDef;
        public HairDef hair;
        public BeardDef beard;
        public Color skinColor = new Color(0.98f, 0.90f, 0.82f, 1f);
        public Color hairColor = Color.white;
        public XenotypeDef xenoType;

        // ---------- 装备 ----------
        public ThingDef weapon;
        public List<ThingDef> apparels;
        public List<ItemEntry> items;

        // ---------- 背景 ----------
        public BackstoryDef childHood;
        public BackstoryDef adultHood;

        // ---------- 数值 ----------
        public List<SkillEntry> skills;
        public List<TraitEntry> traits;
        public List<InnateHediff> hediffInate;

        // ---------- 头像 / 立绘（可选，仅用于招募界面展示） ----------
        public string portraitPath;
        public string standPath;

        private Texture2D portraitInt;

        public Texture2D Portrait
        {
            get
            {
                if (portraitInt == null && !portraitPath.NullOrEmpty())
                {
                    portraitInt = ContentFinder<Texture2D>.Get(portraitPath, false);
                }
                return portraitInt;
            }
        }

        // ---------- 分类 ----------
        public OperatorClassDef operatorType;
        public OperatorSeriesDef series;

        // ---------- 显示 ----------
        public string DisplayName => nickname.NullOrEmpty() ? (name.NullOrEmpty() ? defName : name) : nickname;

        public string GivenName => name.NullOrEmpty() ? defName : name;

        public string FamilyName => surname ?? string.Empty;

        public string Blurb => description.NullOrEmpty() ? (label.NullOrEmpty() ? defName : label) : description;

        public string ClassLabel => operatorType != null ? operatorType.label : string.Empty;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (weapon != null && !weapon.IsWeapon)
            {
                yield return defName + ": <weapon> does not point to a weapon ThingDef (" + weapon.defName + ")";
            }
            if (apparels != null)
            {
                foreach (ThingDef ap in apparels.Where(a => a != null && a.apparel == null))
                {
                    yield return defName + ": <apparels> entry " + ap.defName + " is not apparel";
                }
            }
        }
    }

    /// <summary>XML 里的 <c>&lt;skills&gt;&lt;li&gt;&lt;skill&gt;Shooting&lt;/skill&gt;…</c> 条目。</summary>
    public class SkillEntry
    {
        public SkillDef skill;
        public int level;
        public Passion passion;
        public int fireLevel;   // 兼容旧写法 0/1/2

        public Passion EffectivePassion
        {
            get
            {
                if (passion != Passion.None)
                {
                    return passion;
                }
                return (Passion)Mathf.Clamp(fireLevel, 0, 2);
            }
        }
    }

    /// <summary>特性条目：<c>&lt;li&gt;&lt;def&gt;Tough&lt;/def&gt;&lt;/li&gt;</c>，可选 degree。</summary>
    public class TraitEntry
    {
        public TraitDef def;
        public int degree;
    }

    /// <summary>背包物品条目：<c>&lt;li&gt;&lt;thingDef&gt;MedicineIndustrial&lt;/thingDef&gt;&lt;count&gt;2&lt;/count&gt;&lt;/li&gt;</c></summary>
    public class ItemEntry
    {
        public ThingDef thingDef;
        public int count = 1;
    }

    /// <summary>天生伤痕 / 器官（RJW 等 mod 的体型 hediff）。</summary>
    public class InnateHediff
    {
        public HediffDef hediff;
        public BodyPartDef part;
        public string partCustomLabel;
        public float severity;
        public float serverity = -1f;   // 兼容 MIS 时代的拼写

        public float DefSeverity => serverity >= 0f ? serverity : (severity > 0f ? severity : 1f);

        public override string ToString() => (hediff != null ? hediff.defName : "null") + (part != null ? "@" + part.defName : "");
    }
}