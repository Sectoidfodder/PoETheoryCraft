using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PoETheoryCraft.Utils
{
    public enum ItemRarity
    {
        Normal,
        Magic,
        Rare
    }
    public enum ItemInfluence
    {
        Shaper,
        Elder,
        Redeemer,
        Hunter,
        Crusader,
        Warlord
    }
    public static class EnumConverter
    {
        public static Color RarityToColor(ItemRarity r)
        {
            switch (r)
            {
                case ItemRarity.Normal:
                    return Color.FromRgb(200, 200, 200);
                case ItemRarity.Magic:
                    return Color.FromRgb(136, 136, 255);
                default:
                    return Color.FromRgb(255, 255, 119);
            }
        }
        public static string InfToTag(ItemInfluence inf)
        {
            switch (inf)
            {
                case ItemInfluence.Shaper:
                    return "shaper_tag";
                case ItemInfluence.Elder:
                    return "elder_tag";
                case ItemInfluence.Redeemer:
                    return "redeemer_tag";
                case ItemInfluence.Hunter:
                    return "hunter_tag";
                case ItemInfluence.Warlord:
                    return "warlord_tag";
                default:
                    return "crusader_tag";
            }
        }
    }
}
