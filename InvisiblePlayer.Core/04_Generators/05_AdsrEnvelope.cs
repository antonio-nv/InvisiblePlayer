using System;

namespace InvisiblePlayer.Core.Generators
{
    public enum EnvelopeState
    {
        Idle,
        Attack,
        Decay,
        Sustain,
        Release
    }

    public class AdsrEnvelope
    {
        // Časy v sekundách a úroveň sustainu (0.0 až 1.0)
        public float AttackTime { get; set; } = 0.01f;   // 10 ms rychlý náběh
        public float DecayTime { get; set; } = 0.1f;     // 100 ms pokles
        public float SustainLevel { get; set; } = 0.8f;  // Udržení na 80% hlasitosti
        public float ReleaseTime { get; set; } = 0.3f;   // 300 ms plynulé doznění

        public EnvelopeState State { get; private set; } = EnvelopeState.Idle;
        public float CurrentLevel { get; private set; } = 0.0f;

        public bool IsActive => State != EnvelopeState.Idle;

        public void TriggerGate(bool gateOn)
        {
            if (gateOn)
            {
                State = EnvelopeState.Attack;
            }
            else if (State != EnvelopeState.Idle)
            {
                State = EnvelopeState.Release;
            }
        }

        public float Process(int sampleRate)
        {
            if (sampleRate <= 0 || State == EnvelopeState.Idle)
                return 0.0f;

            float sampleTime = 1.0f / sampleRate;

            switch (State)
            {
                case EnvelopeState.Attack:
                    if (AttackTime <= 0.0f)
                    {
                        CurrentLevel = 1.0f;
                        State = EnvelopeState.Decay;
                    }
                    else
                    {
                        CurrentLevel += sampleTime / AttackTime;
                        if (CurrentLevel >= 1.0f)
                        {
                            CurrentLevel = 1.0f;
                            State = EnvelopeState.Decay;
                        }
                    }
                    break;

                case EnvelopeState.Decay:
                    if (DecayTime <= 0.0f)
                    {
                        CurrentLevel = SustainLevel;
                        State = EnvelopeState.Sustain;
                    }
                    else
                    {
                        CurrentLevel -= (1.0f - SustainLevel) * (sampleTime / DecayTime);
                        if (CurrentLevel <= SustainLevel)
                        {
                            CurrentLevel = SustainLevel;
                            State = EnvelopeState.Sustain;
                        }
                    }
                    break;

                case EnvelopeState.Sustain:
                    CurrentLevel = SustainLevel;
                    break;

                case EnvelopeState.Release:
                    if (ReleaseTime <= 0.0f)
                    {
                        CurrentLevel = 0.0f;
                        State = EnvelopeState.Idle;
                    }
                    else
                    {
                        CurrentLevel -= sampleTime / ReleaseTime;
                        if (CurrentLevel <= 0.0f)
                        {
                            CurrentLevel = 0.0f;
                            State = EnvelopeState.Idle;
                        }
                    }
                    break;
            }

            return Math.Clamp(CurrentLevel, 0.0f, 1.0f);
        }

        public void Reset()
        {
            State = EnvelopeState.Idle;
            CurrentLevel = 0.0f;
        }

      
    }
}