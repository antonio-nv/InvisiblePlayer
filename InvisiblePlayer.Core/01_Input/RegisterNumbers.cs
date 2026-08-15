// REFERENČNÍ TABULKA — zatím nezapojeno do VGA panelu, jen datová příprava.
//
// Zdroj číslování: fyzické popisky na hracím stole nástroje (rejstříky, spojky,
// volné kombinace, crescendo) — čísla 1 až 135 odpovídají 1:1 fyzickým klapkám.
// Čísla patřící spojkám ("Sp.") a volným kombinacím ("V") zůstávají v rozsahu
// rezervovaná (nebudou mít vlastní VoicePreset), ale úmyslně nejsou z číslování
// vynechána, aby platila přesná shoda s popiskem na nástroji.
//
// Syntetické hlasy (Piano, Cembalo, Zvon...) NEJSOU duplikované tady jako
// samostatné konstanty - jejich skutečné číslo žije přímo ve VoicePreset.Number
// každého presetu (_200_Piano_Petrof.cs, _300_Cembalo_RandallHopkirk.cs,
// _400_Bell_Zikmund.cs...). Držet dvě nezávislá místa se stejným číslem je
// zbytečné riziko rozjetí (přesně to, co se stalo s dřívější verzí tohohle
// souboru - Cembalo/Zvon tu měly zastaralá čísla 201/202 místo skutečných 300/400).
//
// Zvláštní případ: rejstřík č. 85 je i přes číselné zařazení do "varhanního"
// rozsahu zvukově zvonkohra (Aeolus 8', ne varhanní píšťaly) — jeho VoicePreset
// odkazuje na BellVoice (Instrument = InstrumentType.Bell), ne na OrganVoice.
// Viz _085_Aeolus v _001_199_organ.cs - tam je skutečné číslo, ne tady.
//
// Barevné schéma pro budoucí VGA panel (poznámka pro pozdější implementaci):
//   - text čísla: výrazná červená
//   - navolený rejstřík:   pozadí zelené
//   - nenavolený rejstřík: pozadí černé

using System.Linq;

namespace InvisiblePlayer.Core.Input
{
    public static class RegisterNumbers
    {
        // 1–135: fyzické ovládací prvky hracího stolu (rejstříky, spojky, kombinace).
        // Použitelné např. pro validaci vstupu ve VGA panelu ("je zadané číslo
        // vůbec platné číslo klapky na nástroji?").
        public static readonly int[] Organ = Enumerable.Range(1, 135).ToArray();
    }
}
