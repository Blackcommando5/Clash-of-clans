namespace Kingdoms
{
    public sealed partial class VillageState
    {
        public const int MaximumWallRowLength = 10;

        // Directions follow the village grid: east (+X), north (+Z), west (-X), south (-Z).
        public static int WallRowDX(int direction) => direction == 0 ? 1 : direction == 2 ? -1 : 0;
        public static int WallRowDZ(int direction) => direction == 1 ? 1 : direction == 3 ? -1 : 0;

        public bool CanPlaceWallRow(int x, int z, int length, int direction, out string reason)
        {
            if (length < 1 || length > MaximumWallRowLength || direction < 0 || direction > 3)
            { reason = "Choose 1 to " + MaximumWallRowLength + " walls and a valid direction."; return false; }
            if (!CanBuy("Wall", out reason)) return false;
            if (Count("Wall") + length > BuildingLimit("Wall"))
            { reason = "This row exceeds your " + BuildingLimit("Wall") + "-wall limit. Shorten it or upgrade the Town Hall."; return false; }
            int cost = BuildingCatalog.Wall.Cost * length;
            if (gold < cost)
            { reason = "This row needs " + cost + " gold. Shorten it or collect more gold."; return false; }
            // Validate coordinates before adding offsets, including extreme integer inputs.
            if (x < -22 || x > 21 || z < -22 || z > 21)
            { reason = "Place every wall inside the village border."; return false; }
            for (int i = 0; i < length; i++)
                if (!CanPlace("Wall", x + i * WallRowDX(direction), z + i * WallRowDZ(direction), out reason))
                { reason = "Wall " + (i + 1) + " / " + length + ": " + reason; return false; }
            reason = length + (length == 1 ? " wall" : " walls") + " | " + cost + " gold | Ready to build.";
            return true;
        }

        public bool TryPlaceWallRow(int x, int z, int length, int direction, long now, out string reason)
        {
            if (!CanPlaceWallRow(x, z, length, direction, out reason)) return false;
            Accrue(now);
            gold -= BuildingCatalog.Wall.Cost * length;
            for (int i = 0; i < length; i++)
                buildings.Add(new PlacedBuilding { kind = "Wall", x = x + i * WallRowDX(direction), z = z + i * WallRowDZ(direction) });
            reason = length + (length == 1 ? " wall built!" : " walls built!");
            return true;
        }
    }
}
