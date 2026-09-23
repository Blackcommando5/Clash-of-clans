using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kingdoms.UI;

public static class HomeVillageArtFactory
{
    const string Art="Assets/Art/HomeVillage";
    static Material wood, beam, roof, stone, gold;
    static Material Mat(string name,Color color)
    {
        var path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,path);return m;
    }
    static GameObject Part(GameObject root,string name,PrimitiveType shape,Vector3 pos,Vector3 scale,Material mat)
    {
        var p=GameObject.CreatePrimitive(shape);p.name=name;p.transform.SetParent(root.transform,false);p.transform.localPosition=pos;p.transform.localScale=scale;
        Object.DestroyImmediate(p.GetComponent<Collider>());p.GetComponent<Renderer>().sharedMaterial=mat;return p;
    }
    static void Box(GameObject root,string name,float x,float y,float z,float sx,float sy,float sz,Material m)
        =>Part(root,name,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(sx,sy,sz),m);

    static GameObject Hall()
    {
        const string path="Assets/Prefabs/HomeVillage/TownHall.prefab";
        var previous=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(previous!=null)return previous;
        var r=new GameObject("Town Hall");
        Box(r,"Stone plinth",0,.16f,0,3.85f,.32f,3.85f,stone);
        Box(r,"Lower timber shell",0,1.13f,0,3.1f,1.85f,2.95f,wood);
        for(int row=0;row<5;row++)
        {
            Box(r,"Front planks",0,.42f+row*.34f,-1.5f,3.2f,.045f,.045f,beam);
            Box(r,"Side planks",1.57f,.42f+row*.34f,0,.045f,.045f,3f,beam);
        }
        foreach(float x in new[]{-1.54f,1.54f})foreach(float z in new[]{-1.48f,1.48f})Box(r,"Corner post",x,1.14f,z,.23f,2.1f,.23f,beam);
        // Tiled, steep roof silhouette with a continuous ridge.
        foreach(float side in new[]{-1f,1f})
        {
            var slope=Part(r,"Roof slope",PrimitiveType.Cube,new Vector3(side*.9f,2.5f,0),new Vector3(2.23f,.18f,3.7f),roof);
            slope.transform.localRotation=Quaternion.Euler(0,0,-side*32);
            for(int row=0;row<4;row++)for(int column=0;column<6;column++)
            {
                float x=side*(.18f+row*.48f);float y=3.06f-Mathf.Abs(x)*.625f;
                var tile=Part(r,"Roof tile",PrimitiveType.Cube,new Vector3(x,y,-1.48f+column*.59f),new Vector3(.55f,.08f,.56f),roof);
                tile.transform.localRotation=Quaternion.Euler(0,0,-side*32);
            }
        }
        Box(r,"Ridge cap",0,3.13f,0,.22f,.22f,3.9f,gold);
        foreach(float z in new[]{-1.79f,1.79f})
        {
            foreach(float side in new[]{-1f,1f})
            {
                var fascia=Part(r,"Roof fascia",PrimitiveType.Cube,new Vector3(side*.9f,2.57f,z),new Vector3(2.3f,.2f,.17f),beam);fascia.transform.localRotation=Quaternion.Euler(0,0,-side*32);
            }
        }
        Box(r,"Door shadow",0,.9f,-1.58f,.9f,1.48f,.12f,beam);
        Box(r,"Door",0,.87f,-1.67f,.69f,1.35f,.09f,wood);
        foreach(float y in new[]{.55f,1.22f})Box(r,"Door iron bands",0,y,-1.74f,.69f,.07f,.035f,beam);
        Part(r,"Door handle",PrimitiveType.Sphere,new Vector3(.2f,.9f,-1.78f),Vector3.one*.09f,gold);
        foreach(float x in new[]{-1.05f,1.05f})
        {
            Box(r,"Window frame",x,1.22f,-1.59f,.62f,.66f,.12f,beam);
            Box(r,"Warm window",x,1.22f,-1.67f,.43f,.47f,.08f,gold);
            Box(r,"Window cross",x,1.22f,-1.73f,.05f,.5f,.04f,beam);
        }
        Box(r,"Entry step",0,.17f,-1.91f,1.2f,.34f,.35f,stone);
        Box(r,"Chimney",.85f,2.96f,.7f,.42f,1.2f,.42f,stone);
        Box(r,"Chimney cap",.85f,3.59f,.7f,.58f,.14f,.58f,beam);
        var collider=r.AddComponent<BoxCollider>();collider.center=new Vector3(0,1.6f,0);collider.size=new Vector3(4,3.2f,4);
        var prefab=PrefabUtility.SaveAsPrefabAsset(r,path);Object.DestroyImmediate(r);return prefab;
    }

    static void RenderIcon(GameObject prefab,string name)
    {
        string path="Assets/Resources/BuildingIcons/"+name+".png";
        var root=new GameObject("Icon Render Rig");root.transform.position=new Vector3(1000,0,1000);
        var model=Object.Instantiate(prefab,root.transform);model.transform.localPosition=Vector3.zero;
        // Alpha-blended glass would replace the render target alpha over the opaque liquid.
        // Keep the vessel contents readable in transparent UI portraits.
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            if(renderer.sharedMaterial!=null && renderer.sharedMaterial.HasProperty("_Surface") && renderer.sharedMaterial.GetFloat("_Surface")>0)
                renderer.enabled=false;
        foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        var cameraObject=new GameObject("Icon Camera");cameraObject.transform.SetParent(root.transform,false);
        var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=2.5f;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<30;
        camera.transform.localPosition=new Vector3(7,6,-7);camera.transform.LookAt(root.transform.position+Vector3.up*1.2f);
        var target=new RenderTexture(384,384,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
        var lightObject=new GameObject("Icon Key Light");lightObject.transform.SetParent(root.transform,false);
        var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(45,-30,0);
        camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(384,384,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,384,384),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
    }

    public static void Build()
    {
        Directory.CreateDirectory(Art);Directory.CreateDirectory("Assets/Prefabs/HomeVillage");Directory.CreateDirectory("Assets/Resources/BuildingIcons");AssetDatabase.Refresh();
        wood=Mat("Warm Planks",new Color(.63f,.35f,.13f));beam=Mat("Dark Beams",new Color(.25f,.13f,.055f));roof=Mat("Clay Tiles",new Color(.88f,.27f,.085f));stone=Mat("Foundation Stone",new Color(.63f,.60f,.49f));gold=Mat("Amber",new Color(1,.73f,.18f));
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");var game=Object.FindFirstObjectByType<VillageGameplay>();
        game.townHallPrefab=Hall();EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);
        RenderIcon(game.goldMinePrefab,"GoldMine");RenderIcon(game.elixirCollectorPrefab,"ElixirCollector");RenderIcon(game.goldStoragePrefab,"GoldStorage");RenderIcon(game.elixirStoragePrefab,"ElixirStorage");
        AssetDatabase.SaveAssets();
    }
}
