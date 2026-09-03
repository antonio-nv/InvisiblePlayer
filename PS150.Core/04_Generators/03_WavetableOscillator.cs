using System;

namespace PS150.Core.Generators
{
    public class WavetableOscillator : IOscillator
    {
        private const int TABLE_SIZE = 2048;
        private readonly float[] _waveTable = new float[TABLE_SIZE];

        private double _phase = 0.0;
        private float _frequencyHz = 440.0f;

        public WavetableOscillator(WaveType waveType)
        {
            GenerateTable(waveType);
        }

        public void SetWaveType(WaveType waveType)
        {
            GenerateTable(waveType);
        }


        public void SetFrequency(float frequencyHz)
        {
            _frequencyHz = Math.Max(0.0f, frequencyHz);
        }

        public void Reset()
        {
            _phase = 0.0;
        }


     



        public float NextSample(int sampleRate)
        {
            if (_frequencyHz <= 0.0f || sampleRate <= 0) return 0.0f;

            // Výpočet posunu fáze pro aktuální frekvenci
            double phaseIncrement = (_frequencyHz * TABLE_SIZE) / sampleRate;

            // Přečtení vzorku z tabulky s lineární interpolací pro hladký zvuk
            int indexA = (int)_phase % TABLE_SIZE;
            int indexB = (indexA + 1) % TABLE_SIZE;
            float fraction = (float)(_phase - (int)_phase);

            float sample = _waveTable[indexA] + fraction * (_waveTable[indexB] - _waveTable[indexA]);

            // Posun fáze pro další vzorek
            _phase += phaseIncrement;
            while (_phase >= TABLE_SIZE)
            {
                _phase -= TABLE_SIZE;
            }

            return sample;
        }

        private void GenerateTable(WaveType type)
        {
            for (int i = 0; i < TABLE_SIZE; i++)
            {
                double angle = (2.0 * Math.PI * i) / TABLE_SIZE;

                _waveTable[i] = type switch
                {
                    WaveType.Sine => (float)Math.Sin(angle),

                    WaveType.Sawtooth => (float)(2.0 * (i / (double)TABLE_SIZE) - 1.0),

                    WaveType.Square => i < TABLE_SIZE / 2 ? 1.0f : -1.0f,

                    WaveType.Triangle => (float)(2.0 * Math.Abs(2.0 * (i / (double)TABLE_SIZE) - 1.0) - 1.0),

                    _ => 0.0f
                };
            }
        }
    }
}