using System;
using System.Collections.Generic;
using InvisiblePlayer.Core.Generators;
using InvisiblePlayer.Core.Tones;

namespace InvisiblePlayer.Core.ToneEngine
{
    // Který "nástroj" (skupina hlasů) má ToneEngine právě používat.
    // Až přibudou desítky varhanních rejstříků, budou se všechny
    // vybírat pořád jen přes InstrumentType.Organ + CurrentOrganPreset.
    public enum InstrumentType
    {
        Organ,
        Piano,
        Cembalo,
        Bell
    }

    public class ActiveNote
    {
        public int NoteNumber { get; set; }   // MIDI číslo noty (např. 60 = C4)
        public double Frequency { get; set; } // Kmitočet v Hz (např. 261.63 Hz)
        public SynthVoice Voice { get; set; } // Libovolný hlas (Organ/Piano/Cembalo/Bell - všechny dědí ze SynthVoice)
    }

    public class ToneEngine
    {
        private readonly double _sampleRate;

        // Seznam právě znějících not - společný pro všechny nástroje
        private readonly List<ActiveNote> _activeNotes = new List<ActiveNote>();

        // Zámek chránící seznam před konfliktem MIDI vlákna a Audio vlákna
        private readonly object _lock = new object();

        // Aktuálně vybraný nástroj. Výchozí je Organ + Bombard 16', stejně jako dřív.
        public InstrumentType CurrentInstrument { get; set; } = InstrumentType.Organ;

        // Aktuálně vybraný varhanní rejstřík (preset). Používá se jen když
        // CurrentInstrument == InstrumentType.Organ. Časem půjde přepínat
        // na kterýkoliv z desítek presetů (_002_..., _003_... atd.).
        public VoicePreset CurrentOrganPreset { get; set; } = _001_Bombard16Preset.Preset;

        public ToneEngine(double sampleRate = 44100.0)
        {
            _sampleRate = sampleRate;
        }

        // =========================================================================
        // 1. REAKCE NA MIDI / KLÁVESNICI (Volá se při stisku a pustití klávesy)
        // =========================================================================

        public void NoteOn(int noteNumber)
        {
            // Přepočet MIDI noty na frekvenci (A440 ladění)
            // TODO (výhledově): zohlednit historickou temperaturu nástroje - viz Temperament.cs
            double freq = 440.0 * Math.Pow(2.0, (noteNumber - 69) / 12.0);

            SynthVoice voice = CreateVoiceForCurrentInstrument();
            voice.NoteOn();

            // Bezpečné přidání do seznamu pod zámkem
            lock (_lock)
            {
                _activeNotes.Add(new ActiveNote
                {
                    NoteNumber = noteNumber,
                    Frequency = freq,
                    Voice = voice
                });
            }
        }

        // Vytvoří novou instanci hlasu podle právě vybraného nástroje.
        // Sem stačí přidat "case" pro každý další nástroj (varhany budou mít
        // vždycky jen jeden case - Organ - protože rejstřík řeší CurrentOrganPreset).
        private SynthVoice CreateVoiceForCurrentInstrument()
        {
            switch (CurrentInstrument)
            {
                case InstrumentType.Piano:
                    return new PianoVoice(_sampleRate);

                case InstrumentType.Cembalo:
                    return new CembaloVoice(_sampleRate);

                case InstrumentType.Bell:
                    return new BellVoice(_sampleRate);

                case InstrumentType.Organ:
                default:
                    return new OrganVoice(CurrentOrganPreset, _sampleRate);
            }
        }

        public void NoteOff(int noteNumber)
        {
            lock (_lock)
            {
                for (int i = 0; i < _activeNotes.Count; i++)
                {
                    if (_activeNotes[i].NoteNumber == noteNumber)
                    {
                        _activeNotes[i].Voice?.NoteOff();
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
                for (int i = _activeNotes.Count - 1; i >= 0; i--)
                {
                    var activeNote = _activeNotes[i];

                    if (activeNote?.Voice != null)
                    {
                        // Vygenerujeme vzorek
                        double sample = activeNote.Voice.GenerateSample(activeNote.Frequency);
                        mixedSample += sample;

                        // ČIŠTĚNÍ PAMĚTI: Pokud nota dozněla (ADSR obálka dojela na konec), smažeme ji ze seznamu!
                        if (activeNote.Voice.IsFinished)
                        {
                            _activeNotes.RemoveAt(i);
                        }
                    }
                }
            }

            return Math.Clamp(mixedSample * 0.3, -1.0, 1.0);
        }
    }
}





public class Temperament
        {
            public string Name { get; set; } = "Rovnoměrná (Equal)";

            // Odchylky v centech od rovnoměrné temperatury, indexováno podle pitch class:
            // [0]=C, [1]=C#, [2]=D, [3]=D#, [4]=E, [5]=F, [6]=F#, [7]=G, [8]=G#, [9]=A, [10]=B(b), [11]=H
            public double[] CentOffsets { get; set; } = new double[12]; // default = samé nuly = rovnoměrná

            public double CentOffset(int midiNoteNumber)
            {
                int pitchClass = ((midiNoteNumber % 12) + 12) % 12;
                return CentOffsets[pitchClass];
            }

            // Melzerovy varhany, 1932, temperatura "Georg Kratky I'" (a' = 440 Hz při 16 °C)
            // Zdroj: kniha o svatovítských varhanách, str. 51.
            public static Temperament MelzerGeorgKratkyI => new Temperament
            {
                Name = "Georg Kratky I' (Melzer 1932)",
                CentOffsets = new double[]
                {
                /* C  */  2,
                /* C# */ -1,
                /* D  */  0,
                /* D# */  1,
                /* E  */ -1,
                /* F  */  3,
                /* F# */ -2,
                /* G  */  1,
                /* G# */  0,
                /* A  */  0,
                /* B  */  2,
                /* H  */ -2,
                }
            };
        }
 
