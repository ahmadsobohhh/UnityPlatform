using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Build-time geometry fitting; no mesh editing or physics queries run in the game.</summary>
public static class StudentCharacterMeshTools
{
    private struct Vertex
    {
        public Vector3 point, normal;
        public Vector2 uv;
        public BoneWeight weight;
    }

    public static Mesh Head(SkinnedMeshRenderer source)
    {
        var original=source.sharedMesh;
        var points=original.vertices;var normals=original.normals;var uvs=original.uv;var weights=original.boneWeights;
        int head=Array.FindIndex(source.bones,b=>b.name=="Head"),neck=Array.FindIndex(source.bones,b=>b.name=="neck_01");
        int spine=Array.FindIndex(source.bones,b=>b.name=="spine_03");
        Vector3 neckAxis=source.bones[neck].position;
        float floor=neckAxis.y-.07f, jaw=source.bones[head].position.y+.015f;
        var output=new List<Vertex>();var triangles=new List<int>();
        Vertex Read(int i)=>new Vertex {point=source.transform.TransformPoint(points[i]),normal=source.transform.TransformDirection(normals[i]),uv=uvs[i],weight=weights[i]};
        Vertex Prepare(Vertex v)
        {
            float headWeight=Influence(v.weight,head);
            if(v.point.y<jaw && headWeight<.65f)
            {
                float t=Mathf.InverseLerp(floor,jaw,v.point.y);
                float radius=Mathf.Lerp(.045f,.057f,t);
                var radial=new Vector3(v.point.x-neckAxis.x,0,v.point.z-neckAxis.z);
                if(radial.magnitude>radius) v.point=new Vector3(neckAxis.x,v.point.y,neckAxis.z)+radial.normalized*radius;
                // The continuous extension follows the neck, never the shoulder/arm bones.
                float neckWeight=Mathf.SmoothStep(.1f,1,t);
                v.weight=new BoneWeight {boneIndex0=neck,weight0=neckWeight,boneIndex1=spine,weight1=1-neckWeight};
                if(radial.sqrMagnitude>.00001f) v.normal=radial.normalized;
            }
            v.point=source.transform.InverseTransformPoint(v.point);
            v.normal=source.transform.InverseTransformDirection(v.normal).normalized;
            return v;
        }
        void Triangle(Vertex a,Vertex b,Vertex c)
        {
            a=Prepare(a);b=Prepare(b);c=Prepare(c);
            if(Vector3.Cross(b.point-a.point,c.point-a.point).sqrMagnitude<1e-13f) return;
            int first=output.Count;output.Add(a);output.Add(b);output.Add(c);
            triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);
        }
        var indices=original.triangles;
        for(int i=0;i<indices.Length;i+=3)
        {
            var input=new[]{Read(indices[i]),Read(indices[i+1]),Read(indices[i+2])};
            var polygon=new List<Vertex>();var boundary=new List<Vertex>();
            for(int edge=0;edge<3;edge++)
            {
                var a=input[edge];var b=input[(edge+1)%3];
                bool insideA=a.point.y>=floor,insideB=b.point.y>=floor;
                if(insideA) polygon.Add(a);
                if(insideA!=insideB)
                {
                    float t=(floor-a.point.y)/(b.point.y-a.point.y);
                    var intersection=new Vertex {point=Vector3.Lerp(a.point,b.point,t),normal=Vector3.Lerp(a.normal,b.normal,t).normalized,
                        uv=Vector2.Lerp(a.uv,b.uv,t),weight=Blend(a.weight,b.weight,t)};
                    polygon.Add(intersection);boundary.Add(intersection);
                }
            }
            for(int j=1;j+1<polygon.Count;j++) Triangle(polygon[0],polygon[j],polygon[j+1]);
            if(boundary.Count==2)
            {
                var center=boundary[0];center.point=new Vector3(neckAxis.x,floor,neckAxis.z);center.normal=Vector3.down;
                center.weight=new BoneWeight {boneIndex0=spine,weight0=1};
                var a=boundary[0];var b=boundary[1];a.normal=b.normal=Vector3.down;
                if(Vector3.Cross(a.point-center.point,b.point-center.point).y>0) (a,b)=(b,a);
                Triangle(center,a,b);
            }
        }
        var mesh=new Mesh {name="Clothed character head"};
        mesh.SetVertices(output.Select(v=>v.point).ToList());mesh.SetNormals(output.Select(v=>v.normal).ToList());
        mesh.SetUVs(0,output.Select(v=>v.uv).ToList());mesh.boneWeights=output.Select(v=>v.weight).ToArray();
        mesh.bindposes=original.bindposes;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();
        return mesh;
    }

    private static float Influence(BoneWeight b,int index)=>(b.boneIndex0==index?b.weight0:0)+(b.boneIndex1==index?b.weight1:0)+(b.boneIndex2==index?b.weight2:0)+(b.boneIndex3==index?b.weight3:0);
    private static BoneWeight Blend(BoneWeight a,BoneWeight b,float t)
    {
        var values=new Dictionary<int,float>();
        void Add(int index,float value) {if(value>0) values[index]=(values.TryGetValue(index,out float prior)?prior:0)+value;}
        Add(a.boneIndex0,a.weight0*(1-t));Add(a.boneIndex1,a.weight1*(1-t));Add(a.boneIndex2,a.weight2*(1-t));Add(a.boneIndex3,a.weight3*(1-t));
        Add(b.boneIndex0,b.weight0*t);Add(b.boneIndex1,b.weight1*t);Add(b.boneIndex2,b.weight2*t);Add(b.boneIndex3,b.weight3*t);
        var top=values.OrderByDescending(p=>p.Value).Take(4).ToArray();float sum=top.Sum(p=>p.Value);
        var result=new BoneWeight();
        for(int i=0;i<top.Length;i++)
        {
            int index=top[i].Key;float weight=top[i].Value/sum;
            switch(i) {case 0:result.boneIndex0=index;result.weight0=weight;break;case 1:result.boneIndex1=index;result.weight1=weight;break;
                case 2:result.boneIndex2=index;result.weight2=weight;break;case 3:result.boneIndex3=index;result.weight3=weight;break;}
        }
        return result;
    }

    /// <summary>Returns the garment's actual open neckline, welding UV/normal seams first.</summary>
    public static int[] Neckline(SkinnedMeshRenderer clothing,Transform neck)
    {
        var points=clothing.sharedMesh.vertices.Select(clothing.transform.TransformPoint).ToArray();
        var welded=new Dictionary<Vector3Int,int>();var representative=new List<int>();var ids=new int[points.Length];
        for(int i=0;i<points.Length;i++)
        {
            var p=points[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
            if(!welded.TryGetValue(key,out int index)) {index=representative.Count;welded.Add(key,index);representative.Add(i);}
            ids[i]=index;
        }
        var edges=new Dictionary<(int,int),int>();
        void Edge(int a,int b)
        {
            if(a==b) return;var key=a<b?(a,b):(b,a);
            edges[key]=edges.TryGetValue(key,out int count)?count+1:1;
        }
        var triangles=clothing.sharedMesh.triangles;
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=ids[triangles[i]],b=ids[triangles[i+1]],c=ids[triangles[i+2]];
            Edge(a,b);Edge(b,c);Edge(c,a);
        }
        var adjacency=new Dictionary<int,List<int>>();
        void Join(int a,int b) {if(!adjacency.TryGetValue(a,out var neighbors)) adjacency[a]=neighbors=new List<int>();neighbors.Add(b);}
        foreach(var edge in edges.Where(e=>e.Value==1)) {Join(edge.Key.Item1,edge.Key.Item2);Join(edge.Key.Item2,edge.Key.Item1);}
        var visited=new HashSet<int>();var candidates=new List<List<int>>();
        foreach(int first in adjacency.Keys)
        {
            if(visited.Contains(first)) continue;
            var component=new List<int>();var pending=new Stack<int>();pending.Push(first);
            while(pending.Count>0)
            {
                int current=pending.Pop();if(!visited.Add(current)) continue;component.Add(current);
                foreach(int neighbor in adjacency[current]) if(!visited.Contains(neighbor)) pending.Push(neighbor);
            }
            var world=component.Select(i=>points[representative[i]]).ToArray();
            if(component.Count>=12 && component.All(i=>adjacency[i].Count==2) &&
                world.Min(p=>p.y)>neck.position.y-.12f && world.Max(p=>p.y)<neck.position.y+.09f &&
                world.Min(p=>p.x)<neck.position.x-.04f && world.Max(p=>p.x)>neck.position.x+.04f &&
                world.All(p=>Vector3.ProjectOnPlane(p-neck.position,Vector3.up).magnitude<.18f)) candidates.Add(component);
        }
        if(candidates.Count!=1) throw new InvalidOperationException($"Expected one open neckline on {clothing.name}; found {candidates.Count}.");
        var loop=new List<int>();int start=candidates[0][0],node=start,previous=-1;
        do
        {
            loop.Add(representative[node]);int next=adjacency[node].First(n=>n!=previous);previous=node;node=next;
        } while(node!=start && loop.Count<=candidates[0].Count);
        if(loop.Count!=candidates[0].Count) throw new InvalidOperationException("The neckline is not a closed loop.");
        return loop.ToArray();
    }

    /// <summary>Skinned upper chest joining the neck to the inside of the actual collar opening.</summary>
    public static Mesh CollarSkin(SkinnedMeshRenderer clothing,Transform neckBone,Transform headBone)
    {
        var loop=Neckline(clothing,neckBone);
        var points=clothing.sharedMesh.vertices;var originalWeights=clothing.sharedMesh.boneWeights;
        var border=loop.Select(i=>clothing.transform.TransformPoint(points[i])).ToArray();
        int neck=Array.FindIndex(clothing.bones,b=>b.name=="neck_01"),spine=Array.FindIndex(clothing.bones,b=>b.name=="spine_03");
        float innerHeight=Mathf.Max(neckBone.position.y+.035f,border.Max(p=>p.y)+.02f);
        float neckWeight=Mathf.SmoothStep(.1f,1,Mathf.InverseLerp(neckBone.position.y-.07f,headBone.position.y+.015f,innerHeight));
        var innerWeight=new BoneWeight {boneIndex0=neck,weight0=neckWeight,boneIndex1=spine,weight1=1-neckWeight};
        var vertices=new List<Vector3>();var weights=new List<BoneWeight>();var triangles=new List<int>();
        const int rings=5;
        for(int ring=0;ring<rings;ring++)
        {
            float t=ring/(float)(rings-1);
            for(int i=0;i<loop.Length;i++)
            {
                Vector3 radial=Vector3.ProjectOnPlane(border[i]-neckBone.position,Vector3.up).normalized;
                Vector3 inner=neckBone.position+radial*.04f;inner.y=innerHeight;
                // Extend beneath the trim, keeping the boundary on the garment's bone weights.
                Vector3 outer=border[i]+radial*.004f-Vector3.up*.003f;
                Vector3 point=Vector3.Lerp(inner,outer,t);point.y=Mathf.Lerp(innerHeight,outer.y,Mathf.SmoothStep(0,1,t));
                vertices.Add(clothing.transform.InverseTransformPoint(point));
                weights.Add(Blend(innerWeight,originalWeights[loop[i]],t));
            }
        }
        void Triangle(int a,int b,int c)
        {
            Vector3 pa=clothing.transform.TransformPoint(vertices[a]),pb=clothing.transform.TransformPoint(vertices[b]),pc=clothing.transform.TransformPoint(vertices[c]);
            if(Vector3.Cross(pb-pa,pc-pa).y<0) (b,c)=(c,b);
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        }
        for(int ring=0;ring<rings-1;ring++) for(int i=0;i<loop.Length;i++)
        {
            int next=(i+1)%loop.Length,a=ring*loop.Length+i,b=ring*loop.Length+next,c=b+loop.Length,d=a+loop.Length;
            Triangle(a,b,d);Triangle(b,c,d);
        }
        var mesh=new Mesh {name="Fitted collar skin"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        mesh.SetUVs(0,vertices.Select(_=>Vector2.zero).ToList());mesh.boneWeights=weights.ToArray();mesh.bindposes=clothing.sharedMesh.bindposes;
        mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }

    public sealed class Scalp : IDisposable
    {
        private readonly GameObject probe;
        private readonly Mesh worldMesh;
        private readonly MeshCollider collider;
        public readonly Vector3 Center;
        public readonly Vector3 Forward;
        private readonly float frontHairline;
        public readonly float Top;
        public readonly Transform HeadBone;
        public Scalp(SkinnedMeshRenderer head,Transform headBone,SkinnedMeshRenderer eyes)
        {
            HeadBone=headBone;worldMesh=new Mesh();head.BakeMesh(worldMesh);
            worldMesh.vertices=worldMesh.vertices.Select(head.transform.TransformPoint).ToArray();worldMesh.RecalculateBounds();
            worldMesh.normals=worldMesh.normals.Select(head.transform.TransformDirection).ToArray();
            Top=worldMesh.bounds.max.y;
            Center=new Vector3(worldMesh.bounds.center.x,Top-.115f,worldMesh.bounds.center.z);
            var eyeMesh=new Mesh();eyes.BakeMesh(eyeMesh);
            Vector3 eyeCenter=eyes.transform.TransformPoint(eyeMesh.bounds.center);
            UnityEngine.Object.DestroyImmediate(eyeMesh);
            Forward=Vector3.ProjectOnPlane(eyeCenter-HeadBone.position,Vector3.up).normalized;
            if(Forward.sqrMagnitude<.9f) throw new InvalidOperationException("Cannot determine the character's face direction.");
            frontHairline=Mathf.Max(Top-.085f,eyeCenter.y+.04f);
            probe=new GameObject("Temporary scalp fitting surface");probe.hideFlags=HideFlags.HideAndDontSave;
            collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=worldMesh;Physics.SyncTransforms();
        }
        public bool Surface(Vector3 direction,out RaycastHit hit)=>collider.Raycast(new Ray(Center+direction.normalized*.65f,-direction.normalized),out hit,1.3f);
        public Mesh Fit(SkinnedMeshRenderer hair)
        {
            var baked=new Mesh();hair.BakeMesh(baked);
            var vertices=baked.vertices;var normals=baked.normals;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector3 world=hair.transform.TransformPoint(vertices[i]);Vector3 direction=world-Center;
                bool scalpRegion=world.y>HeadBone.position.y+.015f || Vector3.Dot(direction,Forward)<.025f && world.y>HeadBone.position.y-.015f;
                if(scalpRegion && Surface(direction,out var hit))
                {
                    float minimum=Vector3.Distance(Center,hit.point)+.0045f;
                    if(direction.magnitude<minimum) world=Center+direction.normalized*minimum;
                }
                vertices[i]=HeadBone.InverseTransformPoint(world);
                normals[i]=HeadBone.InverseTransformDirection(hair.transform.TransformDirection(normals[i])).normalized;
            }
            baked.name="Body-fitted hairstyle";baked.vertices=vertices;baked.normals=normals;baked.RecalculateBounds();baked.RecalculateTangents();
            return baked;
        }
        public Mesh Coils(bool closeCut)
        {
            var positions=new List<Vector3>();var normals=new List<Vector3>();var indices=new List<int>();
            void Vertex(Vector3 point,Vector3 normal) {positions.Add(HeadBone.InverseTransformPoint(point));normals.Add(HeadBone.InverseTransformDirection(normal).normalized);}
            // A solid fitted scalp under the curls prevents skin showing through between tufts.
            var p=worldMesh.vertices;var n=worldMesh.normals;var tr=worldMesh.triangles;
            bool InCap(Vector3 point)
            {
                float front=Mathf.InverseLerp(-.02f,.035f,Vector3.Dot(point-Center,Forward));
                return point.y>Mathf.Lerp(Top-.205f,frontHairline,front);
            }
            for(int i=0;i<tr.Length;i+=3)
            {
                if(!InCap(p[tr[i]]) || !InCap(p[tr[i+1]]) || !InCap(p[tr[i+2]])) continue;
                int start=positions.Count;
                for(int k=0;k<3;k++) {Vector3 normal=(p[tr[i+k]]-Center).normalized;Vertex(p[tr[i+k]]+normal*.0045f,normal);}
                indices.AddRange(new[]{start,start+1,start+2});
            }
            for(int ring=0;ring<=9;ring++)
            {
                float theta=ring*.225f;int count=ring==0?1:Mathf.Max(8,Mathf.RoundToInt(Mathf.Sin(theta)*38));
                for(int j=0;j<count;j++)
                {
                    float phi=(j+.5f*(ring%2))*Mathf.PI*2/count;
                    var direction=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Sin(phi));
                    if(!Surface(direction,out var hit) || !InCap(hit.point)) continue;
                    float radius=(closeCut ? .011f : .019f)*(1+.12f*Mathf.Sin(j*17.3f+ring*3.7f));
                    Vector3 center=hit.point+hit.normal*radius*.62f;
                    int first=positions.Count;const int longitude=6,latitude=4;
                    for(int lat=0;lat<=latitude;lat++) for(int lon=0;lon<=longitude;lon++)
                    {
                        float a=lat*Mathf.PI/latitude,b=lon*Mathf.PI*2/longitude;
                        Vector3 normal=new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b));
                        Vertex(center+normal*radius,normal);
                    }
                    for(int lat=0;lat<latitude;lat++) for(int lon=0;lon<longitude;lon++)
                    {
                        int a=first+lat*(longitude+1)+lon,b=a+longitude+1;
                        indices.AddRange(new[]{a,a+1,b,a+1,b+1,b});
                    }
                }
            }
            var mesh=new Mesh {name=closeCut?"Fitted short curls":"Fitted coils"};mesh.SetVertices(positions);mesh.SetNormals(normals);
            mesh.SetUVs(0,positions.Select(_=>Vector2.zero).ToList());mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
        public void Dispose() {UnityEngine.Object.DestroyImmediate(probe);UnityEngine.Object.DestroyImmediate(worldMesh);}
    }
}
