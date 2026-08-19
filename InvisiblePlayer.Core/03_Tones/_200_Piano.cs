using InvisiblePlayer.Core.Generators;

namespace InvisiblePlayer.Core.Tones
{
    /// <summary>
    /// Ušlechtilé koncertní křídlo Petrof.
    /// Ušlechtilý plstěný úder, dlouhý dozvuk basů a jemné zázněry chóru strun.
    /// Tělo tónu = fyzikální model struny (Karplus-Strong), viz PianoVoice.cs.
    /// </summary>
    public static class _200_Piano_Petrof
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Piano Petrof",
            Number = 200,
            Instrument = InstrumentType.Piano,

            StringDecaySeconds = 9.0,  // Dlouhý ušlechtilý dozvuk
            StringBrightness = 0.52,   // Plný, ne příliš jasný tón

            // Parametry filtru pro úder kladívka a rozladění strun
            ChiffFilterFreqHz = 450.0, // Měkké plstěné kladívko
            ChiffFilterQ = 1.2,        // Mírná inharmonicita
            ModDepth = 0.25            // Jemné rozladění chóru strun (Hz) pro živost
        };
    }

    /// <summary>
    /// Původní stávající elektronický/syntetický tón (zachován jako #201).
    /// </summary>
    public static class _201_Piano_Eletricke
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Piano Elektrické",
            Number = 201,
            Instrument = InstrumentType.Piano,

            StringDecaySeconds = 4.5,  // Kratší, "elektrický" dozvuk
            StringBrightness = 0.40,   // Jasnější, ostřejší tón

            ChiffFilterFreqHz = 1200.0,
            ChiffFilterQ = 1.0,
            ModDepth = 0.05
        };
    }

    /// <summary>
    /// Rozladěné hospodské saloonové piano (Honky-Tonk / Limonádový Joe).
    /// Ostrý úder, silná inharmonicita a silně rozladěný chór strun.
    /// </summary>
    public static class _202_Piano_LimonadovyJoe
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Piano Limonádový Joe",
            Number = 202,
            Instrument = InstrumentType.Piano,

            StringDecaySeconds = 6.0,
            StringBrightness = 0.35,   // Nízké tlumení = drsnější, "rozstřesenější" tón

            ChiffFilterFreqHz = 2200.0, // Opotřebovaná tvrdá plsť až na dřevo
            ChiffFilterQ = 4.5,         // Silná inharmonicita rozladěných strun
            ModDepth = 3.20             // Extrémní rozladění chóru strun pro rozstřesený saloonový tón
        };
    }
}
