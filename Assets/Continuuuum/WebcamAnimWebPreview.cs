using UnityEngine;

/// <summary>WebGL / library preview: reads docId from URL and shows subsection markers.</summary>
[AddComponentMenu("Continuuuum/Webcam Anim Web Preview")]
public sealed class WebcamAnimWebPreview : MonoBehaviour
{
    public string libraryDocId;
    public string subsectionId;
    public double startMs;
    public double endMs;
    public string modelSpec;

    void Start()
    {
        ParseAbsoluteUrl(Application.absoluteURL);
    }

    public void ParseAbsoluteUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;
        int q = url.IndexOf('?');
        if (q < 0)
            return;
        var query = url.Substring(q + 1);
        foreach (var part in query.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0)
                continue;
            string key = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(part.Substring(0, eq));
            string val = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(part.Substring(eq + 1));
            switch (key)
            {
                case "docId":
                case "libraryDocId":
                    libraryDocId = val;
                    break;
                case "subsection":
                    subsectionId = val;
                    break;
                case "model_spec":
                case "modelSpec":
                    modelSpec = val;
                    break;
                case "startMs":
                    double.TryParse(val, out startMs);
                    break;
                case "endMs":
                    double.TryParse(val, out endMs);
                    break;
            }
        }
    }

    public string WebGlPreviewUrl(string editorBase, string apiBase)
    {
        string b = (editorBase ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(b))
            b = "/continuuuum_editor";
        return $"{b}/index.html?docId={UnityEngine.Networking.UnityWebRequest.EscapeURL(libraryDocId ?? "")}" +
               $"&apiBase={UnityEngine.Networking.UnityWebRequest.EscapeURL(apiBase ?? "")}" +
               $"&subsection={UnityEngine.Networking.UnityWebRequest.EscapeURL(subsectionId ?? "")}" +
               $"&startMs={startMs}&endMs={endMs}";
    }
}
