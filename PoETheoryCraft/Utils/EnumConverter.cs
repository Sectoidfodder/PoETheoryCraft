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
        public static IList<string> InfToNames(ItemInfluence inf)
        {
            switch (inf)
            {
                case ItemInfluence.Shaper:
                    return new List<string>() { "The Shaper's", "of Shaping" };
                case ItemInfluence.Elder:
                    return new List<string>() { "Eldritch", "of the Elder" };
                case ItemInfluence.Redeemer:
                    return new List<string>() { "Redeemer's", "of Redemption" };
                case ItemInfluence.Hunter:
                    return new List<string>() { "Hunter's", "of the Hunt" };
                case ItemInfluence.Warlord:
                    return new List<string>() { "Warlord's", "of the Conquest" };
                default:
                    return new List<string>() { "Crusader's", "of the Crusade" };
            }
        }
    }
}
