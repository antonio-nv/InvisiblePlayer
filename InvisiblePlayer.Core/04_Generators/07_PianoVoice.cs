using InvisiblePlayer.Core.Filters;
using System;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Pokročilý fyzikální model akusitckého piana.
    /// Řeší:
    /// 1) Inharmonicitu ocelových strun (B-faktor stiffness).
    /// 2) Rychlejší doznávání vyšších harmonických (per-harmonic exponential decay).
    /// 3) Impuls úderu plstěného kladívka (hammer knock transient).
    /// 4) Fázové zázněry více-strunného chóru (unison detune beating).
    /// </summary>
    public class PianoVoice : SynthVoice
    {
        private const int HarmonicCount = 10;

        // Fáze pro 2 struny v chóru (dávají akustickému pianu živý prostorový dozněv)
        private readonly double[] _phasesStringA = new double[HarmonicCount];
        private readonly double[] _phasesStringB = new double[HarmonicCount];

        private double _elapsedSeconds = 0.0;

        // Parametry z presetu
        private readonly double[] _harmonicAmplitudes;
        private readonly double[] _harmonicDecayRates;
        private readonly double _inharmonicityB;
        private readonly double _detuneAmountHz;

        // Hammer knock (úder kladívka)
        private double _hammerEnvelope = 1.0;
        private readonly NoiseGenerator _hammerNoise = new NoiseGenerator();
        private readonly BandPassFilter _hammerFilter = new BandPassFilter();
        private readonly double _hammerDurationSec;

        // Konec tónu podle prahu -90 dB
        private const double SilenceThresholdDb = -90.0;
        private static readonly double SilenceThresholdLinear = Math.Pow(10.0, SilenceThresholdDb / 20.0);
        private double _lastPeakPartialLevel = 1.0;

        public PianoVoice(double sampleRate) : base(sampleRate)
        {
            // Výchozí hodnoty pro akustické křídlo
            _harmonicAmplitudes = new double[] { 1.0, 0.70, 0.45, 0.30, 0.20, 0.12, 0.08, 0.05, 0.03, 0.01 };
            _harmonicDecayRates = new double[] { 0.8, 1.40, 2.20, 3.10, 4.20, 5.50, 7.00, 8.80, 11.0, 14.0 };
            _inharmonicityB = 0.00015;
            _detuneAmountHz = 0.35;
            _hammerDurationSec = 0.008;

            ConfigureEnvelope();
            _hammerFilter.SetParams(600.0, 1.5, sampleRate);
        }

        public PianoVoice(VoicePreset preset, double sampleRate) : this(sampleRate)
        {
            if (preset != null)
            {
                if (preset.PartialAmplitudes != null && preset.PartialAmplitudes.Length >= HarmonicCount)
                {
                    Array.Copy(preset.PartialAmplitudes, _harmonicAmplitudes, HarmonicCount);
                }

                if (preset.PartialDecayRates != null && preset.PartialDecayRates.Length >= HarmonicCount)
                {
                    Array.Copy(preset.PartialDecayRates, _harmonicDecayRates, HarmonicCount);
                }

                // Inharmonicitu a rozladění bereme z vlastností presetu
                _inharmonicityB = preset.ChiffFilterQ > 0 ? preset.ChiffFilterQ * 0.0001 : 0.00015;
                _detuneAmountHz = preset.ModDepth;

                double filterFreq = preset.ChiffFilterFreqHz > 0 ? preset.ChiffFilterFreqHz : 600.0;
                _hammerFilter.SetParams(filterFreq, 1.5, sampleRate);
            }
        }

        private void ConfigureEnvelope()
        {
            NoteEnvelope.AttackTime = 0.001f; // Okamžitý úder
            NoteEnvelope.DecayTime = 0.01f;
            NoteEnvelope.SustainLevel = 1.0f; // Neklesá pevným časem, řídí ho decay jednotlivých alikvót
            NoteEnvelope.ReleaseTime = 0.20f; // Tlumítko (damper) po pustění klávesy
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _elapsedSeconds = 0.0;
            _hammerEnvelope = 1.0;
            _lastPeakPartialLevel = 1.0;
        }

        public override bool IsFinished =>
            base.IsFinished || (HasStarted && _lastPeakPartialLevel < SilenceThresholdLinear);

        protected override double CalculateWaveform(double frequency)
        {
            double sample = 0.0;
            double peakLevel = 0.0;

            // Výpočet 2 rozladěných strun v chóru pro přirozený dozněv
            double freqA = frequency;
            double freqB = frequency + _detuneAmountHz;

            for (int i = 0; i < HarmonicCount; i++)
            {
                int harmonicIndex = i + 1;

                // Fyzikální vzorec inharmonicity tuhé ocelové struny
                double stretch = Math.Sqrt(1.0 + _inharmonicityB * harmonicIndex * harmonicIndex);
                double ratio = harmonicIndex * stretch;

                double phaseA = AdvancePhase(ref _phasesStringA[i], ratio, freqA);
                double phaseB = AdvancePhase(ref _phasesStringB[i], ratio, freqB);

                // Exponenciální pokles amplitudy dané harmonické
                double decay = Math.Exp(-_elapsedSeconds * _harmonicDecayRates[i]);
                double level = _harmonicAmplitudes[i] * decay;

                double waveA = Math.Sin(phaseA * 2.0 * Math.PI);
                double waveB = Math.Sin(phaseB * 2.0 * Math.PI);

                sample += (waveA + waveB) * 0.5 * level;

                if (level > peakLevel) peakLevel = level;
            }

            _lastPeakPartialLevel = peakLevel;
            sample *= 0.30; // Normalizace

            // Transient úderu plstěného kladívka (dřevěný/plstěný drc)
            if (_hammerEnvelope > 0.001)
            {
                double noise = _hammerNoise.NextSample((int)SampleRate);
                sample += _hammerFilter.Process(noise) * _hammerEnvelope * 0.35;
                _hammerEnvelope *= Math.Exp(-1.0 / (SampleRate * _hammerDurationSec));
            }

            _elapsedSeconds += 1.0 / SampleRate;

            return sample;
        }
    }
}