using System;
using System.Collections.Generic;

namespace Kingdoms
{
    public sealed class TroopDefinition
    {
        public readonly string Id, Name;
        public readonly int Housing, HitPoints, Damage, Range, Speed;
        public TroopDefinition(string id, string name, int housing, int hitPoints, int damage, int range, int speed=28)
        { Id=id; Name=name; Housing=housing; HitPoints=hitPoints; Damage=damage; Range=range; Speed=speed; }
    }

    public static class TroopCatalog
    {
        public static readonly TroopDefinition Raider = new TroopDefinition("Raider", "Raider", 1, 90, 20, 85);
        public static readonly TroopDefinition Archer = new TroopDefinition("Archer", "Archer", 2, 55, 14, 350);
        public static readonly TroopDefinition Tank = new TroopDefinition("Tank", "Tank", 4, 300, 16, 85, 18);
        public static TroopDefinition Find(string id) => id == Raider.Id ? Raider : id == Archer.Id ? Archer : id == Tank.Id ? Tank : null;
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
        public int ArmyCountOf(string troop) => army?.Find(stack => stack != null && stack.troop == troop)?.count ?? 0;
        public int ArmyCount => ArmyCountOf("Raider") + ArmyCountOf("Archer") + ArmyCountOf("Tank");
        public bool TroopUnlocked(string troop) => troop == "Raider" || ((troop == "Archer" || troop == "Tank") && buildings != null && buildings.Exists(b => b != null && b.kind == "Barracks" && b.level >= (troop == "Tank" ? 2 : 1)));
        public bool ArmyReady => IsArmyValid() && ArmyHousing > 0;

        public bool IsArmyValid()
        {
            if (army == null) return false;
            var seen = new HashSet<string>();
            long housing = 0;
            foreach (var stack in army)
            {
                var definition = stack == null ? null : TroopCatalog.Find(stack.troop);
                if (definition == null || !TroopUnlocked(stack.troop) || stack.count <= 0 || !seen.Add(stack.troop)) return false;
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
            if (count > 0 && !TroopUnlocked(troop))
            { reason = troop == "Tank" ? "Upgrade Barracks to level 2 to unlock Tanks." : "Build Barracks to unlock Archers."; return false; }
            var existing = army.Find(stack => stack.troop == troop);
            long housing = (long)ArmyHousing + ((long)count - (existing?.count ?? 0)) * definition.Housing;
            if (housing > ArmyCapacity)
            { reason = "Your army is full. Build or upgrade an Army Camp for more space."; return false; }
            if (count == 0) { if (existing != null) army.Remove(existing); }
            else if (existing != null) existing.count = count;
            else army.Add(new ArmyStack { troop = troop, count = count });
            reason = count == 0 ? definition.Name + "s removed." : "Army prepared and ready.";
            return true;
        }
    }
}
