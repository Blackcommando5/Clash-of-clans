using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kingdoms
{
    [Serializable]
    public sealed class PlacedBuilding
    {
        public string kind;
        public int x, z, storedGold;
        public int Size => kind == "TownHall" ? 4 : 3;
    }

    [Serializable]
    public sealed class VillageState
    {
        public const int MineCost = 150, MineLimit = 3, MineCapacity = 500, ResourceCapacity = 10000;
        public int version = 1;
        public int gold = 1000, elixir = 500, gems = 50;
        public bool collectedFirstGold;
        public long lastProduction;
        public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        public int MineCount => buildings.FindAll(b => b.kind == "GoldMine").Count;
        public int CollectableGold { get { int total = 0; foreach (var b in buildings) total += b.storedGold; return total; } }
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static VillageState Create(long now)
        {
            var state = new VillageState { lastProduction = now };
            state.buildings.Add(new PlacedBuilding { kind = "TownHall", x = -2, z = -2 });
            return state;
        }

        public VillageState Copy() => JsonUtility.FromJson<VillageState>(JsonUtility.ToJson(this));

        public void Accrue(long now)
        {
            long seconds = Math.Max(0, now - lastProduction);
            if (seconds == 0) return;
            foreach (var b in buildings)
                if (b.kind == "GoldMine") b.storedGold = (int)Math.Min(MineCapacity, b.storedGold + Math.Min(seconds, MineCapacity));
            lastProduction = now;
        }

        public bool CanPlaceMine(int x, int z, out string reason)
        {
            if (MineCount >= MineLimit) { reason = "Your village already has 3 Gold Mines."; return false; }
            if (elixir < MineCost) { reason = "You need 150 elixir to build a Gold Mine."; return false; }
            if (x < -22 || z < -22 || x > 19 || z > 19)
            { reason = "Place the mine inside the village border."; return false; }
            foreach (var b in buildings)
                if (x < b.x + b.Size && x + 3 > b.x && z < b.z + b.Size && z + 3 > b.z)
                { reason = "That space is occupied. Choose an empty spot."; return false; }
            reason = "Ready to build - 150 elixir.";
            return true;
        }

        public bool TryPlaceMine(int x, int z, long now, out string reason)
        {
            if (!CanPlaceMine(x, z, out reason)) return false;
            Accrue(now); // A new mine never receives gold for time before it existed.
            elixir -= MineCost;
            buildings.Add(new PlacedBuilding { kind = "GoldMine", x = x, z = z });
            reason = "Gold Mine built! It produces 60 gold per minute.";
            return true;
        }

        public int CollectGold(long now)
        {
            Accrue(now);
            int collected = 0;
            foreach (var b in buildings)
            {
                int take = Math.Min(b.storedGold, ResourceCapacity - gold);
                b.storedGold -= take;
                gold += take;
                collected += take;
            }
            if (collected > 0) collectedFirstGold = true;
            return collected;
        }

        public bool IsValid()
        {
            if (version != 1 || gold < 0 || gold > ResourceCapacity || elixir < 0 || elixir > ResourceCapacity || gems < 0 ||
                lastProduction < 0 || buildings == null || buildings.Count < 1 || buildings.Count > MineLimit + 1) return false;
            int halls = 0, mines = 0;
            for (int i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                if (b == null || (b.kind != "TownHall" && b.kind != "GoldMine")) return false;
                if (b.kind == "TownHall") { halls++; if (b.x != -2 || b.z != -2 || b.storedGold != 0) return false; }
                else mines++;
                if (b.x < -22 || b.z < -22 || b.x > 22 - b.Size || b.z > 22 - b.Size || b.storedGold < 0 || b.storedGold > MineCapacity) return false;
                for (int j = 0; j < i; j++)
                {
                    var other = buildings[j];
                    if (b.x < other.x + other.Size && b.x + b.Size > other.x && b.z < other.z + other.Size && b.z + b.Size > other.z) return false;
                }
            }
            return halls == 1 && mines <= MineLimit;
        }
    }

    public static class VillageSave
    {
        public const string Key = "Kingdoms.Village.v1";
        public static bool TryLoad(out VillageState state, out string error)
        {
            error = "";
            state = null;
            if (!PlayerPrefs.HasKey(Key)) { state = VillageState.Create(VillageState.Now); return true; }
            try
            {
                var loaded = JsonUtility.FromJson<VillageState>(PlayerPrefs.GetString(Key));
                if (loaded == null || !loaded.IsValid()) throw new FormatException("Invalid village data");
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
            if (!state.IsValid()) { error = "The village could not be saved."; return false; }
            bool hadSave = PlayerPrefs.HasKey(Key);
            string previous = PlayerPrefs.GetString(Key, "");
            try { PlayerPrefs.SetString(Key, JsonUtility.ToJson(state)); PlayerPrefs.Save(); return true; }
            catch (Exception e)
            {
                if (hadSave) PlayerPrefs.SetString(Key, previous); else PlayerPrefs.DeleteKey(Key);
                error = "Couldn't save. Your purchase was not completed. Please try again.";
                Debug.LogWarning(e.Message);
                return false;
            }
        }
    }
}
