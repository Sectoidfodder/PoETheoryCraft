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
        public static string CatalystToTag(string c)
        {
            string tag;
            switch (c)
            {
                case "abrasive":
                    tag = "jewellery_attack";
                    break;
                case "fertile":
                    tag = "jewellery_resource";
                    break;
                case "imbued":
                    tag = "jewellery_defense";
                    break;
                case "intrinsic":
                    tag = "jewellery_attribute";
                    break;
                case "prismatic":
                    tag = "jewellery_resistance";
                    break;
                case "tempering":
                    tag = "jewellery_elemental";
                    break;
                case "turbulent":
                    tag = "jewellery_caster";
                    break;
                default:
                    tag = null;
                    break;
            }
            return tag;
        }
        //returns all extra tags that a catalyst uses on top of its dedicated tag
        public static IList<string> GetCatalystAuxTags(string tag)
        {
            switch (tag)
            {
                case "jewellery_attack":
                    return new List<string>() { "attack" };
                case "jewellery_caster":
                    return new List<string>() { "caster" };
                case "jewellery_resource":
                    return new List<string>() { "life", "mana" };
                case "jewellery_defense":
                    //return new List<string>() { "defences" };     //doesn't seem to count according to PoEDB
                case "jewellery_attribute":
                case "jewellery_resistance":
                case "jewellery_elemental":
                default:
                    return new List<string>();
            }
        }
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
