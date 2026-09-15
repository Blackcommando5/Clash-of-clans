using System.Collections.Generic;

namespace Kingdoms
{
    public enum ResourceKind { None, Gold, Elixir }

    public sealed class BuildingDefinition
    {
        public readonly string Id, Name;
        public readonly int Size, Cost, Limit, ProductionPerSecond, ProductionCapacity, StorageBonus;
        public readonly ResourceKind CostResource, Resource;

        public BuildingDefinition(string id, string name, int size, int cost, ResourceKind costResource,
            int limit, ResourceKind resource = ResourceKind.None, int productionPerSecond = 0,
            int productionCapacity = 0, int storageBonus = 0)
        {
            Id = id; Name = name; Size = size; Cost = cost; CostResource = costResource;
            Limit = limit; Resource = resource; ProductionPerSecond = productionPerSecond;
            ProductionCapacity = productionCapacity; StorageBonus = storageBonus;
        }

        public string CostText => Cost.ToString("N0") + " " + CostResource.ToString().ToLowerInvariant();
        public string Description => ProductionPerSecond > 0
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
        public static readonly IReadOnlyList<BuildingDefinition> Shop = System.Array.AsReadOnly(new[] { GoldMine, ElixirCollector, GoldStorage, ElixirStorage });

        public static BuildingDefinition Find(string id)
        {
            if (id == TownHall.Id) return TownHall;
            if (id == Wall.Id) return Wall;
            foreach (var definition in Shop) if (definition.Id == id) return definition;
            return null;
        }
    }
}
