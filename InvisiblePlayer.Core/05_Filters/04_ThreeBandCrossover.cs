using System;

namespace InvisiblePlayer.Core.Filters
{
    /// <summary>
    /// Rozdělí jeden ŠUMOVÝ (širokopásmový) signál do tří kmitočtových pásem -
    /// hloubky / středy / výšky. Používá se uvnitř jednotlivých hlasů (OrganVoice/
    /// PianoVoice/CembaloVoice) k rozdělení chiffu/úderu kladívka/brnknutí brčka
    /// POTÉ, co je zvuk vytvarovaný stávajícím registrovým filtrem (barva zůstává
    /// stejná jako dosud). U ČISTÝCH SINUSOVÝCH TÓNŮ se výhybka nepoužívá vůbec -
    /// tam stačí SynthVoice.AddToBand, protože přesný kmitočet už známe předem.
    ///
    /// Každá instance musí patřit jednomu konkrétnímu hlasu (drží si vlastní stav
    /// filtrů) - nesdílet mezi více současně znějícími notami.
    /// </summary>
    public class ThreeBandCrossover
    {
        public double LowCrossoverHz { get; }
        public double HighCrossoverHz { get; }

        private readonly LowPassFilter _lowPass;   // hloubky:  vše pod LowCrossoverHz
        private readonly BandPassFilter _bandPass; // středy:   pásmo mezi LowCrossoverHz a HighCrossoverHz
        private readonly HighPassFilter _highPass; // výšky:    vše nad HighCrossoverHz

        /// <param name="sampleRate">Vzorkovací kmitočet (musí sedět s ToneEngine, výchozí 44100 Hz).</param>
        /// <param name="lowCrossoverHz">Dělící kmitočet hloubky/středy. Zatím napevno 500 Hz.</param>
        /// <param name="highCrossoverHz">Dělící kmitočet středy/výšky. Zatím napevno 2000 Hz.</param>
        public ThreeBandCrossover(double sampleRate = 44100.0, double lowCrossoverHz = 500.0, double highCrossoverHz = 2000.0)
        {
            if (highCrossoverHz <= lowCrossoverHz)
                throw new ArgumentException("highCrossoverHz musí být větší než lowCrossoverHz.");

            LowCrossoverHz = lowCrossoverHz;
            HighCrossoverHz = highCrossoverHz;

            _lowPass = new LowPassFilter((float)sampleRate);
            _lowPass.SetCutoff((float)lowCrossoverHz);

            _highPass = new HighPassFilter((float)sampleRate);
            _highPass.SetCutoff((float)highCrossoverHz);

            // Střední pásmo = pásmová propust vystředěná geometricky mezi oběma
            // dělícími kmitočty, s Q dopočítaným tak, aby šířka pásma odpovídala
            // rozestupu mezi LowCrossoverHz a HighCrossoverHz.
            _bandPass = new BandPassFilter();
            double centerFreq = Math.Sqrt(lowCrossoverHz * highCrossoverHz);
            double bandwidth = highCrossoverHz - lowCrossoverHz;
            double q = centerFreq / bandwidth;
            _bandPass.SetParams(centerFreq, q, sampleRate);
        }

        /// <summary>
        /// Rozdělí jeden vzorek do tří pásem a vrátí i součet všech tří zpátky
        /// dohromady (pro případ, že by ho volající chtěl použít nefiltrovaně).
        /// </summary>
        public (float Bass, float Mid, float Treble, float Combined) Process(float input)
        {
            float bass = _lowPass.Process(input);
            float treble = _highPass.Process(input);
            float mid = (float)_bandPass.Process(input);

            return (bass, mid, treble, bass + mid + treble);
        }

        public void Reset()
        {
            _lowPass.Reset();
            _highPass.Reset();
            _bandPass.Reset();
        }
    }
}
