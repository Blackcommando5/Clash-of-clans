using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Kingdoms.UI;

// Editable level-one silhouettes measured from the user's September reference images.
public static class ScreenshotReferenceArt
{
    const string Root="Assets/Art/ScreenshotReference";
    static Material timber,beam,brass,iron,elixir,stone,grass,glass;
    static Material Mat(string name,Color color)
    {
        string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};m.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(m,path);return m;
    }
    static GameObject Part(GameObject root,string name,PrimitiveType shape,Vector3 p,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root.transform,false);go.transform.localPosition=p;go.transform.localScale=scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    static void Box(GameObject root,string name,Vector3 p,Vector3 scale,Material material)=>Part(root,name,PrimitiveType.Cube,p,scale,material);
    static void Rod(GameObject root,string name,Vector3 a,Vector3 b,float thickness,Material material)
    { var go=Part(root,name,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(thickness,(b-a).magnitude*.5f,thickness),material);go.transform.up=(b-a).normalized; }
    static void Ring(GameObject root,string name,float y,float radius,float thickness,Material material)
    {
        for(int i=0;i<20;i++)
        {float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;Rod(root,name,new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius),new Vector3(Mathf.Cos(b)*radius,y,Mathf.Sin(b)*radius),thickness,material);}
    }
    public static void Build()
    {
        Directory.CreateDirectory(Root);Directory.CreateDirectory("Assets/Prefabs/ScreenshotReference");AssetDatabase.Refresh();
        timber=Mat("Weathered timber",new Color(.46f,.28f,.12f));beam=Mat("Timber ends",new Color(.7f,.48f,.23f));brass=Mat("Warm brass",new Color(.96f,.7f,.13f));
        iron=Mat("Dark iron",new Color(.25f,.28f,.25f));elixir=Mat("Bright elixir",new Color(.85f,.04f,.95f));stone=Mat("Pale stone",new Color(.55f,.53f,.45f));grass=Mat("Grass platform",new Color(.37f,.57f,.14f));
        glass=Mat("Vessel glass",new Color(.87f,.94f,1,.19f));glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Smoothness",.85f);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;glass.SetOverrideTag("RenderType","Transparent");
        foreach(string kind in new[]{"GoldMine","ElixirCollector","GoldStorage","ElixirStorage","Cannon","ArcherTower"})
        {
            string path="Assets/Prefabs/ScreenshotReference/"+kind+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)
            {
                var root=new GameObject(kind);
                Box(root,"Grass footprint",new Vector3(0,.045f,0),new Vector3(2.9f,.09f,2.9f),grass);
                if(kind=="GoldMine")Mine(root);else if(kind=="ElixirCollector")Collector(root);else if(kind=="GoldStorage")GoldStorage(root);else if(kind=="Cannon")Cannon(root);else if(kind=="ArcherTower")ArcherTower(root);else ElixirStorage(root);
                float height=kind=="ArcherTower" ? 3.8f : 2.7f;
                var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,height*.5f,0);collider.size=new Vector3(3,height,3);
                prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
            }
            typeof(HomeVillageArtFactory).GetMethod("RenderIcon",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{prefab,kind});
        }
        var hall=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HomeVillage/TownHall.prefab");
        if(hall!=null)typeof(HomeVillageArtFactory).GetMethod("RenderIcon",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{hall,"TownHall"});
        AssetDatabase.SaveAssets();
    }
    static void Cannon(GameObject r)
    {
        Box(r,"Carriage",new Vector3(0,.35f,0),new Vector3(1.75f,.44f,1.5f),timber);
        foreach(float x in new[]{-.72f,.72f})Box(r,"Carriage runner",new Vector3(x,.25f,0),new Vector3(.3f,.27f,2.1f),beam);
        Rod(r,"Axle",new Vector3(-1.04f,.53f,.3f),new Vector3(1.04f,.53f,.3f),.24f,iron);
        foreach(float x in new[]{-.97f,.97f})Rod(r,"Wheel",new Vector3(x-.09f,.53f,.3f),new Vector3(x+.09f,.53f,.3f),.87f,iron);
        var breech=new Vector3(0,.84f,.55f);var muzzle=new Vector3(0,1.17f,-1.06f);
        Rod(r,"Iron barrel",breech,muzzle,.75f,iron);
        Part(r,"Breech",PrimitiveType.Sphere,breech,new Vector3(.74f,.74f,.74f),iron);
        var direction=(muzzle-breech).normalized;
        Rod(r,"Muzzle rim",muzzle-direction*.11f,muzzle+direction*.05f,.89f,stone);
        Rod(r,"Muzzle opening",muzzle+direction*.051f,muzzle+direction*.06f,.61f,Mat("Barrel interior",new Color(.035f,.04f,.037f)));
        foreach(float z in new[]{-.4f,.4f})Rod(r,"Carriage bolt",new Vector3(-.88f,.63f,z),new Vector3(.88f,.63f,z),.1f,brass);
    }
    static void ArcherTower(GameObject r)
    {
        foreach(float x in new[]{-.88f,.88f})foreach(float z in new[]{-.88f,.88f})
        { Rod(r,"Tower leg",new Vector3(x,.12f,z),new Vector3(x*.73f,2.85f,z*.73f),.25f,timber);Box(r,"Footing",new Vector3(x,.15f,z),new Vector3(.44f,.27f,.44f),stone); }
        foreach(float side in new[]{-1f,1f})
        {
            Rod(r,"Cross brace",new Vector3(-.85f,.7f,side*.82f),new Vector3(.71f,2.3f,side*.72f),.14f,beam);
            Rod(r,"Cross brace",new Vector3(.85f,.7f,side*.82f),new Vector3(-.71f,2.3f,side*.72f),.14f,beam);
            Box(r,"Platform edge",new Vector3(side*1.1f,2.72f,0),new Vector3(.13f,.27f,2.3f),beam);
        }
        for(int i=0;i<8;i++)Box(r,"Platform plank",new Vector3(-.98f+i*.28f,2.74f,0),new Vector3(.265f,.15f,2.15f),timber);
        foreach(float z in new[]{-1f,1f})Box(r,"Guard rail",new Vector3(0,3.04f,z),new Vector3(2.2f,.38f,.13f),beam);
        foreach(float x in new[]{-.29f,.29f})Rod(r,"Ladder rail",new Vector3(x,.1f,-1.26f),new Vector3(x,2.73f,-.94f),.11f,beam);
        for(int i=0;i<8;i++)Rod(r,"Ladder rung",new Vector3(-.3f,.3f+i*.31f,-1.24f+i*.038f),new Vector3(.3f,.3f+i*.31f,-1.24f+i*.038f),.09f,timber);
        // A mounted bow gives this platform a readable defensive silhouette.
        Box(r,"Bow mount",new Vector3(0,3.09f,0),new Vector3(.22f,.59f,.24f),iron);
        Rod(r,"Bow stock",new Vector3(0,3.35f,.4f),new Vector3(0,3.35f,-.65f),.16f,timber);
        Rod(r,"Bow left limb",new Vector3(-.65f,3.35f,-.35f),new Vector3(0,3.35f,-.65f),.1f,beam);
        Rod(r,"Bow right limb",new Vector3(.65f,3.35f,-.35f),new Vector3(0,3.35f,-.65f),.1f,beam);
        Rod(r,"Bow string",new Vector3(-.65f,3.35f,-.35f),new Vector3(.65f,3.35f,-.35f),.025f,iron);
    }

    static void Mine(GameObject r)
    {
        Box(r,"Cave opening",new Vector3(0,.68f,0),new Vector3(1.62f,1.27f,1.8f),iron);
        for(int i=0;i<5;i++)
        {
            float x=-.93f+i*.465f;
            Rod(r,"Roof logs",new Vector3(x,1.53f-Mathf.Abs(x)*.45f,-1.05f),new Vector3(x,1.53f-Mathf.Abs(x)*.45f,1),.34f,timber);
        }
        foreach(float x in new[]{-.9f,.9f})
        {
            Rod(r,"Portal post",new Vector3(x,.15f,-1),new Vector3(x,1.3f,-1),.32f,timber);
            Rod(r,"Cut log end",new Vector3(x,1.31f,-1.12f),new Vector3(x,1.31f,-1.18f),.27f,beam);
        }
        Rod(r,"Portal header",new Vector3(-1.06f,1.35f,-1.06f),new Vector3(1.06f,1.35f,-1.06f),.35f,timber);
        for(int z=0;z<5;z++)Box(r,"Rail sleeper",new Vector3(.13f,.12f,-1.4f+z*.4f),new Vector3(1,.09f,.14f),timber);
        foreach(float x in new[]{-.22f,.47f})Box(r,"Track",new Vector3(x,.19f,-.68f),new Vector3(.07f,.08f,1.7f),iron);
        Box(r,"Ore cart",new Vector3(.6f,.37f,-1.08f),new Vector3(.68f,.45f,.5f),iron);
        for(int i=0;i<6;i++)Part(r,"Gold ore",PrimitiveType.Sphere,new Vector3(.4f+i%3*.2f,.64f,-1.2f+i/3*.2f),new Vector3(.21f,.17f,.22f),brass);
        Part(r,"Rear rock",PrimitiveType.Sphere,new Vector3(.9f,.49f,.6f),new Vector3(.8f,.95f,1),stone);
    }
    static void Collector(GameObject r)
    {
        Part(r,"Stone base",PrimitiveType.Cylinder,new Vector3(0,.17f,0),new Vector3(1.65f,.12f,1.65f),stone);
        Part(r,"Pump vat",PrimitiveType.Cylinder,new Vector3(0,.77f,0),new Vector3(1.35f,.53f,1.35f),timber);
        Part(r,"Visible elixir",PrimitiveType.Cylinder,new Vector3(0,.82f,0),new Vector3(1.39f,.34f,1.39f),elixir);
        Ring(r,"Lower brass rim",.47f,.73f,.13f,brass);Ring(r,"Upper brass rim",1.21f,.73f,.14f,brass);
        foreach(float x in new[]{-.53f,.53f})foreach(float z in new[]{-.53f,.53f})Box(r,"Tank upright",new Vector3(x,.83f,z),new Vector3(.14f,.86f,.14f),brass);
        Part(r,"Pump lid",PrimitiveType.Cylinder,new Vector3(0,1.34f,0),new Vector3(1.29f,.1f,1.29f),timber);
        Rod(r,"Suction pipe",new Vector3(-1,.14f,.16f),new Vector3(-1,2.2f,.16f),.15f,iron);
        Rod(r,"Pipe bend",new Vector3(-1,2.2f,.16f),new Vector3(-.7f,2.42f,.16f),.15f,iron);
        Rod(r,"Pipe top",new Vector3(-.7f,2.42f,.16f),new Vector3(.2f,2.24f,.16f),.15f,iron);
        Rod(r,"Inlet",new Vector3(.2f,2.24f,.16f),new Vector3(.2f,1.4f,.16f),.15f,brass);
        Part(r,"Valve",PrimitiveType.Cylinder,new Vector3(.6f,1.49f,.3f),new Vector3(.46f,.05f,.46f),brass);
    }
    static void GoldStorage(GameObject r)
    {
        Box(r,"Vault floor",new Vector3(0,.13f,0),new Vector3(2.2f,.2f,2.2f),timber);
        foreach(float side in new[]{-1f,1f})
        {
            Box(r,"Low side",new Vector3(side*1.08f,.44f,0),new Vector3(.15f,.61f,2.2f),stone);
            Box(r,"Low end",new Vector3(0,.44f,side*1.08f),new Vector3(2.2f,.61f,.15f),stone);
        }
        var random=new System.Random(71);
        for(int i=0;i<40;i++)
        {
            float x=(float)random.NextDouble()*1.6f-.8f,z=(float)random.NextDouble()*1.6f-.8f;
            Part(r,"Gold nugget",PrimitiveType.Sphere,new Vector3(x,.46f+(1-Mathf.Max(Mathf.Abs(x),Mathf.Abs(z)))*.65f,z),new Vector3(.33f,.22f,.28f),brass);
        }
        foreach(float x in new[]{-1.08f,1.08f})foreach(float z in new[]{-1.08f,1.08f})Box(r,"Corner post",new Vector3(x,.54f,z),new Vector3(.23f,.85f,.23f),timber);
    }
    static void ElixirStorage(GameObject r)
    {
        Part(r,"Vessel foot",PrimitiveType.Cylinder,new Vector3(0,.18f,0),new Vector3(1.3f,.11f,1.3f),stone);
        Part(r,"Elixir globe",PrimitiveType.Sphere,new Vector3(0,.58f,0),new Vector3(1.5f,.93f,1.5f),elixir);
        Part(r,"Glass dome",PrimitiveType.Sphere,new Vector3(0,.78f,0),new Vector3(1.67f,1.37f,1.67f),glass);
        Ring(r,"Vessel band",.52f,.8f,.09f,brass);
        foreach(float x in new[]{-.86f,.86f})
        {
            Box(r,"Wood support",new Vector3(x,.46f,0),new Vector3(.18f,.85f,.38f),timber);
            Box(r,"Support cap",new Vector3(x,.88f,0),new Vector3(.23f,.09f,.43f),beam);
        }
        Part(r,"Glass glint",PrimitiveType.Sphere,new Vector3(-.43f,1.1f,-.46f),new Vector3(.14f,.23f,.045f),Mat("Glass highlight",new Color(.97f,.91f,1)));
    }
}
