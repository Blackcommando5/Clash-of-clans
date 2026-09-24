using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ArmyBuildingAuthoring
{
    const string Folder = "Assets/Resources/ArmyBuildings";
    static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ScreenshotReference/" + name + ".mat");
    static GameObject Part(GameObject root, string name, Vector3 position, Vector3 scale, Material material, PrimitiveType shape = PrimitiveType.Cube)
    {
        var part = GameObject.CreatePrimitive(shape);
        part.name = name; part.transform.SetParent(root.transform, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    public static void Build()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var wood = Material("Weathered timber"); var beam = Material("Timber ends");
        var grass = Material("Grass platform"); var stone = Material("Pale stone"); var iron = Material("Dark iron");
        if (wood == null || beam == null || grass == null || stone == null || iron == null)
            throw new System.InvalidOperationException("Existing village materials are required.");
        foreach (string kind in new[] { "Barracks", "ArmyCamp" })
        {
            var root = new GameObject(kind);
            Part(root, "Grass footprint", new Vector3(0,.05f,0), new Vector3(2.9f,.1f,2.9f), grass);
            if (kind == "Barracks")
            {
                Part(root, "Training lodge", new Vector3(0,.72f,.15f), new Vector3(2.2f,1.35f,1.85f), wood);
                foreach (float side in new[] { -1f, 1f })
                {
                    var roof = Part(root, "Sloping roof", new Vector3(side*.6f,1.62f,.15f), new Vector3(1.4f,.16f,2.2f), iron);
                    roof.transform.localRotation = Quaternion.Euler(0,0,side*-28);
                    Part(root, "Door post", new Vector3(side*.40f,.62f,-.83f), new Vector3(.14f,1.16f,.15f), beam);
                }
                Part(root, "Door", new Vector3(0,.57f,-.79f), new Vector3(.65f,1.05f,.12f), iron);
                Part(root, "Entrance beam", new Vector3(0,1.25f,-.84f), new Vector3(1,.18f,.18f), beam);
                var sword = Part(root, "Training sword", new Vector3(0,1.1f,-1.09f), new Vector3(.13f,.94f,.09f), stone);
                sword.transform.localRotation = Quaternion.Euler(0,0,-32);
                Part(root, "Sword guard", new Vector3(.18f,.85f,-1.12f), new Vector3(.42f,.09f,.10f), beam);
            }
            else
            {
                Part(root, "Tent floor", new Vector3(-.45f,.15f,.4f), new Vector3(1.6f,.12f,1.6f), wood);
                foreach (float side in new[] { -1f, 1f })
                {
                    var tent = Part(root, "Tent slope", new Vector3(-.45f+side*.4f,.78f,.4f), new Vector3(.12f,1.48f,1.65f), beam);
                    tent.transform.localRotation = Quaternion.Euler(0,0,side*34);
                }
                Part(root, "Training post", new Vector3(.96f,.72f,.78f), new Vector3(.14f,1.4f,.14f), wood);
                Part(root, "Training dummy", new Vector3(.96f,1.05f,.72f), new Vector3(.48f,.56f,.3f), stone, PrimitiveType.Sphere);
                for (int i=0; i<8; i++)
                {
                    float angle=i*Mathf.PI/4;
                    Part(root, "Fire ring stone", new Vector3(.45f+Mathf.Cos(angle)*.4f,.16f,-.75f+Mathf.Sin(angle)*.4f), new Vector3(.22f,.2f,.22f), stone);
                }
                Part(root, "Campfire log", new Vector3(.45f,.18f,-.75f), new Vector3(.55f,.18f,.15f), wood);
            }
            var collider=root.AddComponent<BoxCollider>(); collider.center=new Vector3(0,1,0); collider.size=new Vector3(3,2,3);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+kind+".prefab"); Object.DestroyImmediate(root);
            typeof(HomeVillageArtFactory).GetMethod("RenderIcon",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{prefab,kind});
        }
        AssetDatabase.SaveAssets();
    }
}
