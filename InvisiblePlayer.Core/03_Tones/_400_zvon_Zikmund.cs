using InvisiblePlayer.Core.Generators;
using InvisiblePlayer.Core.ToneEngine;

namespace InvisiblePlayer.Core.Tones
{
    
    public static class _400_Zvon_Zikmund
    {
        // Stejně jako u Cembalo presetu - zvuk je "zadrátovaný" v BellVoice,
        // preset zatím jen vybírá zvukový engine.
        public static VoicePreset Preset => new VoicePreset
        {
            Name = "Zikmund",
            Number = 400,
            Instrument = InstrumentType.Bell,
            Harmonics = new (double Ratio, double Amp)[]
            {
                (0.501, 0.35), (1.000, 0.55), (1.502, 0.4), (2.514, 0.50)
            },

            // Vlastní partiály - klidně i jiný počet než výchozích 7
            PartialRatios = new[] { 0.40, 1.30, 1.19, 1.55, 2.02, 2.60 },
            PartialAmplitudes = new[] { 0.40, 0.60, 0.35, 0.30, 0.55, 0.15 },
            PartialDecayRates = new[] { 0.05, 0.030, 0.60, 0.75, 0.40, 1.20 },

            ChiffFilterFreqHz = 800.0,
            ChiffFilterQ = 1.2,
            ModType = ModulationType.FM,
            ModSpeedHz = 18.5,
            ModDepth = 0.02
        };
    }




}
