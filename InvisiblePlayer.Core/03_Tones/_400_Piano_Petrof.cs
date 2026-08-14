using InvisiblePlayer.Core.Generators;
using InvisiblePlayer.Core.ToneEngine;

namespace InvisiblePlayer.Core.Tones
{
    public static class _200_Piano
    {
        public static VoicePreset Preset => new VoicePreset
        {
            Instrument = InstrumentType.Piano,
            // Pozn.: Harmonics níže se u Piano presetu zatím nevyužívá -
            // PianoVoice má vlastní vnitřní model harmonických a inharmonicity.
            // TADY JE TA ZMĚNA: Přidat přesný typ pole
            Harmonics = new (double Ratio, double Amp)[]
            {
                (1.0, 1.0), (2.0, 0.75), (3.0, 0.60), (4.0, 0.40), (5.0, 0.25)
            },
            ChiffFilterFreqHz = 800.0,
            ChiffFilterQ = 1.2,
            ModType = ModulationType.None,
            ModSpeedHz = 5.5,
            ModDepth = 0.08
        };
    }
}