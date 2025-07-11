using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.UI.Gumps.GridHighLight
{
    public class GridHighlightData
    {
        private static GridHighlightData[] allConfigs;
        private readonly GridHighlightSetupEntry _entry;
        
        private static readonly Queue<uint> _queue = new();
        private static bool hasQueuedItems;

        public static GridHighlightData[] AllConfigs
        {
            get
            {
                if (allConfigs != null)
                    return allConfigs;

                var setup = ProfileManager.CurrentProfile.GridHighlightSetup;
                allConfigs = setup.Select(entry => new GridHighlightData(entry)).ToArray();
                return allConfigs;
            }
            private set => allConfigs = value;
        }

        public string Name
        {
            get => _entry.Name;
            set => _entry.Name = value;
        }

        public ushort Hue
        {
            get => _entry.Hue;
            set => _entry.Hue = value;
        }

        public List<GridHighlightProperty> Properties => _entry.Properties;

        public bool AcceptExtraProperties
        {
            get => _entry.AcceptExtraProperties;
            set => _entry.AcceptExtraProperties = value;
        }

        public int MinimumProperty
        {
            get => _entry.MinimumProperty;
            set => _entry.MinimumProperty = value;
        }

        public List<string> ExcludeNegatives
        {
            get => _entry.ExcludeNegatives;
            set => _entry.ExcludeNegatives = value;
        }

        public bool Overweight
        {
            get => _entry.Overweight;
            set => _entry.Overweight = value;
        }

        public List<string> RequiredRarities
        {
            get => _entry.RequiredRarities;
            set => _entry.RequiredRarities = value;
        }

        public GridHighlightSlot EquipmentSlots
        {
            get => _entry.GridHighlightSlot;
            set => _entry.GridHighlightSlot = value;
        }

        public GridHighlightData()
        {
            _entry = new GridHighlightSetupEntry();
            ProfileManager.CurrentProfile.GridHighlightSetup.Add(_entry);
        }

        private GridHighlightData(GridHighlightSetupEntry entry)
        {
            _entry = entry;
        }
        
      public void Delete()
        {
            ProfileManager.CurrentProfile.GridHighlightSetup.Remove(_entry);
        }

        public static void ProcessItemOpl(uint value)
        {
            _queue.Enqueue(value);
            hasQueuedItems = true;
        }

        public static void ProcessQueue()
        {
            if (!hasQueuedItems)
                return;

            List<ItemPropertiesData> itemData = new(3);
            
            for (int i = 0; i < 3 && _queue.Count > 0; i++)
            {
                uint ser = _queue.Dequeue();
                if(World.Items.TryGetValue(ser, out var item))
                    itemData.Add(new ItemPropertiesData(item));
            }
            
            foreach (GridHighlightData config in AllConfigs)
            {
                foreach (ItemPropertiesData data in itemData)
                {
                    if (config.IsMatch(data))
                    {
                        data.item.MatchesHighlightData = true;
                        data.item.HighlightHue = config.Hue;
                    }
                }
            }
            
            if(_queue.Count == 0)
                hasQueuedItems = false;
        }

        public static GridHighlightData GetGridHighlightData(int index)
        {
            var list = ProfileManager.CurrentProfile.GridHighlightSetup;
            var data = index >= 0 && index < list.Count ? new GridHighlightData(list[index]) : null;
            if (data == null)
            {
                list.Add(new GridHighlightSetupEntry());
                data = new GridHighlightData(list[index]);
            }
            return data;
        }

        public bool IsMatch(ItemPropertiesData itemData)
        {
            if (!itemData.HasData)
                return false;

            return AcceptExtraProperties
                ? IsMatchFromProperties(itemData)
                : IsMatchFromItemPropertiesData(itemData);
        }

        private bool IsMatchFromProperties(ItemPropertiesData itemData)
        {
            if (!MatchesSlot(itemData.item.ItemData.Layer))
                return false;

            if (Overweight &&
                itemData.singlePropertyData.Any(prop =>
                    prop.OriginalString != null &&
                    prop.OriginalString.Trim().IndexOf("Weight: 50 Stones", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return false;
            }

            foreach (string pattern in ExcludeNegatives.Select(e => e.Trim()))
            {
                if (itemData.singlePropertyData.Any(prop =>
                    (prop.Name != null && prop.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (prop.OriginalString != null && prop.OriginalString.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)))
                    return false;
            }

            if (RequiredRarities.Count > 0)
            {
                bool hasRequired = itemData.singlePropertyData.Any(prop =>
                    GridHighlightRules.RarityProperties.Contains(prop.Name) &&
                    RequiredRarities.Any(r => string.Equals(r, prop.Name, StringComparison.OrdinalIgnoreCase)));

                if (!hasRequired)
                    return false;
            }

            int matchingPropertiesCount = 0;
            foreach (var prop in Properties)
            {
                bool matched = itemData.singlePropertyData.Any(p =>
                    ((p.Name != null && p.Name.IndexOf(prop.Name, StringComparison.OrdinalIgnoreCase) >= 0) ||
                     (p.OriginalString != null && p.OriginalString.IndexOf(prop.Name, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                    (prop.MinValue == -1 || p.FirstValue >= prop.MinValue));

                if (matched)
                {
                    matchingPropertiesCount += 1;
                }

                if (!matched && !prop.IsOptional)
                    return false;
            }

            if (matchingPropertiesCount < MinimumProperty)
            {
                return false;
            }

            return true;
        }

        private bool IsMatchFromItemPropertiesData(ItemPropertiesData itemData)
        {
            if (!MatchesSlot(itemData.item.ItemData.Layer))
                return false;

            var props = itemData.singlePropertyData;

            var itemProperties = props.Where(p => GridHighlightRules.Properties.Contains(p.Name)).ToList();
            var itemNegatives = props.Where(p => GridHighlightRules.NegativeProperties.Contains(p.Name)).ToList();
            var itemResistances = props.Where(p => GridHighlightRules.Resistances.Contains(p.Name)).ToList();
            var itemRarities = props.Where(p => GridHighlightRules.RarityProperties.Contains(p.Name)).ToList();

            if (!itemProperties.Any() && !itemNegatives.Any() && !itemResistances.Any() && !itemRarities.Any())
                return false;

            if (Overweight &&
                itemData.singlePropertyData.Any(prop =>
                    prop.OriginalString != null &&
                    prop.OriginalString.Trim().IndexOf("Weight: 50 Stones", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return false;
            }

            foreach (var pattern in ExcludeNegatives.Select(s => s.Trim()))
            {
                if (itemProperties.Any(p => p.Name != null && p.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    itemNegatives.Any(p => p.Name != null && p.Name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0))
                    return false;
            }

            if (RequiredRarities.Count > 0)
            {
                bool hasRequired = itemRarities.Any(r =>
                    RequiredRarities.Any(req => string.Equals(r.Name, req, StringComparison.OrdinalIgnoreCase)));

                if (!hasRequired)
                    return false;
            }

            int matchingPropertiesCount = 0;
            foreach (var prop in Properties)
            {
                var match = itemProperties.FirstOrDefault(p =>
                    string.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    if (!prop.IsOptional)
                        return false;
                }
                else if (prop.MinValue != -1 && match.FirstValue < prop.MinValue)
                {
                    return false;
                }
                matchingPropertiesCount += 1;
            }

            if (matchingPropertiesCount < MinimumProperty)
            {
                return false;
            }

            return true;
        }

        private bool MatchesSlot(byte layer)
        {
            return layer switch
            {
                (byte)Layer.Talisman => EquipmentSlots.Talisman,
                (byte)Layer.OneHanded => EquipmentSlots.RightHand,
                (byte)Layer.TwoHanded => EquipmentSlots.LeftHand,
                (byte)Layer.Helmet => EquipmentSlots.Head,
                (byte)Layer.Earrings => EquipmentSlots.Earring,
                (byte)Layer.Necklace => EquipmentSlots.Neck,
                (byte)Layer.Torso or (byte)Layer.Tunic => EquipmentSlots.Chest,
                (byte)Layer.Shirt => EquipmentSlots.Shirt,
                (byte)Layer.Cloak => EquipmentSlots.Back,
                (byte)Layer.Robe => EquipmentSlots.Robe,
                (byte)Layer.Arms => EquipmentSlots.Arms,
                (byte)Layer.Gloves => EquipmentSlots.Hands,
                (byte)Layer.Bracelet => EquipmentSlots.Bracelet,
                (byte)Layer.Ring => EquipmentSlots.Ring,
                (byte)Layer.Waist => EquipmentSlots.Belt,
                (byte)Layer.Skirt => EquipmentSlots.Skirt,
                (byte)Layer.Legs => EquipmentSlots.Legs,
                (byte)Layer.Pants => EquipmentSlots.Legs,
                (byte)Layer.Shoes => EquipmentSlots.Footwear,

                (byte)Layer.Hair or
                (byte)Layer.Beard or
                (byte)Layer.Face or
                (byte)Layer.Mount or
                (byte)Layer.Backpack or
                (byte)Layer.ShopBuy or
                (byte)Layer.ShopBuyRestock or
                (byte)Layer.ShopSell or
                (byte)Layer.Bank or
                (byte)Layer.Invalid => false,

                _ => true
            };
        }
    }
}
