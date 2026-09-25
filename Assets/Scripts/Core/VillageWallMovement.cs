using System;
using System.Collections.Generic;

namespace Kingdoms
{
    public sealed partial class VillageState
    {
        // Connections use shared cell edges. Diagonal contact never joins two sections.
        public List<int> ConnectedWalls(int index)
        {
            var connected=new List<int>();
            if(index<0 || index>=buildings.Count || buildings[index].kind!="Wall")return connected;
            var visited=new HashSet<int>{index};connected.Add(index);
            for(int next=0;next<connected.Count;next++)
            {
                var wall=buildings[connected[next]];
                for(int i=0;i<buildings.Count;i++)
                {
                    var other=buildings[i];
                    if(other.kind=="Wall" && !visited.Contains(i) && Math.Abs((long)wall.x-other.x)+Math.Abs((long)wall.z-other.z)==1)
                    {visited.Add(i);connected.Add(i);}
                }
            }
            connected.Sort();return connected;
        }

        public bool CanMoveConnectedWalls(int index,int x,int z,out string reason)
            => ValidateWallMove(index,x,z,out _,out reason);

        bool ValidateWallMove(int index,int x,int z,out List<int> connected,out string reason)
        {
            connected=ConnectedWalls(index);
            if(connected.Count==0){reason="Select a wall to move its connected section.";return false;}
            if(x< -22 || x>21 || z< -22 || z>21){reason="Keep every wall inside the village border.";return false;}
            long dx=(long)x-buildings[index].x,dz=(long)z-buildings[index].z;
            var moving=new HashSet<int>(connected);
            foreach(int i in connected)
            {
                long targetX=buildings[i].x+dx,targetZ=buildings[i].z+dz;
                if(targetX< -22 || targetX>21 || targetZ< -22 || targetZ>21)
                {reason="Keep every wall inside the village border.";return false;}
                for(int j=0;j<buildings.Count;j++)
                {
                    if(moving.Contains(j))continue;
                    var obstacle=buildings[j];
                    if(targetX<obstacle.x+obstacle.Size && targetX+1>obstacle.x && targetZ<obstacle.z+obstacle.Size && targetZ+1>obstacle.z)
                    {reason="A wall would overlap another building. Move the whole section to empty cells.";return false;}
                }
            }
            reason="Move "+connected.Count+" connected wall"+(connected.Count==1 ? "" : "s")+" | Free | Levels and upgrades kept.";
            return true;
        }

        public bool TryMoveConnectedWalls(int index,int x,int z,out string reason)
        {
            if(!ValidateWallMove(index,x,z,out var connected,out reason))return false;
            int dx=x-buildings[index].x,dz=z-buildings[index].z;
            foreach(int i in connected){buildings[i].x+=dx;buildings[i].z+=dz;}
            reason="Moved "+connected.Count+" connected wall"+(connected.Count==1 ? "." : "s.");return true;
        }
    }
}
