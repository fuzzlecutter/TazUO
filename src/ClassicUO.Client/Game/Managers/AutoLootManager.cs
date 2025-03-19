using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    internal class AutoLootManager
    {
        public static AutoLootManager Instance { get; private set; } = new AutoLootManager();
        public bool IsLoaded { get { return loaded; } }
        public List<AutoLootItem> AutoLootList { get => autoLootItems; set => autoLootItems = value; }

        private HashSet<uint> quickContainsLookup = new HashSet<uint>();
        private static ConcurrentQueue<uint> lootItems = new ConcurrentQueue<uint>();
        private List<AutoLootItem> autoLootItems = new List<AutoLootItem>();
        private bool loaded = false;
        private readonly string savePath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles", "AutoLoot.json");
        private long nextLootTime = Time.Ticks;
        private ProgressBarGump progressBarGump;
        private int currentLootTotalCount = 0;

        private AutoLootManager() { }


        public bool IsBeingLooted(uint serial)
        {
            return quickContainsLookup.Contains(serial);
        }

        public void LootItem(uint serial)
        {
            LootItem(World.Items.Get(serial));
        }
        public void LootItem(Item i)
        {
            if (i == null || quickContainsLookup.Contains(i.Serial)) return;

            lootItems.Enqueue(i);
            quickContainsLookup.Add(i.Serial);
            currentLootTotalCount++;
        }

        /// <summary>
        /// Check an item against the loot list, if it needs to be auto looted it will be.
        /// I reccomend running this method in a seperate thread if it's a lot of items.
        /// </summary>
        private void CheckAndLoot(Item i)
        {
            if (!loaded || i == null || quickContainsLookup.Contains(i.Serial)) return;

            if (IsOnLootList(i))
            {
                LootItem(i);
            }
        }

        /// <summary>
        /// Check if an item is on the auto loot list.
        /// </summary>
        /// <param name="i">The item to check the loot list against</param>
        /// <returns></returns>
        private bool IsOnLootList(Item i)
        {
            if (!loaded) return false;

            foreach (var entry in autoLootItems)
            {
                if (entry.Match(i))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Add an entry for auto looting to match against when opening corpses.
        /// </summary>
        /// <param name="graphic"></param>
        /// <param name="hue"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public AutoLootItem AddAutoLootEntry(ushort graphic = 0, ushort hue = ushort.MaxValue, string name = "")
        {
            AutoLootItem item = new AutoLootItem() { Graphic = (short)graphic, Hue = hue, Name = name };

            foreach (AutoLootItem entry in autoLootItems)
            {
                if (entry.Equals(item))
                {
                    return entry;
                }
            }

            autoLootItems.Add(item);

            return item;
        }

        /// <summary>
        /// Search through a corpse and check items that need to be looted.
        /// </summary>
        /// <param name="corpse"></param>
        public void HandleCorpse(Item corpse)
        {
            if (corpse != null && corpse.IsCorpse && (!corpse.IsHumanCorpse || ProfileManager.CurrentProfile.AutoLootHumanCorpses) && ProfileManager.CurrentProfile.EnableAutoLoot)
            {
                for (LinkedObject i = corpse.Items; i != null; i = i.Next)
                {
                    CheckAndLoot((Item)i);
                }
            }
        }

        public void TryRemoveAutoLootEntry(string UID)
        {
            int removeAt = -1;

            for (int i = 0; i < autoLootItems.Count; i++)
            {
                if (autoLootItems[i].UID == UID)
                {
                    removeAt = i;
                }
            }

            if (removeAt > -1)
            {
                autoLootItems.RemoveAt(removeAt);
            }
        }

        public void OnSceneLoad()
        {
            Load();
        }

        public void Update()
        {
            if (!loaded) return;

            if (lootItems.IsEmpty)
            {
                progressBarGump?.Dispose();
                return;
            }

            if (nextLootTime > Time.Ticks) return;

            if (lootItems.TryDequeue(out uint moveItem))
            {
                if (lootItems.IsEmpty) //Que emptied out
                {
                    currentLootTotalCount = 0;
                }

                quickContainsLookup.Remove(moveItem);

                CreateProgressBar();

                if (progressBarGump != null && !progressBarGump.IsDisposed)
                {
                    progressBarGump.CurrentPercentage = 1 - ((double)lootItems.Count / (double)currentLootTotalCount);
                }

                Item m = World.Items.Get(moveItem);
                if (m != null && !m.RecentlyLooted)
                {
                    m.RecentlyLooted = true;
                    GameActions.GrabItem(m, m.Amount);
                    nextLootTime = Time.Ticks + ProfileManager.CurrentProfile.MoveMultiObjectDelay;
                }
            }
        }

        private void CreateProgressBar()
        {
            if (ProfileManager.CurrentProfile.EnableAutoLootProgressBar && (progressBarGump == null || progressBarGump.IsDisposed))
            {
                progressBarGump = new ProgressBarGump("Auto looting...", 0)
                {
                    Y = (ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y) - 150
                };
                progressBarGump.CenterXInViewPort();
                UIManager.Add(progressBarGump);
            }
        }

        private void Load()
        {
            if (loaded) return;

            Task.Factory.StartNew(() =>
            {
                if (!File.Exists(savePath))
                {
                    autoLootItems = new List<AutoLootItem>();
                    loaded = true;
                }
                else
                {
                    try
                    {
                        string data = File.ReadAllText(savePath);
                        AutoLootItem[] tItem = JsonSerializer.Deserialize<AutoLootItem[]>(data);
                        autoLootItems = tItem.ToList<AutoLootItem>();
                        loaded = true;
                    }
                    catch
                    {
                        GameActions.Print("There was an error loading your auto loot config file, please check it with a json validator.", 32);
                        loaded = false;
                    }

                }
            });
        }

        public void Save()
        {
            if (loaded)
            {
                try
                {
                    var options = new JsonSerializerOptions() { WriteIndented = true };
                    string fileData = JsonSerializer.Serialize(autoLootItems, options);

                    File.WriteAllText(savePath, fileData);
                }
                catch (Exception e) { Console.WriteLine(e.ToString()); }
            }
        }

        public class AutoLootItem
        {
            public string Name { get; set; } = "";
            public short Graphic { get; set; } = 0;
            public ushort Hue { get; set; } = ushort.MaxValue;
            public string RegexSearch { get; set; } = string.Empty;
            private bool RegexMatch => !string.IsNullOrEmpty(RegexSearch);
            /// <summary>
            /// Do not set this manually.
            /// </summary>
            public string UID { get; set; } = Guid.NewGuid().ToString();

            public bool Match(Item compareTo)
            {
                if (Graphic != -1 && Graphic != compareTo.Graphic) return false;

                if (!HueCheck(compareTo.Hue)) return false;

                if (RegexMatch && !RegexCheck(compareTo)) return false;

                return true;
            }

            private bool HueCheck(ushort value)
            {
                if (Hue == ushort.MaxValue) //Ignore hue.
                {
                    return true;
                }
                else if (Hue == value) //Hue must match, and it does
                {
                    return true;
                }
                else //Hue is not ignored, and does not match
                {
                    return false;
                }
            }

            private bool RegexCheck(Item compareTo)
            {
                string search = "";
                if (World.OPL.TryGetNameAndData(compareTo, out string name, out string data))
                    search += name + data;
                else
                    search = StringHelper.GetPluralAdjustedString(compareTo.ItemData.Name);

                return System.Text.RegularExpressions.Regex.IsMatch(search, RegexSearch, System.Text.RegularExpressions.RegexOptions.Multiline);
            }

            public bool Equals(AutoLootItem other)
            {
                return other.Graphic == Graphic && other.Hue == Hue;
            }
        }
    }
}
