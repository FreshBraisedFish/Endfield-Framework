using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>职业分类（近卫 / 重装 / 术士…），用于招募界面排序与分组图标。</summary>
    public class OperatorClassDef : Def
    {
        public OperatorSeriesDef series;
        public int sortingOrder = 100;
        public string iconPath;

        private Texture2D iconInt;

        public Texture2D Icon
        {
            get
            {
                if (iconInt == null && !iconPath.NullOrEmpty())
                {
                    iconInt = ContentFinder<Texture2D>.Get(iconPath, false);
                }
                return iconInt;
            }
        }
    }

    /// <summary>阵营 / 作品系列。</summary>
    public class OperatorSeriesDef : Def
    {
        public string iconPath;

        private Texture2D iconInt;

        public Texture2D Icon
        {
            get
            {
                if (iconInt == null && !iconPath.NullOrEmpty())
                {
                    iconInt = ContentFinder<Texture2D>.Get(iconPath, false);
                }
                return iconInt;
            }
        }
    }
}