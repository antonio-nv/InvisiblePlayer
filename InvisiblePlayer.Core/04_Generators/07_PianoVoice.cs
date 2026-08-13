using System;

namespace InvisiblePlayer.Core.Generators
{
    public class PianoVoice : SynthVoice
    {
        private double _p1 = 0, _p2 = 0;

        public PianoVoice(double sampleRate) : base(sampleRate)
        {
            NoteEnvelope.AttackTime = 0.005f;  // Rychlé kladívko
            NoteEnvelope.DecayTime = 1.2f;     // Plynulý pokles
            NoteEnvelope.SustainLevel = 0.15f;
            NoteEnvelope.ReleaseTime = 0.4f;
        }

        protected override double CalculateWaveform(double frequency)
        {
            // Posun fází přes společnou metodu
            double phase1 = AdvancePhase(ref _p1, 1.0, frequency); // Základní tón
            double phase2 = AdvancePhase(ref _p2, 2.0, frequency); // 2. harmonická

            return Math.Sin(phase1 * 2.0 * Math.PI) * 0.7
                 + Math.Sin(phase2 * 2.0 * Math.PI) * 0.3;
        }
    }
}