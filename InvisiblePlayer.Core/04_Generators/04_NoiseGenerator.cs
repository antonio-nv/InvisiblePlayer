using System;

namespace InvisiblePlayer.Core.Generators
{
    public class NoiseGenerator : IOscillator
    {
        private readonly Random _random = new Random();

        public void SetFrequency(float frequencyHz)
        {
            // Šum nemá konkrétní frekvenci (pitch), ale rozhraní to vyžaduje
        }

        public void Reset()
        {
            // Není co resetovat u náhodného generátoru
        }

        public float NextSample(int sampleRate)
        {
            // Náhodný vzorek od -1.0 do +1.0
            return (float)(_random.NextDouble() * 2.0 - 1.0);
        }
    }
}