using InvisiblePlayer.Core.Generators;

namespace InvisiblePlayer.Core.Tones
{
    public static class _600_Strings
    {
        public static VoicePreset Preset => new VoicePreset
        {
            
            Name = "Strings",
            Number = 600,
            Instrument = InstrumentType.Organ,
            PartialRatios = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0 },
            PartialAmplitudes = new[] { 1, 0.3, 0.2, 0.15, 0.2, 0.15, 0.3, 0.2, 0.2},
            ChiffFilterFreqHz = 800.0,
            ChiffFilterQ = 1.2,
            ModType = ModulationType.AM,
            ModSpeedHz = 30.5,
            ModDepth = 0.2
        };
    }
}