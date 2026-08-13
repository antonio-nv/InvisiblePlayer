using System;

namespace InvisiblePlayer.Core.Generators
{
    public class BellVoice : SynthVoice
    {
        private double _p1 = 0, _p2 = 0, _p3 = 0;

        public BellVoice(double sampleRate) : base(sampleRate)
        {
            NoteEnvelope.AttackTime = 0.002f;
            NoteEnvelope.DecayTime = 2.5f;     // Dlouhý dojezd
            NoteEnvelope.SustainLevel = 0.0f;
            NoteEnvelope.ReleaseTime = 1.5f;
        }

        protected override double CalculateWaveform(double frequency)
        {
            // Neharmonické složky bronzového zvonu
            double p1 = AdvancePhase(ref _p1, 1.000, frequency);
            double p2 = AdvancePhase(ref _p2, 2.756, frequency);
            double p3 = AdvancePhase(ref _p3, 5.404, frequency);

            return Math.Sin(p1 * 2.0 * Math.PI) * 0.5
                 + Math.Sin(p2 * 2.0 * Math.PI) * 0.3
                 + Math.Sin(p3 * 2.0 * Math.PI) * 0.2;
        }
    }
}
