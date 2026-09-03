using PS150.Core.Generators;

namespace PS150.Core.Tones
{
    /// <summary>
    /// Tělo tónu = fyzikální model struny (Karplus-Strong), viz CembaloVoice.cs.
    /// PartialRatios/PartialAmplitudes se u Cembalo presetů nepoužívají -
    /// barvu teď řídí StringBrightness/StringDecaySeconds a ostrost brnknutí
    /// ChiffFilterFreqHz/ChiffFilterQ.
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
            PickPosition = 0.09,           // Blízko kraje - typické pro brnknutí brčkem
            ExcitationNoiseAmount = 0.10,  // Brnknutí je "čistší" než úder plsti

            ChiffFilterFreqHz = 3800.0, // Ostré brnknutí brčkem (plectrum)
            ChiffFilterQ = 2.0
        };
    }
}
