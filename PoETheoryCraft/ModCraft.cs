using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoETheoryCraft.DataClasses;
using PoETheoryCraft.Utils;

namespace PoETheoryCraft
{
    //A single roll of one stat, made from a PoEModStat template + RNG roll
    public class ModRoll
    {
        public string ID { get; }
        public int Max { get; }
        public int Min { get; }
        public int Roll { get; set; }
        public ModRoll(PoEModStat stat)
        {
            ID = stat.id ?? "";
            Max = stat.max;
            Min = stat.min;
            Roll = RNG.Gen.Next(Min, Max + 1);
        }
        private ModRoll(ModRoll r)
        {
            ID = r.ID;
            Max = r.Max;
            Min = r.Min;
            Roll = r.Roll;
        }
        public ModRoll Copy()
        {
            return new ModRoll(this);
        }
    }
    //Represents one mod on an item, made from a PoEModData template
    public class ModCraft
    {
        public string SourceData { get; }       //key to PoEModData that this is derived from
        public IList<ModRoll> Stats { get; }    //actual statlines granted, including rolls
        public bool IsLocked { get; set; }      //for fractured mods, metamod-locked affixes
        private int _quality;
        public int Quality                      //value maintained by parent item, applicable catalyst quality
        {
            get { return _quality; }
            set
            {
                _quality = value;
                Modified = true;
            }
        }        

        //cache return value for faster repeated calls, as long as mod hasn't changed
        private string ToStringCache;
        private bool Modified = true;
        public ModCraft(PoEModData data)
        {
            SourceData = data.key;
            Stats = new List<ModRoll>();
            if (data.stats != null)
            {
                foreach (PoEModStat s in data.stats)
                {
                    Stats.Add(new ModRoll(s));
                }
            }
            IsLocked = false;
            Quality = 0;
        }
        private ModCraft(ModCraft m)
        {
            SourceData = m.SourceData;
            Stats = new List<ModRoll>();
            foreach (ModRoll r in m.Stats)
            {
                Stats.Add(r.Copy());
            }
            IsLocked = m.IsLocked;
            Quality = m.Quality;
        }
        public ModCraft Copy()
        {
            return new ModCraft(this);
        }
        public void Maximize()
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                Stats[i].Roll = Stats[i].Max;
            }
            Modified = true;
        }
        public void Reroll()
        {
            for (int i = 0; i < Stats.Count; i++)
            {
                Stats[i].Roll = RNG.Gen.Next(Stats[i].Min, Stats[i].Max + 1);
            }
            Modified = true;
        }
        public override string ToString()
        {
            if (!Modified)
                return ToStringCache;
            //unlike its parent PoEModData, translation here is done live to allow roll-dependent syntax
            ToStringCache = StatTranslator.TranslateModCraft(this, Quality);
            if (ToStringCache.Length <= 0)
                ToStringCache = CraftingDatabase.AllMods[SourceData].name;
            Modified = false;
            return ToStringCache;
        }
    }
}
