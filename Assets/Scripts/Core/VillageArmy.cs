using System;
using System.Collections.Generic;

namespace Kingdoms
{
    public sealed class TroopDefinition
    {
        public readonly string Id, Name;
        public readonly int Housing;
        public TroopDefinition(string id, string name, int housing)
        { Id = id; Name = name; Housing = housing; }
    }

    public static class TroopCatalog
    {
        public static readonly TroopDefinition Raider = new TroopDefinition("Raider", "Raider", 1);
        public static TroopDefinition Find(string id) => id == Raider.Id ? Raider : null;
    }

    [Serializable]
    public sealed class ArmyStack
    {
        public string troop;
        public int count;
    }

    public sealed partial class VillageState
    {
        // Existing saves retain starter capacity; camps expand it.
        public const int StarterArmyCapacity = 8;
        public List<ArmyStack> army = new List<ArmyStack>();
        public const int ArmySpacesPerCampLevel = 8;
        public int ArmyCapacity
        {
            get
            {
                int capacity = StarterArmyCapacity;
                if (buildings != null)
                    foreach (var building in buildings)
                        if (building != null && building.kind == "ArmyCamp")
                            capacity += ArmySpacesPerCampLevel * Math.Max(0, Math.Min(3, building.level));
                return capacity;
            }
        }
        public int ArmyHousing
        {
            get
            {
                if (!IsArmyValid()) return 0;
                int total = 0;
                foreach (var stack in army) total += stack.count * TroopCatalog.Find(stack.troop).Housing;
                return total;
            }
        }
        public bool ArmyReady => IsArmyValid() && ArmyHousing > 0;

        public bool IsArmyValid()
        {
            if (army == null) return false;
            var seen = new HashSet<string>();
            long housing = 0;
            foreach (var stack in army)
            {
                var definition = stack == null ? null : TroopCatalog.Find(stack.troop);
                if (definition == null || stack.count <= 0 || !seen.Add(stack.troop)) return false;
                housing += (long)stack.count * definition.Housing;
                if (housing > ArmyCapacity) return false;
            }
            return true;
        }

        // Preparation is free and instant. Zero removes the stack; invalid requests never mutate it.
        public bool TrySetArmyCount(string troop, int count, out string reason)
        {
            var definition = TroopCatalog.Find(troop);
            if (!IsArmyValid() || definition == null || count < 0)
            { reason = "Choose a valid troop count."; return false; }
            var existing = army.Find(stack => stack.troop == troop);
            long housing = (long)ArmyHousing + ((long)count - (existing?.count ?? 0)) * definition.Housing;
            if (housing > ArmyCapacity)
            { reason = "Your army is full. Build or upgrade an Army Camp for more space."; return false; }
            if (count == 0) { if (existing != null) army.Remove(existing); }
            else if (existing != null) existing.count = count;
            else army.Add(new ArmyStack { troop = troop, count = count });
            reason = count == 0 ? "Army cleared." : "Army prepared and ready.";
            return true;
        }
    }
}
