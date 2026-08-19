using InvisiblePlayer.Core.Filters;
using System;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Fyzikální model akustického piana. Tělo tónu (zpívající struna) se
    /// simuluje metodou Karplus-Strong (viz KarplusStrongString.cs) - žádná
    /// ruční tabulka harmonických, bohaté a přirozeně tlumené spektrum
    /// vzniká samo z fyziky zpožďovací smyčky.
    ///
    /// K tomu navíc zůstává (beze změny oproti dřívějšku):
    ///  - dvě mírně rozladěné struny v chóru (unison) pro živý dozvuk se zázněry,
    ///  - krátký šumový transient úderu plstěného kladívka,
    ///  - a nově malá rezonance ozvučné desky (dva paralelní rezonátory),
    ///    která tělu tónu dodává teplo/barvu, kterou samotná struna nemá.
    /// </summary>
    public class PianoVoice : SynthVoice
    {
        private readonly KarplusStrongString _stringA;
        private readonly KarplusStrongString _stringB;
        private readonly NoiseGenerator _exciteNoise = new NoiseGenerator();
        private bool _stringExcited = false;

        private readonly double _stringBrightness;
        private readonly double _stringDecaySeconds;
        private readonly double _detuneAmountHz;

        // Transient úderu plstěného kladívka - krátký šumový "drc" navrch,
        // nezávislý na rezonanci struny samotné (beze změny oproti dřívějšku).
        private double _hammerEnvelope = 1.0;
        private readonly NoiseGenerator _hammerNoise = new NoiseGenerator();
        private readonly BandPassFilter _hammerFilter = new BandPassFilter();
        private readonly double _hammerDurationSec = 0.008;

        // Tělo struny (a hammer transient) je širokopásmový signál, ne čistá
        // sinusovka - musí se doopravdy rozdělit skutečnou výhybkou mezi
        // hloubky/středy/výšky (stejný princip jako u šumu jinde v projektu).
        private readonly ThreeBandCrossover _toneCrossover;
        private readonly ThreeBandCrossover _hammerCrossover;

        // Nepatrná rezonance ozvučné desky - dva rovnoběžné rezonátory přidané
        // k suchému zvuku struny, dodávají tělu tónu teplo/barvu.
        private readonly BandPassFilter _bodyResonanceLow = new BandPassFilter();
        private readonly BandPassFilter _bodyResonanceHigh = new BandPassFilter();
        private const double BodyGainLow = 0.18;
        private const double BodyGainHigh = 0.10;

        // Sledování aktuální hlasitosti (pomalý průměr) pro rozhodnutí, kdy je
        // tón už prakticky neslyšitelný (viz IsFinished).
        private double _lastLevel = 1.0;
        private const double SilenceThresholdDb = -90.0;
        private static readonly double SilenceThresholdLinear = Math.Pow(10.0, SilenceThresholdDb / 20.0);

        public PianoVoice(double sampleRate) : base(sampleRate)
        {
            _stringA = new KarplusStrongString(sampleRate);
            _stringB = new KarplusStrongString(sampleRate);

            _stringBrightness = 0.50;
            _stringDecaySeconds = 8.0;
            _detuneAmountHz = 0.35;

            ConfigureEnvelope();

            _hammerFilter.SetParams(450.0, 1.2, sampleRate);
            _toneCrossover = new ThreeBandCrossover(sampleRate);
            _hammerCrossover = new ThreeBandCrossover(sampleRate);

            _bodyResonanceLow.SetParams(90.0, 3.0, sampleRate);
            _bodyResonanceHigh.SetParams(420.0, 2.0, sampleRate);
        }

        public PianoVoice(VoicePreset preset, double sampleRate) : this(sampleRate)
        {
            if (preset != null)
            {
                _stringBrightness = Math.Clamp(preset.StringBrightness, 0.0, 0.98);
                _stringDecaySeconds = Math.Max(0.2, preset.StringDecaySeconds);
                _detuneAmountHz = preset.ModDepth;

                double hammerFreq = preset.ChiffFilterFreqHz > 0 ? preset.ChiffFilterFreqHz : 450.0;
                double hammerQ = preset.ChiffFilterQ > 0 ? preset.ChiffFilterQ : 1.2;
                _hammerFilter.SetParams(hammerFreq, hammerQ, sampleRate);
            }
        }

        private void ConfigureEnvelope()
        {
            NoteEnvelope.AttackTime = 0.001f;  // Okamžitý úder
            NoteEnvelope.DecayTime = 0.01f;
            NoteEnvelope.SustainLevel = 1.0f;  // Dozvuk řeší struna sama (KS smyčka), ne obálka
            NoteEnvelope.ReleaseTime = 0.20f;  // Tlumítko (damper) po pustění klávesy
        }

        public override void NoteOn()
        {
            base.NoteOn();
            _hammerEnvelope = 1.0;
            _lastLevel = 1.0;
            _stringExcited = false; // "Znovu udeřit" - i kdyby předchozí tón ještě dozníval
        }

        public override bool IsFinished =>
            base.IsFinished || (HasStarted && _lastLevel < SilenceThresholdLinear);

        protected override BandSample CalculateWaveform(double frequency)
        {
            // Strunu rozezníme až tady, při prvním vzorku po NoteOn() - teprve
            // tady známe skutečný kmitočet (viz komentář v ToneEngine.NoteOn).
            if (!_stringExcited)
            {
                double freqA = frequency;
                double freqB = frequency + _detuneAmountHz;

                _stringA.Excite(freqA, _stringBrightness, _stringDecaySeconds, _exciteNoise, (int)SampleRate);
                _stringB.Excite(freqB, _stringBrightness, _stringDecaySeconds, _exciteNoise, (int)SampleRate);

                _stringExcited = true;
            }

            BandSample bands = default;

            float sA = _stringA.NextSample();
            float sB = _stringB.NextSample();
            double stringSum = (sA + sB) * 0.5;

            double body = _bodyResonanceLow.Process(stringSum) * BodyGainLow
                        + _bodyResonanceHigh.Process(stringSum) * BodyGainHigh;

            double toneSample = stringSum + body;

            var toneSplit = _toneCrossover.Process((float)toneSample);
            bands.Bass += toneSplit.Bass;
            bands.Mid += toneSplit.Mid;
            bands.Treble += toneSplit.Treble;

            // Pomalý průměr (EMA) hlasitosti struny - pro IsFinished, ať to
            // netiká zbytečně dál, když je zvuk už prakticky neslyšitelný.
            _lastLevel = _lastLevel * 0.999 + Math.Abs(stringSum) * 0.001;

            // Transient úderu kladívka (beze změny oproti dřívějšímu chování).
            if (_hammerEnvelope > 0.001)
            {
                double noise = _hammerNoise.NextSample((int)SampleRate);
                double shapedHammer = _hammerFilter.Process(noise) * _hammerEnvelope * 0.35;

                var split = _hammerCrossover.Process((float)shapedHammer);
                bands.Bass += split.Bass;
                bands.Mid += split.Mid;
                bands.Treble += split.Treble;

                _hammerEnvelope *= Math.Exp(-1.0 / (SampleRate * _hammerDurationSec));
            }

            return bands;
        }
    }
}
