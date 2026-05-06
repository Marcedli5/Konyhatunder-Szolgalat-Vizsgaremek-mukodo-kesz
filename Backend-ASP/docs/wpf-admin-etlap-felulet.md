# WPF Admin - Etlap felulet

## Osszefoglalo

Az AdminFelulet-WPF projektben az `Etlap` nezet egy egyszerusitett, ideiglenes helyi listakkal mukodo szerkesztoi feluletet kapott. A cel az volt, hogy a hangsuly maga az etlap szerkesztes legyen, ne pedig egy tulzottan dekoralt felulet vagy egy elore bevezetett API-reteg.

## Mi keszult el

- Az `EtlapViewModel` jelenleg nem backend szolgaltatast hasznal.
- A viewmodel egyszeru helyi listakkal dolgozik:
  - eloetelek
  - foetelek
  - koretek
- A valaszthato hetek listaja helyben epul fel.
- A kovetkezo het szandekosan uresen indul, hogy az admin azon tudja osszeallitani az uj etlapot.
- A felhasznaloi tajekoztatas `MessageBox` segitsegevel tortenik.
- A nezet minimalisan torodik kisebb szelessegnel is, mert a napi menusorok `WrapPanel`-ben jelennek meg.

## Mi lett tudatosan elhagyva

- Nincs mock API szolgaltatas.
- Nincs HTTP kommunikacio.
- Nincs kulon szerkesztes informacios blokk az Etlap view-ban.
- Nincs tulreszletezett DTO vagy mentesi keretrendszer.

## Referencia modellek hasznalata

Az AdminFelulet-WPF `Models` mappajaban levo osztalyok referenciakent szolgalnak. A mostani megvalositasnal ezek logikaja lett figyelembe veve, de a view es a viewmodel nincs kozvetlenul ezekre az EF modellekre kotve.

Kulonosen ezek szolgaltak szerkezeti referenciakent:

- `Food`
- `Menu`
- `DailyMenu`
- `MenuItem`
- `MenuAvailability`

Ez azert hasznos, mert a kesobbi backend oldali API hasonlo szemantikaval epulhet fel, mikozben a mostani WPF felulet egyszerubb marad.

## Erintett WPF fajlok

- `AdminFelulet-WPF/ViewModels/EtlapViewModel.cs`
- `AdminFelulet-WPF/Views/EtlapUserControl.xaml`
- `AdminFelulet-WPF/Views/MainView.xaml`
- `AdminFelulet-WPF/ViewModels/MainViewModel.cs`

## Kesoobbi bovitesre javaslat

Ha a backend elkeszul, a jelenlegi helyi listak helyere egy kulon API-s reteg vezetheto be. A jelenlegi UI viselkedes mellett eleg lesz:

1. a hetek lekerdezeset backendrol adni,
2. a napi menusorokat backendrol betolteni,
3. a mentes gombot valodi API hivassal osszekotni.

Igy a mostani egyszeru szerkesztofelulet megtarthato, csak az adatforras cserelodik majd.
