using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers;

public class SpellBarManager
{
    public static List<SpellBarRow> SpellBarRows = [];

    private static string charPath;
    private static string fullSavePath;
    private static string presetPath;
    private const string SAVE_FILE = "SpellBar.json";

    public static SpellDefinition GetSpell(int row, int col)
    {
        return SpellBarRows[row].SpellSlot[col];
    }

    public static void Load()
    {
        charPath = ProfileManager.ProfilePath;
        presetPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "SpellBarPresets");
        fullSavePath = Path.Combine(charPath, SAVE_FILE);

        if (File.Exists(fullSavePath))
        {
            try
            {
                SpellBarRows = JsonSerializer.Deserialize(File.ReadAllText(fullSavePath), SpellBarRowsContext.Default.ListSpellBarRow);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                SetDefaults();
            }
        }
        else
        {
            SetDefaults();
        }
    }

    public static void Unload()
    {
        try
        {
            File.WriteAllText(fullSavePath, JsonSerializer.Serialize(SpellBarRows, SpellBarRowsContext.Default.ListSpellBarRow));;
        }
        catch(Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static void SetDefaults()
    {
        SpellBarRows = [new SpellBarRow().SetSpell(0, SpellDefinition.FullIndexGetSpell(29)).SetSpell(1, SpellDefinition.FullIndexGetSpell(11)).SetSpell(2, SpellDefinition.FullIndexGetSpell(22))];
    }
}

public class SpellBarRow()
{
    public SpellDefinition[] SpellSlot = new SpellDefinition[10];

    public int[] SpellSlotIds {
        get
        {
            List<int> ids = new List<int>();
            foreach (SpellDefinition spell in SpellSlot)
            {
                if (spell == null)
                    ids.Add(-2);
                else
                    ids.Add(spell.ID);
            }
            return ids.ToArray();
        }
        set
        {
            for (int i = 0; i < 10; i++)
            {
                if (value[i] == -2)
                    SpellSlot[i] = null;
                else
                    SpellSlot[i] = SpellDefinition.FullIndexGetSpell(value[i]);
            }
        }
    }
    
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

[JsonSerializable(typeof(List<SpellBarRow>), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class SpellBarRowsContext : JsonSerializerContext { }