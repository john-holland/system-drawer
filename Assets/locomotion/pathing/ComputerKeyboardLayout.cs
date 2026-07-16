using System;
using System.Collections.Generic;

public enum ComputerKeySection
{
    Main,
    Function,
    Nav,
    Numpad,
    Aux,
    Volume
}

public enum ComputerKeyId
{
    Escape, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    PrintScreen, ScrollLock, Pause,
    Aux1, Aux2, Aux3, Aux4, Aux5, Aux6, Aux7, Aux8, Aux9, Aux10, Aux11, Aux12,
    VolumeKnob,
    Grave, D1, D2, D3, D4, D5, D6, D7, D8, D9, D0, Minus, Equals, Backspace,
    Insert, Home, PageUp,
    NumLock, NumpadDivide, NumpadMultiply, NumpadSubtract,
    Tab, Q, W, E, R, T, Y, U, I, O, P, BracketOpen, BracketClose, Backslash,
    Delete, End, PageDown,
    Numpad7, Numpad8, Numpad9, NumpadAdd,
    CapsLock, A, S, D, F, G, H, J, K, L, Semicolon, Quote, Return,
    Numpad4, Numpad5, Numpad6,
    LeftShift, Z, X, C, V, B, N, M, Comma, Period, Slash, RightShift, Up,
    Numpad1, Numpad2, Numpad3, NumpadEnter,
    LeftControl, LeftCommand, LeftAlt, Space, RightAlt, Fn, Option, RightControl,
    Left, Down, Right,
    Numpad0, NumpadDecimal
}

[Serializable]
public sealed class ComputerKeyLayoutEntry
{
    public ComputerKeyId id;
    public ComputerKeySection section;
    public int row;
    public float unitWidth = 1f;
    public float unitHeight = 1f;
    public string legend = "";
    public char unicode;
    public bool isKnob;
}

/// <summary>Sectional keyboard layout per plan rows/bands.</summary>
public static class ComputerKeyboardLayout
{
    public static List<ComputerKeyLayoutEntry> BuildDefault(int auxCount = 3)
    {
        var list = new List<ComputerKeyLayoutEntry>(120);
        // Row 0: Esc | F1-4 | F5-8 | F9-12 | Print Scroll Pause | Aux | Volume
        Add(list, ComputerKeyId.Escape, ComputerKeySection.Function, 0, "Esc", '\0');
        for (int i = 0; i < 12; i++)
            Add(list, (ComputerKeyId)((int)ComputerKeyId.F1 + i), ComputerKeySection.Function, 0, "F" + (i + 1), '\0');
        Add(list, ComputerKeyId.PrintScreen, ComputerKeySection.Nav, 0, "PrtSc", '\0');
        Add(list, ComputerKeyId.ScrollLock, ComputerKeySection.Nav, 0, "ScrLk", '\0');
        Add(list, ComputerKeyId.Pause, ComputerKeySection.Nav, 0, "Pause", '\0');
        int n = MathfClampAux(auxCount);
        for (int i = 0; i < n; i++)
            Add(list, (ComputerKeyId)((int)ComputerKeyId.Aux1 + i), ComputerKeySection.Aux, 0, "Aux" + (i + 1), '\0');
        list.Add(new ComputerKeyLayoutEntry
        {
            id = ComputerKeyId.VolumeKnob,
            section = ComputerKeySection.Volume,
            row = 0,
            unitWidth = 1.2f,
            legend = "Vol",
            isKnob = true
        });

        // Row 1 number
        Add(list, ComputerKeyId.Grave, ComputerKeySection.Main, 1, "`", '`');
        string nums = "1234567890";
        for (int i = 0; i < nums.Length; i++)
            Add(list, (ComputerKeyId)((int)ComputerKeyId.D1 + i), ComputerKeySection.Main, 1, nums[i].ToString(), nums[i]);
        Add(list, ComputerKeyId.Minus, ComputerKeySection.Main, 1, "-", '-');
        Add(list, ComputerKeyId.Equals, ComputerKeySection.Main, 1, "=", '=');
        Add(list, ComputerKeyId.Backspace, ComputerKeySection.Main, 1, "Bksp", '\b', 2f);
        Add(list, ComputerKeyId.Insert, ComputerKeySection.Nav, 1, "Ins", '\0');
        Add(list, ComputerKeyId.Home, ComputerKeySection.Nav, 1, "Home", '\0');
        Add(list, ComputerKeyId.PageUp, ComputerKeySection.Nav, 1, "PgUp", '\0');
        Add(list, ComputerKeyId.NumLock, ComputerKeySection.Numpad, 1, "Num", '\0');
        Add(list, ComputerKeyId.NumpadDivide, ComputerKeySection.Numpad, 1, "/", '/');
        Add(list, ComputerKeyId.NumpadMultiply, ComputerKeySection.Numpad, 1, "*", '*');
        Add(list, ComputerKeyId.NumpadSubtract, ComputerKeySection.Numpad, 1, "-", '-');

        // Row 2 QWERTY
        Add(list, ComputerKeyId.Tab, ComputerKeySection.Main, 2, "Tab", '\t', 1.5f);
        foreach (var c in "QWERTYUIOP")
            Add(list, (ComputerKeyId)Enum.Parse(typeof(ComputerKeyId), c.ToString()), ComputerKeySection.Main, 2, c.ToString(), char.ToLowerInvariant(c));
        Add(list, ComputerKeyId.BracketOpen, ComputerKeySection.Main, 2, "[", '[');
        Add(list, ComputerKeyId.BracketClose, ComputerKeySection.Main, 2, "]", ']');
        Add(list, ComputerKeyId.Backslash, ComputerKeySection.Main, 2, "\\", '\\', 1.5f);
        Add(list, ComputerKeyId.Delete, ComputerKeySection.Nav, 2, "Del", '\0');
        Add(list, ComputerKeyId.End, ComputerKeySection.Nav, 2, "End", '\0');
        Add(list, ComputerKeyId.PageDown, ComputerKeySection.Nav, 2, "PgDn", '\0');
        Add(list, ComputerKeyId.Numpad7, ComputerKeySection.Numpad, 2, "7", '7');
        Add(list, ComputerKeyId.Numpad8, ComputerKeySection.Numpad, 2, "8", '8');
        Add(list, ComputerKeyId.Numpad9, ComputerKeySection.Numpad, 2, "9", '9');
        Add(list, ComputerKeyId.NumpadAdd, ComputerKeySection.Numpad, 2, "+", '+', 1f, 2f);

        // Row 3 Caps
        Add(list, ComputerKeyId.CapsLock, ComputerKeySection.Main, 3, "Caps", '\0', 1.75f);
        foreach (var c in "ASDFGHJKL")
            Add(list, (ComputerKeyId)Enum.Parse(typeof(ComputerKeyId), c.ToString()), ComputerKeySection.Main, 3, c.ToString(), char.ToLowerInvariant(c));
        Add(list, ComputerKeyId.Semicolon, ComputerKeySection.Main, 3, ";", ';');
        Add(list, ComputerKeyId.Quote, ComputerKeySection.Main, 3, "'", '\'');
        Add(list, ComputerKeyId.Return, ComputerKeySection.Main, 3, "Enter", '\n', 2.25f);
        Add(list, ComputerKeyId.Numpad4, ComputerKeySection.Numpad, 3, "4", '4');
        Add(list, ComputerKeyId.Numpad5, ComputerKeySection.Numpad, 3, "5", '5');
        Add(list, ComputerKeyId.Numpad6, ComputerKeySection.Numpad, 3, "6", '6');

        // Row 4 Shift
        Add(list, ComputerKeyId.LeftShift, ComputerKeySection.Main, 4, "Shift", '\0', 2.25f);
        foreach (var c in "ZXCVBNM")
            Add(list, (ComputerKeyId)Enum.Parse(typeof(ComputerKeyId), c.ToString()), ComputerKeySection.Main, 4, c.ToString(), char.ToLowerInvariant(c));
        Add(list, ComputerKeyId.Comma, ComputerKeySection.Main, 4, ",", ',');
        Add(list, ComputerKeyId.Period, ComputerKeySection.Main, 4, ".", '.');
        Add(list, ComputerKeyId.Slash, ComputerKeySection.Main, 4, "/", '/');
        Add(list, ComputerKeyId.RightShift, ComputerKeySection.Main, 4, "Shift", '\0', 2.25f);
        Add(list, ComputerKeyId.Up, ComputerKeySection.Nav, 4, "Up", '\0');
        Add(list, ComputerKeyId.Numpad1, ComputerKeySection.Numpad, 4, "1", '1');
        Add(list, ComputerKeyId.Numpad2, ComputerKeySection.Numpad, 4, "2", '2');
        Add(list, ComputerKeyId.Numpad3, ComputerKeySection.Numpad, 4, "3", '3');
        Add(list, ComputerKeyId.NumpadEnter, ComputerKeySection.Numpad, 4, "Ent", '\n', 1f, 2f);

        // Row 5 bottom
        Add(list, ComputerKeyId.LeftControl, ComputerKeySection.Main, 5, "Ctrl", '\0', 1.25f);
        Add(list, ComputerKeyId.LeftCommand, ComputerKeySection.Main, 5, "Cmd", '\0', 1.25f);
        Add(list, ComputerKeyId.LeftAlt, ComputerKeySection.Main, 5, "Alt", '\0', 1.25f);
        Add(list, ComputerKeyId.Space, ComputerKeySection.Main, 5, "Space", ' ', 6.25f);
        Add(list, ComputerKeyId.RightAlt, ComputerKeySection.Main, 5, "Alt", '\0', 1.25f);
        Add(list, ComputerKeyId.Fn, ComputerKeySection.Aux, 5, "Fn", '\0', 1.25f);
        Add(list, ComputerKeyId.Option, ComputerKeySection.Main, 5, "Opt", '\0', 1.25f);
        Add(list, ComputerKeyId.RightControl, ComputerKeySection.Main, 5, "Ctrl", '\0', 1.25f);
        Add(list, ComputerKeyId.Left, ComputerKeySection.Nav, 5, "Left", '\0');
        Add(list, ComputerKeyId.Down, ComputerKeySection.Nav, 5, "Down", '\0');
        Add(list, ComputerKeyId.Right, ComputerKeySection.Nav, 5, "Right", '\0');
        Add(list, ComputerKeyId.Numpad0, ComputerKeySection.Numpad, 5, "0", '0', 2f);
        Add(list, ComputerKeyId.NumpadDecimal, ComputerKeySection.Numpad, 5, ".", '.');

        return list;
    }

    static int MathfClampAux(int auxCount) => System.Math.Clamp(auxCount, 0, 12);

    static void Add(List<ComputerKeyLayoutEntry> list, ComputerKeyId id, ComputerKeySection section, int row, string legend, char unicode, float w = 1f, float h = 1f)
    {
        list.Add(new ComputerKeyLayoutEntry
        {
            id = id,
            section = section,
            row = row,
            unitWidth = w,
            unitHeight = h,
            legend = legend,
            unicode = unicode
        });
    }
}
