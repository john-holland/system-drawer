using System;
using System.IO;
using UnityEngine;

/// <summary>
/// One line per pathing frame that rebuilt, deferred, or saw a flipped transform flag.
/// Written to <c>Logs/pathing-frame-trace.txt</c> and the console while playing.
/// </summary>
public static class PathingFrameTrace
{
    public static bool Enabled = true;

    static StreamWriter _writer;
    static bool _failed;
    static int _flippedHasChanged;

    public static void NoteFlippedHasChanged(string source)
    {
        _flippedHasChanged++;
    }

    public static int TakeFlippedHasChanged()
    {
        int n = _flippedHasChanged;
        _flippedHasChanged = 0;
        return n;
    }

    public static void Write(string line)
    {
        if (!Enabled || !Application.isPlaying || string.IsNullOrEmpty(line))
            return;

        Debug.Log("[PathingFrame] " + line);
        if (_failed)
            return;

        try
        {
            if (_writer == null)
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "pathing-frame-trace.txt");
                _writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    AutoFlush = true
                };
                _writer.WriteLine("# " + DateTime.Now.ToString("o") + " pathing frame trace");
            }

            _writer.WriteLine(line);
        }
        catch (Exception)
        {
            _failed = true;
        }
    }
}
