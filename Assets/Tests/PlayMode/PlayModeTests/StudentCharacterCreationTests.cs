using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImagineQuest.Characters;
using ImagineQuest.Gameplay;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StudentCharacterCreationTests
{
    private GameObject root;
    private CharacterCreation creator;
    private sealed class Profile : IAppearanceRepository
    {
        public bool IsLocal=>false;
        public bool IsCurrent {get;set;}=true;
        public string Saved=StudentAppearance.Default.AvatarId;
        public int Writes;
        public bool FailLoad;
        public TaskCompletionSource<bool> Pending;
        public TaskCompletionSource<string> PendingLoad;
        public Task<string> LoadAsync()=>PendingLoad!=null?PendingLoad.Task:FailLoad?Task.FromException<string>(new Exception("offline")):Task.FromResult(Saved);
        public async Task SaveAsync(StudentAppearance appearance) {Writes++;if(Pending!=null) await Pending.Task;Saved=appearance.AvatarId;}
    }
    private IEnumerator Open(Profile profile)
    {
        root=new GameObject("Creator test",typeof(Canvas));creator=root.AddComponent<CharacterCreation>();
        creator.Repository=profile;creator.NavigateAfterSave=false;yield return null;yield return null;
    }
    [UnityTest]
    public IEnumerator CreatorShowsClothedAnimatedModelsAndSurvivesAllAppearanceChanges()
    {
        yield return Open(new Profile());Assert.IsTrue(creator.Ready);
        Assert.IsNull(root.transform.Find("3D character creator/Selection summary"));
        Assert.IsFalse(root.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="Selection summary"));
        var theme=StudentCharacterCatalog.Load();Assert.IsNotNull(theme.CreatorBackground);Assert.IsNotNull(theme.MenuFont);
        Assert.AreSame(theme.MenuFont,root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Title").font);
        foreach(int body in new[]{0,1})
        {
            creator.Select(0,body);yield return new WaitUntil(()=>!creator.Preview.IsTransitioning);
            for(int hair=0;hair<StudentAppearance.HairChoiceCount;hair++)
                Assert.AreEqual(StudentAppearance.HairName(body,hair),root.GetComponentsInChildren<Button>().Single(b=>b.name==$"Choice 2 {hair}").GetComponentInChildren<TMP_Text>().text);
            foreach(int hair in new[]{0,1,2,3}) {creator.Select(2,hair);yield return new WaitUntil(()=>!creator.Preview.IsTransitioning);Assert.AreEqual(1,creator.Preview.Avatar.Hairstyles.Count(h=>h.activeSelf));}
            creator.Select(1,body==0?0:5);creator.Select(3,body==0?4:3);
            yield return new WaitUntil(()=>!creator.Preview.IsTransitioning);
            var avatar=creator.Preview.Avatar;var animator=avatar.GetComponent<Animator>();
            Assert.IsTrue(animator.avatar.isHuman && animator.avatar.isValid);Assert.IsFalse(animator.applyRootMotion);
            Assert.AreEqual(body,avatar.Appearance.Body);
            Assert.AreEqual(4,avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r=>r.name.Contains("Peasant")));
            Assert.IsTrue(avatar.SkinRenderers.All(r=>r is SkinnedMeshRenderer));
            foreach(var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
                Assert.AreEqual(renderer.sharedMesh.subMeshCount,renderer.sharedMaterials.Length,"Every clothing submesh must be visible: "+renderer.name);
            var head=avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.sharedMesh.name=="Clothed character head");
            Assert.Less(head.sharedMesh.bounds.size.x,.25f,"The head mesh must not retain the shoulders beneath the shirt.");
            if(body==0)
            {
                var sleeve=avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Male_Peasant_Arms");
                var skinBlock=new MaterialPropertyBlock();sleeve.GetPropertyBlock(skinBlock,0);
                Assert.Less(((Vector4)StudentAppearance.SkinColors[0]-(Vector4)skinBlock.GetColor("_BaseColor")).sqrMagnitude,.000001f);
                var clothBlock=new MaterialPropertyBlock();sleeve.GetPropertyBlock(clothBlock,1);
                Assert.IsTrue(clothBlock.isEmpty,"Skin choices must not tint the uniform.");
            }
            var ankle=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            animator.SetFloat("Speed",1);yield return new WaitForSecondsRealtime(.3f);Vector3 foot=ankle.position;
            yield return new WaitForSecondsRealtime(.3f);Assert.Greater(Vector3.Distance(foot,ankle.position),.01f,"Walking must animate the legs.");
            animator.SetFloat("Speed",0);
            foreach(var size in new[]{new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1024,768)})
            {
                Screen.SetResolution(size.x,size.y,false);yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
                foreach(var text in root.GetComponentsInChildren<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy && !string.IsNullOrEmpty(t.text)))
                {text.ForceMeshUpdate();Assert.IsFalse(text.isTextOverflowing,text.name+" overflows");}
                string folder=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");
                if(!string.IsNullOrEmpty(folder)) {ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Creator-"+body+"-"+Screen.width+"x"+Screen.height+".png"));yield return null;}
            }
            creator.Preview.Rotate(180);yield return null;yield return new WaitForEndOfFrame();
            string backFolder=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");
            if(!string.IsNullOrEmpty(backFolder)) {ScreenCapture.CaptureScreenshot(Path.Combine(backFolder,"Clothing-back-"+body+".png"));yield return null;}
            creator.Preview.ResetView();
        }
        var before=creator.Appearance;creator.Select(1,-1);creator.Select(0,100);Assert.AreEqual(before,creator.Appearance);
        creator.Select(0,0);creator.Select(0,1);creator.Select(2,0);creator.Select(2,3);
        yield return new WaitUntil(()=>!creator.Preview.IsTransitioning);
        Assert.AreEqual(creator.Appearance,creator.Preview.Avatar.Appearance,"Rapid selection must settle on the latest choice.");
    }
    [UnityTest]
    public IEnumerator AvatarSelectionSceneOpensTheNewCreatorWithoutLegacyClassControls()
    {
        yield return SceneManager.LoadSceneAsync("StudentAvatarSelect");yield return null;
        Assert.IsNull(UnityEngine.Object.FindFirstObjectByType<StudentCharacterSelect>());
        creator=UnityEngine.Object.FindFirstObjectByType<CharacterCreation>();Assert.IsNotNull(creator);
        root=creator.gameObject;creator.Repository=new Profile();creator.NavigateAfterSave=false;
        root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Retry profile").onClick.Invoke();yield return null;
        Assert.IsTrue(creator.Ready);Assert.IsNotNull(creator.Preview.Avatar);
        Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<CharacterCreation>(FindObjectsSortMode.None).Length);
    }
    [UnityTest]
    public IEnumerator SavingLocksSelectionsDeduplicatesClicksAndRecoversFromFailure()
    {
        var profile=new Profile {Pending=new TaskCompletionSource<bool>()};yield return Open(profile);
        creator.Select(0,1);var selected=creator.Appearance;creator.ConfirmSelection();creator.ConfirmSelection();
        Assert.AreEqual(1,profile.Writes);Assert.IsTrue(creator.Saving);
        creator.Select(0,0);Assert.AreEqual(selected,creator.Appearance);
        profile.Pending.SetException(new Exception("offline"));yield return null;yield return null;
        Assert.IsFalse(creator.Saving);Assert.AreEqual(selected,creator.Appearance);
        profile.Pending=null;creator.ConfirmSelection();yield return null;
        Assert.AreEqual(selected.AvatarId,profile.Saved);
        profile.IsCurrent=false;creator.ConfirmSelection();Assert.AreEqual(2,profile.Writes);Assert.IsFalse(creator.Ready);
    }
    [UnityTest]
    public IEnumerator MissingProfileCannotOverwriteSavedAppearance()
    {
        var profile=new Profile {FailLoad=true};yield return Open(profile);
        Assert.IsFalse(creator.Ready);creator.ConfirmSelection();Assert.AreEqual(0,profile.Writes);
        Assert.IsTrue(root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Retry profile").gameObject.activeSelf);
        profile.FailLoad=false;profile.Saved=new StudentAppearance {Body=1,Skin=3,Hair=2,HairColor=5}.AvatarId;
        root.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Retry profile").onClick.Invoke();yield return null;
        Assert.IsTrue(creator.Ready);Assert.AreEqual(profile.Saved,creator.Appearance.AvatarId);
    }
    [UnityTest]
    public IEnumerator PreviewControlsAndPortraitsShareTheSavedAppearance()
    {
        yield return Open(new Profile());var avatar=creator.Preview.Avatar;
        var rotation=avatar.transform.localRotation;creator.Preview.Rotate(90);Assert.AreNotEqual(rotation,avatar.transform.localRotation);
        creator.Preview.ResetView();Assert.Less(Quaternion.Angle(rotation,avatar.transform.localRotation),.01f);
        Assert.IsTrue(creator.Preview.ToggleWalk());Assert.AreEqual(1,avatar.GetComponent<Animator>().GetFloat("Speed"));
        Assert.IsFalse(creator.Preview.ToggleWalk());
        var portraitRoot=new GameObject("Profile portrait",typeof(RectTransform),typeof(Image));portraitRoot.transform.SetParent(root.transform,false);
        var selected=new StudentAppearance {Body=1,Skin=1,Hair=2,HairColor=3};
        var portrait=CharacterPreviewViewport.Portrait(portraitRoot.GetComponent<Image>(),selected.AvatarId);
        Assert.AreEqual(selected,portrait.Avatar.Appearance);Assert.IsTrue(portrait.Texture.IsCreated());
        portrait.gameObject.SetActive(false);yield return null;Assert.IsFalse(portrait.Avatar.gameObject.activeInHierarchy);
        portrait.gameObject.SetActive(true);yield return null;Assert.IsTrue(portrait.Avatar.gameObject.activeInHierarchy);
        Assert.AreNotSame(portrait.Texture,creator.Preview.Texture);
        foreach(int body in new[]{0,1})
        {
            selected.Body=body;portrait.Show(selected);yield return new WaitForEndOfFrame();
            string folder=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");
            if(!string.IsNullOrEmpty(folder))
            {
                var previous=RenderTexture.active;var capture=new Texture2D(256,256,TextureFormat.RGBA32,false);
                try {RenderTexture.active=portrait.Texture;capture.ReadPixels(new Rect(0,0,256,256),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(folder,"Portrait-"+body+".png"),capture.EncodeToPNG());}
                finally {RenderTexture.active=previous;UnityEngine.Object.Destroy(capture);}
            }
        }
    }
    [UnityTest]
    public IEnumerator LeavingDuringSaveDoesNotNavigateOrAccessDestroyedUi()
    {
        var profile=new Profile {Pending=new TaskCompletionSource<bool>()};yield return Open(profile);
        creator.ConfirmSelection();UnityEngine.Object.Destroy(root);yield return null;
        profile.Pending.SetResult(true);yield return null;yield return null;
        Assert.AreEqual(1,profile.Writes);
    }
    [UnityTest]
    public IEnumerator DirectShipEntryRefreshesTheSavedGirlWithoutAClassAndIgnoresStaleAccounts()
    {
        QuestSession.Instance?.Clear();yield return SceneManager.LoadSceneAsync("QuestArena");yield return null;
        var ship=UnityEngine.Object.FindFirstObjectByType<CelestialClockQuestBootstrap>();
        var player=UnityEngine.Object.FindFirstObjectByType<CelestialClockPlayerController>();
        var girl=new StudentAppearance {Body=1,Skin=3,Hair=2,HairColor=5};
        var delayed=new Profile {PendingLoad=new TaskCompletionSource<string>()};
        var load=ship.RefreshStudentAppearanceAsync(delayed);yield return null;
        delayed.PendingLoad.SetResult(girl.AvatarId);yield return new WaitUntil(()=>load.IsCompleted);
        Assert.IsFalse(load.IsFaulted);Assert.AreEqual(girl,player.GetComponentInChildren<StudentCharacterAvatar>().Appearance);
        Assert.IsNotNull(player.GetComponentInChildren<Animator>());
        var switched=new Profile {PendingLoad=new TaskCompletionSource<string>()};
        var stale=ship.RefreshStudentAppearanceAsync(switched);switched.IsCurrent=false;
        switched.PendingLoad.SetResult(StudentAppearance.Default.AvatarId);yield return new WaitUntil(()=>stale.IsCompleted);
        Assert.AreEqual(girl,player.GetComponentInChildren<StudentCharacterAvatar>().Appearance);
        var failed=new Profile {FailLoad=true};LogAssert.Expect(LogType.Warning,"[Quest Arena] Saved appearance unavailable; using this account's cached or default skin.");
        var failure=ship.RefreshStudentAppearanceAsync(failed);yield return new WaitUntil(()=>failure.IsCompleted);
        Assert.AreEqual(girl,player.GetComponentInChildren<StudentCharacterAvatar>().Appearance);
    }
    [UnityTest]
    public IEnumerator ShipUsesTheExactSavedAppearanceAndTheSameClothingMeshes()
    {
        bool existed=PlayerPrefs.HasKey("GuestPirateAppearance");string previous=PlayerPrefs.GetString("GuestPirateAppearance","");
        try
        {
            var selected=new StudentAppearance {Body=1,Skin=4,Hair=3,HairColor=2};StudentAvatarIdentity.SaveGuest(selected.AvatarId);
            QuestSession.Instance?.Clear();yield return SceneManager.LoadSceneAsync("QuestArena");yield return null;
            var ship=UnityEngine.Object.FindFirstObjectByType<CelestialClockQuestBootstrap>();Assert.IsNotNull(ship);
            var player=UnityEngine.Object.FindFirstObjectByType<CelestialClockPlayerController>();
            var avatar=player.GetComponentInChildren<StudentCharacterAvatar>();Assert.IsNotNull(avatar);Assert.AreEqual(selected,avatar.Appearance);
            var source=StudentCharacterCatalog.Load().Bodies[1].GetComponent<StudentCharacterAvatar>();
            Assert.AreSame(((SkinnedMeshRenderer)source.SkinRenderers[0]).sharedMesh,((SkinnedMeshRenderer)avatar.SkinRenderers[0]).sharedMesh);
            Assert.IsTrue(avatar.GetComponent<Animator>().avatar.isHuman);
            yield return new WaitForSecondsRealtime(4.5f);
            string folder=Environment.GetEnvironmentVariable("IMAGINEQUEST_CHARACTER_OUTPUT");
            if(!string.IsNullOrEmpty(folder)) {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"Saved-character-on-ship.png"));yield return null;}
            var camera=player.GetComponentInChildren<Camera>();float originalFov=camera.fieldOfView;player.enabled=false;
            try
            {
                foreach(int body in new[]{0,1})
                {
                    selected.Body=body;var refresh=ship.RefreshStudentAppearanceAsync(new Profile {Saved=selected.AvatarId});
                    yield return new WaitUntil(()=>refresh.IsCompleted);Assert.IsFalse(refresh.IsFaulted);
                    avatar=player.GetComponentInChildren<StudentCharacterAvatar>();
                    Assert.IsTrue(avatar.SkinRenderers.Any(r=>r.name=="Collar skin"),"The ship must use the repaired chest skin.");
                    var neck=avatar.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Neck);
                    var eyes=avatar.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="Eyes");
                    var eyeMesh=new Mesh();eyes.BakeMesh(eyeMesh);
                    Vector3 face=Vector3.ProjectOnPlane(eyes.transform.TransformPoint(eyeMesh.bounds.center)-neck.position,Vector3.up).normalized;
                    UnityEngine.Object.Destroy(eyeMesh);
                    camera.fieldOfView=45;camera.transform.position=neck.position+face*.8f+Vector3.up*.4f;camera.transform.LookAt(neck.position+Vector3.up*.03f);
                    yield return new WaitForEndOfFrame();
                    if(!string.IsNullOrEmpty(folder)) {ScreenCapture.CaptureScreenshot(Path.Combine(folder,$"Ship-collar-{body}.png"));yield return null;}
                }
            }
            finally {if(player!=null) player.enabled=true;if(camera!=null) camera.fieldOfView=originalFov;}
        }
        finally {if(existed) PlayerPrefs.SetString("GuestPirateAppearance",previous);else PlayerPrefs.DeleteKey("GuestPirateAppearance");PlayerPrefs.Save();}
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(root!=null) UnityEngine.Object.Destroy(root);yield return null;
        QuestSession.Instance?.Clear();
    }
}
