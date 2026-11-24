namespace GamexBusinessPage.Models;

/// <summary>
/// The home page FAQ. Single source of truth: the same text goes into the HTML,
/// the FAQPage structured data and the Markdown version read by AI agents.
/// </summary>
public static class HomeFaq
{
    public static IReadOnlyList<(string Question, string Answer)> Entries { get; } = new List<(string, string)>
    {
        ("Ile kosztuje wynajem maszyny budowlanej z operatorem?",
         "Stawkę ustalamy indywidualnie, bo zbyt wiele zależy od konkretów: jaka maszyna, ile godzin pracy, jak daleko od Olkusza i czy potrzebny jest dodatkowy osprzęt. Rozliczamy się godzinowo albo za dobę, a przy dłuższych kontraktach schodzimy z ceny. Wycena nic nie kosztuje i zwykle mamy ją gotową następnego dnia roboczego."),

        ("Czy maszynę można wynająć bez operatora?",
         "Zazwyczaj nie – większość sprzętu wydajemy z naszym operatorem. Koparka warta kilkaset tysięcy złotych w rękach kogoś, kto siada na niej pierwszy raz, kończy się zwykle awarią albo wypadkiem, a dla Ciebie operator oznacza mniej formalności: nie potrzebujesz uprawnień UDT ani nie odpowiadasz za przeglądy i ubezpieczenie. W zależności od maszyny i czasu wynajmu można się jednak dogadać – zapytaj o konkretny model i termin."),

        ("Gdzie pracujecie?",
         "Trzon zleceń to Olkusz i powiat olkuski, ale regularnie jeździmy po całej Małopolsce, na Śląsk i do Świętokrzyskiego. Byliśmy z maszynami w Bukownie, Wolbromiu, Kluczach, Trzebini, Chrzanowie, Sławkowie, Jaworznie, Dąbrowie Górniczej, Zawierciu, Krakowie i Kielcach. Przy większych kontraktach zdarza nam się wyjeżdżać dalej – warto zapytać."),

        ("Jak szybko maszyna stanie na budowie?",
         "Zwykle w ciągu jednej, dwóch dób od potwierdzenia zlecenia. Transport mamy własny – naczepy niskopodwoziowe, lawetę i zestaw z HDS – więc nie czekamy na wolny termin u przewoźnika. Jeśli sprawa jest pilna, zadzwoń: czasem da się przerzucić sprzęt jeszcze tego samego dnia."),

        ("Czy pracujecie dla gmin i inwestorów publicznych?",
         "Tak, i to od początku działalności. Realizowaliśmy zadania dla samorządów, spółdzielni mieszkaniowych, deweloperów i firm budowlanych – drogi gminne, powiatowe i wojewódzkie, chodniki, parkingi, place manewrowe. Dokumentację odbiorową przygotowujemy w komplecie, referencje wysyłamy na życzenie."),

        ("Jakie prace drogowe i brukarskie wykonujecie?",
         "Roboty ziemne i korytowanie, podbudowy z kruszywa, nawierzchnie z mas bitumicznych, frezowanie starego asfaltu, kostkę brukową, chodniki, krawężniki, zjazdy, parkingi i odwodnienia liniowe. Bierzemy też samo przygotowanie terenu pod inwestycję, jeśli resztę robi kto inny.")
    };
}
