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

        // AM/FM modulace (tremolo/vibrato) - viz komentář u CalculateWaveform.
        private readonly ModulationType _modType;
        private readonly double _modSpeedHz;
        private readonly double _modDepth;
        private double _modPhase = 0.0;

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

            _modType = preset.ModType;
            _modSpeedHz = preset.ModSpeedHz;
            _modDepth = preset.ModDepth;

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

            // AM/FM modulace (tremolo/vibrato) - společný pomalý LFO (ModSpeedHz),
            // podle ModType se uplatní buď na KMITOČET (FM = vibrato), nebo na
            // VÝSLEDNOU HLASITOST (AM = tremolo). POZOR: ModDepth má u těchhle
            // dvou úplně jiný rozsah, než jaký se používá jinde v projektu pro
            // "chorus v centech" (tam bývá ModDepth v jednotkách centů, klidně
            // desítky) - tady je to ZLOMEK (podíl), takže:
            //   FM: ModDepth ~ 0.002-0.01  (0,2-1 % kolísání kmitočtu - jemné vibrato)
            //   AM: ModDepth ~ 0.1-0.4     (10-40 % kolísání hlasitosti - slyšitelné tremolo)
            // Hodnoty jako 4.0 (centy odjinud v projektu) by u FM úplně rozladily
            // tón, u AM by zase přetáčely hlasitost do záporných hodnot (proto je
            // dole ošetřený Clamp).
            double effectiveFrequency = frequency;
            double amplitudeMultiplier = 1.0;

            if (_modType != ModulationType.None && _modSpeedHz > 0.0)
            {
                double modPhase = AdvancePhase(ref _modPhase, 1.0, _modSpeedHz);
                double modulator = Math.Sin(modPhase * 2.0 * Math.PI);

                if (_modType == ModulationType.FM)
                {
                    effectiveFrequency = frequency * (1.0 + modulator * _modDepth);
                }
                else if (_modType == ModulationType.AM)
                {
                    amplitudeMultiplier = Math.Clamp(1.0 + modulator * _modDepth, 0.0, 2.0);
                }
            }

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
                double harmonicFreq = effectiveFrequency * _partialRatios[i];
                double phase = AdvancePhase(ref _phases[i], _partialRatios[i], effectiveFrequency);
                double value = Math.Sin(phase * 2.0 * Math.PI) * _partialAmplitudes[i] * amplitudeMultiplier;

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
