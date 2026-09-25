using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdoms
{
    [Serializable]
    public sealed class PlacedBuilding
    {
        public string kind;
        // Keep the original field name for migration of version 1 saves.
        public int x, z, storedGold, storedElixir;
        public int level = 1;
        public long upgradeStarted, upgradeFinishes;
        public int Size => BuildingCatalog.Find(kind)?.Size ?? 0;
    }

    [Serializable]
    public sealed partial class VillageState
    {
        public const int MineCost = 150, MineLimit = 3, MineCapacity = 500, ResourceCapacity = BuildingCatalog.BaseCapacity;
        public const int SaveVersion = 9;
        public int version = SaveVersion;
        public int gold = 1000, elixir = 500, gems = 50;
        public bool collectedFirstGold;
        public long lastProduction;
        public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        public int MineCount => Count("GoldMine");
        public int CollectableGold => Stored(ResourceKind.Gold);
        public int CollectableElixir => Stored(ResourceKind.Elixir);
        public int GoldCapacity => Capacity(ResourceKind.Gold);
        public int ElixirCapacity => Capacity(ResourceKind.Elixir);
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static VillageState Create(long now)
        {
            var state = new VillageState { lastProduction = now };
            state.buildings.Add(new PlacedBuilding { kind = "TownHall", x = -2, z = -2 });
            return state;
        }

        public VillageState Copy() => JsonUtility.FromJson<VillageState>(JsonUtility.ToJson(this));
        public int Count(string kind) => buildings.FindAll(b => b != null && b.kind == kind).Count;
        public int Balance(ResourceKind resource) => resource == ResourceKind.Gold ? gold : resource == ResourceKind.Elixir ? elixir : 0;

        int Stored(ResourceKind resource)
        {
            int total = 0;
            foreach (var b in buildings) total += resource == ResourceKind.Gold ? b.storedGold : b.storedElixir;
            return total;
        }

        public int Capacity(ResourceKind resource)
        {
            int capacity = BuildingCatalog.BaseCapacity;
            foreach (var b in buildings)
            {
                var definition = b == null ? null : BuildingCatalog.Find(b.kind);
                if (definition != null && definition.Resource == resource) capacity += definition.StorageBonus * b.level;
            }
            return capacity;
        }

        public void Accrue(long now)
        {
            // Do not grant production or move the watermark backward after a clock change.
            if (now <= lastProduction) return;
            foreach (var b in buildings)
            {
                var definition = BuildingCatalog.Find(b.kind);
                long activeSince = lastProduction;
                if (b.upgradeFinishes > 0)
                {
                    if (now < b.upgradeFinishes) continue;
                    activeSince = Math.Max(activeSince, b.upgradeFinishes);
                    b.level++;b.upgradeStarted=0;b.upgradeFinishes=0;
                }
                if (definition == null || definition.ProductionPerSecond == 0) continue;
                int producerCapacity = definition.ProductionCapacity * b.level;
                long produced = Math.Min(now-activeSince, producerCapacity) * definition.ProductionPerSecond * b.level;
                if (definition.Resource == ResourceKind.Gold)
                    b.storedGold = (int)Math.Min(producerCapacity, b.storedGold + produced);
                else if (definition.Resource == ResourceKind.Elixir)
                    b.storedElixir = (int)Math.Min(producerCapacity, b.storedElixir + produced);
            }
            lastProduction = now;
        }

        public bool CanBuy(string kind, out string reason)
        {
            var definition = BuildingCatalog.Find(kind);
            if (definition == null || kind == "TownHall") { reason = "Choose a building from the shop."; return false; }
            if (kind == "ArmyCamp" && Count("Barracks") == 0) { reason = "Build Barracks first to unlock Army Camps."; return false; }
            if (Count(kind) >= BuildingLimit(kind)) { reason = definition.Name + " limit reached (" + BuildingLimit(kind) + ") at Town Hall " + TownHallLevel + "."; return false; }
            if (Balance(definition.CostResource) < definition.Cost) { reason = "You need " + definition.CostText + "."; return false; }
            reason = "Ready to build - " + definition.CostText + ".";
            return true;
        }

        public bool CanPlace(string kind, int x, int z, out string reason)
        {
            if (!CanBuy(kind, out reason)) return false;
            int size = BuildingCatalog.Find(kind).Size;
            if (x < -22 || z < -22 || x > 22 - size || z > 22 - size)
            { reason = "Place the building inside the village border."; return false; }
            foreach (var b in buildings)
                if (x < b.x + b.Size && x + size > b.x && z < b.z + b.Size && z + size > b.z)
                { reason = "That space is occupied. Choose an empty spot."; return false; }
            return true;
        }

        public bool TryPlace(string kind, int x, int z, long now, out string reason)
        {
            if (!CanPlace(kind, x, z, out reason)) return false;
            Accrue(now); // New producers never receive resources for time before their purchase.
            var definition = BuildingCatalog.Find(kind);
            if (definition.CostResource == ResourceKind.Gold) gold -= definition.Cost;
            else elixir -= definition.Cost;
            buildings.Add(new PlacedBuilding { kind = kind, x = x, z = z });
            reason = definition.Name + " built!";
            return true;
        }

        public bool CanPlaceMine(int x, int z, out string reason) => CanPlace("GoldMine", x, z, out reason);
        public bool TryPlaceMine(int x, int z, long now, out string reason) => TryPlace("GoldMine", x, z, now, out reason);
        public int CollectGold(long now) => Collect(ResourceKind.Gold, now);

        public int Collect(ResourceKind resource, long now, PlacedBuilding selected = null)
        {
            if (resource == ResourceKind.None || (selected != null && !buildings.Contains(selected))) return 0;
            Accrue(now);
            int capacity = Capacity(resource), collected = 0;
            foreach (var b in buildings)
            {
                if (selected != null && b != selected) continue;
                int stored = resource == ResourceKind.Gold ? b.storedGold : b.storedElixir;
                int take = Math.Min(stored, Math.Max(0, capacity - Balance(resource)));
                if (resource == ResourceKind.Gold) { b.storedGold -= take; gold += take; }
                else { b.storedElixir -= take; elixir += take; }
                collected += take;
            }
            if (resource == ResourceKind.Gold && collected > 0) collectedFirstGold = true;
            return collected;
        }

        public bool IsValid()
        {
            if (version != SaveVersion || (tutorialMilestones & ~TutorialAll)!=0 || !IsCampaignValid() || !IsArmyValid() || !IsBattleHistoryValid() || buildings == null || buildings.Count < 1 ||
                gold < 0 || elixir < 0 || gems < 0 || lastProduction < 0) return false;
            int maximumBuildings = 1;
            foreach (var definition in BuildingCatalog.Purchasable) maximumBuildings += BuildingLimit(definition.Id);
            if (buildings.Count > maximumBuildings) return false;
            int halls = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                var definition = b == null ? null : BuildingCatalog.Find(b.kind);
                if (definition == null) return false;
                if (b.kind == "TownHall") halls++;
                if (b.kind == "Barracks" && (b.level>2 || (b.level==2 && b.upgradeFinishes!=0))) return false;
                if (b.kind == "ArmyCamp" && Count("Barracks") != 1) return false;
                if (b.level<1 || b.level>3 || b.upgradeStarted<0 || b.upgradeFinishes<0) return false;
                if (b.kind!="TownHall" && b.level>TownHallLevel+1) return false;
                if (b.upgradeFinishes==0 ? b.upgradeStarted!=0 : b.level>=3 || b.upgradeFinishes<=b.upgradeStarted || b.upgradeFinishes-b.upgradeStarted!=UpgradeSeconds(b)) return false;
                if (Count(b.kind) > BuildingLimit(b.kind) || b.x < -22 || b.z < -22 || b.x > 22 - b.Size || b.z > 22 - b.Size) return false;
                int goldLimit = definition.Resource == ResourceKind.Gold ? definition.ProductionCapacity*b.level : 0;
                int elixirLimit = definition.Resource == ResourceKind.Elixir ? definition.ProductionCapacity*b.level : 0;
                if (b.storedGold < 0 || b.storedGold > goldLimit || b.storedElixir < 0 || b.storedElixir > elixirLimit) return false;
                for (int j = 0; j < i; j++)
                {
                    var other = buildings[j];
                    if (b.x < other.x + other.Size && b.x + b.Size > other.x && b.z < other.z + other.Size && b.z + b.Size > other.z) return false;
                }
            }
            return halls == 1 && BusyBuilders<=BuilderCount && gold <= GoldCapacity && elixir <= ElixirCapacity;
        }

        public static bool TryDeserialize(string json, out VillageState state)
        {
            state = null;
            try
            {
                var loaded = JsonUtility.FromJson<VillageState>(json);
                if (loaded == null) return false;
                if (loaded.version == 1)
                {
                    // Validate old-format constraints before permitting migration.
                    if (loaded.buildings == null || loaded.buildings.Count > 4 || loaded.gold > ResourceCapacity || loaded.elixir > ResourceCapacity) return false;
                    foreach (var b in loaded.buildings)
                        if (b == null || (b.kind != "TownHall" && b.kind != "GoldMine") || b.storedElixir != 0) return false;
                    loaded.version = 2;
                }
                if (loaded.version == 2)
                {
                    if (loaded.buildings == null) return false;
                    foreach (var b in loaded.buildings)
                    {
                        if (b==null) return false;
                        // Version 2 had no levels, movable Town Hall, or upgrade jobs.
                        if(b.kind=="TownHall" && (b.x!=-2 || b.z!=-2)) return false;
                        b.level=1;b.upgradeStarted=0;b.upgradeFinishes=0;
                    }
                    loaded.version=3;
                }
                if (loaded.version == 3)
                {
                    loaded.army = new List<ArmyStack>();
                    loaded.version = 4;
                }
                if (loaded.version == 4)
                {
                    loaded.campaignCleared=0;loaded.ResetCampaignRun();loaded.version=5;
                }
                if (loaded.version == 5)
                {
                    loaded.campaignArchers=0;loaded.version=6;
                }
                if (loaded.version == 6) { loaded.campaignTanks=0;loaded.version=7; }
                if (loaded.version == 7) { loaded.tutorialMilestones=0;loaded.tutorialHintsPaused=false;loaded.version=8; }
                if (loaded.version == 8) { loaded.MigrateBattleHistory();loaded.version=9; }
                if (!loaded.IsValid()) return false;
                loaded.tutorialMilestones=loaded.TutorialProgress;
                state = loaded;
                return true;
            }
            catch (Exception) { return false; }
        }
    }

    public static class VillageSave
    {
        // Keep the key stable to find existing villages; the payload carries the version.
        public const string Key = "Kingdoms.Village.v1";
        public const string LegacyBackupKey = "Kingdoms.Village.pre-v2";
        public static bool TryLoad(out VillageState state, out string error)
        {
            error = ""; state = null;
            if (!PlayerPrefs.HasKey(Key)) { state = VillageState.Create(VillageState.Now); return true; }
            try
            {
                string json = PlayerPrefs.GetString(Key);
                if (!VillageState.TryDeserialize(json, out var loaded)) throw new FormatException("Invalid village data");
                var original = JsonUtility.FromJson<VillageState>(json);
                string backupKey=original.version==1 ? LegacyBackupKey : "Kingdoms.Village.pre-v"+(original.version+1);
                if (original.version < VillageState.SaveVersion && !PlayerPrefs.HasKey(backupKey))
                { PlayerPrefs.SetString(backupKey, json); PlayerPrefs.Save(); }
                loaded.Accrue(VillageState.Now);
                state = loaded;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Village save was preserved because it could not be loaded: " + e.Message);
                error = "Your village save could not be opened. Restart the game to try again.";
                return false;
            }
        }

        public static bool TryWrite(VillageState state, out string error)
        {
            error = "";
            if (state == null || !state.IsValid()) { error = "The village could not be saved."; return false; }
            bool hadSave = PlayerPrefs.HasKey(Key);
            string previous = PlayerPrefs.GetString(Key, "");
            var snapshot=state.Copy();snapshot.tutorialMilestones=state.TutorialProgress;
            try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(snapshot)); PlayerPrefs.Save(); state.tutorialMilestones=snapshot.tutorialMilestones;return true; }
            catch (Exception e)
            {
                if (hadSave) PlayerPrefs.SetString(Key, previous); else PlayerPrefs.DeleteKey(Key);
                error = "Couldn't save. Your action was not completed. Please try again.";
                Debug.LogWarning(e.Message);
                return false;
            }
        }
    }
}
