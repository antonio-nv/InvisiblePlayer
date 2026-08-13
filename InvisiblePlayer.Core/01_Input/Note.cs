// Soubor: InvisiblePlayer.Core/Input/Note.cs
using System;

namespace InvisiblePlayer.Core.Input
{
    public readonly struct Note
    {
        public int Number { get; }
        public float FrequencyHz { get; }

        public Note(int midiNumber)
        {
            Number = midiNumber;
            // Výpočet přímo u zdroje - komorní A4 (69) = 440 Hz
            FrequencyHz = (float)(440.0 * Math.Pow(2.0, (midiNumber - 69) / 12.0));
        }
    }
}