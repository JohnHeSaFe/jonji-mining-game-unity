using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panelMain;
    public GameObject panelWaiting;
    public GameObject panelJoin;

    [Header("Join Panel")]
    public TMP_InputField ipInputField;

    [Header("Status Text")]
    public TextMeshProUGUI statusText;

    void Start()
    {
        // Wire buttons at runtime — editor-time AddListener calls are not serialized
        WireButton(panelMain,    "Host GameBtn",   OnHostClicked);
        WireButton(panelMain,    "Join GameBtn",   OnJoinClicked);
        WireButton(panelWaiting, "CancelBtn",      OnBackClicked);
        WireButton(panelJoin,    "ConnectBtn",     OnConnectClicked);
        WireButton(panelJoin,    "BackBtn",        OnBackClicked);

        ShowMain();
    }

    void WireButton(GameObject panel, string btnName, UnityEngine.Events.UnityAction action)
    {
        if (panel == null) return;
        var btn = panel.transform.Find(btnName);
        if (btn != null)
            btn.GetComponent<Button>()?.onClick.AddListener(action);
        else
            Debug.LogWarning("[Lobby] Button not found: " + btnName + " in " + panel.name);
    }

    // ── Button callbacks ─────────────────────────────────────────────────────

    public void OnHostClicked()
    {
        EnsureNetworkManager();
        if (GameNetworkManager.Instance.StartServer())
            ShowWaiting("Waiting for player...");
        else
            ShowStatus("Failed to start server. Is port 7777 available?");
    }

    public void OnJoinClicked()
    {
        ShowJoin();
    }

    public void OnConnectClicked()
    {
        string ip = (ipInputField != null && ipInputField.text.Trim().Length > 0)
            ? ipInputField.text.Trim()
            : "127.0.0.1";

        EnsureNetworkManager();
        if (GameNetworkManager.Instance.StartClient(ip))
            ShowWaiting("Connecting to " + ip + "...");
        else
            ShowStatus("Invalid IP address.");
    }

    public void OnBackClicked()
    {
        if (GameNetworkManager.Instance != null)
            GameNetworkManager.Instance.Shutdown();
        ShowMain();
    }

    // ── UI state helpers ─────────────────────────────────────────────────────

    void ShowMain()
    {
        SetActive(panelMain,    true);
        SetActive(panelWaiting, false);
        SetActive(panelJoin,    false);
        ShowStatus("");
    }

    void ShowWaiting(string msg)
    {
        SetActive(panelMain,    false);
        SetActive(panelWaiting, true);
        SetActive(panelJoin,    false);
        ShowStatus(msg);
    }

    void ShowJoin()
    {
        SetActive(panelMain,    false);
        SetActive(panelWaiting, false);
        SetActive(panelJoin,    true);
        ShowStatus("Enter the host's IP address.");
    }

    void ShowStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    void EnsureNetworkManager()
    {
        if (GameNetworkManager.Instance != null) return;
        new GameObject("GameNetworkManager").AddComponent<GameNetworkManager>();
    }
}
