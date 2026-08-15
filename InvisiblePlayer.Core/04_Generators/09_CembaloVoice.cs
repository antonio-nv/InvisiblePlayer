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
    ///     harmonické mnohem rychleji než základní tón (na rozdíl od klavíru, kde
    ///     odeznívají víc "společně").
    ///
    /// Amplitudy harmonických, jejich rychlosti dozvuku a ADSR obálku lze volitelně
    /// přepsat z VoicePreset (viz konstruktor s parametrem preset).
    /// </summary>
    public class CembaloVoice : SynthVoice
    {
        private readonly double[] _phases = new double[6];

        // Amplitudy a rychlosti dozvuku - VLASTNÍ KOPIE pro tuhle instanci (.Clone()),
        // ne sdílená reference na Default pole (viz vysvětlení v PianoVoice.cs).
        private readonly double[] _amplitudes;
        private readonly double[] _decayRatesPerSec;

        // Kolik sekund uplynulo od NoteOn - používá se pro nezávislé dozvuky harmonických
        private double _elapsedSeconds = 0.0;

        // Výchozí hodnoty - cembalo je bohatší na vyšší harmonické než klavír
        private static readonly double[] DefaultAmplitudes = { 1.0, 0.7, 0.55, 0.4, 0.3, 0.2 };
        // Čím vyšší harmonická, tím rychleji sama odezní (nezávisle na hlavní ADSR obálce)
        private static readonly double[] DefaultDecayRatesPerSec = { 1.0, 2.2, 3.5, 5.0, 7.0, 9.5 };

        // Pluck (brnknutí) - krátký ostrý šumový impuls
        private double _pluckEnvelope = 1.0;
        private readonly NoiseGenerator _pluckNoise = new NoiseGenerator();
        private readonly BandPassFilter _pluckFilter = new BandPassFilter();
        private const double PluckDurationSec = 0.004; // 4 ms - ostřejší než klavírní kladívko

        public CembaloVoice(double sampleRate) : base(sampleRate)
        {
            NoteEnvelope.AttackTime = 0.001f;  // Prakticky okamžitý náběh
            NoteEnvelope.DecayTime = 0.9f;
            NoteEnvelope.SustainLevel = 0.0f;  // Cembalo nemá sustain - struna jen doznívá
            NoteEnvelope.ReleaseTime = 0.12f;

            _amplitudes = (double[])DefaultAmplitudes.Clone();
            _decayRatesPerSec = (double[])DefaultDecayRatesPerSec.Clone();

            _pluckFilter.SetParams(3800.0, 2.0, sampleRate); // vyšší a užší než u klavíru
        }

        // Nový konstruktor - s presetem. preset.Harmonics přepíše amplitudy,
        // preset.PartialDecayRates (stejné pole jako u BellVoice, jen znovu použité
        // pro jiný účel - rychlost dozvuku každé harmonické) přepíše dozvuky.
        public CembaloVoice(VoicePreset preset, double sampleRate) : this(sampleRate)
        {
            if (preset?.Envelope != null)
            {
                NoteEnvelope.AttackTime = preset.Envelope.AttackTime;
                NoteEnvelope.DecayTime = preset.Envelope.DecayTime;
                NoteEnvelope.SustainLevel = preset.Envelope.SustainLevel;
                NoteEnvelope.ReleaseTime = preset.Envelope.ReleaseTime;
            }

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
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _elapsedSeconds = 0.0;
            _pluckEnvelope = 1.0;
        }

        protected override double CalculateWaveform(double frequency)
        {
            double sample = 0.0;

            for (int i = 0; i < _phases.Length; i++)
            {
                int harmonicNumber = i + 1;
                double phase = AdvancePhase(ref _phases[i], harmonicNumber, frequency);

                double individualDecay = Math.Exp(-_elapsedSeconds * _decayRatesPerSec[i]);
                sample += Math.Sin(phase * 2.0 * Math.PI) * _amplitudes[i] * individualDecay;
            }

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
