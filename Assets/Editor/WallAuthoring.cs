using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Kingdoms;
using Kingdoms.UI;

public static class WallAuthoring
{
    public static void Build()
    {
        const string dir="Assets/Art/Walls";
        Directory.CreateDirectory(dir);Directory.CreateDirectory("Assets/Prefabs/Walls");AssetDatabase.Refresh();
        var stone=Material(dir+"/Stone.mat",new Color(.52f,.56f,.61f));
        var trim=Material(dir+"/Coping.mat",new Color(.76f,.73f,.61f));
        const string path="Assets/Prefabs/Walls/StoneWall.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(prefab==null)
        {
            var root=new GameObject("Stone Wall",typeof(WallSegment));
            Box(root,"Stone Foundation",new Vector3(0,.12f,0),new Vector3(.8f,.24f,.8f),stone);
            Box(root,"Stone Pillar",new Vector3(0,.64f,0),new Vector3(.6f,1.05f,.6f),stone);
            Box(root,"Coping",new Vector3(0,1.18f,0),new Vector3(.76f,.2f,.76f),trim);
            for(int i=0;i<3;i++)Box(root,"Stone Course "+i,new Vector3(0,.3f+i*.29f,0),new Vector3(.64f,.06f,.64f),trim);
            var segment=root.GetComponent<WallSegment>();
            segment.north=Box(root,"North Link",new Vector3(0,.65f,.4f),new Vector3(.44f,.8f,.6f),stone);
            segment.south=Box(root,"South Link",new Vector3(0,.65f,-.4f),new Vector3(.44f,.8f,.6f),stone);
            segment.east=Box(root,"East Link",new Vector3(.4f,.65f,0),new Vector3(.6f,.8f,.44f),stone);
            segment.west=Box(root,"West Link",new Vector3(-.4f,.65f,0),new Vector3(.6f,.8f,.44f),stone);
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.65f,0);collider.size=new Vector3(.95f,1.3f,.95f);
            prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
        }
        var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
        game.AddEditableWalls(prefab);EditorUtility.SetDirty(game);
        RenderPortrait();StateChecks();
    }

    public static void RenderPortrait()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Walls/StoneWall.prefab");
        if(prefab==null)throw new InvalidOperationException("Build the wall prefab before rendering its portrait.");
        Directory.CreateDirectory("Assets/Resources/BuildingIcons");
        typeof(HomeVillageArtFactory).GetMethod("RenderIcon",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{prefab,"Wall"});
    }

    static GameObject Box(GameObject root,string name,Vector3 pos,Vector3 scale,Material material)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(root.transform,false);
        part.transform.localPosition=pos;part.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;
        return part;
    }
    static Material Material(string path,Color color)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=color;
        AssetDatabase.CreateAsset(material,path);return material;
    }

    static void Check(bool ok,string message){if(!ok)throw new Exception("Walls: "+message);}
    static void StateChecks()
    {
        var s=VillageState.Create(1000);
        Check(s.TryPlace("Wall",5,5,1000,out _) && s.gold==975,"purchase cost");
        Check(!s.TryPlace("Wall",5,5,1000,out _) && s.gold==975,"overlap does not charge");
        Check(!s.TryPlace("Wall",22,0,1000,out _),"boundary");
        Check(s.TryMove(1,6,5,out _) && s.gold==975,"free move");
        Check(s.CanUpgrade(1,out _),"wall upgrade available");
        Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var restored) && restored.Count("Wall")==1,"wall save reload");
        for(int i=0;i<24;i++)Check(s.TryPlace("Wall",-20+i%12,10+i/12,1000,out _),"wall limit setup");
        Check(!s.CanBuy("Wall",out _) && s.IsValid(),"wall limit");
        var poor=VillageState.Create(1000);poor.gold=24;
        Check(!poor.TryPlace("Wall",5,5,1000,out _) && poor.Count("Wall")==0,"insufficient gold");
    }
}
