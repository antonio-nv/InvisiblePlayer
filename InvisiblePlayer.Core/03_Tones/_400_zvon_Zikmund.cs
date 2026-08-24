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
            //Harmonics = new (double Ratio, double Amp)[]
            //{
            //    (0.501, 0.35), (1.000, 0.55), (1.502, 0.4), (2.514, 0.50)
            //},

            // Vlastní partiály - klidně i jiný počet než výchozích 7
            PartialRatios = new[] { 0.500, 1.0, 1.2, 1,5, 2.0, 3.0 },
            PartialAmplitudes = new[] { 0.20, 0.60, 0.35, 0.30, 0.55, 0.15 },
            PartialDecayRates = new[] { 0.5, 0.6, 0.6, 0.75, 0.8, 0.9 },

            //PartialRatios = new[] { 1, 0000, 2, 0000, 3, 0263, 4, 0263, 5, 0263, 6, 0526, 12, 1053, 14, 1316 },
            //PartialAmplitudes = new[] { 1, 0000, 0, 1080, 0, 6041, 0, 2456, 0, 1618, 0, 0326, 0, 0285, 0, 0267 },


            ChiffFilterFreqHz = 800.0,
            ChiffFilterQ = 1.2,
            ModType = ModulationType.FM,
            ModSpeedHz = 18.5,
            ModDepth = 0.02
        };
    }




}
