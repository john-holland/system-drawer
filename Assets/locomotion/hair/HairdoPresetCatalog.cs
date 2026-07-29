using System;

/// <summary>Named basic haircuts for the hairdo designer blend list.</summary>
public enum HairdoCutKind
{
    Buzz = 0,
    Crew = 1,
    Bob = 2,
    Shoulder = 3,
    Long = 4,
    Mullet = 5,
    SidePart = 6,
    CenterPart = 7,
    Undercut = 8,
    Waves = 9,
    Curls = 10,
    Ringlets = 11
}

public static class HairdoPresetCatalog
{
    public static readonly HairdoCutKind[] All =
    {
        HairdoCutKind.Buzz,
        HairdoCutKind.Crew,
        HairdoCutKind.Bob,
        HairdoCutKind.Shoulder,
        HairdoCutKind.Long,
        HairdoCutKind.Mullet,
        HairdoCutKind.SidePart,
        HairdoCutKind.CenterPart,
        HairdoCutKind.Undercut,
        HairdoCutKind.Waves,
        HairdoCutKind.Curls,
        HairdoCutKind.Ringlets
    };

    public static string DisplayName(HairdoCutKind kind) => kind switch
    {
        HairdoCutKind.Buzz => "Buzz",
        HairdoCutKind.Crew => "Crew",
        HairdoCutKind.Bob => "Bob",
        HairdoCutKind.Shoulder => "Shoulder",
        HairdoCutKind.Long => "Long",
        HairdoCutKind.Mullet => "Mullet",
        HairdoCutKind.SidePart => "Side Part",
        HairdoCutKind.CenterPart => "Center Part",
        HairdoCutKind.Undercut => "Undercut",
        HairdoCutKind.Waves => "Waves",
        HairdoCutKind.Curls => "Curls",
        HairdoCutKind.Ringlets => "Ringlets",
        _ => kind.ToString()
    };

    public static HairdoParams Get(HairdoCutKind kind)
    {
        return kind switch
        {
            HairdoCutKind.Buzz => Buzz(),
            HairdoCutKind.Crew => Crew(),
            HairdoCutKind.Bob => Bob(),
            HairdoCutKind.Shoulder => Shoulder(),
            HairdoCutKind.Long => Long(),
            HairdoCutKind.Mullet => Mullet(),
            HairdoCutKind.SidePart => SidePart(),
            HairdoCutKind.CenterPart => CenterPart(),
            HairdoCutKind.Undercut => Undercut(),
            HairdoCutKind.Waves => Waves(),
            HairdoCutKind.Curls => Curls(),
            HairdoCutKind.Ringlets => Ringlets(),
            _ => Crew()
        };
    }

    static HairdoParams Buzz() => new HairdoParams
    {
        maxStrandLengthM = 0.08f,
        peakHeightM = 0.06f,
        gaussianSigma = 0.35f,
        plumeTipHold = 0.9f,
        gaussianFluxGain = 0.4f,
        hairlineFront = 1f,
        hairlineSide = 1f,
        hairlineBack = 1f,
        hairlineCrown = 1f,
        fringeHeight = 0f,
        flare = 1.02f,
        partMode = HairdoPartMode.None,
        curlAmount = 0f
    };

    static HairdoParams Crew() => new HairdoParams
    {
        maxStrandLengthM = 0.12f,
        peakHeightM = 0.1f,
        gaussianSigma = 0.4f,
        plumeTipHold = 0.75f,
        gaussianFluxGain = 0.6f,
        hairlineFront = 1.05f,
        hairlineSide = 0.95f,
        hairlineBack = 0.9f,
        hairlineCrown = 1.05f,
        fringeHeight = 0.1f,
        flare = 1.08f,
        partMode = HairdoPartMode.None,
        curlAmount = 0f
    };

    static HairdoParams Bob() => new HairdoParams
    {
        maxStrandLengthM = 0.22f,
        peakHeightM = 0.18f,
        gaussianSigma = 0.45f,
        plumeTipHold = 0.6f,
        gaussianFluxGain = 0.9f,
        hairlineFront = 1f,
        hairlineSide = 1.05f,
        hairlineBack = 1.05f,
        hairlineCrown = 1f,
        fringeHeight = 0.2f,
        flare = 1.12f,
        partMode = HairdoPartMode.None,
        partStrength = 0.4f,
        curlAmount = 0.05f,
        curlFrequency = 2f,
        curlTightness = 0.25f
    };

    static HairdoParams Shoulder() => new HairdoParams
    {
        maxStrandLengthM = 0.35f,
        peakHeightM = 0.28f,
        gaussianSigma = 0.5f,
        plumeTipHold = 0.4f,
        gaussianFluxGain = 1.1f,
        hairlineFront = 1f,
        hairlineSide = 1.05f,
        hairlineBack = 1.1f,
        hairlineCrown = 0.95f,
        fringeHeight = 0.15f,
        flare = 1.25f,
        partMode = HairdoPartMode.None,
        curlAmount = 0.1f,
        curlFrequency = 2.5f,
        curlTightness = 0.3f
    };

    static HairdoParams Long() => new HairdoParams
    {
        maxStrandLengthM = 0.5f,
        peakHeightM = 0.4f,
        gaussianSigma = 0.55f,
        plumeTipHold = 0.2f,
        gaussianFluxGain = 1.4f,
        hairlineFront = 0.95f,
        hairlineSide = 1f,
        hairlineBack = 1.1f,
        hairlineCrown = 0.9f,
        fringeHeight = 0.25f,
        flare = 1.35f,
        partMode = HairdoPartMode.None,
        curlAmount = 0.08f,
        curlFrequency = 2f,
        curlTightness = 0.2f
    };

    static HairdoParams Mullet() => new HairdoParams
    {
        maxStrandLengthM = 0.4f,
        peakHeightM = 0.32f,
        gaussianSigma = 0.48f,
        plumeTipHold = 0.35f,
        gaussianFluxGain = 1.2f,
        hairlineFront = 0.75f,
        hairlineSide = 0.9f,
        hairlineBack = 1.35f,
        hairlineCrown = 0.95f,
        fringeHeight = 0.05f,
        flare = 1.3f,
        partMode = HairdoPartMode.None,
        curlAmount = 0.15f,
        curlFrequency = 2.5f,
        curlTightness = 0.35f
    };

    static HairdoParams SidePart() => new HairdoParams
    {
        maxStrandLengthM = 0.2f,
        peakHeightM = 0.16f,
        gaussianSigma = 0.42f,
        plumeTipHold = 0.65f,
        gaussianFluxGain = 0.85f,
        hairlineFront = 1f,
        hairlineSide = 0.85f,
        hairlineBack = 1f,
        hairlineCrown = 1.05f,
        fringeHeight = 0.15f,
        sideTiltDeg = 8f,
        flare = 1.1f,
        partMode = HairdoPartMode.Left,
        partWidthM = 0.012f,
        partStrength = 1f,
        curlAmount = 0f
    };

    static HairdoParams CenterPart() => new HairdoParams
    {
        maxStrandLengthM = 0.28f,
        peakHeightM = 0.22f,
        gaussianSigma = 0.46f,
        plumeTipHold = 0.5f,
        gaussianFluxGain = 1f,
        hairlineFront = 1f,
        hairlineSide = 1f,
        hairlineBack = 1.05f,
        hairlineCrown = 1f,
        fringeHeight = 0.2f,
        flare = 1.15f,
        partMode = HairdoPartMode.Center,
        partWidthM = 0.01f,
        partStrength = 1f,
        curlAmount = 0.05f,
        curlFrequency = 2f,
        curlTightness = 0.25f
    };

    static HairdoParams Undercut() => new HairdoParams
    {
        maxStrandLengthM = 0.18f,
        peakHeightM = 0.2f,
        gaussianSigma = 0.38f,
        plumeTipHold = 0.7f,
        gaussianFluxGain = 0.7f,
        hairlineFront = 1.1f,
        hairlineSide = 0.45f,
        hairlineBack = 0.5f,
        hairlineCrown = 1.2f,
        fringeHeight = 0.1f,
        flare = 1.05f,
        partMode = HairdoPartMode.None,
        curlAmount = 0f
    };

    static HairdoParams Waves() => new HairdoParams
    {
        maxStrandLengthM = 0.32f,
        peakHeightM = 0.26f,
        gaussianSigma = 0.5f,
        plumeTipHold = 0.45f,
        gaussianFluxGain = 1f,
        hairlineFront = 1f,
        hairlineSide = 1.05f,
        hairlineBack = 1.1f,
        hairlineCrown = 0.95f,
        fringeHeight = 0.18f,
        flare = 1.2f,
        partMode = HairdoPartMode.None,
        curlAmount = 0.45f,
        curlFrequency = 2f,
        curlTightness = 0.25f
    };

    static HairdoParams Curls() => new HairdoParams
    {
        maxStrandLengthM = 0.28f,
        peakHeightM = 0.24f,
        gaussianSigma = 0.48f,
        plumeTipHold = 0.55f,
        gaussianFluxGain = 0.9f,
        hairlineFront = 1.02f,
        hairlineSide = 1.05f,
        hairlineBack = 1.08f,
        hairlineCrown = 1f,
        fringeHeight = 0.15f,
        flare = 1.18f,
        partMode = HairdoPartMode.None,
        curlAmount = 0.8f,
        curlFrequency = 4f,
        curlTightness = 0.55f
    };

    static HairdoParams Ringlets() => new HairdoParams
    {
        maxStrandLengthM = 0.22f,
        peakHeightM = 0.2f,
        gaussianSigma = 0.42f,
        plumeTipHold = 0.65f,
        gaussianFluxGain = 0.75f,
        hairlineFront = 1.05f,
        hairlineSide = 1f,
        hairlineBack = 1.05f,
        hairlineCrown = 1.05f,
        fringeHeight = 0.12f,
        flare = 1.1f,
        partMode = HairdoPartMode.None,
        curlAmount = 0.95f,
        curlFrequency = 6.5f,
        curlTightness = 0.9f
    };
}
