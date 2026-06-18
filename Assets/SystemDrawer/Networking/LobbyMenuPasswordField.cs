using UnityEngine;
using UnityEngine.UI;

/// <summary>Optional lobby password input plank for MenuRagdoll spatial menus.</summary>
[AddComponentMenu("System Drawer/Networking/Lobby Menu Password Field")]
public sealed class LobbyMenuPasswordField : MonoBehaviour
{
    public enum FieldMode { Host, Join }

    public FieldMode fieldMode = FieldMode.Join;
    public InputField inputField;
    public MenuRagdoll menuRagdoll;

    void Awake()
    {
        if (menuRagdoll == null)
            menuRagdoll = FindAnyObjectByType<MenuRagdoll>();
        if (inputField == null)
            inputField = GetComponentInChildren<InputField>(true);
        if (inputField != null)
            inputField.onEndEdit.AddListener(OnEndEdit);
    }

    void OnDestroy()
    {
        if (inputField != null)
            inputField.onEndEdit.RemoveListener(OnEndEdit);
    }

    void OnEndEdit(string value)
    {
        if (menuRagdoll == null)
            return;
        switch (fieldMode)
        {
            case FieldMode.Host:
                menuRagdoll.hostLobbyPassword = value;
                break;
            case FieldMode.Join:
                menuRagdoll.joinLobbyPassword = value;
                break;
        }
    }
}
