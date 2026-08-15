using InvisiblePlayer.Core.Filters;
using System;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Model cembala (pracovní název "Randall & Hopkirk" - žádný konkrétní historický
    /// nástroj, jen interní jméno presetu).
    /// Dva hlavní jevy, které dělají cembalo cembalem (na rozdíl od klavíru):
    ///  1) BRNKNUTÍ BRČKEM (plectrum) - ostrý, velmi krátký šumový "click" na začátku,
    ///     mnohem kratší a "sušší" než úder klavírního kladívka.
    ///  2) NEZÁVISLÉ DOZNÍVÁNÍ HARMONICKÝCH - u brnkané struny odeznívají vyšší
    ///     harmonické mnohem rychleji než základní tón.
    ///
    /// DŮLEŽITÁ ZMĚNA: dokud klávesu držíš, sdílená ADSR obálka po náběhu zůstává
    /// na hladině 1.0 a neklesá - o doznívání se stará VÝHRADNĚ fyzikální model
    /// (amplitudy + rychlosti dozvuku). Teprve puštění klávesy spustí Release,
    /// coby přiblížení reálné dušičce (damperu), která strunu rychle utlumí.
    /// Kromě toho konec tónu hlídá i pokles pod práh -90 dB (kdybys klávesu držel
    /// dlouho poté, co je zvuk fyzicky už neslyšitelný).
    /// </summary>
    public class CembaloVoice : SynthVoice
    {
        private readonly double[] _phases = new double[6];

        private readonly double[] _amplitudes;
        private readonly double[] _decayRatesPerSec;

        private double _elapsedSeconds = 0.0;

        private static readonly double[] DefaultAmplitudes = { 1.0, 0.7, 0.55, 0.4, 0.3, 0.2 };
        private static readonly double[] DefaultDecayRatesPerSec = { 1.0, 2.2, 3.5, 5.0, 7.0, 9.5 };

        // --- Konec tónu podle prahu, ne (jen) podle pevného času ---
        private const double SilenceThresholdDb = -90.0;
        private static readonly double SilenceThresholdLinear = Math.Pow(10.0, SilenceThresholdDb / 20.0);
        private double _lastPeakPartialLevel = 1.0;

        // Pluck (brnknutí) - krátký ostrý šumový impuls
        private double _pluckEnvelope = 1.0;
        private readonly NoiseGenerator _pluckNoise = new NoiseGenerator();
        private readonly BandPassFilter _pluckFilter = new BandPassFilter();
        private const double PluckDurationSec = 0.004;

        public CembaloVoice(double sampleRate) : base(sampleRate)
        {
            _amplitudes = (double[])DefaultAmplitudes.Clone();
            _decayRatesPerSec = (double[])DefaultDecayRatesPerSec.Clone();

            ConfigureEnvelope();

            _pluckFilter.SetParams(3800.0, 2.0, sampleRate);
        }

        public CembaloVoice(VoicePreset preset, double sampleRate) : this(sampleRate)
        {
            if (preset?.Harmonics != null && preset.Harmonics.Length == _amplitudes.Length)
            {
                for (int i = 0; i < _amplitudes.Length; i++)
                {
                    _amplitudes[i] = preset.Harmonics[i].Amplitude;
                }
            }

            if (preset?.PartialDecayRates != null && preset.PartialDecayRates.Length == _decayRatesPerSec.Length)
            {
                for (int i = 0; i < _decayRatesPerSec.Length; i++)
                {
                    _decayRatesPerSec[i] = preset.PartialDecayRates[i];
                }
            }

            // preset?.Envelope se tu záměrně nepoužívá - obálka je teď pevně daná
            // ConfigureEnvelope() (náběh + hold na 1.0), aby doznívání řídil čistě
            // fyzikální model. Kdybys chtěl jiný Release (rychlost dušičky), uprav
            // hodnotu přímo v ConfigureEnvelope(), ne přes preset.
        }

        private void ConfigureEnvelope()
        {
            NoteEnvelope.AttackTime = 0.001f;  // Prakticky okamžitý náběh
            NoteEnvelope.DecayTime = 0.01f;    // Rychle "usedne" na sustain
            NoteEnvelope.SustainLevel = 1.0f;  // ...a dál drží 1.0 - neklesá sama
            NoteEnvelope.ReleaseTime = 0.12f;  // Puštění klávesy = dušička (damper)
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _elapsedSeconds = 0.0;
            _pluckEnvelope = 1.0;
            _lastPeakPartialLevel = 1.0;
        }

        public override bool IsFinished =>
            base.IsFinished || (HasStarted && _lastPeakPartialLevel < SilenceThresholdLinear);

        protected override double CalculateWaveform(double frequency)
        {
            double sample = 0.0;
            double peakLevel = 0.0;

            for (int i = 0; i < _phases.Length; i++)
            {
                int harmonicNumber = i + 1;
                double phase = AdvancePhase(ref _phases[i], harmonicNumber, frequency);

                double individualDecay = Math.Exp(-_elapsedSeconds * _decayRatesPerSec[i]);
                double level = _amplitudes[i] * individualDecay;

                sample += Math.Sin(phase * 2.0 * Math.PI) * level;

                if (level > peakLevel) peakLevel = level;
            }

            _lastPeakPartialLevel = peakLevel;
            sample *= 0.35;

            // Pluck - ostrý krátký "click" na začátku
            if (_pluckEnvelope > 0.001)
            {
                double noise = _pluckNoise.NextSample((int)SampleRate);
                sample += _pluckFilter.Process(noise) * _pluckEnvelope * 0.25;
                _pluckEnvelope *= Math.Exp(-1.0 / (SampleRate * PluckDurationSec));
            }

            _elapsedSeconds += 1.0 / SampleRate;

            return sample;
        }
    }
}
