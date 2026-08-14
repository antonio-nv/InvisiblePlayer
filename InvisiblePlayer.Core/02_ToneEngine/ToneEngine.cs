using System;
using System.Collections.Generic;
using InvisiblePlayer.Core.Generators;
using InvisiblePlayer.Core.Tones;
using InvisiblePlayer.Core.Input;

namespace InvisiblePlayer.Core.ToneEngine
{
    public class ActiveNote
    {
        public int NoteNumber { get; set; }   // MIDI číslo noty (např. 60 = C4)
        public double Frequency { get; set; } // Kmitočet v Hz (např. 261.63 Hz)
        public OrganVoice Voice { get; set; } // Samostatná instance hlasu
    }



    public class ToneEngine
    {

        private readonly double _sampleRate;
        private readonly Temperament _temperament;

        // Seznam právě znějících hlasů pro rejstřík Bombard
        private readonly List<ActiveNote> _activeBombardNotes = new List<ActiveNote>();

        // Zámek chránící seznam před konfliktem MIDI vlákna a Audio vlákna
        private readonly object _lock = new object();

        public ToneEngine(double sampleRate = 44100.0, Temperament? temperament = null)
        {
            _sampleRate = sampleRate;
            _temperament = temperament ?? new Temperament(); // default = rovnoměrná (samé nuly = beze změny chování)
        }

        // =========================================================================
        // 1. REAKCE NA MIDI / KLÁVESNICI (Volá se při stisku a pustití klávesy)
        // =========================================================================

        public void NoteOn(int noteNumber)
        {
            // Přepočet MIDI noty na frekvenci (A440 ladění + volitelná historická temperatura)
            double freq = 440.0 * Math.Pow(2.0, (noteNumber - 69) / 12.0)
                        * Math.Pow(2.0, _temperament.CentOffset(noteNumber) / 1200.0);

            // Vytvoříme nový hlas pro tento tón načtením presetu Bombard 16'
            var voice = new OrganVoice(_001_Bombard16Preset.Preset, _sampleRate);
            voice.NoteOn();

            // Bezpečné přidání do seznamu pod zámkem
            lock (_lock)
            {
                _activeBombardNotes.Add(new ActiveNote
                {
                    NoteNumber = noteNumber,
                    Frequency = freq,
                    Voice = voice
                });
            }
        }

        public void NoteOff(int noteNumber)
        {
            lock (_lock)
            {
                for (int i = 0; i < _activeBombardNotes.Count; i++)
                {
                    if (_activeBombardNotes[i].NoteNumber == noteNumber)
                    {
                        _activeBombardNotes[i].Voice?.NoteOff();
                    }
                }
            }
        }


        // =========================================================================
        // 2. GENEROVÁNÍ ZVUKU PRO ZVUKOVKU (Volá audio karta v reálném čase)
        // =========================================================================

        public double GenerateNextMixSample()
        {
            double mixedSample = 0.0;

            lock (_lock)
            {
                for (int i = _activeBombardNotes.Count - 1; i >= 0; i--)
                {
                    var activeNote = _activeBombardNotes[i];

                    if (activeNote?.Voice != null)
                    {
                        // Vygenerujeme vzorek
                        double sample = activeNote.Voice.GenerateSample(activeNote.Frequency);
                        mixedSample += sample;

                        // ČIŠTĚNÍ PAMĚTI: Pokud nota dozněla (ADSR obálka dojela na konec), smažeme ji ze seznamu!
                        if (activeNote.Voice.IsFinished)
                        {
                            _activeBombardNotes.RemoveAt(i);
                        }
                    }
                }
            }

            return Math.Clamp(mixedSample * 0.3, -1.0, 1.0);
        }
    }
}



 
