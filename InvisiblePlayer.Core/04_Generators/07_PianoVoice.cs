using InvisiblePlayer.Core.Filters;
using System;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Model klavíru (obecný, ne konkrétní model jako Petrof).
    /// Dva hlavní jevy, které dělají klavír klavírem:
    ///  1) INHARMONICITA - tuhost struny způsobuje, že vyšší harmonické nejsou
    ///     přesné celočíselné násobky základní frekvence, ale jsou mírně "vytažené" nahoru.
    ///  2) ÚDER KLADÍVKA - krátký šumový impuls na začátku tónu (filtrovaný přes
    ///     pásmovou propust), který dává tónu ten charakteristický "cvak".
    ///
    /// Amplitudy harmonických a ADSR obálku lze volitelně přepsat z VoicePreset
    /// (viz konstruktor s parametrem preset) - hodí se pro víc "barev" klavíru
    /// (jasnější/temnější zvuk) beze změny téhle třídy.
    /// </summary>
    public class PianoVoice : SynthVoice
    {
        // Fáze pro 6 harmonických (základní tón + 5 vyšších)
        private readonly double[] _phases = new double[6];

        // Amplitudy jednotlivých harmonických - VLASTNÍ KOPIE pro tuhle instanci
        // (viz .Clone() níže), ne sdílená reference na DefaultAmplitudes. Kdyby
        // ukazovaly na stejné pole, přepsání presetem by natrvalo poškodilo
        // výchozí hodnoty i pro všechny další klavíry vytvořené bez presetu.
        private readonly double[] _amplitudes;

        // Koeficient inharmonicity - u reálného klavíru cca 0.0001 (basy) až 0.001 (výšky).
        // Fyzika struny zůstává vždy stejná bez ohledu na preset - to dělá klavír klavírem.
        private const double InharmonicityCoefficient = 0.00035;

        // Výchozí amplitudy - vyšší harmonické tišší (přibližně odpovídá úderu kladívka)
        private static readonly double[] DefaultAmplitudes = { 1.0, 0.55, 0.30, 0.18, 0.10, 0.06 };

        // Šum kladívka (podobný principu jako "chiff" u varhan)
        private double _hammerEnvelope = 1.0;
        private readonly NoiseGenerator _hammerNoise = new NoiseGenerator();
        private readonly BandPassFilter _hammerFilter = new BandPassFilter();
        private const double HammerDurationSec = 0.008; // 8 ms - velmi krátký "cvak"

        public PianoVoice(double sampleRate) : base(sampleRate)
        {
            // Klasická klavírní obálka: rychlý úder, plynulý pokles, nízký sustain
            NoteEnvelope.AttackTime = 0.003f;
            NoteEnvelope.DecayTime = 1.4f;
            NoteEnvelope.SustainLevel = 0.10f;
            NoteEnvelope.ReleaseTime = 0.35f;

            _amplitudes = (double[])DefaultAmplitudes.Clone();

            _hammerFilter.SetParams(2500.0, 1.5, sampleRate);
        }

        // Nový konstruktor - s presetem. Umožňuje přepsat amplitudy harmonických
        // (preset.Harmonics) a ADSR obálku (preset.Envelope), např. pro klavír
        // s jinou barvou zvuku než výchozí.
        public PianoVoice(VoicePreset preset, double sampleRate) : this(sampleRate)
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
            // Poznámka: preset.Harmonics[i].FrequencyMultiplier se u klavíru záměrně
            // nepoužívá - fyzikální "roztažení" harmonických (inharmonicita) počítá
            // tahle třída vždy sama (viz InharmonicityCoefficient výše). Preset tedy
            // řídí jen HLASITOST jednotlivých harmonických, ne jejich přesný poměr.
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _hammerEnvelope = 1.0; // Reset šumu kladívka pro nový úder
        }

        protected override double CalculateWaveform(double frequency)
        {
            double sample = 0.0;

            for (int i = 0; i < _phases.Length; i++)
            {
                int harmonicNumber = i + 1;

                // Inharmonicita: f_n = n * f0 * sqrt(1 + B * n^2)
                double stretch = Math.Sqrt(1.0 + InharmonicityCoefficient * harmonicNumber * harmonicNumber);
                double ratio = harmonicNumber * stretch;

                double phase = AdvancePhase(ref _phases[i], ratio, frequency);
                sample += Math.Sin(phase * 2.0 * Math.PI) * _amplitudes[i];
            }

            // Normalizace (součet amplitud harmonických)
            sample *= 0.5;

            // Šum kladívka - krátký "cvak" na začátku, filtrovaný do vyšších frekvencí
            if (_hammerEnvelope > 0.001)
            {
                double noise = _hammerNoise.NextSample((int)SampleRate);
                sample += _hammerFilter.Process(noise) * _hammerEnvelope * 0.15;
                _hammerEnvelope *= Math.Exp(-1.0 / (SampleRate * HammerDurationSec));
            }

            return sample;
        }
    }
}
