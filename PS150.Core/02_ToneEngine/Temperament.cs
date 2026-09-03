using System;
using System.Collections.Generic;
using PS150.Core.Generators;
using PS150.Core.Tones;
using PS150.Core.Input;


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

    // Obyčejná rovnoměrná temperatura - stejný půltón všude (12. odmocnina
    // ze 2). Tohle je i výchozí hodnota bezparametrického konstruktoru výše,
    // tahle static property je jen pro čitelnost tam, kde chceš mít volbu
    // temperatury vypsanou explicitně (viz VoicePreset.Temperament).
    public static Temperament Equal => new Temperament
    {
        Name = "Rovnoměrná (Equal)",
        CentOffsets = new double[12] // samé nuly
    };

    // Středotónové ladění, čtvrtiny komatu (Quarter-comma meantone) - typické
    // renesanční/raně barokní ladění. Kvinty jsou zúžené o 1/4 syntonického
    // komatu (~5,38 centu), takže velké tercie vycházejí ČISTÉ (poměr 5:4,
    // 386,3 centu) místo mírně "tvrdších" rovnoměrných (400 centů). Řetězec
    // kvint je tu postavený tradičně od Es po Gis (Es-B-F-C-G-D-A-E-H-Fis-Cis-Gis) -
    // "vlčí kvinta" (silně rozladěná) leží mezi Gis a Es, tady se nepoužívá.
    // Prakticky: durové akordy v "blízkých" tóninách (C, G, D, F...) zní
    // nádherně čistě, čím dál do křížků/bé se jde, tím víc to "křičí".
    public static Temperament QuarterCommaMeantone => new Temperament
    {
        Name = "Středotónové (Quarter-comma meantone)",
        CentOffsets = new double[]
        {
                /* C  */   0.00,
                /* C# */ -23.95,
                /* D  */  -6.84,
                /* D# */ +10.26,   // Es
                /* E  */ -13.69,
                /* F  */  +3.42,
                /* F# */ -20.53,
                /* G  */  -3.42,
                /* G# */ -27.37,
                /* A  */ -10.26,
                /* B  */  +6.84,   // B (česky) = anglicky Bb
                /* H  */ -17.11,   // H (česky) = anglicky B
        }
    };

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