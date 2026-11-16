using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImagineQuest.Characters;
using ImagineQuest.Gameplay;
using ImagineQuest.Student;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class CharacterCreation : MonoBehaviour
{
    private readonly Color ink=new Color(.035f,.025f,.018f), card=new Color(.055f,.04f,.025f,.88f), gold=new Color(.76f,.63f,.38f), muted=new Color(.76f,.72f,.61f);
    private readonly List<Button> choices=new List<Button>();
    private readonly List<(Button button,int kind,int index)> selections=new List<(Button,int,int)>();
    private CharacterPreviewViewport preview;
    private Button save,retry,pose;
    private TMP_Text status,saveLabel;
    private StudentCharacterCatalog catalog;
    private Material menuMaterial,readingMaterial;
    private IAppearanceRepository repository;
    private bool ready,saving,loading,edited;
    private int generation;
    private string returnScene;
    private StudentAppearance appearance=StudentAppearance.Default;
    public IAppearanceRepository Repository {get=>repository;set=>repository=value;}
    public bool NavigateAfterSave {get;set;}=true;
    public StudentAppearance Appearance=>appearance;
    public bool Ready=>ready;
    public bool Saving=>saving;
    public CharacterPreviewViewport Preview=>preview;
    private void Awake()
    {
        returnScene=AppearanceEditorRoute.Consume();
        catalog=StudentCharacterCatalog.Load();
        menuMaterial=ReadableMaterial(catalog?.MenuFont);
        readingMaterial=ReadableMaterial(catalog?.ReadingFont);
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        foreach(Transform child in transform) child.gameObject.SetActive(false);
        Build();
    }
    private void Start()=>LoadProfile();
    private async void LoadProfile()
    {
        if(loading || saving) return;
        loading=true;ready=false;int version=++generation;status.text="Opening your character…";retry.gameObject.SetActive(false);Refresh();
        try
        {
            if(repository==null)
            {
                if(Environment.GetEnvironmentVariable("IMAGINEQUEST_OFFLINE_TESTS")=="1")
                {status.text="Choose your navigator. Preview mode • no profile changes.";return;}
                var connection=FirebaseAppearanceRepository.Connect();
                var connected=await WithTimeout(connection);
                if(this==null || version!=generation) return;
                repository=connected;
            }
            var load=repository.LoadAsync();
            string saved=await WithTimeout(load);
            if(this==null || version!=generation) return;
            if(!repository.IsCurrent) throw new InvalidOperationException("Account changed.");
            if(!edited) appearance=StudentAppearance.FromAvatarId(saved);
            ready=true;
            status.text="";
        }
        catch(Exception)
        {
            if(this==null || version!=generation) return;
            status.text="We couldn't open your profile. Explore the styles and retry when connected.";retry.gameObject.SetActive(true);
        }
        finally {if(this!=null && version==generation) {loading=false;Refresh();}}
    }
    private static async Task<T> WithTimeout<T>(Task<T> operation)
    {
        if(await Task.WhenAny(operation,Task.Delay(20000))==operation) return await operation;
        // A timed-out connection can still finish. Observe errors without applying stale results.
        _=ObserveCompletion(operation);
        throw new TimeoutException();
    }
    private static async Task ObserveCompletion(Task operation) {try {await operation;} catch(Exception) {}}
    public void Select(int kind,int index)
    {
        if(saving) return;
        var next=appearance;
        switch(kind) {case 0:if(index!=next.Body && next.Hair>=4) next.Hair=next.Body==0?(next.Hair==4?2:3):(next.Hair==4?2:1);next.Body=index;break;case 1:next.Skin=index;break;case 2:if(index<0 || index>=StudentAppearance.HairChoiceCount) return;next.Hair=index;break;case 3:next.HairColor=index;break;default:return;}
        if(!next.Valid) return;
        appearance=next;edited=true;preview.Show(appearance,true);Refresh(false);
    }
    public async void ConfirmSelection()
    {
        if(!ready || saving || repository==null) return;
        if(!repository.IsCurrent) {ready=false;status.text="Your account changed. Return and sign in again.";Refresh();return;}
        saving=true;var selected=appearance;int version=++generation;status.text="Saving your navigator…";Refresh();
        try
        {
            var operation=repository.SaveAsync(selected);
            if(await Task.WhenAny(operation,Task.Delay(18000))!=operation && this!=null && version==generation)
                status.text="Still waiting for a connection. You can wait here or go back.";
            await operation;
            if(this==null || version!=generation) return;
            if(!repository.IsCurrent) throw new InvalidOperationException("Account changed.");
            status.text="Navigator saved. You're ready to sail.";
            if(NavigateAfterSave) SceneTransition.LoadScene(returnScene??(repository.IsLocal?"QuestArena":"StudentHub"));
        }
        catch(Exception)
        {
            if(this==null || version!=generation) return;
            status.text="Your appearance couldn't be saved. Your choices are still here—try again.";
            if(!repository.IsCurrent) ready=false;
        }
        finally {if(this!=null && version==generation) {saving=false;Refresh();}}
    }
    private void Back() {++generation;SceneTransition.LoadScene(returnScene??(repository!=null && repository.IsLocal?"QuestArena":"WelcomePage"));}
    private void Refresh(bool updatePreview=true)
    {
        if(preview==null) return;
        if(updatePreview) try {preview.Show(appearance);} catch(Exception) {ready=false;status.text="Character artwork couldn't load. Please reopen the game.";}
        foreach(var option in selections)
        {
            int selected=option.kind==0?appearance.Body:option.kind==1?appearance.Skin:option.kind==2?appearance.Hair:appearance.HairColor;
            bool active=selected==option.index;option.button.GetComponent<Outline>().enabled=active;
            if(option.kind==2) option.button.GetComponentInChildren<TMP_Text>().text=StudentAppearance.HairName(appearance.Body,option.index);
            if(option.kind==0 || option.kind==2)
                option.button.GetComponent<CharacterCreatorButtonMotion>().SetRestColor(active?new Color(.43f,.33f,.17f,.98f):new Color(.25f,.19f,.10f,.88f));
        }
        foreach(var choice in choices) choice.interactable=!saving;
        save.interactable=ready && !saving;
        saveLabel.text=saving?"Saving…":returnScene!=null?"Save Changes":"Set Sail";
    }
    private void Build()
    {
        var canvas=GetComponent<Canvas>()??gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=GetComponent<CanvasScaler>()??gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        if(GetComponent<GraphicRaycaster>()==null) gameObject.AddComponent<GraphicRaycaster>();
        var root=Panel("3D character creator",transform,0,0,1,1,ink);
        var background=StatusUi.Rect("Captain's cabin",root,0,0,1,1);
        var backdrop=background.gameObject.AddComponent<Image>();backdrop.sprite=catalog?.CreatorBackground;backdrop.raycastTarget=false;
        var aspect=background.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
        aspect.aspectRatio=backdrop.sprite!=null?backdrop.sprite.rect.width/backdrop.sprite.rect.height:16f/9;
        background.localScale=Vector3.one*1.045f;background.gameObject.AddComponent<CharacterCreatorCabinDrift>();
        StatusUi.Panel("Candlelit shade",root,0,0,1,1,new Color(.035f,.02f,.01f,.26f));
        root=StatusUi.Rect("Creator content",root,0,0,1,1);root.gameObject.AddComponent<CharacterCreatorEntrance>();
        Label("Title",root,"Choose Your Pirate",.07f,.88f,.93f,.98f,62,new Color(.95f,.90f,.78f)).alignment=TextAlignmentOptions.Center;
        var intro=Label("Introduction",root,"Your voyage begins aboard the Aurora.",.10f,.835f,.90f,.885f,23,muted);
        ReadingFont(intro);intro.alignment=TextAlignmentOptions.Center;
        Panel("Top brass rule",root,.20f,.822f,.80f,.824f,new Color(.55f,.44f,.24f,.6f));
        var stage=Panel("Character stage",root,.06f,.18f,.53f,.805f,new Color(.045f,.03f,.02f,.70f));Frame(stage);
        var raw=StatusUi.Rect("Live 3D character",stage,.02f,.035f,.98f,.95f);raw.gameObject.AddComponent<RawImage>();
        preview=raw.gameObject.AddComponent<CharacterPreviewViewport>();preview.Initialize();
        Label("Stage eyebrow",stage,"The Aurora's Crew",.045f,.90f,.955f,.98f,26,gold).alignment=TextAlignmentOptions.Center;
        var hint=Label("Stage instructions",stage,"Drag to turn  ·  Scroll to zoom",.045f,.02f,.955f,.065f,15,muted);
        ReadingFont(hint);hint.alignment=TextAlignmentOptions.Center;
        var right=Panel("Appearance controls",root,.57f,.18f,.94f,.805f,card);Frame(right);
        Label("Appearance title",right,"Your Look",.07f,.88f,.93f,.98f,32,gold).alignment=TextAlignmentOptions.Center;
        Option(right,"Boy",0,0,.07f,.715f,.475f,.80f);Option(right,"Girl",0,1,.505f,.715f,.93f,.80f);
        ReadingFont(Label("Skin label",right,"Skin Tone",.07f,.63f,.94f,.70f,24,new Color(.94f,.90f,.79f)));
        for(int i=0;i<6;i++) Swatch(right,1,i,StudentAppearance.SkinColors[i],.07f+i*.146f,.55f);
        ReadingFont(Label("Hair label",right,"Hairstyle",.07f,.45f,.94f,.52f,24,new Color(.94f,.90f,.79f)));
        for(int i=0;i<4;i++) {float left=i%2==0?.07f:.505f;float bottom=i<2?.36f:.27f;Option(right,StudentAppearance.HairName(appearance.Body,i),2,i,left,bottom,left+.405f,bottom+.075f);}
        ReadingFont(Label("Hair color label",right,"Hair Colour",.07f,.18f,.94f,.25f,24,new Color(.94f,.90f,.79f)));
        for(int i=0;i<6;i++) Swatch(right,3,i,StudentAppearance.HairColors[i],.07f+i*.146f,.10f);
        var tools=StatusUi.Rect("Preview tools",root,.06f,.065f,.53f,.135f);
        Button("Turn left",tools,"Turn Left",0,0,.23f,1,()=>preview.Rotate(-30));Button("Turn right",tools,"Turn Right",.25f,0,.48f,1,()=>preview.Rotate(30));
        pose=Button("Preview walking",tools,"Walk",.50f,0,.73f,1,()=>pose.GetComponentInChildren<TMP_Text>().text=preview.ToggleWalk()?"Idle":"Walk");
        Button("Reset camera",tools,"Reset",.75f,0,1,1,()=>preview.ResetView());
        status=Label("Save status",root,"Opening your character…",.57f,.135f,.94f,.177f,17,muted);
        ReadingFont(status);
        save=Button("Save appearance",root,"Set Sail",.70f,.065f,.94f,.135f,ConfirmSelection);
        save.GetComponent<CharacterCreatorButtonMotion>().SetRestColor(new Color(.46f,.35f,.16f,.98f));
        saveLabel=save.GetComponentInChildren<TMP_Text>();saveLabel.fontSize=29;
        Button("Back",root,"Back",.57f,.065f,.68f,.135f,Back);
        retry=Button("Retry profile",root,"Retry Connection",.74f,.015f,.94f,.06f,LoadProfile);retry.gameObject.SetActive(false);
        if(EventSystem.current==null) new GameObject("Character editor input",typeof(EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        Refresh();
    }
    private RectTransform Panel(string name,Transform parent,float l,float b,float r,float t,Color color)=>StatusUi.Panel(name,parent,l,b,r,t,color,true);
    private TMP_Text Label(string name,Transform parent,string text,float l,float b,float r,float t,int size,Color color)
    {
        var label=StatusUi.Label(name,parent,text,l,b,r,t,size,color);
        if(catalog?.MenuFont!=null) {label.font=catalog.MenuFont;label.fontSharedMaterial=menuMaterial;}
        return label;
    }
    private Button Button(string name,Transform parent,string label,float l,float b,float r,float t,Action action)
    {
        var button=StatusUi.Button(name,parent,label,l,b,r,t,action);button.transition=Selectable.Transition.None;
        var text=button.GetComponentInChildren<TMP_Text>();text.fontSize=24;text.color=new Color(.95f,.90f,.78f);
        if(catalog?.MenuFont!=null) {text.font=catalog.MenuFont;text.fontSharedMaterial=menuMaterial;}
        button.gameObject.AddComponent<CharacterCreatorButtonMotion>().SetRestColor(new Color(.28f,.22f,.10f,.88f));
        button.gameObject.AddComponent<ButtonSFX>();return button;
    }
    private void Frame(RectTransform rect)
    {
        Color trim=new Color(.5f,.39f,.20f,.65f);
        StatusUi.Panel("Brass top",rect,0,.998f,1,1,trim);
        StatusUi.Panel("Brass bottom",rect,0,0,1,.002f,trim);
        StatusUi.Panel("Brass left",rect,0,0,.0015f,1,trim);
        StatusUi.Panel("Brass right",rect,.9985f,0,1,1,trim);
    }
    private static Material ReadableMaterial(TMP_FontAsset font)
    {
        if(font==null) return null;
        // Keep the shared game font, but use a screen-owned material: its original
        // heavy outline swallowed the thin letter faces at creator button sizes.
        var material=new Material(font.material) {name="Character menu lettering"};
        material.SetColor("_FaceColor",Color.white);material.SetColor("_OutlineColor",new Color(.025f,.015f,.008f));
        material.SetFloat("_FaceDilate",.05f);material.SetFloat("_OutlineWidth",.04f);return material;
    }
    private void ReadingFont(TMP_Text label)
    {if(catalog?.ReadingFont!=null) {label.font=catalog.ReadingFont;label.fontSharedMaterial=readingMaterial;}}
    private void Option(Transform parent,string name,int kind,int index,float l,float b,float r,float t)
    {
        var button=Button("Choice "+kind+" "+index,parent,name,l,b,r,t,()=>Select(kind,index));choices.Add(button);selections.Add((button,kind,index));
        var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=gold;outline.effectDistance=new Vector2(2,-2);
    }
    private void Swatch(Transform parent,int kind,int index,Color color,float left,float bottom)
    {
        string name=kind==1?StudentAppearance.SkinNames[index]:StudentAppearance.HairColorNames[index];
        var button=Button("Choice "+kind+" "+index+" "+name,parent,"",left,bottom,left+.12f,bottom+.062f,()=>Select(kind,index));button.image.color=color;
        choices.Add(button);selections.Add((button,kind,index));
        button.GetComponent<CharacterCreatorButtonMotion>().SetRestColor(color);
        var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=gold;outline.effectDistance=new Vector2(3,-3);
    }
    private void OnDestroy(){++generation;if(menuMaterial!=null) Destroy(menuMaterial);if(readingMaterial!=null) Destroy(readingMaterial);}
}
