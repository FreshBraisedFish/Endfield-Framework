using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>
    /// 面部表情（UNAGI FacialAnimation）部件配置。
    /// 命名空间与类名保持不变，方便已有干员 mod 直接沿用 &lt;li Class="Endfield.FA_Extension"&gt;。
    /// </summary>
    public class FA_Extension : DefModExtension
    {
        public string headTypeDef;
        public string eyeTypeDef;
        public string browTypeDef;
        public string lidTypeDef;
        public string mouthTypeDef;
        public string skinTypeDef;
        public string eyeballColorDef;
        public string eyeballShapeDef;
        public float eyeballColorR = -1f;
        public float eyeballColorG = -1f;
        public float eyeballColorB = -1f;
        public float eyeballColorA = -1f;
        public float eyeballColorR2 = -1f;
        public float eyeballColorG2 = -1f;
        public float eyeballColorB2 = -1f;
        public float eyeballColorA2 = -1f;

        // 名字/简介的本地化请用原版 DefInjected：
        //   Languages/English/DefInjected/Endfield.OperatorDef/XXX.xml
        //   <EF_Recruit_XXX.name>...</EF_Recruit_XXX.name>
        //   <EF_Recruit_XXX.nickname>...</EF_Recruit_XXX.nickname>
        //   <EF_Recruit_XXX.description>...</EF_Recruit_XXX.description>
    }

    /// <summary>招募终端的参数（挂在 ThingDef 的 modExtensions 上）。</summary>
    public class RecruitConsoleExtension : DefModExtension
    {
        public int silverCost;
        public int cooldownTicks = 600;
    }

    /// <summary>额外能力：招募完成后直接授予这些 AbilityDef。</summary>
    public class EF_AbilityExtension : DefModExtension
    {
        public List<string> extraAbilities;
    }
}