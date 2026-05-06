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
2. Állitsd be az adatbázis kapcsolatokat az `appsettings.json` fajlokban.
3. Hozd létre a 2 adatbázist a következő parancsok beírásával a package-manager-console-ba ebben a sorrendben.
   - Add-Migration Identity -Context ApplicationDbContext
   - Update-Database -Context ApplicationDbContext
   - Add-Migration VizsgaremekEtlap -Context VizsgaremekEtlapContext
   - Update-Database -Context VizsgaremekEtlapContext
4. Inditsd el egyszerre a 3 projektet(AdminFelulet-WPF,Frontend-ASP,Backend-ASP).


## Backend indítása

A backend projekt:

```text
Backend-ASP
```

Visual Studioban indithato a `http` vagy `https` profillal.

Alapertelmezett cimek:

- HTTP: `http://localhost:5233`
- HTTPS: `https://localhost:7179`

A backend adja az API vegpontokat a webes feluletnek es a WPF admin alkalmazasnak is.

## Frontend indítása

A felhasznaloi webes felulet projektje:

```text
Frontend-ASP
```

Itt erheto el az etlap, a kosar, a profil, a sajat rendelesek, valamint az informacios oldalak. A kosar es rendelés funkciokhoz be kell jelentkezni.

## WPF admin felület indítása

Az admin alkalmazas projektje:

```text
AdminFelulet-WPF
```

Inditas elott ellenorizd az alabbi fajlt:

```text
AdminFelulet-WPF/apiSettings.json
```

Ebben a `BaseUrl` ertekenek a backend cimet kell tartalmaznia, peldaul:

```json
{
  "BaseUrl": "http://localhost:5233/"
}
```

Ha a backend mas porton fut, ezt az erteket is at kell irni.

## Fő funkciók

### Webes felület

- etlap megtekintese
- kosar kezelese
- rendeles leadása
- sajat rendelesek megtekintese
- profil oldal
- kapcsolat es informacios oldalak

### Backend

- felhasznaloi API vegpontok
- admin API vegpontok
- kosar kezeles
- rendeles kezeles
- etlap es menu adatok kezelese
- hibajegy rogzitese

### WPF admin felület

- heti etlap szerkesztese
- etelek kezelese
- uj rendeles felvetele
- rendelesek keresese, modositasa es torlese

## Gyakori hibák

### Nem indul a WPF admin felület API kapcsolata

Ellenorizd, hogy fut-e a `Backend-ASP`, es az `apiSettings.json` fajlban jo port van-e megadva.

### Adatbázis kapcsolati hiba

Ellenorizd az `appsettings.json` fajlokban a connection stringeket, valamint azt, hogy fut-e a SQL Server es a MySQL/MariaDB szerver.

### MySQL script hiba az allergének táblánál

A projekt jelenlegi modellje az `allergens` tablaban `id`, `name` es `description` mezoket hasznal. Emiatt a lentebb szereplo scriptben nem szerepel `code` oszlophoz tartozo unique constraint.


