using System;

namespace InvisiblePlayer.Core.Generators
{
    /// <summary>
    /// Fyzikální model jedné struny metodou Karplus-Strong (rozšířená verze,
    /// Jaffe &amp; Smith 1983 - "Extended Karplus-Strong").
    ///
    /// PRINCIP: krátký šumový impuls (úder kladívka / drnknutí brčkem) se pošle
    /// do zpožďovací smyčky, jejíž délka odpovídá jedné periodě tónu. Uvnitř
    /// smyčky je jednoduchá dolní propust (průměrování dvou po sobě jdoucích
    /// vzorků), která impuls při každém průchodu smyčkou trochu "obrousí" -
    /// vyšší harmonické tak samy od sebe mizí rychleji než základní tón,
    /// přesně jako u skutečné kmitající struny. Žádná ruční tabulka amplitud
    /// harmonických není potřeba - bohaté, přirozeně tlumené spektrum vznikne
    /// samo z fyziky smyčky.
    ///
    /// Jako pěkný vedlejší efekt: vysoké tóny (kde je perioda krátká a filtr
    /// ve smyčce tedy stihne "zabrat" víckrát za sekundu) doznívají samy
    /// rychleji než basové tóny - stejně jako na skutečném nástroji - aniž by
    /// to bylo potřeba kdekoliv ručně nastavovat.
    /// </summary>
    public class KarplusStrongString
    {
        private readonly double _sampleRate;
        private float[]? _buffer;
        private int _writeIndex;
        private double _delayFrac;
        private double _brightness;
        private double _loopGain;
        private double _prevOut;

        public KarplusStrongString(double sampleRate)
        {
            _sampleRate = sampleRate;
        }

        /// <summary>
        /// "Rozezní" strunu na daný kmitočet - naplní zpožďovací smyčku
        /// bílým šumem (to je ten úder/drnknutí) a nastaví, jak jasně/tmavě
        /// zní a jak dlouho doznívá.
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
        /// <param name="noise">Zdroj bílého šumu pro počáteční impuls.</param>
        /// <param name="sampleRateInt">Vzorkovací kmitočet (pro NoiseGenerator).</param>
        public void Excite(double frequencyHz, double brightness, double decaySeconds, NoiseGenerator noise, int sampleRateInt)
        {
            if (frequencyHz < 1.0) frequencyHz = 1.0;

            double delaySamples = _sampleRate / frequencyHz;
            int delayInt = Math.Max(2, (int)delaySamples);
            _delayFrac = delaySamples - delayInt;

            int bufferLen = delayInt + 1;
            if (_buffer == null || _buffer.Length != bufferLen)
            {
                _buffer = new float[bufferLen];
            }

            // Naplnit celou smyčku bílým šumem - to je ten počáteční úder/drnknutí,
            // ze kterého vznikne celý dozvuk tónu.
            for (int i = 0; i < bufferLen; i++)
            {
                _buffer[i] = (float)noise.NextSample(sampleRateInt);
            }

            _writeIndex = 0;
            _brightness = Math.Clamp(brightness, 0.0, 0.98);
            _prevOut = 0.0;

            // Zesílení smyčky spočtené tak, aby ZÁKLADNÍ tón doznil o -60 dB
            // (= 10^(-3)) za decaySeconds. -60dB = 10^(-3) na 60 dB stupnici.
            double periodsInDecay = Math.Max(1.0, decaySeconds * frequencyHz);
            _loopGain = Math.Pow(10.0, -3.0 / periodsInDecay);
        }

        /// <summary>Vypočítá další vzorek výstupu struny (jeden krok smyčkou).</summary>
        public float NextSample()
        {
            if (_buffer == null || _buffer.Length < 2) return 0f;

            int bufferLen = _buffer.Length;
            int i0 = _writeIndex;
            int i1 = (i0 + 1) % bufferLen;

            // Frakční doladění (lineární interpolace) - doladí smyčku přesně
            // na daný kmitočet, ne jen na nejbližší celý vzorek.
            double delayed = (1.0 - _delayFrac) * _buffer[i0] + _delayFrac * _buffer[i1];

            // Dolní propust ve smyčce (průměrování) + celkový útlum za jeden
            // průchod smyčkou - tady vzniká přirozené tlumení vyšších harmonických.
            double filtered = ((1.0 - _brightness) * delayed + _brightness * _prevOut) * _loopGain;
            _prevOut = filtered;

            _buffer[i0] = (float)filtered;
            _writeIndex = i1;

            return (float)filtered;
        }
    }
}
