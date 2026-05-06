# Konyhatunder Szolgalat Vizsgaremek

Ez a projekt egy ételrendelesi és adminisztrácios rendszer. Három fő részből áll:

- `Backend-ASP`: ASP.NET Core backend API.
- `Frontend-ASP`: Razor Pages alapú webes felulet a felhasználóknak.
- `AdminFelulet-WPF`: WPF alapú admin felület étlap, ételek és rendelések kezelésére.

## Szükséges programok

- Windows operációs rendszer
- Visual Studio 2022 vagy ujabb
- .NET 9 SDK
- SQL Server vagy SQL Server LocalDB az ASP.NET Identity adatbázishoz
- MySQL vagy MariaDB a `vizsgaremek_etlap` adatbázishoz

## Projekt megnyitása

1. Nyisd meg a `Konyhatunder-Szolgalat-Vizsgaremek.sln` fájlt Visual Studioban.
2. Állitsd be az adatbázis kapcsolatokat az `appsettings.json` fájlokban.
3. Hozd létre a 2 adatbázist a következő parancsok beírásával a package-manager-console-ba ebben a sorrendben.
   - Update-Database -Context ApplicationDbContext
   - Update-Database -Context VizsgaremekEtlapContext
4. Inditsd el egyszerre a 3 projektet(AdminFelulet-WPF,Frontend-ASP,Backend-ASP).


## Backend indítása

A backend projekt:

```text
Backend-ASP
```

Visual Studioban inditható a `http` vagy `https` profillal.

Alapértelmezett cimek:

- HTTP: `http://localhost:5233`
- HTTPS: `https://localhost:7179`

A backend adja az API végpontokat a webes felületnek és a WPF admin alkalmazásnak is.

## Frontend indítása

A felhasználói webes felület projektje:

```text
Frontend-ASP
```

Itt érhető el az étlap, a kosár, a profil, a sajat rendelések, valamint az információs oldalak. A kosár és rendelés funkciókhoz be kell jelentkezni.

## WPF admin felület indítása

Az admin alkalmazás projektje:

```text
AdminFelulet-WPF
```

Inditás elött ellenörizd az alábbi fájlt:

```text
AdminFelulet-WPF/apiSettings.json
```

Ebben a `BaseUrl` értékenek a backend cimet kell tartalmaznia, példaul:

```json
{
  "BaseUrl": "http://localhost:5233/"
}
```

Ha a backend más porton fut, ezt az érteket is át kell irni.

## Fő funkciók

### Webes felület

- étlap megtekintése
- kosár kezelése
- rendelés leadása
- saját rendelések megtekintése
- profil oldal
- kapcsolat és informácios oldalak

### Backend

- felhasználoi API vegpontok
- admin API végpontok
- kosár kezeles
- rendelés kezelés
- étlap és menü adatok kezelése
- hibajegy rögzitese

### WPF admin felület

- heti étlap szerkesztese
- ételek kezelése
- új rendelés felvétele
- rendelések keresése, módosítása és törlese

## Gyakori hibák

### Nem indul a WPF admin felület API kapcsolata

Ellenörizd, hogy fut-e a `Backend-ASP`, és az `apiSettings.json` fájlban jó port van-e megadva.

### Adatbázis kapcsolati hiba

Ellenörizd az `appsettings.json` fájlokban a connection stringeket, valamint azt, hogy fut-e az SQL Server es a MySQL/MariaDB szerver.

### MySQL script hiba az allergének táblánál

A projekt jelenlegi modellje az `allergens` táblában `id`, `name` és `description` mezőket használ. Emiatt a lentebb szereplő scriptben nem szerepel `code` oszlophoz tartozó unique constraint.

### A wpf-be nem töltődnek be az adatok

A projektnél ez előfordulhat mivel a backend túl lassan tölt be ilyenkor csak újra kell indítani a projektet.




