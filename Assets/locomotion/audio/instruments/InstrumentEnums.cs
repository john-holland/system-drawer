using System;

namespace Locomotion.Audio
{
    public enum InstrumentFamily
    {
        Generic,
        Strings,
        Wind,
        FreeReed,
        Percussion,
        Keyboard,
        Resonance,
        /// <summary>DAC, drum machine, digital rack — dry/wet, LFO, PWM, wave shapes.</summary>
        Electronic
    }

    /// <summary>Option keys owned by the Electronic family (also used by DAC / drum-machine machines).</summary>
    public static class ElectronicOptionKeys
    {
        public const string DryWet = "drywet";
        public const string Lfo = "lfo";
        public const string LfoDepth = "lfodepth";
        public const string LfoRate = "lforate";
        public const string Pwm = "pwm";
        public const string PwmWidth = "pwmwidth";
        public const string WaveShape = "waveshape";
        public const string Modulation = "modulation";
        public const string Oscillation = "oscillation";
        public const string Dac = "dac";
        public const string DrumMachine = "drummachine";
        public const string DigitalFx = "digitalfx";

        public static bool IsElectronicOption(string optionKey)
        {
            if (string.IsNullOrEmpty(optionKey)) return false;
            string k = optionKey.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");
            return k is DryWet or Lfo or LfoDepth or LfoRate or Pwm or PwmWidth
                or WaveShape or Modulation or Oscillation or Dac or DrumMachine or DigitalFx
                or "square" or "saw" or "wet" or "dry";
        }
    }

    public enum MusicScaleMode
    {
        Ionian,
        Dorian,
        Phrygian,
        Lydian,
        Mixolydian,
        Aeolian,
        Locrian
    }

    public enum InstrumentWaveShape
    {
        Sine,
        Square,
        Saw,
        Triangle
    }

    public enum InstrumentOpenCloseKind
    {
        BinaryLatch,
        Attenuated
    }
}
