using InvisiblePlayer.Core.Generators;

namespace InvisiblePlayer.Core.Tones
{
    /// <summary>
    /// Tělo tónu = fyzikální model struny (Karplus-Strong), viz CembaloVoice.cs.
    /// Harmonics/PartialAmplitudes se u Cembalo presetů nepoužívají (na rozdíl
    /// od dřívějška) - barvu teď řídí StringBrightness/StringDecaySeconds a
    /// ostrost brnknutí ChiffFilterFreqHz/ChiffFilterQ.
    /// </summary>
    public static class _300_Cembalo_RandallHopkirk
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Number = 300,
            Name = "Cembalo (Randall & Hopkirk)",
            Instrument = InstrumentType.Cembalo,

            StringDecaySeconds = 3.5,  // Rychlejší dozvuk než klavír
            StringBrightness = 0.30,   // Jasnější, "kovovější" tón brnkané struny

            ChiffFilterFreqHz = 3800.0, // Ostré brnknutí brčkem (plectrum)
            ChiffFilterQ = 2.0
        };
    }
}
