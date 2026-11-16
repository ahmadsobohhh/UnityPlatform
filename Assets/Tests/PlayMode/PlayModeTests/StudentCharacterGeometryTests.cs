using System;
using System.Collections;
using System.IO;
using System.Linq;
using ImagineQuest.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StudentCharacterGeometryTests
{
    private GameObject canvas,probe;
    private Mesh baked;
    [Serializable] private sealed class CollarProbeSnapshot
    {
        public Vector3 neck,head;
        public Vector3[] vertices,rim,samples;
        public int[] triangles;
    }
    [UnityTest]
    public IEnumerator OpenCollarsHaveChestSkinFromElevatedViewsDuringMovement()
    {
        canvas=new GameObject("Collar inspection",typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var viewport=new GameObject("Collar portrait",typeof(RectTransform),typeof(RawImage));viewport.transform.SetParent(canvas.transform,false);
        var preview=viewport.AddComponent<CharacterPreviewViewport>();preview.Initialize(true);
        probe=new GameObject("Chest verification collider",typeof(MeshCollider));baked=new Mesh();
        string output=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");var gaps=new System.Collections.Generic.List<string>();
        for(int body=0;body<2;body++)
        {
            preview.Show(new StudentAppearance {Body=body,Skin=2,Hair=0,HairColor=1});
            var avatar=preview.Avatar;var animator=avatar.GetComponent<Animator>();animator.Update(0);
            var neck=animator.GetBoneTransform(HumanBodyBones.Neck);var skull=animator.GetBoneTransform(HumanBodyBones.Head);
            var torso=avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name==(body==0?"Male":"Female")+"_Peasant_Body");
            // Four measured neckline vertices in the pinned source outfits: right, left,
            // back and front slit. Bake the garment each pose; a chest-bone offset does
            // not follow the collar's neck/clavicle weighting while running or jumping.
            Assert.AreEqual(body==0?3006:1730,torso.sharedMesh.vertexCount,"Update the neckline fixture if the source outfit topology changes.");
            var rimIndices=body==0?new[]{44,561,689,64}:new[]{719,1476,772,737};
            int missingWithoutCollar=0;
            foreach(string pose in new[]{"idle","walk","run","jump"})
            {
                animator.SetFloat("Speed",pose=="walk"?1:pose=="run"?2:0);animator.SetBool("isGrounded",pose!="jump");
                animator.Play(pose=="jump"?"Airborne":"On deck",0,.3f);animator.Update(.01f);
                var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
                var headTriangles=new System.Collections.Generic.List<int>();
                foreach(var skin in avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.sharedMesh.name=="Clothed character head" || r.name=="Collar skin"))
                {
                    var part=new Mesh();skin.BakeMesh(part);int first=vertices.Count;
                    var points=part.vertices.Select(skin.transform.TransformPoint).ToArray();vertices.AddRange(points);
                    var indices=part.triangles.Select(i=>i+first).ToArray();triangles.AddRange(indices);
                    if(skin.name!="Collar skin") headTriangles.AddRange(indices);
                    UnityEngine.Object.Destroy(part);
                }
                baked.Clear();baked.SetVertices(vertices);baked.SetTriangles(triangles,0);baked.RecalculateBounds();
                var collider=probe.GetComponent<MeshCollider>();collider.sharedMesh=baked;
                var shirt=new Mesh();torso.BakeMesh(shirt);var shirtVertices=shirt.vertices;
                var rimPoints=rimIndices.Select(i=>torso.transform.TransformPoint(shirtVertices[i])).ToArray();
                var samples=new Vector3[rimIndices.Length];int failuresBefore=gaps.Count;
                var withoutCollar=new Mesh();withoutCollar.SetVertices(vertices);withoutCollar.SetTriangles(headTriangles,0);withoutCollar.RecalculateBounds();
                for(int i=0;i<rimIndices.Length;i++)
                {
                    Vector3 rim=rimPoints[i];
                    Vector3 sample=Vector3.Lerp(neck.position,rim,.8f)+avatar.transform.up*.35f;
                    samples[i]=sample;
                    var ray=new Ray(sample,-avatar.transform.up);
                    // Probe the actual visible skin, including the leaning head. Cutting off
                    // triangles at the neck would invent holes inside the hollow neck mesh.
                    // The control retains the same head but omits the collar, proving that
                    // these samples can detect the original missing upper-chest surface.
                    collider.sharedMesh=withoutCollar;
                    if(!collider.Raycast(ray,out _,.55f)) missingWithoutCollar++;
                    collider.sharedMesh=baked;
                    if(!collider.Raycast(ray,out _,.55f)) gaps.Add($"body {body}, {pose}, opening sample {i}");
                }
                if(gaps.Count>failuresBefore && !string.IsNullOrEmpty(output))
                    File.WriteAllText(Path.Combine(output,$"Collar-probe-{body}-{pose}.json"),JsonUtility.ToJson(new CollarProbeSnapshot
                    {neck=neck.position,head=skull.position,vertices=vertices.ToArray(),triangles=triangles.ToArray(),rim=rimPoints,samples=samples}));
                UnityEngine.Object.Destroy(shirt);
                collider.sharedMesh=null;
                UnityEngine.Object.Destroy(withoutCollar);
                if(string.IsNullOrEmpty(output)) continue;
                foreach(int angle in new[]{0,90,180,270}) foreach(int pitch in new[]{15,45,70})
                {
                    preview.ResetView();preview.Rotate(angle+18);
                    var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.targetTexture==preview.Texture);
                    Vector3 focus=neck.position+Vector3.up*.005f;float elevation=pitch*Mathf.Deg2Rad;
                    camera.transform.position=focus+new Vector3(0,Mathf.Sin(elevation),Mathf.Cos(elevation))*.65f;camera.transform.LookAt(focus);
                    yield return new WaitForEndOfFrame();
                    var previous=RenderTexture.active;var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
                    try {RenderTexture.active=preview.Texture;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,$"Collar-{body}-{pose}-{angle}-{pitch}.png"),image.EncodeToPNG());}
                    finally {RenderTexture.active=previous;UnityEngine.Object.Destroy(image);}
                }
            }
            Assert.Greater(missingWithoutCollar,0,$"Body {body}: collar probes must detect missing skin when the upper-chest join is omitted.");
        }
        Assert.IsEmpty(gaps,"Missing skin inside the collar: "+string.Join("; ",gaps));
    }
    [UnityTest]
    public IEnumerator BothBodiesHaveContinuousNecksAndFittedHairThroughIdleWalkRunAndJump()
    {
        canvas=new GameObject("Geometry inspection",typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var viewportObject=new GameObject("Inspection portrait",typeof(RectTransform),typeof(RawImage));viewportObject.transform.SetParent(canvas.transform,false);
        var preview=viewportObject.AddComponent<CharacterPreviewViewport>();preview.Initialize(true);
        probe=new GameObject("Neck verification collider",typeof(MeshCollider));baked=new Mesh();
        string output=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");
        for(int body=0;body<2;body++) for(int style=0;style<6;style++)
        {
            var selected=new StudentAppearance {Body=body,Skin=2,Hair=style,HairColor=1};preview.Show(selected);
            var avatar=preview.Avatar;var animator=avatar.GetComponent<Animator>();
            Assert.AreEqual(1,avatar.Hairstyles.Count(h=>h.activeSelf));
            if(style==0) Assert.IsEmpty(avatar.Hairstyles[0].GetComponentsInChildren<Renderer>(),"Bald must not retain a scalp cap.");
            var head=avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.sharedMesh.name=="Clothed character head");
            foreach(string pose in new[]{"idle","walk","run","jump"})
            {
                animator.SetFloat("Speed",pose=="walk"?1:pose=="run"?2:0);
                animator.SetBool("isGrounded",pose!="jump");animator.Play(pose=="jump"?"Airborne":"On deck",0,.3f);animator.Update(.01f);
                head.BakeMesh(baked);baked.vertices=baked.vertices.Select(head.transform.TransformPoint).ToArray();baked.RecalculateBounds();
                var collider=probe.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=baked;
                var neck=animator.GetBoneTransform(HumanBodyBones.Neck);var skull=animator.GetBoneTransform(HumanBodyBones.Head);
                Vector3 axis=(skull.position-neck.position).normalized;
                Vector3 side=Vector3.Cross(axis,Vector3.forward).normalized;if(side.sqrMagnitude<.1f) side=Vector3.right;
                Vector3 forward=Vector3.Cross(side,axis).normalized;
                foreach(float height in new[]{.1f,.4f,.7f}) foreach(int angle in new[]{0,90,180,270})
                {
                    Vector3 center=Vector3.Lerp(neck.position,skull.position,height);
                    float radians=angle*Mathf.Deg2Rad;Vector3 direction=side*Mathf.Cos(radians)+forward*Mathf.Sin(radians);
                    Assert.IsTrue(collider.Raycast(new Ray(center+direction*.3f,-direction),out _,.6f),$"Open neck: body {body}, style {style}, {pose}, level {height}, angle {angle}");
                }
                collider.sharedMesh=null;
                if(style!=0)
                {
                    var eyes=avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Eyes");
                    eyes.BakeMesh(baked);
                    var eyePoints=baked.vertices.Select(eyes.transform.TransformPoint).ToArray();
                    Vector3 eyeCenter=eyePoints.Aggregate(Vector3.zero,(sum,p)=>sum+p)/eyePoints.Length;
                    Vector3 face=Vector3.ProjectOnPlane(eyeCenter-skull.position,avatar.transform.up).normalized;
                    Vector3 across=Vector3.Cross(avatar.transform.up,face).normalized;
                    float halfSpacing=eyePoints.Max(p=>Mathf.Abs(Vector3.Dot(p-eyeCenter,across)))*.55f;
                    var hair=avatar.Hairstyles[style].GetComponent<MeshFilter>();
                    baked.Clear();baked.vertices=hair.sharedMesh.vertices.Select(hair.transform.TransformPoint).ToArray();
                    baked.triangles=hair.sharedMesh.triangles;baked.RecalculateBounds();collider.sharedMesh=baked;
                    foreach(float eye in new[]{-halfSpacing,halfSpacing})
                        Assert.IsFalse(collider.Raycast(new Ray(eyeCenter+across*eye+face*.3f,-face),out var obstruction,.294f),
                            $"Hair covers an eye: body {body}, style {style}, {pose}; eyes={eyeCenter}, face={face}, obstruction={obstruction.point}");
                    collider.sharedMesh=null;
                }
                if(style>=StudentAppearance.HairChoiceCount || string.IsNullOrEmpty(output)) continue;
                foreach(int angle in new[]{0,90,180,270})
                {
                    preview.ResetView();preview.Rotate(angle+18);
                    // Running leans the head forward. Keep the inspection centered on
                    // the animated head so every view includes the scalp and collar.
                    var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.targetTexture==preview.Texture);
                    Vector3 focus=skull.position+Vector3.up*.065f;
                    camera.transform.position=focus+Vector3.forward*1.08f;camera.transform.LookAt(focus);
                    yield return new WaitForEndOfFrame();
                    var previous=RenderTexture.active;var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
                    try {RenderTexture.active=preview.Texture;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,$"Fit-{body}-{style}-{pose}-{angle}.png"),image.EncodeToPNG());}
                    finally {RenderTexture.active=previous;UnityEngine.Object.Destroy(image);}
                }
            }
        }
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(canvas!=null) UnityEngine.Object.Destroy(canvas);if(probe!=null) UnityEngine.Object.Destroy(probe);if(baked!=null) UnityEngine.Object.Destroy(baked);yield return null;
    }
}
