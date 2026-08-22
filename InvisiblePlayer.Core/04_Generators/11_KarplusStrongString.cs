using System;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Fyzikální model jedné struny metodou Karplus-Strong (rozšířená verze,
    /// Jaffe &amp; Smith 1983 - "Extended Karplus-Strong").
    ///
    /// PRINCIP: krátký impuls (úder kladívka / drnknutí brčkem) se pošle do
    /// zpožďovací smyčky, jejíž délka odpovídá jedné periodě tónu. Uvnitř
    /// smyčky je jednoduchá dolní propust (průměrování dvou po sobě jdoucích
    /// vzorků), která impuls při každém průchodu smyčkou trochu "obrousí" -
    /// vyšší harmonické tak samy od sebe mizí rychleji než základní tón.
    ///
    /// DVĚ VYLEPŠENÍ oproti nejjednodušší učebnicové verzi:
    ///
    ///  1) DETERMINISTICKÝ TVAR BUDICÍHO IMPULSU MÍSTO ČISTÉHO ŠUMU: čistý
    ///     bílý šum (klasická, "učebnicová" varianta KS) má v KAŽDÉ konkrétní
    ///     realizaci NÁHODNĚ rozloženou energii mezi nízkými harmonickými -
    ///     ověřeno numerickou simulací, u některých tónů skutečně vyšla 2.
    ///     harmonická silnější než základní tón (proto ten dojem "hraje o
    ///     oktávu výš"). Skutečná struna se navíc nerozezní náhodně, ale podle
    ///     PŘESNÉHO tvaru výchylky v místě úderu/drnknutí - ten je trojúhelníkový
    ///     (lineární náběh k vrcholu v bodě doteku, pak lineární pokles).
    ///     Fourierova řada trojúhelníku MATEMATICKY ZARUČUJE, že základní tón
    ///     je vždy nejsilnější složka (ověřeno simulací pro basy/středy/výšky).
    ///     Malá příměs šumu (viz noiseAmount) se přidá jen pro texturu/"dech"
    ///     zvuku, ne jako nosič základního tónu.
    ///
    ///  2) ALLPASS FRAKČNÍ ZPOŽDĚNÍ (Thiran 1. řádu) místo prosté lineární
    ///     interpolace: lineární interpolace mezi dvěma vzorky se chová jako
    ///     mírná dolní propust navíc a hlavně nepřesně dolaďuje výšku,
    ///     obzvlášť citelně u vysokých tónů (kde na periodu připadá jen pár
    ///     vzorků) - tam se to projevovalo jako zázněje/součtové kmitočty.
    ///     Allpass filtr zpoždění doladí přesně, beze změny amplitudy na
    ///     žádném kmitočtu.
    /// </summary>
    public class KarplusStrongString
    {
        private readonly double _sampleRate;
        private float[]? _buffer;
        private int _writeIndex;

        // Allpass filtr pro frakční doladění (Thiran 1. řádu)
        private double _allpassCoeff;
        private double _allpassPrevIn;
        private double _allpassPrevOut;

        // Dolní propust ve smyčce (brightness) + celkový útlum za jeden průchod
        private double _brightness;
        private double _loopGain;
        private double _loopFilterPrev;

        public KarplusStrongString(double sampleRate)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// "Rozezní" strunu na daný kmitočet - naplní zpožďovací smyčku
        /// tvarem výchylky odpovídající úderu/drnknutí a nastaví, jak
        /// jasně/tmavě zní a jak dlouho doznívá.
        /// </summary>
        /// <param name="frequencyHz">Kmitočet tónu v Hz.</param>
        /// <param name="brightness">
        /// 0.0-0.98: síla dolní propusti ve smyčce. Nižší = jasnější/ostřejší
        /// zvuk (cembalo), vyšší = tmavší/plnější zvuk (klavír).
        /// </param>
        /// <param name="decaySeconds">
        /// Za kolik sekund doznívá ZÁKLADNÍ tón o -60 dB. Vyšší harmonické
        /// doznívají samy rychleji, netřeba nastavovat zvlášť.
        /// </param>
        /// <param name="noise">Zdroj bílého šumu pro texturu (viz noiseAmount).</param>
        /// <param name="sampleRateInt">Vzorkovací kmitočet (pro NoiseGenerator).</param>
        /// <param name="pickPosition">
        /// 0.02-0.5: poloha úderu/drnknutí jako podíl délky struny od kraje.
        /// Blíž kraji (menší číslo) = víc vyšších harmonických, jasnější/
        /// "kovovější" tón (cembalo). Blíž středu (0.5) = měkčí, temnější tón.
        /// </param>
        /// <param name="noiseAmount">
        /// 0.0-1.0: kolik šumu se přimíchá k deterministickému tvaru výchylky
        /// (textura úderu/drnknutí). Základní tón nese vždy hlavně ten
        /// deterministický tvar, ne šum.
        /// </param>
        public void Excite(double frequencyHz, double brightness, double decaySeconds, NoiseGenerator noise,
            int sampleRateInt, double pickPosition = 0.125, double noiseAmount = 0.15)
        {
            if (frequencyHz < 1.0) frequencyHz = 1.0;

            double delaySamples = _sampleRate / frequencyHz;
            int delayInt = Math.Max(1, (int)delaySamples);
            double frac = delaySamples - delayInt;

            // Allpass koeficient definovaný pro frac v rozsahu 0 až 1 (bez horní
            // meze) - když by vyšlo frac těsně u 1, "překlopíme" o jeden celý
            // vzorek zpoždění navíc.
            if (frac > 0.999)
            {
                delayInt += 1;
                frac = 0.0;
            }
            _allpassCoeff = Math.Clamp((1.0 - frac) / (1.0 + frac), 0.0, 0.9999);
            _allpassPrevIn = 0.0;
            _allpassPrevOut = 0.0;

            int bufferLen = delayInt;
            if (_buffer == null || _buffer.Length != bufferLen)
            {
                _buffer = new float[bufferLen];
            }

            FillExcitation(_buffer, noise, sampleRateInt, pickPosition, noiseAmount);

            _writeIndex = 0;
            _brightness = Math.Clamp(brightness, 0.0, 0.98);
            _loopFilterPrev = 0.0;

            // Zesílení smyčky spočtené tak, aby ZÁKLADNÍ tón doznil o -60 dB
            // (= 10^(-3)) za decaySeconds.
            double periodsInDecay = Math.Max(1.0, decaySeconds * frequencyHz);
            _loopGain = Math.Pow(10.0, -3.0 / periodsInDecay);
        }

        private static void FillExcitation(float[] buffer, NoiseGenerator noise, int sampleRateInt,
            double pickPosition, double noiseAmount)
        {
            int n = buffer.Length;

            // Deterministický trojúhelníkový tvar výchylky - viz komentář u
            // třídy, bod 1. peakIndex je omezený na <1, n-2>, ať jsou oba
            // jmenovatele níže vždy aspoň 1 (žádné dělení nulou na krajích).
            double clampedPick = Math.Clamp(pickPosition, 0.02, 0.5);
            int peakIndex = Math.Max(1, Math.Min(n - 2, (int)(n * clampedPick)));

            var shape = new float[n];
            for (int i = 0; i <= peakIndex; i++)
            {
                shape[i] = (float)i / peakIndex;
            }
            for (int i = peakIndex; i < n; i++)
            {
                shape[i] = (float)(n - 1 - i) / (n - 1 - peakIndex);
            }

            double clampedNoise = Math.Clamp(noiseAmount, 0.0, 1.0);

            for (int i = 0; i < n; i++)
            {
                float noiseSample = noise.NextSample(sampleRateInt);
                buffer[i] = (float)((1.0 - clampedNoise) * shape[i] + clampedNoise * noiseSample);
            }
        }

        /// <summary>Vypočítá další vzorek výstupu struny (jeden krok smyčkou).</summary>
        public float NextSample()
        {
            if (_buffer == null || _buffer.Length < 1) return 0f;

            int bufferLen = _buffer.Length;
            double rawDelayed = _buffer[_writeIndex];

            // Allpass frakční doladění (viz komentář u třídy, bod 2).
            double allpassOut = _allpassCoeff * rawDelayed + _allpassPrevIn - _allpassCoeff * _allpassPrevOut;
            _allpassPrevIn = rawDelayed;
            _allpassPrevOut = allpassOut;

            // Dolní propust ve smyčce (tlumení vyšších harmonických) + celkový
            // útlum za jeden průchod smyčkou.
            double filtered = ((1.0 - _brightness) * allpassOut + _brightness * _loopFilterPrev) * _loopGain;
            _loopFilterPrev = filtered;

            _buffer[_writeIndex] = (float)filtered;
            _writeIndex = (_writeIndex + 1) % bufferLen;

            return (float)filtered;
        }
    }
}
