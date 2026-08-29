/// <summary>Lemma keys for GameSession save/load and local server structure.</summary>
public static class GameSessionLemmaPropertyKeys
{
    public const string GameSession = "game-session";
    public const string Saving = "saving";
    public const string Loading = "loading";
    public const string LocalSave = "local-save";
    public const string SaveServerToLocal = "save-server-to-local";
    public const string LocalServer = "local-server";

    public static readonly string[] LemmaPlaceholders =
    {
        "game-session", "saving", "loading", "local-save", "save-server-to-local", "local-server"
    };
}
