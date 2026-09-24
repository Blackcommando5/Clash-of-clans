using System;
using System.Collections.Generic;

namespace Kingdoms
{
    // Immutable authored combat data. Every battle creates its own mutable entities.
    public sealed class EnemyLayout
    {
        public readonly struct Building
        {
            public readonly int Id, X, Z, HitPoints, Damage, Range, HalfSize;
            public readonly string Kind;
            public Building(int id,string kind,int x,int z)
            {
                var d=BuildingCatalog.Find(kind);Id=id;Kind=kind;X=x;Z=z;HalfSize=d.Size*50;
                HitPoints=kind=="TownHall" ? 600 : kind=="Wall" ? 80 : d.IsDefense ? d.HitPoints : 300;
                Damage=d.DamagePerSecond;Range=(int)(d.Range*100);
            }
        }
        public readonly int Id, Revision=1, MinX, MaxX, MinZ, MaxZ;
        public readonly bool Sealed;
        public readonly string Name, Approach;
        public readonly IReadOnlyList<Building> Buildings;
        internal EnemyLayout(int id,string name,string approach,bool sealedWalls,List<Building> buildings,int minX=-1000,int maxX=1000,int minZ=-1400,int maxZ=-800)
        {
            Id=id;Name=name;Approach=approach;Sealed=sealedWalls;MinX=minX;MaxX=maxX;MinZ=minZ;MaxZ=maxZ;
            Buildings=Array.AsReadOnly(buildings.ToArray());
            var ids=new HashSet<int>();int halls=0;
            if(minX>=maxX || minZ>=maxZ || minX< -1700 || maxX>1700 || minZ< -1700 || maxZ>1700)throw new ArgumentException("Invalid deployment bounds.");
            foreach(var b in Buildings)
            {
                if(!ids.Add(b.Id) || b.Id<0 || b.Id>=100 || Math.Abs(b.X)+b.HalfSize>1700 || Math.Abs(b.Z)+b.HalfSize>1700)throw new ArgumentException("Invalid authored building.");
                if(b.Kind=="TownHall")halls++;
                if(b.X+b.HalfSize>minX && b.X-b.HalfSize<maxX && b.Z+b.HalfSize>minZ && b.Z-b.HalfSize<maxZ)throw new ArgumentException("Building intersects deployment zone.");
                foreach(var other in Buildings)
                    // The two legacy encounters retain their original edge-overlapping cannon/wall placement.
                    if(id>=2 && b.Id!=other.Id && Math.Abs(b.X-other.X)<b.HalfSize+other.HalfSize && Math.Abs(b.Z-other.Z)<b.HalfSize+other.HalfSize)throw new ArgumentException("Overlapping authored buildings.");
            }
            if(halls!=1)throw new ArgumentException("A layout requires one Town Hall.");
        }
    }
    public static class EnemyLayoutCatalog
    {
        public static readonly EnemyLayout Gate=Original(false), Keep=Original(true);
        public static readonly EnemyLayout Crossfire=CreateCrossfire(), Hillfort=CreateHillfort();
        public static EnemyLayout Find(int id)=>id==0 ? Gate : id==1 ? Keep : id==2 ? Crossfire : id==3 ? Hillfort : null;
        static void Wall(List<EnemyLayout.Building> b,int x,int z)=>b.Add(new EnemyLayout.Building(10+b.Count,"Wall",x,z));
        static EnemyLayout Original(bool sealedWalls)
        {
            var b=new List<EnemyLayout.Building>{new EnemyLayout.Building(0,"TownHall",0,300),new EnemyLayout.Building(1,"Cannon",-450,-100),new EnemyLayout.Building(2,"ArcherTower",450,-100)};
            for(int x=-300;x<=300;x+=100){Wall(b,x,650);if(x!=0 || sealedWalls)Wall(b,x,-50);}
            for(int z=50;z<650;z+=100){Wall(b,-300,z);Wall(b,300,z);}
            return new EnemyLayout(sealedWalls ? 1 : 0,sealedWalls ? "Sealed Keep" : "Gate Outpost",sealedWalls ? "Sealed walls: breach the enclosure." : "Open entrance: approach through the gate.",sealedWalls,b);
        }
        static EnemyLayout CreateCrossfire()
        {
            var b=new List<EnemyLayout.Building>{new EnemyLayout.Building(0,"TownHall",0,600),new EnemyLayout.Building(1,"Cannon",-600,0),new EnemyLayout.Building(2,"ArcherTower",0,-350),new EnemyLayout.Building(3,"Cannon",600,0)};
            for(int x=-800;x<=800;x+=100)if(Math.Abs(x)>=200)Wall(b,x,250);
            for(int z=450;z<=950;z+=100){Wall(b,-400,z);Wall(b,400,z);}
            return new EnemyLayout(2,"Crossfire Pass","Three defenses guard an open central approach.",false,b,-1200,1200,-1500,-950);
        }
        static EnemyLayout CreateHillfort()
        {
            var b=new List<EnemyLayout.Building>{new EnemyLayout.Building(0,"TownHall",-350,650),new EnemyLayout.Building(1,"Cannon",-800,-150),new EnemyLayout.Building(2,"ArcherTower",650,450),new EnemyLayout.Building(3,"Cannon",100,-350),new EnemyLayout.Building(4,"GoldStorage",600,-50)};
            for(int x=-650;x<=-50;x+=100){Wall(b,x,300);Wall(b,x,1000);}
            for(int z=400;z<1000;z+=100){Wall(b,-650,z);Wall(b,-50,z);}
            return new EnemyLayout(3,"Hillfort","Breach the sealed hall and destroy the storage too.",true,b,-900,900,-1500,-1000);
        }
    }
}
