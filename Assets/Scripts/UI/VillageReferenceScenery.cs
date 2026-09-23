using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        readonly List<Mesh> generatedSceneryMeshes=new List<Mesh>();
        readonly List<Material> generatedSceneryMaterials=new List<Material>();
        // Baked into a handful of meshes, with no colliders inside the playable grid.
        public void BuildReferenceScenery()
        {
            if(transform.Find("Reference Village Scenery")!=null)return;
            var root=new GameObject("Reference Village Scenery");root.transform.SetParent(transform,false);
            var foliage=new List<CombineInstance>();var bright=new List<CombineInstance>();var trunks=new List<CombineInstance>();var rocks=new List<CombineInstance>();
            var mesh=ReferenceStoneMesh();var random=new System.Random(923);
            float Next(float min,float max)=>min+(float)random.NextDouble()*(max-min);
            void Add(List<CombineInstance> list,Vector3 p,Vector3 scale)=>list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,Next(0,360),0),scale)});
            for(int i=0;i<210;i++)
            {
                float along=Next(-26,35),edge=Next(24,36);
                Vector3 p=i%3==0 ? new Vector3(along,0,edge) : i%3==1 ? new Vector3(edge,0,along) : new Vector3(along,0,-edge);
                if(p.x<-26)continue;
                float size=Next(.8f,1.65f);
                Add(trunks,p+Vector3.up*size,new Vector3(.23f,size,.23f));
                Add(foliage,p+Vector3.up*size*2,new Vector3(size,size*1.15f,size));
                Add(bright,p+new Vector3(-.28f,size*2.65f,-.1f),new Vector3(size*.7f,size*.67f,size*.7f));
            }
            for(int i=0;i<75;i++)
            {
                float z=Next(-38,38),x=Next(-29,-25);float size=Next(.3f,1.15f);
                Add(rocks,new Vector3(x,size*.35f,z),new Vector3(size,size*.7f,size*.85f));
            }
            BakeReferenceMesh(root.transform,"Forest canopy",foliage,new Color(.24f,.43f,.075f));
            BakeReferenceMesh(root.transform,"Sunlit leaves",bright,new Color(.43f,.64f,.12f));
            BakeReferenceMesh(root.transform,"Tree trunks",trunks,new Color(.38f,.23f,.1f));
            BakeReferenceMesh(root.transform,"Shore rocks",rocks,new Color(.47f,.48f,.43f));
            if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);
        }

        static Mesh ReferenceStoneMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int rings=6,sides=9;
            for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
            { float a=x*Mathf.PI*2/sides,b=y*Mathf.PI/rings;vertices.Add(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))); }
            for(int y=0;y<rings;y++)for(int x=0;x<sides;x++)
            { int n=y*(sides+1)+x;triangles.AddRange(new[]{n,n+1,n+sides+1,n+1,n+sides+2,n+sides+1}); }
            var mesh=new Mesh{name="Scenery rounded stone"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }

        void BakeReferenceMesh(Transform parent,string name,List<CombineInstance> parts,Color color)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};material.SetFloat("_Smoothness",.05f);
            generatedSceneryMeshes.Add(mesh);generatedSceneryMaterials.Add(material);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        void ReleaseReferenceScenery()
        {
            foreach(var mesh in generatedSceneryMeshes)if(mesh!=null)Destroy(mesh);
            foreach(var material in generatedSceneryMaterials)if(material!=null)Destroy(material);
        }
    }
}
