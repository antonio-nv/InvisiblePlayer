using InvisiblePlayer.Core.Filters;
using NAudio.SoundFont;
using System;

namespace InvisiblePlayer.Core.Generators
{


    public class OrganVoice : SynthVoice
    {
        private readonly VoicePreset _preset;
        private readonly double[] _partialRatios;
        private readonly double[] _partialAmplitudes;
        private double[] _phases;
        private double _chiffEnvelope = 1.0;
        private readonly NoiseGenerator _noiseGen = new NoiseGenerator();
        private readonly BandPassFilter _chiffFilter = new BandPassFilter();

        // Chiff je ŠUM (širokopásmový signál) - i po vytvarování registrovým
        // _chiffFilter má reálnou šířku spektra, takže se (na rozdíl od
        // sinusových harmonických) musí doopravdy rozdělit skutečnou výhybkou,
        // ne jen zařadit podle jednoho kmitočtu.
        private readonly ThreeBandCrossover _chiffCrossover;

        public OrganVoice(VoicePreset preset, double sampleRate) : base(sampleRate)
        {
            _preset = preset;

            // Bezpečnostní záchytka - kdyby na rejstřík při tom velkém ručním
            // přepisu ze svatovítské analýzy někdo zapomněl (nebo ho teprve
            // rozepisuje), radši tichý čistý sinus na základním tónu, než pád.
            bool hasPartials =
                preset.PartialRatios != null && preset.PartialAmplitudes != null &&
                preset.PartialRatios.Length > 0 &&
                preset.PartialRatios.Length == preset.PartialAmplitudes.Length;

            _partialRatios = hasPartials ? preset.PartialRatios : new double[] { 1.0 };
            _partialAmplitudes = hasPartials ? preset.PartialAmplitudes : new double[] { 1.0 };
            _phases = new double[_partialRatios.Length];

            // VARHANNÍ OBÁLKA:
            NoteEnvelope.AttackTime = 0.015f;  // Rychlý náběh
            NoteEnvelope.DecayTime = 0.05f;
            NoteEnvelope.SustainLevel = 1.0f;  // <--- DRŽÍ 100% HLASITOST POKUD DRŽÍŠ KLÁVESU!
            NoteEnvelope.ReleaseTime = 0.03f;  // Rychlé vypnutí po uvolnění

            _chiffFilter.SetParams(_preset.ChiffFilterFreqHz, _preset.ChiffFilterQ, sampleRate);
            _chiffCrossover = new ThreeBandCrossover(sampleRate);
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _chiffEnvelope = 1.0; // Reset obálky pro startovní zapraskání píšťaly
        }

        protected override BandSample CalculateWaveform(double frequency)
        {
            BandSample bands = default;

            // 1. Zvuk alikvótních píšťal - stejné pole jako u Bell/AdditiveString
            // (PartialRatios/PartialAmplitudes), jen se tady VŮBEC nepoužívá
            // PartialDecayRates - foukaná píšťala hraje na konstantní hlasitosti,
            // dokud se drží klávesa, celý tón "zhasne" najednou přes ADSR
            // Release (viz konstruktor výš), ne alikvot po alikvotu. Čistá
            // sinusovka má přesně daný kmitočet, takže ji rovnou zařadíme do
            // správného pásma podle toho, kolik Hz doopravdy má - žádné
            // filtrování netřeba.
            for (int i = 0; i < _partialRatios.Length; i++)
            {
                double harmonicFreq = frequency * _partialRatios[i];
                double phase = AdvancePhase(ref _phases[i], _partialRatios[i], frequency);
                double value = Math.Sin(phase * 2.0 * Math.PI) * _partialAmplitudes[i];

                AddToBand(ref bands, harmonicFreq, value);
            }

            // 2. Chiff (zapraskání vzduchu při otevření ventilu) - barva chiffu
            // zůstává stejná jako dosud (tvaruje ji _chiffFilter rejstříku),
            // ale protože je to šum, jeho energii je nutné doopravdy rozdělit
            // mezi pásma skutečnou výhybkou (_chiffCrossover), ne jen podle
            // jednoho kmitočtu.
            if (_chiffEnvelope > 0.001)
            {
                double noise = _noiseGen.NextSample((int)SampleRate);
                double shapedChiff = _chiffFilter.Process(noise) * _chiffEnvelope * _preset.ChiffNoiseGain;

                var split = _chiffCrossover.Process((float)shapedChiff);
                bands.Bass += split.Bass;
                bands.Mid += split.Mid;
                bands.Treble += split.Treble;

                _chiffEnvelope *= Math.Exp(-1.0 / (SampleRate * _preset.ChiffDurationSec));
            }

            return bands;
        }
    }
}
