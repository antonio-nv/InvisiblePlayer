namespace InvisiblePlayer.Core.Generators
{
    public interface IOscillator
    {
        /// <summary>
        /// Vygeneruje a vrátí další jeden audio vzorek (sample) v rozsahu -1.0f až +1.0f.
        /// </summary>
        /// <param name="sampleRate">Vzorkovací frekvence (např. 44100 Hz nebo 48000 Hz)</param>
        float NextSample(int sampleRate);

        /// <summary>
        /// Nastaví požadovanou frekvenci tónu (v Hz).
        /// </summary>
        void SetFrequency(float frequencyHz);

        /// <summary>
        /// Vynuluje fází oscilátoru (např. při novém stisku klávesy).
        /// </summary>
        void Reset();
        
    }
}