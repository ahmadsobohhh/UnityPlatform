using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupLoginUI
{
    static readonly Color CardBg = new Color(0.05f, 0.05f, 0.08f, 0.85f);
    static readonly Color InputBg = new Color(0.12f, 0.12f, 0.15f, 0.9f);
    static readonly Color InputBorder = new Color(0.35f, 0.3f, 0.25f, 0.6f);
    static readonly Color LabelColor = new Color(0.85f, 0.82f, 0.75f, 1f);
    static readonly Color PlaceholderColor = new Color(0.5f, 0.48f, 0.42f, 1f);
    static readonly Color BtnColor = new Color(0.65f, 0.55f, 0.35f, 1f);
    static readonly Color BtnTextColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    static readonly Color LinkColor = new Color(0.8f, 0.7f, 0.5f, 1f);
    static readonly Color WarningColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    static readonly Color ConfirmColor = new Color(0.3f, 0.9f, 0.4f, 1f);

    static TMP_FontAsset menuFont;

    [MenuItem("Tools/Setup Login UI in WelcomePage")]
    public static void Run()
    {
        string scenePath = "Assets/Scenes/UniversalPages/WelcomePage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { EditorUtility.DisplayDialog("Error", "Canvas not found.", "OK"); return; }

        LoadFont();

        // Clean up old panels if re-running
        DestroyChild(canvas.transform, "LoginPanel");
        DestroyChild(canvas.transform, "RegisterPanel");

        var loginPanel = BuildLoginPanel(canvas.transform);
        var registerPanel = BuildRegisterPanel(canvas.transform);

        // Move panels before FadeOverlay
        var fade = canvas.transform.Find("FadeOverlay");
        if (fade != null)
        {
            int idx = fade.GetSiblingIndex();
            loginPanel.transform.SetSiblingIndex(idx);
            registerPanel.transform.SetSiblingIndex(idx);
        }

        SetupUIManagerComponent(canvas, loginPanel, registerPanel);
        SetupAuthManagerComponent(canvas, loginPanel, registerPanel);
        FixFadeOverlay(canvas.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Done", "Login & Register UI added to WelcomePage!", "OK");
    }

    static void LoadFont()
    {
        string[] guids = AssetDatabase.FindAssets("Treamd SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("BlackPearl SDF t:TMP_FontAsset", new[] { "Assets/Text" });
        if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
            menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));

        if (menuFont != null)
            Debug.Log($"[SetupLoginUI] Using font: {menuFont.name}");
        else
            Debug.LogWarning("[SetupLoginUI] No TMP font asset found!");
    }

    static void FixFadeOverlay(Transform canvas)
    {
        var fade = canvas.Find("FadeOverlay");
        if (fade == null) return;
        var img = fade.GetComponent<Image>();
        if (img != null)
            img.color = new Color(0, 0, 0, 0);
    }

    // ─── LOGIN PANEL ────────────────────────────────────────────
    static GameObject BuildLoginPanel(Transform canvas)
    {
        var panel = CreatePanel(canvas, "LoginPanel");
        var contentGO = CreateCard(panel.transform, "LoginCard", 480f, 560f);
        var content = contentGO.transform;
        float y = 210f;

        CreateLabel(content, "TitleTxt", "Sign In", 56f, y, 420f, 70f);
        y -= 90f;

        CreateLabel(content, "WarningText", "", 24f, y + 20f, 420f, 35f, WarningColor);
        var confirmGO = CreateLabel(content, "ConfirmText", "", 24f, y + 20f, 420f, 35f, ConfirmColor);
        confirmGO.SetActive(false);
        y -= 20f;

        CreateInputField(content, "UserInput", "Username or Email", false, y);
        y -= 85f;
        CreateInputField(content, "PasswordInput", "Password", true, y);
        y -= 100f;

        CreateButton(content, "SignInBtn", "Sign In", y, 360f, 55f);
        y -= 75f;

        CreateLabel(content, "AccountTxt", "Don't have an account?", 22f, y, 420f, 30f, LabelColor);
        y -= 35f;
        CreateLabel(content, "SignUpLink", "Sign Up", 28f, y, 420f, 35f, LinkColor);
        y -= 55f;
        CreateBackButton(content, y);

        return panel;
    }

    // ─── REGISTER PANEL ─────────────────────────────────────────
    static GameObject BuildRegisterPanel(Transform canvas)
    {
        var panel = CreatePanel(canvas, "RegisterPanel");
        var contentGO = CreateCard(panel.transform, "RegisterCard", 500f, 800f);
        var content = contentGO.transform;
        float y = 350f;

        CreateLabel(content, "TitleTxt", "Create Account", 50f, y, 440f, 65f);
        y -= 70f;

        CreateLabel(content, "WarningText", "", 24f, y + 10f, 440f, 35f, WarningColor);
        y -= 30f;

        // Role toggles
        var toggleRow = CreateEmpty(content, "SelectBoxes", 0f, y, 400f, 50f).transform;
        CreateToggle(toggleRow, "StudentBox", "Student", -100f, true);
        CreateToggle(toggleRow, "TeacherBox", "Teacher", 100f, false);
        y -= 65f;

        // Student fields
        var studentUI = CreateEmpty(content, "StudentUI", 0f, y - 130f, 440f, 360f);
        float sy = 150f;
        CreateInputField(studentUI.transform, "FirstInput", "First Name", false, sy); sy -= 75f;
        CreateInputField(studentUI.transform, "LastInput", "Last Name", false, sy); sy -= 75f;
        CreateInputField(studentUI.transform, "UserInput", "Username", false, sy); sy -= 75f;
        CreateInputField(studentUI.transform, "PasswordInput", "Password", true, sy); sy -= 75f;
        CreateInputField(studentUI.transform, "VerifyPassInput", "Verify Password", true, sy);

        // Teacher fields (inactive by default)
        var teacherUI = CreateEmpty(content, "TeacherUI", 0f, y - 160f, 440f, 430f);
        float ty = 180f;
        CreateInputField(teacherUI.transform, "FirstInput", "First Name", false, ty); ty -= 70f;
        CreateInputField(teacherUI.transform, "LastInput", "Last Name", false, ty); ty -= 70f;
        CreateInputField(teacherUI.transform, "UserInput", "Username", false, ty); ty -= 70f;
        CreateInputField(teacherUI.transform, "EmailInput", "Email", false, ty); ty -= 70f;
        CreateInputField(teacherUI.transform, "PasswordInput", "Password", true, ty); ty -= 70f;
        CreateInputField(teacherUI.transform, "VerifyPassInput", "Verify Password", true, ty);
        teacherUI.SetActive(false);

        float bottomY = -350f;
        CreateButton(content, "SignUpBtn", "Sign Up", bottomY, 360f, 55f);
        bottomY -= 65f;

        CreateLabel(content, "AccountTxt", "Already have an account?", 22f, bottomY, 440f, 30f, LabelColor);
        bottomY -= 35f;
        CreateLabel(content, "SignInLink", "Sign In", 28f, bottomY, 440f, 35f, LinkColor);
        bottomY -= 55f;
        CreateBackButton(content, bottomY);

        return panel;
    }

    // ─── HELPERS ────────────────────────────────────────────────

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.AddComponent<CanvasGroup>();
        go.SetActive(true);
        return go;
    }

    static GameObject CreateCard(Transform parent, string name, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(60f, -20f);
        rect.sizeDelta = new Vector2(width, height);

        var img = go.AddComponent<Image>();
        img.color = CardBg;
        img.raycastTarget = true;

        // Rounded corners aren't possible without a sprite, so keep it clean
        return go;
    }

    static void CreateInputField(Transform parent, string name, string placeholder, bool isPassword, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(380f, 55f);

        var bg = go.AddComponent<Image>();
        bg.color = InputBg;

        // Text Area
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(14f, 6f);
        taRect.offsetMax = new Vector2(-14f, -6f);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        var phRect = phGO.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        var phTmp = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 26f;
        phTmp.fontStyle = FontStyles.Bold;
        phTmp.color = PlaceholderColor;
        phTmp.alignment = TextAlignmentOptions.Left;
        if (menuFont != null) phTmp.font = menuFont;

        // Text
        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(textArea.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var txtTmp = txtGO.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 26f;
        txtTmp.fontStyle = FontStyles.Bold;
        txtTmp.color = LabelColor;
        txtTmp.alignment = TextAlignmentOptions.Left;
        if (menuFont != null) txtTmp.font = menuFont;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = txtTmp;
        input.placeholder = phTmp;
        input.fontAsset = menuFont;
        input.pointSize = 26;
        if (isPassword) input.contentType = TMP_InputField.ContentType.Password;

        var nav = input.navigation;
        nav.mode = Navigation.Mode.Vertical;
        input.navigation = nav;
    }

    static GameObject CreateButton(Transform parent, string name, string label, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = BtnColor;

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        go.AddComponent<ButtonHoverEffect>();
        go.AddComponent<ButtonPulseEffect>();

        // Label
        var txtGO = new GameObject("Text (TMP)");
        txtGO.transform.SetParent(go.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 30f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = BtnTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;

        return go;
    }

    static void CreateBackButton(Transform parent, float y)
    {
        var go = new GameObject("BackBtn");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(150f, 45f);

        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        go.AddComponent<ButtonHoverEffect>();

        var txtGO = new GameObject("Text (TMP)");
        txtGO.transform.SetParent(go.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "< Back";
        tmp.fontSize = 28f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = LabelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (menuFont != null) tmp.font = menuFont;
    }

    static void CreateToggle(Transform parent, string name, string label, float x, bool isOn)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(150f, 40f);

        go.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        // Checkbox background
        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(go.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.anchoredPosition = new Vector2(15f, 0f);
        bgRect.sizeDelta = new Vector2(24f, 24f);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = InputBg;

        // Checkmark
        var checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(bgGO.transform, false);
        var checkRect = checkGO.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(16f, 16f);
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = BtnColor;

        // Label
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lblRect = lblGO.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 0f);
        lblRect.anchorMax = new Vector2(1f, 1f);
        lblRect.offsetMin = new Vector2(35f, 0f);
        lblRect.offsetMax = Vector2.zero;
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        lblTmp.text = label;
        lblTmp.fontSize = 30f;
        lblTmp.fontStyle = FontStyles.Bold;
        lblTmp.color = LabelColor;
        lblTmp.alignment = TextAlignmentOptions.Left;
        if (menuFont != null) lblTmp.font = menuFont;

        var toggle = go.AddComponent<Toggle>();
        toggle.isOn = isOn;
        toggle.graphic = checkImg;
        toggle.targetGraphic = bgImg;

        var toggleColors = toggle.colors;
        toggleColors.normalColor = Color.white;
        toggleColors.highlightedColor = new Color(1, 1, 1, 0.8f);
        toggle.colors = toggleColors;
    }

    static GameObject CreateLabel(Transform parent, string name, string text, float size, float y, float w, float h, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(w, h);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color ?? LabelColor;
        tmp.alignment = align;
        if (menuFont != null) tmp.font = menuFont;

        return go;
    }

    static GameObject CreateEmpty(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
        return go;
    }

    // ─── COMPONENT WIRING ───────────────────────────────────────

    static void SetupUIManagerComponent(GameObject canvas, GameObject loginPanel, GameObject registerPanel)
    {
        var mgr = canvas.GetComponent<UIManager>();
        if (mgr == null) mgr = canvas.AddComponent<UIManager>();

        var so = new SerializedObject(mgr);

        var mainMenuT = canvas.transform.Find("MainMenu");
        if (mainMenuT != null)
        {
            var grp = mainMenuT.GetComponent<CanvasGroup>();
            if (grp == null) grp = mainMenuT.gameObject.AddComponent<CanvasGroup>();
            so.FindProperty("mainMenuGroup").objectReferenceValue = grp;
        }

        so.FindProperty("loginGroup").objectReferenceValue = loginPanel.GetComponent<CanvasGroup>();
        so.FindProperty("registerGroup").objectReferenceValue = registerPanel.GetComponent<CanvasGroup>();
        so.ApplyModifiedProperties();

        // Wire button events
        WireButtonOnClick(canvas.transform, "MainMenu/BeginBtn", canvas, "MainMenu", "Play");

        var loginCard = loginPanel.transform.Find("LoginCard");
        if (loginCard != null)
        {
            WireButtonOnClick(loginCard, "SignUpLink", canvas, "MainMenu", "ShowRegister");
            WireButtonOnClick(loginCard, "BackBtn", canvas, "MainMenu", "BackToMenu");
            SetupTabNavigation(loginCard, "SignInBtn");
        }

        var registerCard = registerPanel.transform.Find("RegisterCard");
        if (registerCard != null)
        {
            WireButtonOnClick(registerCard, "SignInLink", canvas, "MainMenu", "ShowLogin");
            WireButtonOnClick(registerCard, "BackBtn", canvas, "MainMenu", "BackToMenu");
            SetupTabNavigation(registerCard, "SignUpBtn");
        }
    }

    static void SetupAuthManagerComponent(GameObject canvas, GameObject loginPanel, GameObject registerPanel)
    {
        var existing = GameObject.Find("AuthManager");
        if (existing != null && existing.GetComponent<AuthManager>() != null)
        {
            // Already exists, wire it
        }
        else
        {
            existing = new GameObject("AuthManager");
            existing.AddComponent<AuthManager>();
        }

        var auth = existing.GetComponent<AuthManager>();
        var so = new SerializedObject(auth);

        var loginCard = loginPanel.transform.Find("LoginCard");
        if (loginCard != null)
        {
            SetRef(so, "userLoginField", loginCard, "UserInput");
            SetRef(so, "passwordLoginField", loginCard, "PasswordInput");
            SetRef(so, "warningLoginText", loginCard, "WarningText");
            SetRef(so, "confirmLoginText", loginCard, "ConfirmText");

            WireButtonOnClick(loginCard, "SignInBtn", existing, "AuthManager", "LoginButton");
        }

        var registerCard = registerPanel.transform.Find("RegisterCard");
        if (registerCard != null)
        {
            // Student fields
            var studentUI = registerCard.Find("StudentUI");
            if (studentUI != null)
            {
                SetRef(so, "firstnameRegisterField", studentUI, "FirstInput");
                SetRef(so, "lastnameRegisterField", studentUI, "LastInput");
                SetRef(so, "usernameRegisterField", studentUI, "UserInput");
                SetRef(so, "passwordRegisterField", studentUI, "PasswordInput");
                SetRef(so, "passwordRegisterVerifyField", studentUI, "VerifyPassInput");
            }

            SetRef(so, "warningRegisterText", registerCard, "WarningText");

            // Teacher fields
            var teacherUI = registerCard.Find("TeacherUI");
            if (teacherUI != null)
            {
                SetRef(so, "TfirstnameRegisterField", teacherUI, "FirstInput");
                SetRef(so, "TlastnameRegisterField", teacherUI, "LastInput");
                SetRef(so, "TusernameRegisterField", teacherUI, "UserInput");
                SetRef(so, "TemailRegisterField", teacherUI, "EmailInput");
                SetRef(so, "TpasswordRegisterField", teacherUI, "PasswordInput");
                SetRef(so, "TpasswordRegisterVerifyField", teacherUI, "VerifyPassInput");
            }

            // Teacher toggle
            var selectBoxes = registerCard.Find("SelectBoxes");
            if (selectBoxes != null)
            {
                var teacherBox = selectBoxes.Find("TeacherBox");
                if (teacherBox != null)
                    so.FindProperty("isTeacherToggle").objectReferenceValue = teacherBox.GetComponent<Toggle>();
            }

            WireButtonOnClick(registerCard, "SignUpBtn", existing, "AuthManager", "RegisterButton");
        }

        so.FindProperty("TwarningRegisterText").objectReferenceValue =
            so.FindProperty("warningRegisterText").objectReferenceValue;

        so.ApplyModifiedProperties();
    }

    static void SetRef(SerializedObject so, string prop, Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child == null) return;

        var p = so.FindProperty(prop);
        if (p == null) return;

        var comp = child.GetComponent<TMP_InputField>();
        if (comp != null) { p.objectReferenceValue = comp; return; }

        var txt = child.GetComponent<TMP_Text>();
        if (txt != null) { p.objectReferenceValue = txt; return; }
    }

    static void WireButtonOnClick(Transform searchRoot, string btnPath, GameObject target, string componentName, string methodName)
    {
        var btnT = searchRoot.Find(btnPath);
        if (btnT == null) return;

        var btn = btnT.GetComponent<Button>();
        if (btn == null)
        {
            btn = btnT.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
        }

        // Clear all existing persistent listeners
        int count = btn.onClick.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, i);

        var comp = target.GetComponent(componentName);
        if (comp == null)
        {
            Debug.LogWarning($"[SetupLoginUI] Component '{componentName}' not found on '{target.name}'");
            return;
        }

        var method = comp.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (method == null)
        {
            Debug.LogWarning($"[SetupLoginUI] Method '{methodName}' not found on '{componentName}'");
            return;
        }

        var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), comp, method) as UnityEngine.Events.UnityAction;
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
    }

    static void SetupTabNavigation(Transform card, string submitBtnName)
    {
        var go = card.gameObject;
        var nav = go.GetComponent<ChangeInput>();
        if (nav == null) nav = go.AddComponent<ChangeInput>();

        var so = new SerializedObject(nav);
        var submitT = card.Find(submitBtnName);
        if (submitT != null)
        {
            var btnProp = so.FindProperty("submitButton");
            if (btnProp != null)
            {
                btnProp.objectReferenceValue = submitT.GetComponent<Button>();
                so.ApplyModifiedProperties();
            }
        }

        var inputFields = go.GetComponentsInChildren<TMPro.TMP_InputField>(true);
        for (int i = 0; i < inputFields.Length; i++)
        {
            var inputNav = inputFields[i].navigation;
            inputNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
            inputNav.selectOnDown = i < inputFields.Length - 1 ? inputFields[i + 1] : inputFields[0];
            inputNav.selectOnUp = i > 0 ? inputFields[i - 1] : inputFields[inputFields.Length - 1];
            inputFields[i].navigation = inputNav;
        }
    }

    static void DestroyChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }
}
