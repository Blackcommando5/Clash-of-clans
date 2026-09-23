using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    // Small resolution-independent HUD illustrations, rendered as native UI geometry.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class VillageIcon : MaskableGraphic
    {
        public string kind="Gold";
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            if(kind=="Map")
            {
                Polygon(vh,r,new[]{new Vector2(.04f,.16f),new Vector2(.18f,.82f),new Vector2(.85f,.94f),new Vector2(.98f,.28f)},new Color(.98f,.9f,.68f));
                Disc(vh,r,.36f,.54f,.12f,.09f,new Color(.75f,.34f,.19f));
                Disc(vh,r,.7f,.37f,.1f,.07f,new Color(.75f,.34f,.19f));
                for(int i=0;i<4;i++)Disc(vh,r,.44f+i*.055f,.46f-i*.025f,.016f,.016f,new Color(.43f,.3f,.17f));
            }
            else if(kind=="Elixir")
            {
                Polygon(vh,r,new[]{new Vector2(.5f,.96f),new Vector2(.18f,.42f),new Vector2(.14f,.2f),new Vector2(.3f,.06f),new Vector2(.7f,.06f),new Vector2(.86f,.2f),new Vector2(.82f,.42f)},new Color(.46f,.08f,.59f));
                Polygon(vh,r,new[]{new Vector2(.5f,.84f),new Vector2(.25f,.4f),new Vector2(.22f,.22f),new Vector2(.36f,.13f),new Vector2(.68f,.13f),new Vector2(.78f,.27f),new Vector2(.7f,.47f)},new Color(.93f,.32f,.97f));
                Disc(vh,r,.4f,.36f,.1f,.17f,new Color(1,.8f,1));
            }
            else if(kind=="Gems")
            {
                Polygon(vh,r,new[]{new Vector2(.5f,.05f),new Vector2(.05f,.6f),new Vector2(.25f,.9f),new Vector2(.75f,.9f),new Vector2(.95f,.6f)},new Color(.1f,.42f,.1f));
                Polygon(vh,r,new[]{new Vector2(.5f,.12f),new Vector2(.14f,.6f),new Vector2(.33f,.83f),new Vector2(.7f,.83f),new Vector2(.86f,.6f)},new Color(.4f,.91f,.2f));
                Polygon(vh,r,new[]{new Vector2(.5f,.12f),new Vector2(.35f,.62f),new Vector2(.65f,.62f)},new Color(.74f,1,.42f));
                Polygon(vh,r,new[]{new Vector2(.35f,.62f),new Vector2(.33f,.83f),new Vector2(.7f,.83f),new Vector2(.65f,.62f)},Color.white);
            }
            else if(kind=="Builder")
            {
                Polygon(vh,r,new[]{new Vector2(.12f,.05f),new Vector2(.3f,.03f),new Vector2(.74f,.78f),new Vector2(.55f,.86f)},new Color(.48f,.25f,.09f));
                Polygon(vh,r,new[]{new Vector2(.22f,.75f),new Vector2(.39f,.96f),new Vector2(.96f,.65f),new Vector2(.8f,.41f)},new Color(.85f,.85f,.76f));
                Polygon(vh,r,new[]{new Vector2(.27f,.75f),new Vector2(.39f,.91f),new Vector2(.92f,.63f),new Vector2(.85f,.54f)},Color.white);
            }
            else if(kind=="Shop")
            {
                Quad(vh,r,.1f,.1f,.9f,.65f,new Color(.51f,.26f,.08f));
                Quad(vh,r,.14f,.16f,.86f,.61f,new Color(.91f,.64f,.26f));
                for(int i=0;i<4;i++) Quad(vh,r,.14f+i*.18f,.16f,.19f+i*.18f,.61f,new Color(.65f,.37f,.1f));
                Quad(vh,r,.08f,.59f,.92f,.7f,new Color(1,.84f,.44f));
                Quad(vh,r,.25f,.71f,.4f,.92f,new Color(1,.76f,.12f));
                Polygon(vh,r,new[]{new Vector2(.6f,.7f),new Vector2(.47f,.85f),new Vector2(.64f,.98f),new Vector2(.82f,.84f)},new Color(.43f,.95f,.23f));
            }
            else if(kind=="Info" || kind=="Tasks" || kind=="Stats" || kind=="Social" || kind=="Settings" || kind=="Move")
            {
                if(kind=="Settings")
                {
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;float x=.5f+Mathf.Cos(a)*.31f,y=.5f+Mathf.Sin(a)*.31f;Quad(vh,r,x-.1f,y-.1f,x+.1f,y+.1f,new Color(.94f,.94f,.87f));}
                    Disc(vh,r,.5f,.5f,.34f,.34f,Color.white);Disc(vh,r,.5f,.5f,.13f,.13f,new Color(.4f,.43f,.38f));
                }
                else if(kind=="Stats")
                { for(int i=0;i<3;i++)Quad(vh,r,.12f+i*.27f,.12f,.33f+i*.27f,.48f+i*.2f,Color.white); }
                else if(kind=="Tasks")
                { Quad(vh,r,.15f,.08f,.85f,.9f,new Color(.97f,.96f,.82f));Quad(vh,r,.32f,.78f,.68f,.98f,new Color(.6f,.37f,.16f));for(int i=0;i<3;i++)Quad(vh,r,.25f,.22f+i*.17f,.74f,.27f+i*.17f,new Color(.4f,.47f,.46f)); }
                else if(kind=="Social")
                { Polygon(vh,r,new[]{new Vector2(.13f,.88f),new Vector2(.87f,.88f),new Vector2(.82f,.31f),new Vector2(.5f,.05f),new Vector2(.18f,.31f)},new Color(1,.82f,.24f));Quad(vh,r,.42f,.2f,.76f,.76f,new Color(.72f,.22f,.13f));Disc(vh,r,.68f,.46f,.27f,.2f,Color.white);for(int i=0;i<3;i++)Disc(vh,r,.55f+i*.12f,.46f,.035f,.035f,Color.black); }
                else if(kind=="Move")
                { Quad(vh,r,.43f,.12f,.57f,.88f,Color.white);Quad(vh,r,.12f,.43f,.88f,.57f,Color.white);Polygon(vh,r,new[]{new Vector2(.28f,.75f),new Vector2(.5f,.98f),new Vector2(.72f,.75f)},Color.white);Polygon(vh,r,new[]{new Vector2(.75f,.28f),new Vector2(.98f,.5f),new Vector2(.75f,.72f)},Color.white); }
                else { Quad(vh,r,.12f,.22f,.88f,.94f,new Color(.16f,.56f,.8f));Polygon(vh,r,new[]{new Vector2(.35f,.25f),new Vector2(.5f,.04f),new Vector2(.65f,.25f)},new Color(.16f,.56f,.8f));Quad(vh,r,.45f,.35f,.56f,.65f,Color.white);Disc(vh,r,.5f,.77f,.07f,.07f,Color.white); }
            }
            else if(kind!="Gold")
            {
                // Neutral catalogue illustrations for systems that are not playable yet.
                Color stone=new Color(.58f,.59f,.57f),wood=new Color(.38f,.3f,.2f),metal=new Color(.23f,.25f,.25f);
                Polygon(vh,r,new[]{new Vector2(.02f,.25f),new Vector2(.5f,.06f),new Vector2(.98f,.25f),new Vector2(.5f,.47f)},new Color(.4f,.51f,.28f));
                if(kind.Contains("Bomb") || kind=="SeekingAirMine")
                { Disc(vh,r,.5f,.43f,.23f,.26f,metal);Quad(vh,r,.47f,.65f,.53f,.83f,wood);Disc(vh,r,.4f,.53f,.065f,.075f,stone); }
                else if(kind=="Cannon" || kind=="Mortar")
                { Quad(vh,r,.24f,.23f,.76f,.4f,wood);Polygon(vh,r,new[]{new Vector2(.32f,.34f),new Vector2(.49f,.34f),new Vector2(.82f,.72f),new Vector2(.6f,.91f),new Vector2(.41f,.75f)},metal);Disc(vh,r,.69f,.8f,.14f,.1f,stone);Disc(vh,r,.69f,.8f,.09f,.065f,Color.black); }
                else if(kind=="Wall")
                { Quad(vh,r,.3f,.23f,.69f,.66f,stone);Quad(vh,r,.23f,.61f,.76f,.73f,new Color(.73f,.73f,.65f));for(int i=0;i<3;i++)Quad(vh,r,.24f+i*.2f,.7f,.36f+i*.2f,.82f,stone); }
                else if(kind=="ArcherTower" || kind=="AirDefense")
                { Quad(vh,r,.24f,.22f,.34f,.76f,wood);Quad(vh,r,.65f,.22f,.75f,.76f,wood);Quad(vh,r,.15f,.68f,.85f,.78f,stone);Quad(vh,r,.3f,.79f,.71f,.91f,wood); }
                else if(kind=="ArmyCamp" || kind=="SpringTrap")
                { for(int i=0;i<5;i++)Disc(vh,r,.22f+i*.14f,.3f+Mathf.Sin(i*2f)*.07f,.09f,.06f,stone);Polygon(vh,r,new[]{new Vector2(.32f,.34f),new Vector2(.5f,.76f),new Vector2(.65f,.32f)},new Color(.84f,.51f,.22f)); }
                else
                { Quad(vh,r,.24f,.27f,.77f,.63f,wood);Polygon(vh,r,new[]{new Vector2(.12f,.57f),new Vector2(.52f,.91f),new Vector2(.9f,.58f)},stone);Quad(vh,r,.43f,.26f,.61f,.52f,metal); }
            }
            else
            {
                Disc(vh,r,.5f,.48f,.43f,.44f,new Color(.57f,.3f,.04f));
                Disc(vh,r,.5f,.54f,.4f,.4f,new Color(1,.72f,.08f));
                Disc(vh,r,.5f,.54f,.29f,.29f,new Color(1,.88f,.31f));
                Quad(vh,r,.43f,.33f,.56f,.75f,new Color(.83f,.49f,.03f));
            }
        }
        static void Quad(VertexHelper v,Rect r,float x0,float y0,float x1,float y1,Color c)
            => Polygon(v,r,new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)},c);
        static void Disc(VertexHelper v,Rect r,float x,float y,float rx,float ry,Color c)
        {
            var points=new Vector2[24];for(int i=0;i<24;i++){float a=i*Mathf.PI/12;points[i]=new Vector2(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry);}Polygon(v,r,points,c);
        }
        static void Polygon(VertexHelper v,Rect r,Vector2[] points,Color c)
        {
            int start=v.currentVertCount;
            foreach(var p in points) v.AddVert(new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height),c,Vector2.zero);
            for(int i=1;i<points.Length-1;i++) v.AddTriangle(start,start+i,start+i+1);
        }
    }
}
