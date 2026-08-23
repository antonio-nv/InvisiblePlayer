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
            Temperament = Temperament.QuarterCommaMeantone,
            StringDecaySeconds = 9.0,  // Dlouhý ušlechtilý dozvuk
            StringBrightness = 0.82,   // Plný, ne příliš jasný tón  bylo 0.52
            PickPosition = 0.3,           // Měkký úder plstěného kladívka blíž středu struny   bylo 0.15
            ExcitationNoiseAmount = 0.20,  // Trocha texturního šumu k dechu úderu

            // Parametry filtru pro úder kladívka a rozladění strun
            ChiffFilterFreqHz = 450.0, // Měkké plstěné kladívko
            ChiffFilterQ = 1.2,        // Mírná inharmonicita
            ModDepth = 4.0              // Jemné rozladění chóru strun (v CENTECH) pro živost
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
            Temperament = Temperament.QuarterCommaMeantone,
            StringDecaySeconds = 4.5,  // Kratší, "elektrický" dozvuk
            StringBrightness = 0.40,   // Jasnější, ostřejší tón
            PickPosition = 0.10,
            ExcitationNoiseAmount = 0.10,

            ChiffFilterFreqHz = 1200.0,
            ChiffFilterQ = 1.0,
            ModDepth = 1.5
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
            Temperament = Temperament.QuarterCommaMeantone,
            StringDecaySeconds = 6.0,
            StringBrightness = 0.35,   // Nízké tlumení = drsnější, "rozstřesenější" tón
            PickPosition = 0.06,           // Blízko kraje - drsný, "cinkavý" opotřebovaný tón
            ExcitationNoiseAmount = 0.35,  // Hodně texturního šumu - opotřebovaná plsť/struny

            ChiffFilterFreqHz = 2200.0, // Opotřebovaná tvrdá plsť až na dřevo
            ChiffFilterQ = 4.5,         // Silná inharmonicita rozladěných strun
            ModDepth = 30.0             // Extrémní rozladění chóru strun (v CENTECH) pro rozstřesený saloonový tón
        };
    }
}
