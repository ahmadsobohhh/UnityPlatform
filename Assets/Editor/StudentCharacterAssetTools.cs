using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using ImagineQuest.Characters;
using System.Collections.Generic;
using UnityEngine;

public static class StudentCharacterAssetTools
{
    public const string Art = "Assets/Art/StudentCharacters";
    public const string Generated = "Assets/Art/StudentCharacters/Generated";
    [MenuItem("Imagine Quest/Character/Rebuild Character Art")]
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
            if(path.Contains("Superhero") && path.Contains("Ligh")) importer.isReadable=true;
            importer.SaveAndReimport();
        }
        var clothes=Material("Deckhand linen and leather","T_Peasant_BaseColor");
        var eyes=Material("Warm brown eyes","T_Eye_Brown");
        var hair=Material("Hair",null,"T_Hair_1_Normal");
        var longHair=Material("Long hair",null,"T_Hair_2_Normal");
        var curls=Material("Textured curls",null);
        var catalog=AssetDatabase.LoadAssetAtPath<StudentCharacterCatalog>("Assets/Resources/StudentCharacters/Catalog.asset");
        if(catalog==null) { catalog=ScriptableObject.CreateInstance<StudentCharacterCatalog>(); AssetDatabase.CreateAsset(catalog,"Assets/Resources/StudentCharacters/Catalog.asset"); }
        catalog.Locomotion=BuildLocomotion(); catalog.Bodies=new GameObject[2];
        catalog.CreatorBackground=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Student/StudentHubBG.png");
        catalog.MenuFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Text/WelcomePage/Treamd SDF.asset");
        catalog.ReadingFont=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Text/WelcomePage/Almendra-Regular SDF.asset");
        var previewScene=EditorSceneManager.NewPreviewScene();
        try
        {for(int body=0;body<2;body++)
        {
            bool female=body==1; string gender=female?"Female":"Male";
            var outfit=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+gender+"_Peasant.fbx"));
            SceneManager.MoveGameObjectToScene(outfit,previewScene);
            outfit.name=gender+" deckhand";
            var animator=outfit.GetComponent<Animator>();
            if(animator==null || animator.avatar==null || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("Invalid Humanoid outfit: "+gender);
            animator.runtimeAnimatorController=catalog.Locomotion; animator.applyRootMotion=false;
            var skin=Material(gender+" skin",female?"T_Superhero_Female_Light_BaseColor":"T_Superhero_Male_Ligh",female?"T_Superhero_Female_Normal":"T_Superhero_Male_Normal");
            var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/Superhero_"+gender+"_FullBody.fbx"));
            SceneManager.MoveGameObjectToScene(source,previewScene);
            var exposedSkin=ExposedSkinMaterial(gender,source,skin);
            var skinParts=new List<Renderer>(); var skinSlots=new List<int>(); var hairParts=new List<Renderer>();
            foreach(var renderer in outfit.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // The male sleeve mesh also contains exposed skin. Preserve every submesh;
                // collapsing its two material slots makes the shoulders and elbows disappear.
                var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    bool isSkin=materials[slot]!=null && materials[slot].name.Contains("Regular");
                    materials[slot]=isSkin?exposedSkin:clothes;
                    if(isSkin) {skinParts.Add(renderer);skinSlots.Add(slot);}
                }
                renderer.sharedMaterials=materials;
            }
            var skeleton=outfit.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            SkinnedMeshRenderer headRenderer=null,eyesRenderer=null;
            foreach(var renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var copy=CopyMesh(renderer,outfit.transform,skeleton);
                if(renderer.name=="Eyes") {copy.sharedMaterials=new[]{eyes};eyesRenderer=copy;}
                else if(renderer.name=="Eyebrows") { copy.sharedMaterials=new[]{hair}; hairParts.Add(copy); }
                else
                {
                    var mesh=StudentCharacterMeshTools.Head(renderer);
                    string meshPath=Generated+"/"+gender+"Head.asset";
                    var existingHead=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(existingHead==null) AssetDatabase.CreateAsset(mesh,meshPath);
                    else {EditorUtility.CopySerialized(mesh,existingHead);UnityEngine.Object.DestroyImmediate(mesh);mesh=existingHead;EditorUtility.SetDirty(mesh);}
                    copy.sharedMesh=mesh; copy.sharedMaterials=new[]{skin};
                    headRenderer=copy;
                    skinParts.Add(copy);skinSlots.Add(0);
                }
            }
            UnityEngine.Object.DestroyImmediate(source);
            var torso=outfit.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name==gender+"_Peasant_Body");
            var collar=CopyMesh(torso,outfit.transform,skeleton);collar.name="Collar skin";
            var collarMesh=StudentCharacterMeshTools.CollarSkin(torso,skeleton["neck_01"],skeleton["Head"]);
            collarMesh.name=gender+"CollarSkin";collar.sharedMesh=StoreMesh(Generated+"/"+gender+"CollarSkin.asset",collarMesh);
            collar.sharedMaterials=new[]{exposedSkin};skinParts.Add(collar);skinSlots.Add(0);
            var styles=new GameObject[6];
            styles[0]=new GameObject("Hairstyle 0 Bald");styles[0].transform.SetParent(skeleton["Head"],false);
            using(var scalp=new StudentCharacterMeshTools.Scalp(headRenderer,skeleton["Head"],eyesRenderer))
            for(int style=1;style<6;style++)
            {
                bool curly=female?style==1:style==3;
                Mesh mesh;Material material;
                if(curly) {mesh=scalp.Coils(female);material=curls;}
                else
                {
                    string name=female?new[]{"","","Hair_Long","Hair_Buns","Hair_SimpleParted","Hair_BuzzedFemale"}[style]:
                        new[]{"","Hair_Buzzed","Hair_SimpleParted","","Hair_Long","Hair_Buns"}[style];
                    var hairSource=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+name+".fbx"));
                    SceneManager.MoveGameObjectToScene(hairSource,previewScene);
                    var temporary=CopyMesh(hairSource.GetComponentInChildren<SkinnedMeshRenderer>(),outfit.transform,skeleton);
                    mesh=scalp.Fit(temporary);material=name=="Hair_Long" || name=="Hair_Buns"?longHair:hair;
                    UnityEngine.Object.DestroyImmediate(temporary.gameObject);UnityEngine.Object.DestroyImmediate(hairSource);
                }
                mesh=StoreMesh(Generated+"/"+gender+"Hair"+style+".asset",mesh);
                var go=new GameObject("Hairstyle "+style,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(skeleton["Head"],false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
                styles[style]=go;hairParts.Add(renderer);
            }
            var avatar=outfit.AddComponent<StudentCharacterAvatar>(); avatar.Body=body; avatar.SkinRenderers=skinParts.ToArray();avatar.SkinMaterialIndices=skinSlots.ToArray();
            avatar.HairRenderers=hairParts.ToArray(); avatar.Hairstyles=styles;
            var appearance=StudentAppearance.Default; appearance.Body=body; avatar.Apply(appearance);
            catalog.Bodies[body]=PrefabUtility.SaveAsPrefabAsset(outfit,Generated+"/"+gender+"Deckhand.prefab");
            UnityEngine.Object.DestroyImmediate(outfit);
        }} finally {EditorSceneManager.ClosePreviewScene(previewScene);}
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("STUDENT CHARACTER ASSETS BUILT");
    }
    private static Mesh StoreMesh(string path,Mesh mesh)
    {
        var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(prior==null) {AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);return prior;
    }
    private static Material ExposedSkinMaterial(string gender,GameObject source,Material headMaterial)
    {
        var body=source.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name!="Eyes" && r.name!="Eyebrows");
        float neckY=source.GetComponentsInChildren<Transform>().First(t=>t.name=="neck_01").position.y;
        var texture=(Texture2D)headMaterial.GetTexture("_BaseMap");var vertices=body.sharedMesh.vertices;var uv=body.sharedMesh.uv;
        Color sum=Color.clear;int count=0;
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 point=body.transform.TransformPoint(vertices[i]);
            if(point.y>neckY && point.y<neckY+.065f && Mathf.Abs(point.x)<.075f) {sum+=texture.GetPixelBilinear(uv[i].x,uv[i].y);count++;}
        }
        Color neutral=count>0?sum/count:new Color(.84f,.65f,.49f);neutral.a=1;
        string path=Generated+"/"+gender+" exposed skin.asset";
        var flat=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(flat==null) {flat=new Texture2D(1,1,TextureFormat.RGBA32,false) {name=gender+" matched skin"};AssetDatabase.CreateAsset(flat,path);}
        flat.SetPixel(0,0,neutral);flat.Apply();EditorUtility.SetDirty(flat);
        var material=Material(gender+" exposed skin",null);material.SetTexture("_BaseMap",flat);EditorUtility.SetDirty(material);return material;
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
