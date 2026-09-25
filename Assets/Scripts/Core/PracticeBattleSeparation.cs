using System;

namespace Kingdoms
{
    public sealed partial class PracticeBattle
    {
        readonly int[] separationX=new int[MaximumArmySize],separationZ=new int[MaximumArmySize];
        static readonly int[] separationDX={1,1,0,-1,-1,-1,0,1},separationDZ={0,1,1,1,0,-1,-1,-1};
        static int CrowdRadius(Entity troop)=>troop.Kind=="Tank" ? 32 : 24;

        bool ClearMovement(int x,int z,int nextX,int nextZ)
        {
            if(nextX< -1800 || nextX>1800 || nextZ< -1800 || nextZ>1800)return false;
            int steps=Math.Max(Math.Abs(nextX-x),Math.Abs(nextZ-z));
            for(int i=1;i<=steps;i++)
                if(Blocked(x+(nextX-x)*i/steps,z+(nextZ-z)*i/steps))return false;
            return !Blocked(nextX,nextZ);
        }

        void SeparateTroops()
        {
            Array.Clear(separationX,0,raiders.Count);Array.Clear(separationZ,0,raiders.Count);
            // Accumulate from the same positions, then apply bounded corrections in stable ID order.
            for(int i=0;i<raiders.Count;i++)
            {
                var a=raiders[i];if(!a.Alive)continue;
                for(int j=i+1;j<raiders.Count;j++)
                {
                    var b=raiders[j];if(!b.Alive)continue;
                    int dx=a.X-b.X,dz=a.Z-b.Z,spacing=CrowdRadius(a)+CrowdRadius(b);
                    long squared=(long)dx*dx+(long)dz*dz;if(squared>=(long)spacing*spacing)continue;
                    if(squared==0)
                    {
                        int direction=(a.Id*3+b.Id*5)&7;dx=separationDX[direction];dz=separationDZ[direction];
                        squared=dx*dx+dz*dz;
                    }
                    int distance=(int)Math.Ceiling(Math.Sqrt(squared));
                    int strength=(spacing-distance+1)/2;
                    int pushX=dx*strength/distance,pushZ=dz*strength/distance;
                    separationX[i]+=pushX;separationZ[i]+=pushZ;
                    separationX[j]-=pushX;separationZ[j]-=pushZ;
                }
            }
            for(int i=0;i<raiders.Count;i++)
            {
                var troop=raiders[i];if(!troop.Alive)continue;
                int dx=separationX[i],dz=separationZ[i];
                int distance=(int)Math.Ceiling(Math.Sqrt((long)dx*dx+(long)dz*dz));
                if(distance==0)continue;
                const int maxShift=12;
                if(distance>maxShift){dx=dx*maxShift/distance;dz=dz*maxShift/distance;}
                int x=troop.X+dx,z=troop.Z+dz;
                // Keep the rounded navigation cell reachable too, so separation cannot strand a route at a corner.
                int cell=Cell(x+GridStep/2,z+GridStep/2);
                if(ClearMovement(troop.X,troop.Z,x,z) && ClearMovement(x,z,CellX(cell),CellZ(cell)))
                {troop.X=x;troop.Z=z;}
            }
        }
    }
}
