using System;

namespace InvisiblePlayer.Core.Generators
{
    public class CembaloVoice : SynthVoice
    {
        private double _phase = 0;

        public CembaloVoice(double sampleRate) : base(sampleRate)
        {
            NoteEnvelope.AttackTime = 0.001f; // Trsnutí
            NoteEnvelope.DecayTime = 0.8f;
            NoteEnvelope.SustainLevel = 0.0f; // Bez sustainu
            NoteEnvelope.ReleaseTime = 0.15f;
        }

        protected override double CalculateWaveform(double frequency)
        {
            double phase = AdvancePhase(ref _phase, 1.0, frequency);

            // Ostrá plectrová pilovitá vlna
            return 2.0 * (phase - Math.Floor(phase + 0.5));
        }
    }
}