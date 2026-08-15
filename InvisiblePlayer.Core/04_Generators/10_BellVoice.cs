using System;
using System.Diagnostics;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Model velkého bronzového zvonu (inspirováno svatovítským Zikmundem, 1549) -
    /// jde o obecný fyzikální model zvonu, ne o změřený vzorek konkrétního zvonu.
    ///
    /// Reálný zvon nemá harmonické spektrum jako struna nebo píšťala - má tzv.
    /// PARTIÁLY s vlastními tradičními názvy a poměry vůči základní frekvenci:
    ///   Hum     (~0.5x)  - hluboký "podtón", nejdéle doznívající
    ///   Prime   (1.0x)   - základní tón
    ///   Tierce  (~1.2x)  - malá tercie (dává zvonu ten charakteristický "mollový" nádech)
    ///   Kvinta  (~1.5x)
    ///   Nominál (2.0x)   - nejsilnější partiál, ten, který ucho vnímá jako "výšku" zvonu
    ///
    /// Partiály jsou od teď konfigurovatelné přes VoicePreset (viz konstruktor
    /// s parametrem preset) - když preset nic nevyplní, použije se výchozí
    /// obecný model zvonu (hodnoty níže, DefaultPartial*).
    ///
    /// Navíc: typické "vlnění/třepotání" (beating) velkých zvonů vzniká, když zvon
    /// není dokonale osově symetrický - dvě velmi blízké frekvence (např. dva mírně
    /// rozladěné Nominály) spolu interferují a hlasitost pravidelně "pulzuje".
    /// </summary>
    public class BellVoice : SynthVoice
    {
        private readonly double[] _phases;
        private double _elapsedSeconds = 0.0;

        // Poměry, amplitudy a rychlosti dozvuku partiálů - buď z presetu, nebo výchozí.
        private readonly double[] _partialRatios;
        private readonly double[] _partialAmplitudes;
        private readonly double[] _partialDecayRates;

        // Výchozí obecný model zvonu (Hum, Prime, Tierce, Kvinta, Nominál, +2 vyšší)
        private static readonly double[] DefaultPartialRatios =
        {
            0.501, 1.000, 1.199, 1.502, 2.000, 2.514, 3.011
        };
        private static readonly double[] DefaultPartialAmplitudes =
        {
            0.35, 0.55, 0.40, 0.30, 0.50, 0.20, 0.12
        };
        private static readonly double[] DefaultPartialDecayRates =
        {
            0.12, 0.35, 0.55, 0.70, 0.45, 1.10, 1.60
        };

        // Index Nominálu ve výchozím poli (pro efekt beatingu, viz níže) - u výchozího
        // modelu je to index 4. Pokud preset dodá vlastní pole jiné délky/pořadí,
        // beating se váže na tenhle stejný index - dej pozor, ať v presetu odpovídá
        // taky Nominálu, jinak bude "vlnit" jiný partiál, než čekáš.
        private const int NominalIndex = 4;

        // Druhý, mírně rozladěný Nominál pro efekt "vlnění" (beating)
        private const double DetunedNominalRatio = 2.006;
        private double _detunedNominalPhase = 0.0;

        // --- FM "wobble" (nakřáplost) - volitelné, řízené z VoicePreset ---
        private double _modPhase = 0.0;
        private readonly double _modSpeedHz;
        private readonly double _modDepth;
        private readonly bool _modEnabled;

        public BellVoice(double sampleRate) : base(sampleRate)
        {
            _partialRatios = DefaultPartialRatios;
            _partialAmplitudes = DefaultPartialAmplitudes;
            _partialDecayRates = DefaultPartialDecayRates;
            _phases = new double[_partialRatios.Length];

            NoteEnvelope.AttackTime = 0.002f;   // Okamžitý úder srdce o plášť
            NoteEnvelope.DecayTime = 3.0f;
            NoteEnvelope.SustainLevel = 0.0f;   // Zvon nemá sustain, jen dozvuk
            NoteEnvelope.ReleaseTime = 2.5f;    // Dlouhý dojezd i po "puštění" (u zvonu spíš teoretické)

            _modEnabled = false;
        }

        // Nový konstruktor - s presetem. Umožňuje přepsat partiály i FM wobble
        // (ModType == FM) z VoicePreset, např. pro rejstřík č. 85 (Aeolus).
        public BellVoice(VoicePreset preset, double sampleRate) : base(sampleRate)
        {
            bool customPartials =
                preset?.PartialRatios != null &&
                preset.PartialAmplitudes != null &&
                preset.PartialDecayRates != null &&
                preset.PartialRatios.Length == preset.PartialAmplitudes.Length &&
                preset.PartialRatios.Length == preset.PartialDecayRates.Length;

            if (customPartials)
            {
                _partialRatios = preset.PartialRatios;
                _partialAmplitudes = preset.PartialAmplitudes;
                _partialDecayRates = preset.PartialDecayRates;
            }
            else
            {
                if (preset?.PartialRatios != null || preset?.PartialAmplitudes != null || preset?.PartialDecayRates != null)
                {
                    // Preset se o vlastní partiály pokusil, ale pole nesedí délkou
                    // (nebo některé chybí) - raději spadneme na bezpečný výchozí model,
                    // než abychom za běhu spadli na IndexOutOfRange.
                    Debug.WriteLine($"[BellVoice] Preset '{preset?.Name}' má nekompletní/nesouhlasící partiály (Ratios/Amplitudes/DecayRates musí mít stejnou délku) - použit výchozí model zvonu.");
                }

                _partialRatios = DefaultPartialRatios;
                _partialAmplitudes = DefaultPartialAmplitudes;
                _partialDecayRates = DefaultPartialDecayRates;
            }

            _phases = new double[_partialRatios.Length];

            NoteEnvelope.AttackTime = 0.002f;
            NoteEnvelope.DecayTime = 3.0f;
            NoteEnvelope.SustainLevel = 0.0f;
            NoteEnvelope.ReleaseTime = 2.5f;

            if (preset != null && preset.ModType == ModulationType.FM)
            {
                _modEnabled = true;
                _modSpeedHz = preset.ModSpeedHz;
                _modDepth = preset.ModDepth;
            }
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _elapsedSeconds = 0.0;
        }

        protected override double CalculateWaveform(double frequency)
        {
            double effectiveFrequency = frequency;

            if (_modEnabled)
            {
                // Pomalé kolísání základní frekvence - aplikuje se na všechny partiály
                // společně (celý zvon "plave"), ne na každý partiál zvlášť.
                double modPhase = AdvancePhase(ref _modPhase, 1.0, _modSpeedHz);
                double modulator = Math.Sin(modPhase * 2.0 * Math.PI);
                effectiveFrequency = frequency * (1.0 + modulator * _modDepth);
            }

            double sample = 0.0;

            for (int i = 0; i < _partialRatios.Length; i++)
            {
                double phase = AdvancePhase(ref _phases[i], _partialRatios[i], effectiveFrequency);
                double decay = Math.Exp(-_elapsedSeconds * _partialDecayRates[i]);
                sample += Math.Sin(phase * 2.0 * Math.PI) * _partialAmplitudes[i] * decay;
            }

            // Druhý, mírně rozladěný Nominál - stejná amplituda/dozvuk jako hlavní Nominál,
            // ale jiná frekvence -> vznikne slyšitelné "vlnění" hlasitosti (beating).
            // Použije se, jen pokud preset skutečně má partiál na NominalIndex (tj.
            // pole je dost dlouhé) - jinak by přístup mimo rozsah pole spadl.
            if (_partialRatios.Length > NominalIndex)
            {
                double detunedPhase = AdvancePhase(ref _detunedNominalPhase, DetunedNominalRatio, effectiveFrequency);
                double nominalDecay = Math.Exp(-_elapsedSeconds * _partialDecayRates[NominalIndex]);
                sample += Math.Sin(detunedPhase * 2.0 * Math.PI) * _partialAmplitudes[NominalIndex] * nominalDecay;
            }

            _elapsedSeconds += 1.0 / SampleRate;

            // Normalizace - partiálů je dohromady 8, ať zvon nepřebuzuje výstup
            return sample * 0.3;
        }
    }
}
