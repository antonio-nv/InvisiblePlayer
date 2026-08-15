using InvisiblePlayer.Core.Generators;

namespace InvisiblePlayer.Core.Tones
{
    public static class _001_Bombard16Preset
    {
        public static VoicePreset Preset => new VoicePreset
        {
            // TADY JE TA ZMĚNA: Přidat přesný typ pole
            Name = "Aeolus",
            Number = 85,
            Harmonics = new (double Ratio, double Amp)[]
            {
                (1.0, 1.0), (2.0, 0.75), (3.0, 0.60), (4.0, 0.40), (5.0, 0.25)
            },
            ChiffFilterFreqHz = 800.0,
            ChiffFilterQ = 1.2,
            ModType = ModulationType.AM,
            ModSpeedHz = 5.5,
            ModDepth = 0.08
        };
    }
    public static class _085_Aeolus
    {
        // Stejně jako u Cembalo presetu - zvuk je "zadrátovaný" v BellVoice,
        // preset zatím jen vybírá zvukový engine.
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Aeolus",
            Number = 85,
            Instrument = InstrumentType.Bell,
            Harmonics = new (double Ratio, double Amp)[]
            {
                (0.501, 1.0), (2.0, 0.75), (3.0, 0.60), (4.0, 0.40), (5.0, 0.25)
            },
            ChiffFilterFreqHz = 800.0,
            ChiffFilterQ = 1.2,
            ModType = ModulationType.FM,
            ModSpeedHz = 5.5,
            ModDepth = 0.08
        };
    }

}