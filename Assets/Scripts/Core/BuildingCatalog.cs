using System.Collections.Generic;

namespace Kingdoms
{
    public enum ResourceKind { None, Gold, Elixir }

    public sealed class BuildingDefinition
    {
        public readonly string Id, Name;
        public readonly int Size, Cost, Limit, ProductionPerSecond, ProductionCapacity, StorageBonus;
        public readonly ResourceKind CostResource, Resource;
        public readonly int HitPoints, DamagePerSecond;
        public readonly float Range;
        public bool IsDefense => HitPoints > 0;

        public BuildingDefinition(string id, string name, int size, int cost, ResourceKind costResource,
            int limit, ResourceKind resource = ResourceKind.None, int productionPerSecond = 0,
            int productionCapacity = 0, int storageBonus = 0, int hitPoints = 0, int damagePerSecond = 0, float range = 0)
        {
            Id = id; Name = name; Size = size; Cost = cost; CostResource = costResource;
            Limit = limit; Resource = resource; ProductionPerSecond = productionPerSecond;
            ProductionCapacity = productionCapacity; StorageBonus = storageBonus;
            HitPoints=hitPoints;DamagePerSecond=damagePerSecond;Range=range;
        }

        public string CostText => Cost.ToString("N0") + " " + CostResource.ToString().ToLowerInvariant();
        public string Description => Id == "Barracks" ? "Unlocks Archers and Army Camps\nPreparation stays free and instant" : Id == "ArmyCamp" ? "+8 army spaces per level\nKeeps capacity during upgrades" : IsDefense ? "Damage: "+DamagePerSecond+" / second\nRange: "+Range+" cells" : ProductionPerSecond > 0
            ? (ProductionPerSecond * 60) + " " + Resource.ToString().ToLowerInvariant() + " / minute\nHolds " + ProductionCapacity.ToString("N0")
            : "+" + StorageBonus.ToString("N0") + " " + Resource.ToString().ToLowerInvariant() + " capacity";
    }

    // Shared by placement, economy and shop. Limits describe the current Town Hall 1 tier.
    public static class BuildingCatalog
    {
        public const int BaseCapacity = 10000;
        public static readonly BuildingDefinition TownHall = new BuildingDefinition("TownHall", "Town Hall", 4, 0, ResourceKind.None, 1);
        public static readonly BuildingDefinition GoldMine = new BuildingDefinition("GoldMine", "Gold Mine", 3, 150, ResourceKind.Elixir, 3, ResourceKind.Gold, 1, 500);
        public static readonly BuildingDefinition ElixirCollector = new BuildingDefinition("ElixirCollector", "Elixir Collector", 3, 150, ResourceKind.Gold, 3, ResourceKind.Elixir, 1, 500);
        public static readonly BuildingDefinition GoldStorage = new BuildingDefinition("GoldStorage", "Gold Storage", 3, 300, ResourceKind.Elixir, 2, ResourceKind.Gold, storageBonus: 5000);
        public static readonly BuildingDefinition ElixirStorage = new BuildingDefinition("ElixirStorage", "Elixir Storage", 3, 300, ResourceKind.Gold, 2, ResourceKind.Elixir, storageBonus: 5000);
        public static readonly BuildingDefinition Wall = new BuildingDefinition("Wall", "Wall", 1, 25, ResourceKind.Gold, 25);
        public static readonly BuildingDefinition Cannon = new BuildingDefinition("Cannon", "Cannon", 3, 250, ResourceKind.Gold, 2, hitPoints: 420, damagePerSecond: 9, range: 9);
        public static readonly BuildingDefinition ArcherTower = new BuildingDefinition("ArcherTower", "Archer Tower", 3, 1000, ResourceKind.Gold, 1, hitPoints: 380, damagePerSecond: 11, range: 10);
        public static readonly BuildingDefinition Barracks = new BuildingDefinition("Barracks", "Barracks", 3, 200, ResourceKind.Elixir, 1);
        public static readonly BuildingDefinition ArmyCamp = new BuildingDefinition("ArmyCamp", "Army Camp", 3, 250, ResourceKind.Elixir, 1);
        public static readonly IReadOnlyList<BuildingDefinition> Purchasable = System.Array.AsReadOnly(new[] { GoldMine, ElixirCollector, GoldStorage, ElixirStorage, Cannon, ArcherTower, Wall, Barracks, ArmyCamp });
        public static readonly IReadOnlyList<BuildingDefinition> Shop = System.Array.AsReadOnly(new[] { GoldMine, ElixirCollector, GoldStorage, ElixirStorage });

        public static BuildingDefinition Find(string id)
        {
            if (id == TownHall.Id) return TownHall;
            if (id == Wall.Id) return Wall;
            foreach (var definition in Purchasable) if (definition.Id == id) return definition;
            return null;
        }
    }
}
