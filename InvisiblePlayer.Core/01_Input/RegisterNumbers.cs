// NÁVRH / REFERENČNÍ TABULKA — zatím nezapojeno do VGA panelu, jen datová příprava.
//
// Zdroj číslování: fyzické popisky na hracím stole nástroje (rejstříky, spojky,
// volné kombinace, crescendo) — čísla 1 až 135 odpovídají 1:1 fyzickým klapkám.
// Čísla patřící spojkám ("Sp.") a volným kombinacím ("V") zůstávají v tabulce
// rezervovaná (nebudou mít vlastní VoicePreset), ale úmyslně nejsou z číslování
// vynechána, aby platila přesná shoda s popiskem na nástroji.
//
// Syntetické hlasy mimo varhanní rejstříky navazují na již existující konvenci
// v repu (_200_Piano_Petrof.cs) a pokračují od čísla 200 výš:
//   200 = Piano (Petrof)   — již existuje
//   201 = Cembalo
//   202 = Zvon (Zikmund)
//
// Zvláštní případ: rejstřík č. 85 je i přes číselné zařazení do "varhanního"
// rozsahu zvukově zvonkohra (Aeolus 8', ne varhanní píšťaly) — jeho VoicePreset
// by měl typově odkazovat na zvonový generátor, ne na standardní OrganVoice.
//
// Barevné schéma pro budoucí VGA panel (poznámka pro pozdější implementaci):
//   - text čísla: výrazná červená
//   - navolený rejstřík:   pozadí zelené
//   - nenavolený rejstřík: pozadí černé

using System.Collections.Generic;
using System.Linq;

namespace InvisiblePlayer.Core.Tones
{
    public static class RegisterNumbers
    {
        // 1–135: fyzické ovládací prvky hracího stolu (rejstříky, spojky, kombinace).
        public static readonly int[] Organ = Enumerable.Range(1, 135).ToArray();

        // Syntetické hlasy mimo varhanní rejstříky.
        public const int Piano = 200;
        public const int Cembalo = 201;
        public const int Zvon = 202;

        // Rejstřík v organovém rozsahu, ale zvukově jde o zvon (viz poznámka výše).
        public const int ZvonkohraAeolus = 85;

        // Kompletní seznam pro potřeby panelu (organové rejstříky + syntetické hlasy).
        public static readonly int[] All = Organ.Concat(new[] { Piano, Cembalo, Zvon }).ToArray();
    }
}
