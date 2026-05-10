using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class LobbyUIBuilder
{
    [MenuItem("Tools/Build Lobby UI")]
    public static void BuildLobbyUI()
    {
        // ── GameNetworkManager ────────────────────────────────────────────────
        var nmGO = new GameObject("GameNetworkManager");
        nmGO.AddComponent<GameNetworkManager>();

        // ── Lobby Canvas ──────────────────────────────────────────────────────
        var canvasGO = new GameObject("LobbyCanvas");
        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        Transform root = canvasGO.transform;

        Color dark = new Color(0.08f, 0.08f, 0.12f, 0.94f);

        // ── PanelMain ─────────────────────────────────────────────────────────
        GameObject panelMain = Panel("PanelMain", root, dark, true);
        Label("JONJI MINING", panelMain.transform, new Vector2(0, 200), 52);
        Button hostBtn = Btn("Host Game", panelMain.transform, new Vector2(0, 40), new Vector2(420, 80));
        Button joinBtn = Btn("Join Game", panelMain.transform, new Vector2(0, -60), new Vector2(420, 80));

        // ── PanelWaiting ──────────────────────────────────────────────────────
        GameObject panelWaiting = Panel("PanelWaiting", root, dark, false);
        Label("Waiting for player...", panelWaiting.transform, new Vector2(0, 40), 40);
        Button cancelBtn = Btn("Cancel", panelWaiting.transform, new Vector2(0, -80), new Vector2(260, 60));

        // ── PanelJoin ─────────────────────────────────────────────────────────
        GameObject panelJoin = Panel("PanelJoin", root, dark, false);
        Label("Enter Host IP Address", panelJoin.transform, new Vector2(0, 140), 36);
        TMP_InputField ipField = InputField(panelJoin.transform, new Vector2(0, 40));
        Button connectBtn = Btn("Connect", panelJoin.transform, new Vector2(0, -60), new Vector2(320, 70));
        Button backBtn    = Btn("Back",    panelJoin.transform, new Vector2(0, -150), new Vector2(200, 55));
        backBtn.image.color = new Color(0.35f, 0.35f, 0.4f);

        // ── Status text ───────────────────────────────────────────────────────
        TextMeshProUGUI statusTMP = Label("", root, new Vector2(0, -480), 24);
        statusTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 50);

        // ── LobbyController ───────────────────────────────────────────────────
        var lobbyGO = new GameObject("LobbyManager");
        var lobby = lobbyGO.AddComponent<LobbyController>();
        lobby.panelMain    = panelMain;
        lobby.panelWaiting = panelWaiting;
        lobby.panelJoin    = panelJoin;
        lobby.ipInputField = ipField;
        lobby.statusText   = statusTMP;

        hostBtn.onClick.AddListener(lobby.OnHostClicked);
        joinBtn.onClick.AddListener(lobby.OnJoinClicked);
        connectBtn.onClick.AddListener(lobby.OnConnectClicked);
        cancelBtn.onClick.AddListener(lobby.OnBackClicked);
        backBtn.onClick.AddListener(lobby.OnBackClicked);

        // ── Save ──────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Debug.Log("[LobbyUIBuilder] Done.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // All helpers: create RectTransform BEFORE SetParent to avoid Unity warnings
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject Panel(string name, Transform parent, Color bg, bool active)
    {
        // new GameObject with RectTransform in ctor → RT exists before SetParent
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.GetComponent<Image>().color = bg;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.SetActive(active);
        return go;
    }

    static Button Btn(string label, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.GetComponent<Image>().color = new Color(0.18f, 0.55f, 1f);
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var lbl = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = lbl.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 28;
        tmp.color = Color.white;

        return go.GetComponent<Button>();
    }

    static TextMeshProUGUI Label(string text, Transform parent, Vector2 pos, float fontSize)
    {
        var go = new GameObject((text.Length > 0 ? text : "Status") + "_lbl",
                                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700, 70);
        rt.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        return tmp;
    }

    static TMP_InputField InputField(Transform parent, Vector2 pos)
    {
        var fieldGO = new GameObject("IPField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fieldGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        fieldGO.transform.SetParent(parent, false);
        var frt = fieldGO.GetComponent<RectTransform>();
        frt.sizeDelta = new Vector2(520, 72);
        frt.anchoredPosition = pos;

        var bgGO = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.22f);
        bgGO.transform.SetParent(fieldGO.transform, false);
        bgGO.transform.SetAsFirstSibling();
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var taGO = new GameObject("TextArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        taGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        taGO.transform.SetParent(fieldGO.transform, false);
        taGO.AddComponent<RectMask2D>();
        var taRT = taGO.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 6); taRT.offsetMax = new Vector2(-10, -6);

        var phGO = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(taGO.transform, false);
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var phTMP = phGO.GetComponent<TextMeshProUGUI>();
        phTMP.text = "e.g. 192.168.1.10";
        phTMP.color = new Color(0.5f, 0.5f, 0.5f);
        phTMP.fontSize = 26;

        var itGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        itGO.transform.SetParent(taGO.transform, false);
        var itRT = itGO.GetComponent<RectTransform>();
        itRT.anchorMin = Vector2.zero; itRT.anchorMax = Vector2.one;
        itRT.offsetMin = itRT.offsetMax = Vector2.zero;
        var itTMP = itGO.GetComponent<TextMeshProUGUI>();
        itTMP.fontSize = 28; itTMP.color = Color.white;

        var input = fieldGO.AddComponent<TMP_InputField>();
        input.textViewport = taRT;
        input.textComponent = itTMP;
        input.placeholder = phTMP;
        return input;
    }
}
