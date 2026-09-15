using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kingdoms.UI;

// Produces editable primitive-based resource prefabs matching the starter village.
public static class ResourceBuildingFactory
{
    const string Art = "Assets/Art/ResourceBuildings";
    const string Prefabs = "Assets/Prefabs/ResourceBuildings";
    static Material stone, timber, gold, metal, elixir;

    static Material Material(string name, Color color, float smoothness = .2f)
    {
        string path = Art + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color; material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Part(GameObject root, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(shape); part.name = name;
        part.transform.SetParent(root.transform, false); part.transform.localPosition = position; part.transform.localScale = scale;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    static void Box(GameObject root, string name, float x, float y, float z, float sx, float sy, float sz, Material material)
        => Part(root, name, PrimitiveType.Cube, new Vector3(x,y,z), new Vector3(sx,sy,sz), material);

    static GameObject Create(string kind)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + kind + ".prefab");
        if (existing != null) return existing;
        var root = new GameObject(kind);
        Box(root,"Stone foundation",0,.12f,0,2.85f,.24f,2.85f,stone);
        if (kind == "GoldStorage")
        {
            Box(root,"Timber vault",0,.7f,0,2.25f,1.1f,2.25f,timber);
            Box(root,"Dark open interior",0,1.28f,0,2.05f,.1f,2.05f,metal);
            for (int x=-1;x<=1;x++) for(int z=-1;z<=1;z++)
                Box(root,"Gold ingot",x*.58f,1.42f,z*.55f,.49f,.22f,.39f,gold);
            for (int x=-1;x<=1;x+=2)
            {
                Box(root,"Vault band",x*.82f,.77f,-1.14f,.15f,1.2f,.1f,metal);
                Box(root,"Top rim",x*1.14f,1.38f,0,.16f,.18f,2.4f,gold);
            }
            Box(root,"Front crest",0,.85f,-1.21f,.65f,.65f,.1f,gold);
        }
        else
        {
            bool collector = kind == "ElixirCollector";
            Part(root,"Tank foot",PrimitiveType.Cylinder,new Vector3(0,.4f,0),new Vector3(2.2f,.16f,2.2f),metal);
            Part(root,"Elixir vessel",PrimitiveType.Cylinder,new Vector3(0,1.25f,0),new Vector3(collector ? 1.35f : 1.95f,.72f,collector ? 1.35f : 1.95f),elixir);
            foreach(float y in new[]{.65f,1.8f})
                Part(root,"Brass tank band",PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(collector ? 1.5f : 2.08f,.075f,collector ? 1.5f : 2.08f),gold);
            Part(root,"Tank lid",PrimitiveType.Cylinder,new Vector3(0,2.02f,0),new Vector3(collector ? 1.5f : 2.08f,.1f,collector ? 1.5f : 2.08f),metal);
            if (collector)
            {
                Box(root,"Pump housing",.95f,.65f,.4f,.55f,.9f,.65f,timber);
                Box(root,"Pump riser",.95f,1.75f,.4f,.2f,1.6f,.2f,gold);
                Box(root,"Inlet pipe",.45f,2.48f,.4f,1.2f,.2f,.2f,gold);
                Part(root,"Pump globe",PrimitiveType.Sphere,new Vector3(0,2.43f,0),Vector3.one*.55f,elixir);
            }
            else
                for(int x=-1;x<=1;x+=2) Box(root,"Tank brace",x*1.1f,1.2f,0,.16f,1.9f,.3f,timber);
        }
        var collider = root.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.35f,0);collider.size=new Vector3(3,2.7f,3);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + kind + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    public static void Build()
    {
        Directory.CreateDirectory(Art); Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
        stone=Material("Stone",new Color(.53f,.51f,.43f));timber=Material("Timber",new Color(.48f,.25f,.10f));
        gold=Material("Brass",new Color(1f,.68f,.1f),.45f);metal=Material("Iron",new Color(.19f,.23f,.25f));
        elixir=Material("Elixir",new Color(.66f,.12f,.82f),.7f);
        var collector=Create("ElixirCollector");var goldStorage=Create("GoldStorage");var elixirStorage=Create("ElixirStorage");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
        var game=Object.FindFirstObjectByType<VillageGameplay>();
        if(game==null) throw new System.InvalidOperationException("Main Scene has no VillageGameplay component.");
        game.elixirCollectorPrefab=collector;game.goldStoragePrefab=goldStorage;game.elixirStoragePrefab=elixirStorage;
        EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
}
