using System;

namespace PS150.Core.Filters
{
    public class HighPassFilter
    {
        private float _sampleRate;
        private float _cutoffHz = 1000.0f;
        private float _resonance = 0.707f; // Q faktor (0.707 = plochá odezva / Butterworth)

        // Vnitřní stav filtru (paměť pro předchozí vzorky)
        private float _v0, _v1;

        // Koeficienty
        private float _a1, _a2, _b0, _b1, _b2;

        public HighPassFilter(float sampleRate = 44100.0f)
        {
            _sampleRate = sampleRate;
            RecalculateCoefficients();
        }

        public void SetSampleRate(float sampleRate)
        {
            _sampleRate = sampleRate;
            RecalculateCoefficients();
        }

        public void SetCutoff(float cutoffHz)
        {
            // Omezení mezní frekvence (max ~45% vzorkovací frekvence kvůli Nyquistu)
            _cutoffHz = Math.Clamp(cutoffHz, 20.0f, _sampleRate * 0.45f);
            RecalculateCoefficients();
        }

        public void SetResonance(float resonance)
        {
            // Resonance / Q faktor: 0.707 je neutrál, vyšší hodnoty (např. do 10.0) vytváří "pískání/zpěv"
            _resonance = Math.Clamp(resonance, 0.1f, 10.0f);
            RecalculateCoefficients();
        }

        private void RecalculateCoefficients()
        {
            // Výpočet Biquad High-Pass koeficientů podle Robert Bristow-Johnson Audio EQ Cookbook.
            // Stejný postup jako u LowPassFilter, liší se jen vzorce pro b0/b1/b2.
            float w0 = 2.0f * MathF.PI * _cutoffHz / _sampleRate;
            float cosw0 = MathF.Cos(w0);
            float alpha = MathF.Sin(w0) / (2.0f * _resonance);

            float b0 = (1.0f + cosw0) / 2.0f;
            float b1 = -(1.0f + cosw0);
            float b2 = (1.0f + cosw0) / 2.0f;
            float a0 = 1.0f + alpha;
            float a1 = -2.0f * cosw0;
            float a2 = 1.0f - alpha;

            // Normalizace přes a0
            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public float Process(float input)
        {
            // Direct Form II Transposed struktura pro stabilní výpočet
            float output = _b0 * input + _v0;
            _v0 = _b1 * input - _a1 * output + _v1;
            _v1 = _b2 * input - _a2 * output;

            return output;
        }

        public void Reset()
        {
            _v0 = 0.0f;
            _v1 = 0.0f;
        }
    }
}
