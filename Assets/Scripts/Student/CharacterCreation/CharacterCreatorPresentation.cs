using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Uses the menu's warm colours, gentle lift, and shared button sounds.</summary>
[RequireComponent(typeof(Button))]
public sealed class CharacterCreatorButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    private Button button;
    private RectTransform rect;
    private TMP_Text label;
    private Vector2 origin;
    private Color rest;
    private bool hover, pressed, focused;

    private void Awake()
    {
        button=GetComponent<Button>();rect=(RectTransform)transform;label=GetComponentInChildren<TMP_Text>();
        origin=rect.anchoredPosition;rest=button.image.color;
    }
    public void SetRestColor(Color color) {rest=color;if(!hover && !pressed && !focused) button.image.color=color;}
    private void Update()
    {
        bool enabled=button.IsInteractable();
        bool highlight=enabled && (hover || focused);
        float blend=1-Mathf.Exp(-16*Time.unscaledDeltaTime);
        float scale=enabled && pressed ? .97f : highlight ? 1.035f : 1f;
        transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*scale,blend);
        rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,origin+Vector2.up*(highlight?2:0),blend);
        Color target=enabled && pressed?rest*.84f:highlight?Color.Lerp(rest,new Color(.72f,.59f,.34f,rest.a),.24f):rest;
        target.a=rest.a*(enabled?1:.48f);
        button.image.color=Color.Lerp(button.image.color,target,blend);
        if(label!=null) label.color=Color.Lerp(label.color,highlight?new Color(1,.96f,.83f):new Color(.95f,.90f,.78f,enabled?1:.5f),blend);
    }
    public void OnPointerEnter(PointerEventData data) {hover=true;}
    public void OnPointerExit(PointerEventData data) {hover=false;pressed=false;}
    public void OnPointerDown(PointerEventData data) {if(data.button==PointerEventData.InputButton.Left) pressed=true;}
    public void OnPointerUp(PointerEventData data) {pressed=false;}
    public void OnSelect(BaseEventData data) {focused=true;}
    public void OnDeselect(BaseEventData data) {focused=false;pressed=false;}
    public void OnSubmit(BaseEventData data) {if(button.IsInteractable()) AudioManager.Instance?.PlayClick();}
    private void OnDisable()
    {
        hover=pressed=focused=false;
        if(rect!=null) {rect.anchoredPosition=origin;rect.localScale=Vector3.one;}
        if(button!=null) button.image.color=rest;
    }
}

[RequireComponent(typeof(CanvasGroup))]
public sealed class CharacterCreatorEntrance : MonoBehaviour
{
    private IEnumerator Start()
    {
        var group=GetComponent<CanvasGroup>();var rect=(RectTransform)transform;Vector2 origin=rect.anchoredPosition;
        group.alpha=0;
        float elapsed=0;
        while(elapsed<.32f)
        {
            elapsed+=Time.unscaledDeltaTime;float t=1-Mathf.Pow(1-Mathf.Clamp01(elapsed/.32f),3);
            group.alpha=t;rect.anchoredPosition=origin+Vector2.down*(12*(1-t));yield return null;
        }
        group.alpha=1;rect.anchoredPosition=origin;
    }
}

public sealed class CharacterCreatorCabinDrift : MonoBehaviour
{
    private RectTransform rect;
    private void Awake() {rect=(RectTransform)transform;}
    private void Update()
    {
        Vector2 pointer=Mouse.current!=null?Mouse.current.position.ReadValue():new Vector2(Screen.width*.5f,Screen.height*.5f);
        Vector2 target=new Vector2((pointer.x/Mathf.Max(1,Screen.width)-.5f)*-10,Mathf.Sin(Time.unscaledTime*.16f)*3);
        rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,target,1-Mathf.Exp(-2*Time.unscaledDeltaTime));
    }
}
