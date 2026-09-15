using System;

namespace Kingdoms
{
    public sealed partial class VillageState
    {
        public const int BuilderCount=2;
        public int TownHallLevel
        {
            get { var hall=buildings?.Find(b=>b!=null && b.kind=="TownHall");return hall==null ? 1 : Math.Max(1,Math.Min(3,hall.level)); }
        }
        public int BusyBuilders => buildings.FindAll(b=>b!=null && b.upgradeFinishes>0).Count;
        public int BuildingLimit(string kind)
        {
            var d=BuildingCatalog.Find(kind);
            return kind=="Wall" ? 25*TownHallLevel : d==null ? 0 : d.Limit+(kind=="TownHall" ? 0 : TownHallLevel-1);
        }

        // Initial Kingdoms balancing. These are not verified Clash of Clans values.
        public static int UpgradeCost(PlacedBuilding b) => b.kind=="TownHall" ? (b.level==1 ? 1000 : 4000) : BuildingCatalog.Find(b.kind).Cost*(b.level==1 ? 2 : 5);
        public static int UpgradeSeconds(PlacedBuilding b) => b.kind=="TownHall" ? (b.level==1 ? 60 : 300) : (b.level==1 ? 30 : 120);
        public static ResourceKind UpgradeResource(PlacedBuilding b) => b.kind=="TownHall" ? ResourceKind.Gold : BuildingCatalog.Find(b.kind).CostResource;

        public bool CanUpgrade(int index,out string reason)
        {
            if(index<0 || index>=buildings.Count) { reason="Select a building.";return false; }
            var b=buildings[index];
            if(b.kind=="Wall") { reason="Wall upgrades are not available yet.";return false; }
            if(b.upgradeFinishes>0) { reason="This building is already upgrading.";return false; }
            if(b.level>=3) { reason="Maximum available level reached.";return false; }
            if(b.kind!="TownHall" && b.level>=TownHallLevel+1) { reason="Upgrade your Town Hall first.";return false; }
            if(BusyBuilders>=BuilderCount) { reason="Both builders are busy.";return false; }
            if(Balance(UpgradeResource(b))<UpgradeCost(b)) { reason="You need "+UpgradeCost(b).ToString("N0")+" "+UpgradeResource(b).ToString().ToLowerInvariant()+".";return false; }
            reason="Ready to upgrade.";return true;
        }

        public bool TryStartUpgrade(int index,long now,out string reason)
        {
            Accrue(now);
            if(!CanUpgrade(index,out reason)) return false;
            var b=buildings[index];
            long start=Math.Max(now,lastProduction);
            if(start>long.MaxValue-UpgradeSeconds(b)) { reason="The device time is invalid.";return false; }
            if(UpgradeResource(b)==ResourceKind.Gold) gold-=UpgradeCost(b);else elixir-=UpgradeCost(b);
            b.upgradeStarted=start;b.upgradeFinishes=start+UpgradeSeconds(b);
            reason=BuildingCatalog.Find(b.kind).Name+" upgrade started.";return true;
        }

        public bool CanMove(int index,int x,int z,out string reason)
        {
            if(index<0 || index>=buildings.Count) { reason="Select a building.";return false; }
            var selected=buildings[index];int size=selected.Size;
            if(x < -22 || z < -22 || x > 22-size || z > 22-size) { reason="Stay inside the village border.";return false; }
            for(int i=0;i<buildings.Count;i++)
            {
                if(i==index) continue;
                var b=buildings[i];
                if(x<b.x+b.Size && x+size>b.x && z<b.z+b.Size && z+size>b.z) { reason="That space is occupied.";return false; }
            }
            reason="Ready to move. No resource cost.";return true;
        }

        public bool TryMove(int index,int x,int z,out string reason)
        {
            if(!CanMove(index,x,z,out reason)) return false;
            buildings[index].x=x;buildings[index].z=z;reason="Building moved.";return true;
        }
    }
}
