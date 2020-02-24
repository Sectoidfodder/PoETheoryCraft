# PoETheoryCraft
A crafting simulator for Path of Exile


v0.2.0
- Search/Filter Enabled -

> You can now search through your mass crafting results just like on a trade site.
      Example: https://imgur.com/a/qqjlVE8
> There are only a few pseudo stats pre-defined for searching at the moment.
      You can define your own in user_pseudo_stats.json, following the format of the examples.
      They're basically custom weighted searches that you can save for repeated use.

Simulation:
> Positive fossil modifiers now stack additively, to be consistent with test results.
> Corrupted Essence mods from Glyphic/Tangled now obey weight modifiers from other fossils.
> Prevented crafting +quality mods on an item that already has a +quality mod.
> Tentatively added 1% mod weight per 1% matching catalyst quality for all currencies.
      All I got from testing with 200 rolls is that the effect is very little, if any at all.
      It could be that catalyst quality only affects currencies that remove quality - that'll be the next thing to test.
      You can turn the bonus off or change it to anything in App.config

UI:
> Mod preview now shows each weight as a percent of total rollable affix weight.
> Made mod preview ignore existing mods on item when reroll currencies are selected (chaos, alch, alt, trans, essences, fossils).

Misc:
> Fixed problem exporting items to PoB due to bad rare item name formatting.  Right-click anywhere on an item or the mass-craft area for the option to copy one or all items to clipboard.



v0.1.0
- Initial Features -

Mechanics:
> All craftable gear and jewel bases.
> All relevant currencies and essences; all relevant fossils except Sanctified Fossil.
> Tentative Catalyst support - will change mod values, but no effect on roll weights (needs much more testing).

Functions:
> Mod View - Shows all mods that can be rolled on a given item and their weights; can force-add any mod to item.
> Crafted Mod View - Shows all bench crafts that can be applied and their costs.
> Mass Craft - Apply selected currency to an item any number of times (in parallel) and show results.
      Click on any stat line or item property to sort all results by that condition.
      Copy one item, one page, or all results to clipboard via right-click context menu.
> Post-roll Actions - Automatically apply specified bench mods, or maximize mod rolls (as if divining) for better comparison.
> Currency tracker - Tallies currency spent (except when mass crafting), can be reset whenever.


- In Progress (coming soon) -

> Stash page; moving items between bench, mass craft results, and stash.
> Trade-site-esque search/filter functions for mass craft results.
> Ability to lock mods on an item while rolling.
      Not like metamod locks, can't be ignored by essences/fossils.
      Useful for simulating Awakener's Orb (force add, lock two influenced mods, then roll w/ chaos).
      Also kinda usable for fractured mods, but the simulator is unlikely to ever support legacy rolls not in current game data.


- Looking for in-game Data/Info -

> Is Exalt/Aug/Regal consumed when used on an item that can roll no mods due to ilvl/metamod restrictions?
      If it's a Conqueror's Exalt, is influence added to the item?
> Catalyst effect on weights.
      Limited testing shows very little (if any) effect on chaos rolls.
      May only affect currencies that remove Catalyst quality.
> Everything about Beastiary crafting.
      They sometimes ignore ilvl - when exactly?  Beast level used instead?
      How do they interact with metamods?
      How do they interact with Catalyst quality?
> Sanctified Fossils.
      With enough data, can first determine *how* they affect mod weights by looking at frequency of tiers within the same group.
      Then determine *how much* the effect is by simulating rolls with different effect magnitudes and fitting to real rolls.
      Won't be doing any of this until mid-next-league when I'll actually have the currency to burn.
> Can Fishing Rods be bench-crafted?  Bench craft data doesn't include anything for that item class.



-- Credits --

> PyPoE for digging through the 30GB Content.ggpk for the relevant bits.
      https://github.com/OmegaK2/PyPoE
> RePoE for translating those relevant bits to a easy-to-consume format.
      https://github.com/brather1ng/RePoE
