using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers;

public class SpellBarManager
{
    public static List<SpellBarRow> SpellBarRows = [new SpellBarRow().SetDummySpells(1), new SpellBarRow().SetDummySpells(11), new SpellBarRow().SetDummySpells(21)];
}

public class SpellBarRow()
{
    public SpellDefinition[] SpellSlot = new SpellDefinition[10];

    public SpellBarRow SetDummySpells(int s = 0)
    {
        for (int i = 0; i < 10; i++)
            SpellSlot[i] = SpellDefinition.FullIndexGetSpell(i + s);
        
        return this;
    }
    
    public SpellBarRow SetSpell(int slot, SpellDefinition spell)
    {
        SpellSlot[slot] = spell;

        return this;
    }
}