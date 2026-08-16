using InvisiblePlayer.Core.Generators;

namespace InvisiblePlayer.Core.Tones
{
    /// <summary>
    /// Ušlechtilé koncertní křídlo Petrof.
    /// Ušlechtilý plstěný úder, dlouhý dozvuk basů a jemné zázněry chóru strun.
    /// </summary>
    public static class _200_Piano_Petrof
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Piano Petrof",
            Number = 200,
            Instrument = InstrumentType.Piano,

            // Amplitudy prvních 10 harmonických
            PartialAmplitudes = new double[] { 1.00, 0.75, 0.50, 0.35, 0.22, 0.14, 0.08, 0.05, 0.03, 0.01 },

            // Rychlost exponenciálního zhasínání v dB/s pro jednotlivé alikvóty
            PartialDecayRates = new double[] { 0.45, 0.90, 1.50, 2.40, 3.60, 5.00, 6.80, 8.50, 11.00, 14.00 },

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

            PartialAmplitudes = new double[] { 1.00, 0.80, 0.60, 0.40, 0.20, 0.10, 0.05, 0.02, 0.01, 0.00 },
            PartialDecayRates = new double[] { 1.20, 1.80, 2.50, 3.50, 5.00, 7.00, 9.00, 12.00, 15.00, 20.00 },

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

            // Bohaté spektrum vytažených vyšších harmonických (plechový charakter)
            PartialAmplitudes = new double[] { 1.00, 0.90, 0.80, 0.70, 0.60, 0.50, 0.40, 0.30, 0.20, 0.10 },

            // Pomalé zhasínání výšek - struny rezonují a "řezají"
            PartialDecayRates = new double[] { 0.70, 0.95, 1.20, 1.60, 2.10, 2.80, 3.60, 4.80, 6.00, 8.00 },

            ChiffFilterFreqHz = 2200.0, // Opotřebovaná tvrdá plsť až na dřevo
            ChiffFilterQ = 4.5,         // Silná inharmonicita rozladěných strun
            ModDepth = 3.20             // Extrémní rozladění chóru strun pro rozstřesený saloonový tón
        };
    }
}