using System;
using System.Collections.Generic;

namespace Kingdoms
{
    public enum PracticeOutcome { Running, Victory, Defeat, Surrendered, Timeout }

    // Integer positions (100 units/cell), fixed 100 ms ticks, stable list/ID ordering.
    // This encounter owns all combat state and never receives a VillageState.
    public sealed class PracticeBattle
    {
        public const int TickMilliseconds=100, ArmySize=8, MaximumArmySize=80, TimeLimitTicks=1800;
        public sealed class Entity
        {
            public int Id, X, Z, HitPoints, MaxHitPoints, Damage, Range, HalfSize, NextAttack;
            public string Kind;
            public bool Alive=>HitPoints>0;
        }
        public struct Strike { public int From,To; public Strike(int from,int to){From=from;To=to;} }
        public readonly struct Deployment
        {
            public readonly int Tick,X,Z;
            public readonly string Troop;
            public Deployment(int tick,int x,int z,string troop){Tick=tick;X=x;Z=z;Troop=troop;}
        }
        public sealed class Recording
        {
            public const int RulesVersion=5;
            public readonly bool SealedEnclosure;
            public readonly int EndTick, ArmyBudget, ArcherBudget, TankBudget;
            public readonly PracticeOutcome Outcome;
            public readonly ulong FinalHash;
            public readonly IReadOnlyList<Deployment> Deployments;
            internal Recording(PracticeBattle battle)
            {
                TankBudget=battle.TankBudget;ArcherBudget=battle.ArcherBudget;ArmyBudget=battle.ArmyBudget;SealedEnclosure=battle.sealedEnclosure;EndTick=battle.Tick;Outcome=battle.Outcome;
                FinalHash=battle.StateHash();Deployments=Array.AsReadOnly(battle.deployments.ToArray());
            }
        }
        readonly bool sealedEnclosure;
        public bool SealedEnclosure=>sealedEnclosure;
        readonly List<Deployment> deployments=new List<Deployment>();
        readonly List<Entity> buildings=new List<Entity>();
        readonly List<Entity> raiders=new List<Entity>();
        readonly List<Strike> strikes=new List<Strike>();
        public IReadOnlyList<Entity> Buildings=>buildings;
        public IReadOnlyList<Entity> Raiders=>raiders;
        public IReadOnlyList<Strike> Strikes=>strikes;
        public int Tick {get;private set;}
        public int ArmyBudget {get;}
        public int ArcherBudget {get;}
        public int TankBudget {get;}
        public int RaiderBudget => ArmyBudget-ArcherBudget-TankBudget;
        public int RemainingOf(string troop) => troop=="Raider" ? RaiderBudget-raiders.FindAll(r=>r.Kind=="Raider").Count : troop=="Archer" ? ArcherBudget-raiders.FindAll(r=>r.Kind=="Archer").Count : troop=="Tank" ? TankBudget-raiders.FindAll(r=>r.Kind=="Tank").Count : 0;
        public int Remaining=>ArmyBudget-raiders.Count;
        public PracticeOutcome Outcome {get;private set;}=PracticeOutcome.Running;
        public int DestroyedBuildings=>buildings.FindAll(b=>b.Kind!="Wall" && !b.Alive).Count;
        public int TotalBuildings=>buildings.FindAll(b=>b.Kind!="Wall").Count;
        public int Destruction=>DestroyedBuildings*100/TotalBuildings;
        public int Survivors=>raiders.FindAll(r=>r.Alive).Count;
        // Practice score only: each independent objective earns one star, with no economy reward.
        public int Stars=>(buildings.Exists(b=>b.Kind=="TownHall" && !b.Alive) ? 1 : 0)
            +(DestroyedBuildings*2>=TotalBuildings ? 1 : 0)+(DestroyedBuildings==TotalBuildings ? 1 : 0);
        const int GridSize=73, GridStep=50, GridOffset=1800;
        sealed class Route { public Entity Target; public readonly Queue<int> Cells=new Queue<int>(); }
        readonly Dictionary<int,Route> routes=new Dictionary<int,Route>();

        public PracticeBattle(bool sealedEnclosure=false, int armyBudget=ArmySize, int archerBudget=0, int tankBudget=0)
        {
            if(armyBudget<1 || armyBudget>MaximumArmySize || archerBudget<0 || tankBudget<0 || (long)archerBudget+tankBudget>armyBudget || (long)armyBudget+archerBudget+3L*tankBudget>MaximumArmySize)throw new ArgumentOutOfRangeException(nameof(armyBudget));
            TankBudget=tankBudget;ArcherBudget=archerBudget;ArmyBudget=armyBudget;this.sealedEnclosure=sealedEnclosure;
            buildings.Add(new Entity{Id=0,Kind="TownHall",X=0,Z=300,HitPoints=600,MaxHitPoints=600,HalfSize=200});
            AddDefense(BuildingCatalog.Cannon,1,-450,-100);
            AddDefense(BuildingCatalog.ArcherTower,2,450,-100);
            for(int x=-300;x<=300;x+=100)
            {
                AddWall(x,650);
                if(x!=0 || sealedEnclosure)AddWall(x,-50);
            }
            for(int z=50;z<650;z+=100){AddWall(-300,z);AddWall(300,z);}
        }
        void AddWall(int x,int z)=>buildings.Add(new Entity{Id=10+buildings.Count,Kind="Wall",X=x,Z=z,HalfSize=50,HitPoints=80,MaxHitPoints=80});
        void AddDefense(BuildingDefinition definition,int id,int x,int z)
        {
            buildings.Add(new Entity{Id=id,Kind=definition.Id,X=x,Z=z,HalfSize=definition.Size*50,
                HitPoints=definition.HitPoints,MaxHitPoints=definition.HitPoints,Damage=definition.DamagePerSecond,Range=(int)(definition.Range*100)});
        }
        // Southern entry strip, in hundredths of a world cell. Shared by rules and the ground overlay.
        public const int DeploymentMinX=-1000, DeploymentMaxX=1000, DeploymentMinZ=-1400, DeploymentMaxZ=-800;
        public bool CanDeployAt(int x,int z,string troop,out string reason)
        {
            if(Outcome!=PracticeOutcome.Running){reason="This attack has ended.";return false;}
            if(TroopCatalog.Find(troop)==null){reason="Choose a valid troop.";return false;}
            if(RemainingOf(troop)<=0){reason="No "+troop+"s remain. Select another troop type.";return false;}
            if(x<DeploymentMinX || x>DeploymentMaxX || z<DeploymentMinZ || z>DeploymentMaxZ)
            {reason="Deploy inside the outlined southern area.";return false;}
            if(Blocked(x,z)){reason="That deployment point is occupied.";return false;}
            reason="Ready to deploy.";return true;
        }
        public bool DeployAt(int x,int z,string troop="Raider")
        {
            if(!CanDeployAt(x,z,troop,out _))return false;
            deployments.Add(new Deployment(Tick,x,z,troop));
            var definition=TroopCatalog.Find(troop);
            raiders.Add(new Entity{Id=100+raiders.Count,Kind=troop,X=x,Z=z,
                HitPoints=definition.HitPoints,MaxHitPoints=definition.HitPoints,Damage=definition.Damage,Range=definition.Range});
            return true;
        }
        public bool Deploy(int lane, string troop="Raider")
        {
            if(lane<0 || lane>2)return false;
            return DeployAt((lane-1)*650+(raiders.Count%3-1)*35,-1350,troop);
        }
        static long DistanceSquared(Entity a,Entity b)
        {long x=a.X-b.X,z=a.Z-b.Z;return x*x+z*z;}
        static long EdgeDistanceSquared(Entity a,Entity b)
        {long x=Math.Max(0,Math.Abs(a.X-b.X)-b.HalfSize),z=Math.Max(0,Math.Abs(a.Z-b.Z)-b.HalfSize);return x*x+z*z;}
        Entity Closest(Entity source,List<Entity> candidates,bool edge)
        {
            Entity nearest=null;long best=long.MaxValue;
            foreach(var entity in candidates)
            {
                if(!entity.Alive)continue;
                long distance=edge ? EdgeDistanceSquared(source,entity) : DistanceSquared(source,entity);
                if(distance<best){nearest=entity;best=distance;}
            }
            return nearest;
        }
        void Attack(Entity attacker,Entity target)
        {
            if(Tick<attacker.NextAttack)return;
            target.HitPoints=Math.Max(0,target.HitPoints-attacker.Damage);
            if(!target.Alive)routes.Clear();
            attacker.NextAttack=Tick+10;strikes.Add(new Strike(attacker.Id,target.Id));
        }
        static int Cell(int x,int z)=>((z+GridOffset)/GridStep)*GridSize+(x+GridOffset)/GridStep;
        static int CellX(int cell)=>(cell%GridSize)*GridStep-GridOffset;
        static int CellZ(int cell)=>(cell/GridSize)*GridStep-GridOffset;
        bool Blocked(int x,int z,Entity except=null)
        {
            foreach(var b in buildings)
                if(b.Alive && b!=except && Math.Abs(x-b.X)<b.HalfSize+15 && Math.Abs(z-b.Z)<b.HalfSize+15)return true;
            return false;
        }
        bool CanHit(Entity from,Entity target)
        {
            if(EdgeDistanceSquared(from,target)>(long)from.Range*from.Range)return false;
            int x=Math.Max(target.X-target.HalfSize,Math.Min(target.X+target.HalfSize,from.X));
            int z=Math.Max(target.Z-target.HalfSize,Math.Min(target.Z+target.HalfSize,from.Z));
            for(int i=0;i<=10;i++)if(Blocked(from.X+(x-from.X)*i/10,from.Z+(z-from.Z)*i/10,target))return false;
            return true;
        }
        Route FindRoute(Entity raider,bool walls)
        {
            bool preferDefenses=raider.Kind=="Tank" && !walls && buildings.Exists(b=>b.Alive && b.Damage>0);
            int start=Cell(raider.X+GridStep/2,raider.Z+GridStep/2);
            var previous=new int[GridSize*GridSize];for(int i=0;i<previous.Length;i++)previous[i]=-1;
            var pending=new Queue<int>();pending.Enqueue(start);previous[start]=start;
            var probe=new Entity{Range=raider.Range};
            // Cardinal neighbours prevent corner cutting; ordering makes equal routes repeatable.
            int[] dx={0,-1,1,0},dz={1,0,0,-1};
            while(pending.Count>0)
            {
                int cell=pending.Dequeue();probe.X=CellX(cell);probe.Z=CellZ(cell);
                foreach(var b in buildings)
                {
                    if(!b.Alive || (preferDefenses && b.Damage==0) || (b.Kind=="Wall")!=walls || !CanHit(probe,b))continue;
                    var route=new Route{Target=b};var reverse=new Stack<int>();
                    for(int c=cell;c!=start;c=previous[c])reverse.Push(c);
                    route.Cells.Enqueue(start);while(reverse.Count>0)route.Cells.Enqueue(reverse.Pop());return route;
                }
                for(int i=0;i<4;i++)
                {
                    int x=cell%GridSize+dx[i],z=cell/GridSize+dz[i];
                    if(x<0 || x>=GridSize || z<0 || z>=GridSize)continue;
                    int next=z*GridSize+x;
                    if(previous[next]!=-1 || Blocked(CellX(next),CellZ(next)))continue;
                    previous[next]=cell;pending.Enqueue(next);
                }
            }
            return null;
        }
        void MoveOrAttack(Entity raider)
        {
            if(!routes.TryGetValue(raider.Id,out var route))
            {
                route=FindRoute(raider,false) ?? FindRoute(raider,true);
                if(route==null)return;
                routes[raider.Id]=route;
            }
            if(CanHit(raider,route.Target)){Attack(raider,route.Target);return;}
            if(route.Cells.Count==0){routes.Remove(raider.Id);return;}
            int cell=route.Cells.Peek(),dx=CellX(cell)-raider.X,dz=CellZ(cell)-raider.Z;
            int distance=Math.Max(1,(int)Math.Ceiling(Math.Sqrt((long)dx*dx+(long)dz*dz)));
            int step=Math.Min(TroopCatalog.Find(raider.Kind).Speed,distance);
            raider.X+=dx*step/distance;raider.Z+=dz*step/distance;
            if(raider.X==CellX(cell) && raider.Z==CellZ(cell))route.Cells.Dequeue();
        }
        public void Step()
        {
            strikes.Clear();if(Outcome!=PracticeOutcome.Running)return;
            Tick++;
            foreach(var defense in buildings)
            {
                if(!defense.Alive || defense.Damage==0)continue;
                var target=Closest(defense,raiders,false);
                if(target!=null && DistanceSquared(defense,target)<=(long)defense.Range*defense.Range)Attack(defense,target);
            }
            foreach(var raider in raiders)
            {
                if(!raider.Alive)continue;
                MoveOrAttack(raider);
            }
            if(buildings.TrueForAll(b=>b.Kind=="Wall" || !b.Alive))Outcome=PracticeOutcome.Victory;
            else if(Remaining==0 && raiders.TrueForAll(r=>!r.Alive))Outcome=PracticeOutcome.Defeat;
            else if(Tick>=TimeLimitTicks)Outcome=PracticeOutcome.Timeout;
        }
        public void Surrender(){if(Outcome==PracticeOutcome.Running)Outcome=PracticeOutcome.Surrendered;strikes.Clear();}
        public Recording Record()
        {
            if(Outcome==PracticeOutcome.Running)throw new InvalidOperationException("Finish the encounter before recording its result.");
            return new Recording(this);
        }
        // Stable numeric FNV-1a hash; no runtime-dependent string hash codes.
        public ulong StateHash()
        {
            ulong hash=14695981039346656037UL;
            Action<int> add=value=>{unchecked{for(int i=0;i<4;i++){hash^=(byte)(value>>(i*8));hash*=1099511628211UL;}}};
            add(Recording.RulesVersion);add(ArmyBudget);add(ArcherBudget);add(TankBudget);add(sealedEnclosure ? 1 : 0);add(Tick);add((int)Outcome);add(Remaining);
            foreach(var list in new[]{buildings,raiders})
            {
                add(list.Count);
                foreach(var e in list){add(e.Id);add(e.Kind=="Archer" ? 1 : e.Kind=="Tank" ? 2 : 0);add(e.X);add(e.Z);add(e.HitPoints);add(e.MaxHitPoints);add(e.Damage);add(e.Range);add(e.HalfSize);add(e.NextAttack);}
            }
            return hash;
        }
    }

    // Replays accepted commands at their original tick, independently of frame rate.
    public sealed class PracticeReplay
    {
        readonly PracticeBattle.Recording recording;
        int nextCommand;
        public PracticeBattle Battle {get;}
        public bool Finished=>Battle.Outcome!=PracticeOutcome.Running;
        public bool Matches=>Finished && Battle.Tick==recording.EndTick && Battle.Outcome==recording.Outcome && Battle.StateHash()==recording.FinalHash;
        public PracticeReplay(PracticeBattle.Recording recording)
        {
            this.recording=recording ?? throw new ArgumentNullException(nameof(recording));
            Battle=new PracticeBattle(recording.SealedEnclosure,recording.ArmyBudget,recording.ArcherBudget,recording.TankBudget);ApplyCommands();
        }
        void ApplyCommands()
        {
            while(nextCommand<recording.Deployments.Count && recording.Deployments[nextCommand].Tick==Battle.Tick)
            {
                var command=recording.Deployments[nextCommand++];Battle.DeployAt(command.X,command.Z,command.Troop);
            }
            if(Battle.Tick==recording.EndTick && recording.Outcome==PracticeOutcome.Surrendered)Battle.Surrender();
        }
        public void Step()
        {
            if(Finished){Battle.Step();return;}
            Battle.Step();ApplyCommands();
        }
    }
}
