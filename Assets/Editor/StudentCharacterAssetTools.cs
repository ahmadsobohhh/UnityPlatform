using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using ImagineQuest.Characters;
using System.Collections.Generic;
using UnityEngine;

public static class StudentCharacterAssetTools
{
    public const string Art = "Assets/Art/StudentCharacters";
    public const string Generated = "Assets/Art/StudentCharacters/Generated";
    public static void Build()
    {
        Directory.CreateDirectory(Generated); Directory.CreateDirectory("Assets/Resources/StudentCharacters");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string path in AssetDatabase.FindAssets("t:Model",new[]{Art+"/Models"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (!path.Contains("Peasant") && !path.Contains("UAL1")) continue;
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.isReadable=true; importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            var desc=importer.humanDescription;
            var names=new Dictionary<string,string> {
                {"Hips","pelvis"},{"Spine","spine_01"},{"Chest","spine_02"},{"UpperChest","spine_03"},{"Neck","neck_01"},{"Head","Head"},
                {"LeftShoulder","clavicle_l"},{"RightShoulder","clavicle_r"},{"LeftUpperArm","upperarm_l"},{"RightUpperArm","upperarm_r"},
                {"LeftLowerArm","lowerarm_l"},{"RightLowerArm","lowerarm_r"},{"LeftHand","hand_l"},{"RightHand","hand_r"},
                {"LeftUpperLeg","thigh_l"},{"RightUpperLeg","thigh_r"},{"LeftLowerLeg","calf_l"},{"RightLowerLeg","calf_r"},
                {"LeftFoot","foot_l"},{"RightFoot","foot_r"},{"LeftToes","ball_l"},{"RightToes","ball_r"}
            };
            foreach(string side in new[]{"Left","Right"}) foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})
                for(int i=1;i<=3;i++) names[side+finger+new[]{"","Proximal","Intermediate","Distal"}[i]]=
                    (finger=="Little"?"pinky":finger.ToLowerInvariant())+"_0"+i+"_"+(side=="Left"?"l":"r");
            desc.human=names.Select(pair=>new HumanBone {humanName=pair.Key,boneName=pair.Value,limit=new HumanLimit {useDefaultValues=true}}).ToArray();
            importer.humanDescription=desc;
            if(path.Contains("UAL1"))
            {
                var clips=importer.defaultClipAnimations;
                foreach(var clip in clips) { clip.loopTime=clip.name.Contains("Loop"); clip.lockRootRotation=true; clip.lockRootHeightY=true; clip.lockRootPositionXZ=true; }
                importer.clipAnimations=clips;
            }
            importer.SaveAndReimport();
        }
        foreach(string path in AssetDatabase.FindAssets("t:Texture2D",new[]{Art+"/Textures"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.maxTextureSize=1024;
            if(path.Contains("Normal")) importer.textureType=TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
        var clothes=Material("Deckhand linen and leather","T_Peasant_BaseColor");
        var eyes=Material("Warm brown eyes","T_Eye_Brown");
        var hair=Material("Hair",null,"T_Hair_1_Normal");
        var longHair=Material("Long hair",null,"T_Hair_2_Normal");
        var catalog=AssetDatabase.LoadAssetAtPath<StudentCharacterCatalog>("Assets/Resources/StudentCharacters/Catalog.asset");
        if(catalog==null) { catalog=ScriptableObject.CreateInstance<StudentCharacterCatalog>(); AssetDatabase.CreateAsset(catalog,"Assets/Resources/StudentCharacters/Catalog.asset"); }
        catalog.Locomotion=BuildLocomotion(); catalog.Bodies=new GameObject[2];
        for(int body=0;body<2;body++)
        {
            bool female=body==1; string gender=female?"Female":"Male";
            var outfit=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+gender+"_Peasant.fbx"));
            outfit.name=gender+" deckhand";
            var animator=outfit.GetComponent<Animator>();
            if(animator==null || animator.avatar==null || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("Invalid Humanoid outfit: "+gender);
            animator.runtimeAnimatorController=catalog.Locomotion; animator.applyRootMotion=false;
            var skin=Material(gender+" skin",female?"T_Superhero_Female_Light_BaseColor":"T_Superhero_Male_Ligh",female?"T_Superhero_Female_Normal":"T_Superhero_Male_Normal");
            var skinParts=new List<Renderer>(); var skinSlots=new List<int>(); var hairParts=new List<Renderer>();
            foreach(var renderer in outfit.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // The male sleeve mesh also contains exposed skin. Preserve every submesh;
                // collapsing its two material slots makes the shoulders and elbows disappear.
                var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    bool exposedSkin=materials[slot]!=null && materials[slot].name.Contains("Regular");
                    materials[slot]=exposedSkin?skin:clothes;
                    if(exposedSkin) {skinParts.Add(renderer);skinSlots.Add(slot);}
                }
                renderer.sharedMaterials=materials;
            }
            var skeleton=outfit.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/Superhero_"+gender+"_FullBody.fbx"));
            foreach(var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var copy=CopyMesh(renderer,outfit.transform,skeleton);
                if(renderer.name=="Eyes") copy.sharedMaterials=new[]{eyes};
                else if(renderer.name=="Eyebrows") { copy.sharedMaterials=new[]{hair}; hairParts.Add(copy); }
                else
                {
                    var neck=source.GetComponentsInChildren<Transform>().First(t=>t.name=="Head").position.y-.12f;
                    var mesh=HeadMesh(renderer,neck);
                    string meshPath=Generated+"/"+gender+"Head.asset";
                    var existingHead=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(existingHead==null) AssetDatabase.CreateAsset(mesh,meshPath);
                    else {EditorUtility.CopySerialized(mesh,existingHead);UnityEngine.Object.DestroyImmediate(mesh);mesh=existingHead;EditorUtility.SetDirty(mesh);}
                    copy.sharedMesh=mesh; copy.sharedMaterials=new[]{skin};
                    skinParts.Add(copy);skinSlots.Add(0);
                }
            }
            UnityEngine.Object.DestroyImmediate(source);
            var styles=new GameObject[4];
            string[] hairNames={female?"Hair_BuzzedFemale":"Hair_Buzzed","Hair_SimpleParted","Hair_Long","Hair_Buns"};
            for(int style=0;style<4;style++)
            {
                var hairSource=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+hairNames[style]+".fbx"));
                var copy=CopyMesh(hairSource.GetComponentInChildren<SkinnedMeshRenderer>(),outfit.transform,skeleton);
                copy.name="Hairstyle "+style; copy.sharedMaterials=new[]{style<2?hair:longHair}; hairParts.Add(copy); styles[style]=copy.gameObject;
                UnityEngine.Object.DestroyImmediate(hairSource);
            }
            var avatar=outfit.AddComponent<StudentCharacterAvatar>(); avatar.Body=body; avatar.SkinRenderers=skinParts.ToArray();avatar.SkinMaterialIndices=skinSlots.ToArray();
            avatar.HairRenderers=hairParts.ToArray(); avatar.Hairstyles=styles;
            var appearance=StudentAppearance.Default; appearance.Body=body; avatar.Apply(appearance);
            catalog.Bodies[body]=PrefabUtility.SaveAsPrefabAsset(outfit,Generated+"/"+gender+"Deckhand.prefab");
            UnityEngine.Object.DestroyImmediate(outfit);
        }
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("STUDENT CHARACTER ASSETS BUILT");
    }
    private static Material Material(string name,string texture,string normal=null)
    {
        string path=Generated+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
        material.SetColor("_BaseColor",Color.white); material.SetFloat("_Smoothness",.22f);
        if(texture!=null) material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+texture+".png"));
        if(normal!=null) {material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+normal+".png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.65f);}
        EditorUtility.SetDirty(material); return material;
    }
    private static SkinnedMeshRenderer CopyMesh(SkinnedMeshRenderer source,Transform parent,Dictionary<string,Transform> skeleton)
    {
        var go=new GameObject(source.name); go.transform.SetParent(parent,false);
        go.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation); go.transform.localScale=source.transform.lossyScale;
        var copy=go.AddComponent<SkinnedMeshRenderer>(); copy.sharedMesh=source.sharedMesh;
        copy.bones=source.bones.Select(b=>skeleton[b.name]).ToArray(); copy.rootBone=skeleton[source.rootBone.name];
        copy.localBounds=source.localBounds; copy.updateWhenOffscreen=true; return copy;
    }
    private static Mesh HeadMesh(SkinnedMeshRenderer source,float cutoff)
    {
        var original=source.sharedMesh; var vertices=original.vertices;
        var triangles=original.triangles; var kept=new List<int>(); var map=new Dictionary<int,int>(); var order=new List<int>();
        for(int i=0;i<triangles.Length;i+=3)
        {
            if(Enumerable.Range(0,3).Any(j=>source.transform.TransformPoint(vertices[triangles[i+j]]).y<cutoff)) continue;
            for(int j=0;j<3;j++) { int index=triangles[i+j]; if(!map.ContainsKey(index)) {map[index]=order.Count;order.Add(index);} kept.Add(map[index]); }
        }
        if(kept.Count<300) throw new InvalidOperationException("Head clipping removed too much geometry.");
        var mesh=new Mesh {name="Clothed character head"};
        mesh.vertices=order.Select(i=>vertices[i]).ToArray(); mesh.normals=order.Select(i=>original.normals[i]).ToArray();
        mesh.uv=order.Select(i=>original.uv[i]).ToArray(); mesh.boneWeights=order.Select(i=>original.boneWeights[i]).ToArray();
        if(original.tangents.Length==vertices.Length) mesh.tangents=order.Select(i=>original.tangents[i]).ToArray();
        mesh.bindposes=original.bindposes; mesh.triangles=kept.ToArray(); mesh.RecalculateBounds(); return mesh;
    }
    private static RuntimeAnimatorController BuildLocomotion()
    {
        string path=Generated+"/DeckhandLocomotion.controller";
        var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(existing!=null && existing.layers.Length>0 && existing.layers[0].stateMachine.states.Length>0) return existing;
        if(existing!=null) AssetDatabase.DeleteAsset(path);
        var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        controller.AddParameter("isGrounded",AnimatorControllerParameterType.Bool);
        controller.parameters=controller.parameters.Select(p=>{if(p.name=="isGrounded") p.defaultBool=true;return p;}).ToArray();
        var clips=AssetDatabase.LoadAllAssetsAtPath(Art+"/Models/UAL1_Standard.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
        AnimationClip Clip(string suffix)=>clips.Single(c=>c.name.EndsWith("|"+suffix,StringComparison.Ordinal));
        var tree=new BlendTree {name="Idle Walk Run",blendParameter="Speed",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(tree,controller); tree.AddChild(Clip("Idle_Loop"),0); tree.AddChild(Clip("Walk_Loop"),1); tree.AddChild(Clip("Jog_Fwd_Loop"),2);
        var machine=controller.layers[0].stateMachine; var locomotion=machine.AddState("On deck"); locomotion.motion=tree; machine.defaultState=locomotion;
        var airborne=machine.AddState("Airborne"); airborne.motion=Clip("Jump_Loop");
        var jump=locomotion.AddTransition(airborne);jump.hasExitTime=false;jump.duration=.12f;jump.AddCondition(AnimatorConditionMode.IfNot,0,"isGrounded");
        var land=airborne.AddTransition(locomotion);land.hasExitTime=false;land.duration=.12f;land.AddCondition(AnimatorConditionMode.If,0,"isGrounded");
        EditorUtility.SetDirty(controller);return controller;
    }
    public static void Inspect()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var report = new StringBuilder();
        foreach (string path in AssetDatabase.FindAssets("t:Model", new[]{Art}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            report.AppendLine(path);
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                report.AppendLine($"  MESH {renderer.name} verts={renderer.sharedMesh.vertexCount} bounds={renderer.sharedMesh.bounds} root={renderer.rootBone?.name}");
                report.AppendLine("    MATERIALS " + string.Join(",", renderer.sharedMaterials.Select(m=>m?.name)));
                report.AppendLine("    BONES " + string.Join(",", renderer.bones.Select(b=>b.name)));
                var vertices=renderer.sharedMesh.vertices;
                report.AppendLine($"    VERTEX Y {vertices.Min(v=>v.y)}..{vertices.Max(v=>v.y)} rendererScale={renderer.transform.lossyScale}");
            }
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))
                report.AppendLine($"  CLIP {clip.name} duration={clip.length}");
        }
        string output=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) throw new InvalidOperationException("Set character output directory.");
        File.WriteAllText(Path.Combine(output,"asset-inspection.txt"),report.ToString());
        Debug.Log("STUDENT CHARACTER INSPECTION COMPLETE");
    }
}
