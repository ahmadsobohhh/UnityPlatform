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
    private readonly Color ink=new Color(.025f,.044f,.065f), card=new Color(.045f,.073f,.097f), gold=new Color(.94f,.73f,.39f), muted=new Color(.58f,.68f,.72f);
    private readonly List<Button> choices=new List<Button>();
    private readonly List<(Button button,int kind,int index)> selections=new List<(Button,int,int)>();
    private CharacterPreviewViewport preview;
    private Button save,retry,pose;
    private TMP_Text status,summary,saveLabel;
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
            status.text=repository.IsLocal?"Your navigator will be saved on this device.":"Confirm to save your navigator to your student profile.";
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
        switch(kind) {case 0:next.Body=index;break;case 1:next.Skin=index;break;case 2:next.Hair=index;break;case 3:next.HairColor=index;break;default:return;}
        if(!next.Valid) return;
        appearance=next;edited=true;Refresh();
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
    private void Refresh()
    {
        if(preview==null) return;
        try {preview.Show(appearance);} catch(Exception) {ready=false;status.text="Character artwork couldn't load. Please reopen the game.";}
        summary.text=appearance.BodyName+"  ·  "+StudentAppearance.SkinNames[appearance.Skin]+"  ·  "+StudentAppearance.HairNames[appearance.Hair]+"  ·  "+StudentAppearance.HairColorNames[appearance.HairColor];
        foreach(var option in selections)
        {
            int selected=option.kind==0?appearance.Body:option.kind==1?appearance.Skin:option.kind==2?appearance.Hair:appearance.HairColor;
            bool active=selected==option.index;option.button.GetComponent<Outline>().enabled=active;
            if(option.kind==0 || option.kind==2) option.button.image.color=active?new Color(.14f,.28f,.32f):new Color(.075f,.115f,.145f);
        }
        foreach(var choice in choices) choice.interactable=!saving;
        save.interactable=ready && !saving;
        saveLabel.text=saving?"SAVING…":repository!=null && repository.IsLocal?"SAIL TO THE AURORA":"SAVE MY NAVIGATOR";
    }
    private void Build()
    {
        var canvas=GetComponent<Canvas>()??gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=GetComponent<CanvasScaler>()??gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        if(GetComponent<GraphicRaycaster>()==null) gameObject.AddComponent<GraphicRaycaster>();
        var root=Panel("3D character creator",transform,0,0,1,1,ink);
        StatusUi.Rect("Ocean gradient",root,0,0,1,1).gameObject.AddComponent<CharacterCreatorBackdrop>().raycastTarget=false;
        Label("Brand",root,"IMAGINE QUEST     /     YOUR VOYAGE",.045f,.92f,.75f,.97f,16,gold);
        Label("Title",root,"Meet your navigator.",.045f,.835f,.70f,.92f,46,Color.white).fontStyle=FontStyles.Bold;
        Label("Introduction",root,"Make yourself at home aboard the Aurora.",.047f,.79f,.64f,.84f,19,muted);
        Panel("Top brass rule",root,.045f,.777f,.955f,.779f,new Color(.45f,.34f,.20f));
        var stage=Panel("Character stage",root,.045f,.135f,.565f,.755f,new Color(.035f,.062f,.083f,.75f));
        var raw=StatusUi.Rect("Live 3D character",stage,.02f,.03f,.98f,.98f);raw.gameObject.AddComponent<RawImage>();
        preview=raw.gameObject.AddComponent<CharacterPreviewViewport>();preview.Initialize();
        Label("Stage eyebrow",stage,"THE AURORA'S CREW",.045f,.91f,.85f,.97f,13,gold);
        Label("Stage instructions",stage,"DRAG TO TURN   ·   SCROLL TO ZOOM",.045f,.025f,.96f,.07f,12,muted).alignment=TextAlignmentOptions.Center;
        var right=Panel("Appearance controls",root,.59f,.135f,.955f,.755f,card);
        Label("Appearance title",right,"YOUR APPEARANCE",.07f,.89f,.94f,.97f,18,gold);
        Label("Body label",right,"Navigator",.07f,.81f,.94f,.87f,19,Color.white);
        Option(right,"Boy",0,0,.07f,.715f,.475f,.80f);Option(right,"Girl",0,1,.505f,.715f,.93f,.80f);
        Label("Skin label",right,"Skin tone",.07f,.63f,.94f,.69f,18,Color.white);
        for(int i=0;i<6;i++) Swatch(right,1,i,StudentAppearance.SkinColors[i],.07f+i*.146f,.55f);
        Label("Hair label",right,"Hairstyle",.07f,.45f,.94f,.51f,18,Color.white);
        for(int i=0;i<4;i++) {float left=i%2==0?.07f:.505f;float bottom=i<2?.36f:.27f;Option(right,StudentAppearance.HairNames[i],2,i,left,bottom,left+.405f,bottom+.075f);}
        Label("Hair color label",right,"Hair colour",.07f,.18f,.94f,.24f,18,Color.white);
        for(int i=0;i<6;i++) Swatch(right,3,i,StudentAppearance.HairColors[i],.07f+i*.146f,.10f);
        Label("Uniform note",right,"DECKHAND UNIFORM  ·  READY TO SAIL",.07f,.025f,.94f,.065f,11,muted);
        summary=Label("Selection summary",root,"",.045f,.085f,.56f,.125f,16,muted);
        var tools=StatusUi.Rect("Preview tools",root,.045f,.025f,.565f,.075f);
        Button("Turn left",tools,"TURN LEFT",0,0,.23f,1,()=>preview.Rotate(-30));Button("Turn right",tools,"TURN RIGHT",.25f,0,.48f,1,()=>preview.Rotate(30));
        pose=Button("Preview walking",tools,"WALK",.50f,0,.73f,1,()=>pose.GetComponentInChildren<TMP_Text>().text=preview.ToggleWalk()?"IDLE":"WALK");
        Button("Reset camera",tools,"RESET VIEW",.75f,0,1,1,()=>preview.ResetView());
        status=Label("Save status",root,"Opening your character…",.59f,.068f,.955f,.12f,14,muted);
        save=Button("Save appearance",root,"SAVE MY NAVIGATOR",.715f,.018f,.955f,.065f,ConfirmSelection);save.image.color=new Color(.72f,.51f,.23f);
        saveLabel=save.GetComponentInChildren<TMP_Text>();saveLabel.color=new Color(.055f,.042f,.025f);saveLabel.fontStyle=FontStyles.Bold;
        Button("Back",root,"BACK",.59f,.018f,.70f,.065f,Back);
        retry=Button("Retry profile",root,"RETRY CONNECTION",.755f,.92f,.955f,.97f,LoadProfile);retry.gameObject.SetActive(false);
        if(EventSystem.current==null) new GameObject("Character editor input",typeof(EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        Refresh();
    }
    private RectTransform Panel(string name,Transform parent,float l,float b,float r,float t,Color color)=>StatusUi.Panel(name,parent,l,b,r,t,color,true);
    private TMP_Text Label(string name,Transform parent,string text,float l,float b,float r,float t,int size,Color color)=>StatusUi.Label(name,parent,text,l,b,r,t,size,color);
    private Button Button(string name,Transform parent,string label,float l,float b,float r,float t,Action action)
    {var button=StatusUi.Button(name,parent,label,l,b,r,t,action);button.GetComponentInChildren<TMP_Text>().fontSize=15;return button;}
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
        var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=gold;outline.effectDistance=new Vector2(3,-3);
    }
    private void OnDestroy(){++generation;}
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class CharacterCreatorBackdrop : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;
        mesh.AddVert(new Vector2(rect.xMin,rect.yMin),new Color(.015f,.026f,.04f),Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMin,rect.yMax),new Color(.055f,.10f,.13f),Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMax,rect.yMax),new Color(.025f,.045f,.065f),Vector2.zero);
        mesh.AddVert(new Vector2(rect.xMax,rect.yMin),new Color(.015f,.026f,.04f),Vector2.zero);
        mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
    }
}
