using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace ImagineQuest.Characters
{
    /// <summary>Owns an isolated, lit 3D stage. No preview lights or meshes touch the gameplay world.</summary>
    public sealed class CharacterPreviewViewport : MonoBehaviour, IDragHandler, IScrollHandler, IPointerDownHandler
    {
        private static int nextStage;
        private GameObject stage;
        private Camera previewCamera;
        private RenderTexture texture;
        private StudentCharacterAvatar avatar;
        private Material platformMaterial, trimMaterial;
        private float yaw=-18, distance=3.65f;
        private bool walking, portrait;
        private Coroutine appearanceTransition;
        private StudentAppearance pendingAppearance;
        public bool IsTransitioning => appearanceTransition!=null;
        public StudentCharacterAvatar Avatar => avatar;
        public RenderTexture Texture => texture;
        public void Initialize(bool headshot=false)
        {
            if(stage!=null) return;
            portrait=headshot;
            var image=GetComponent<RawImage>() ?? gameObject.AddComponent<RawImage>();
            image.raycastTarget=!headshot; image.color=Color.white;
            var aspect=gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=headshot?1:.75f;
            stage=new GameObject("Character preview stage");
            stage.transform.position=new Vector3(1000+(++nextStage)*30,1000,1000);
            var cameraObject=new GameObject("Portrait camera",typeof(Camera)); cameraObject.transform.SetParent(stage.transform,false);
            previewCamera=cameraObject.GetComponent<Camera>(); previewCamera.clearFlags=CameraClearFlags.SolidColor;
            previewCamera.backgroundColor=new Color(.025f,.045f,.065f,0);
            previewCamera.cullingMask=1<<30; previewCamera.nearClipPlane=.05f; previewCamera.farClipPlane=10;
            previewCamera.fieldOfView=headshot?28:35; previewCamera.allowHDR=false;
            texture=new RenderTexture(headshot?256:768,headshot?256:1024,24,RenderTextureFormat.ARGB32) {name="Student 3D preview",antiAliasing=4};
            texture.Create(); previewCamera.targetTexture=texture; image.texture=texture;
            Light("Soft key",new Vector3(2.5f,2.8f,3),new Color(1f,.87f,.71f),38);
            Light("Ocean fill",new Vector3(-2,1.8f,2),new Color(.50f,.73f,1),24);
            Light("Rim",new Vector3(1.2f,2.4f,-2),new Color(.70f,.87f,1),45);
            if(!headshot) CreatePlatform();
            Frame();
        }
        private void CreatePlatform()
        {
            platformMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            platformMaterial.SetColor("_BaseColor",new Color(.10f,.065f,.035f));platformMaterial.SetFloat("_Smoothness",.32f);
            trimMaterial=new Material(platformMaterial);trimMaterial.SetColor("_BaseColor",new Color(.48f,.31f,.13f));
            trimMaterial.SetFloat("_Metallic",.6f);
            PlatformPart("Brass stage rim",new Vector3(0,-.074f,0),new Vector3(1.28f,.017f,1.28f),trimMaterial);
            PlatformPart("Deckhand stage",new Vector3(0,-.035f,0),new Vector3(1.24f,.035f,1.24f),platformMaterial);
        }
        private void PlatformPart(string name,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cylinder);part.name=name;part.transform.SetParent(stage.transform,false);
            part.transform.localPosition=position;part.transform.localScale=scale;part.layer=30;
            part.GetComponent<Renderer>().sharedMaterial=material;Destroy(part.GetComponent<Collider>());
        }
        private void Light(string name,Vector3 position,Color color,float intensity)
        {
            var light=new GameObject(name,typeof(Light)); light.transform.SetParent(stage.transform,false);light.transform.localPosition=position;
            var source=light.GetComponent<Light>();source.type=LightType.Point;source.range=8;source.intensity=intensity;
            source.color=color;source.cullingMask=1<<30;source.shadows=LightShadows.None;
        }
        public void Show(StudentAppearance appearance,bool animate=false)
        {
            Initialize(portrait);
            pendingAppearance=appearance.Valid?appearance:StudentAppearance.Default;
            if(animate && avatar!=null && isActiveAndEnabled)
            {
                if(appearanceTransition==null && !avatar.Appearance.Equals(pendingAppearance))
                    appearanceTransition=StartCoroutine(ChangeAppearance());
                return;
            }
            if(appearanceTransition!=null) {StopCoroutine(appearanceTransition);appearanceTransition=null;}
            ApplyNow(pendingAppearance);GetComponent<RawImage>().color=Color.white;
        }
        private IEnumerator ChangeAppearance()
        {
            do
            {
                yield return Fade(1,.25f,.09f);
                ApplyNow(pendingAppearance);
                yield return Fade(.25f,1,.16f);
            } while(!avatar.Appearance.Equals(pendingAppearance));
            appearanceTransition=null;
        }
        private IEnumerator Fade(float from,float to,float duration)
        {
            var image=GetComponent<RawImage>();float elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,elapsed/duration);
                image.color=new Color(1,1,1,Mathf.Lerp(from,to,t));yield return null;
            }
            image.color=new Color(1,1,1,to);
        }
        private void ApplyNow(StudentAppearance appearance)
        {
            if(avatar==null || avatar.Body!=appearance.Body)
            {
                if(avatar!=null) {avatar.gameObject.SetActive(false);Destroy(avatar.gameObject);}
                avatar=StudentCharacterAvatar.Create(stage.transform,appearance);
                SetLayer(avatar.gameObject,30);
            }
            else avatar.Apply(appearance);
            avatar.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var animator=avatar.GetComponent<Animator>();animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            animator.SetFloat("Speed",walking?1:0); Frame();
        }
        public void Rotate(float degrees) {yaw=Mathf.Repeat(yaw+degrees,360); if(avatar!=null) avatar.transform.localRotation=Quaternion.Euler(0,yaw,0);}
        public void ResetView() {yaw=-18;distance=3.65f;Rotate(0);Frame();}
        public bool ToggleWalk() {walking=!walking;if(avatar!=null) avatar.GetComponent<Animator>().SetFloat("Speed",walking?1:0);return walking;}
        private void Frame()
        {
            if(previewCamera==null) return;
            previewCamera.transform.localPosition=portrait?new Vector3(0,1.66f,1.05f):new Vector3(0,1.06f,distance);
            previewCamera.transform.LookAt(stage.transform.position+Vector3.up*(portrait?1.62f:.95f));
        }
        public void OnPointerDown(PointerEventData data) { }
        public void OnDrag(PointerEventData data) {if(!portrait) Rotate(-data.delta.x*.45f);}
        public void OnScroll(PointerEventData data) {if(!portrait) {distance=Mathf.Clamp(distance-data.scrollDelta.y*.14f,2.4f,4.2f);Frame();}}
        private static void SetLayer(GameObject root,int layer) {foreach(var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=layer;}
        private void OnEnable() {if(stage!=null) stage.SetActive(true);}
        private void OnDisable()
        {
            if(appearanceTransition!=null) {StopCoroutine(appearanceTransition);appearanceTransition=null;ApplyNow(pendingAppearance);GetComponent<RawImage>().color=Color.white;}
            if(stage!=null) stage.SetActive(false);
        }
        private void OnDestroy()
        {
            if(previewCamera!=null) previewCamera.targetTexture=null;
            if(texture!=null) {texture.Release();Destroy(texture);}
            if(stage!=null) Destroy(stage);
            if(platformMaterial!=null) Destroy(platformMaterial);
            if(trimMaterial!=null) Destroy(trimMaterial);
        }
        public static CharacterPreviewViewport Portrait(Image original,string avatarId)
        {
            var preview=original.GetComponentInChildren<CharacterPreviewViewport>(true);
            if(preview==null)
            {
                var root=new GameObject("3D portrait",typeof(RectTransform),typeof(RawImage));root.transform.SetParent(original.transform,false);
                var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                preview=root.AddComponent<CharacterPreviewViewport>();preview.Initialize(true);
            }
            original.enabled=false;preview.gameObject.SetActive(true);preview.Show(StudentAppearance.FromAvatarId(avatarId));return preview;
        }
    }
}
