# Konyhatunder Szolgalat Vizsgaremek

Ez a projekt egy etelrendelesi es adminisztracios rendszer. Harom fo reszbol all:

- `Backend-ASP`: ASP.NET Core backend API.
- `Frontend-ASP`: Razor Pages alapu webes felulet a felhasznaloknak.
- `AdminFelulet-WPF`: WPF alapu admin felulet etlap, etelek es rendelesek kezelesere.

## Szükséges programok

- Windows operacios rendszer
- Visual Studio 2022 vagy ujabb
- .NET 9 SDK
- SQL Server vagy SQL Server LocalDB az ASP.NET Identity adatbazishoz
- MySQL vagy MariaDB a `vizsgaremek_etlap` adatbazishoz

## Projekt megnyitása

1. Nyisd meg a `Konyhatunder-Szolgalat-Vizsgaremek.sln` fajlt Visual Studioban.
2. Allitsd be az adatbazis kapcsolatokat az `appsettings.json` fajlokban.
3. Hozd letre a MySQL/MariaDB adatbazist a README vegén talalhato SQL script segitsegevel.
4. Inditsd el eloszor a `Backend-ASP` projektet.
5. Ezutan indithato a `Frontend-ASP` webes felulet es az `AdminFelulet-WPF` admin alkalmazas.

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

## MySQL / MariaDB adatbázis script

```sql
-- Alap beállítás
CREATE DATABASE IF NOT EXISTS vizsgaremek_etlap
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_hungarian_ci;

USE vizsgaremek_etlap;

CREATE TABLE users (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  full_name VARCHAR(200) NOT NULL,
  email VARCHAR(255) NOT NULL,
  phone VARCHAR(30) NULL,
  address VARCHAR(140) NULL,
  password_hash VARCHAR(255) NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT uq_users_email UNIQUE (email)
) ENGINE=InnoDB;

CREATE TABLE roles (
  id SMALLINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(50) NOT NULL,
  CONSTRAINT uq_roles_name UNIQUE (name)
) ENGINE=InnoDB;

CREATE TABLE user_roles (
  user_id BIGINT UNSIGNED NOT NULL,
  role_id SMALLINT UNSIGNED NOT NULL,
  PRIMARY KEY (user_id, role_id),
  CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles(id) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE categories (
  id INT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(40) NOT NULL,
  CONSTRAINT uq_categories_name UNIQUE (name)
) ENGINE=InnoDB;

CREATE TABLE foods (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(200) NOT NULL,
  category_id INT UNSIGNED NOT NULL,

  kcal DECIMAL(7,2) NULL,
  protein_g DECIMAL(7,2) NULL,
  fat_g DECIMAL(7,2) NULL,
  carbs_g DECIMAL(7,2) NULL,
  salt_g DECIMAL(7,2) NULL,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT fk_foods_category FOREIGN KEY (category_id) REFERENCES categories(id) ON DELETE RESTRICT,
  INDEX ix_foods_category (category_id),
  INDEX ix_foods_name (name)
) ENGINE=InnoDB;

CREATE TABLE allergens (
  id SMALLINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(120) NOT NULL,
  description VARCHAR(255) NULL,

  CONSTRAINT uq_allergens_name UNIQUE (name)
) ENGINE=InnoDB;

CREATE TABLE food_allergens (
  food_id BIGINT UNSIGNED NOT NULL,
  allergen_id SMALLINT UNSIGNED NOT NULL,

  PRIMARY KEY (food_id, allergen_id),

  CONSTRAINT fk_food_allergen_food
    FOREIGN KEY (food_id)
    REFERENCES foods(id)
    ON DELETE CASCADE,

  CONSTRAINT fk_food_allergen_allergen
    FOREIGN KEY (allergen_id)
    REFERENCES allergens(id)
    ON DELETE RESTRICT,

  INDEX ix_food_allergens_allergen (allergen_id)
) ENGINE=InnoDB;

CREATE TABLE ingredients (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(120) NOT NULL,
  base_unit ENUM('g','kg','ml','l','db') NOT NULL,
  CONSTRAINT uq_ingredients_name UNIQUE (name)
) ENGINE=InnoDB;

CREATE TABLE recipe_items (
  food_id BIGINT UNSIGNED NOT NULL,
  ingredient_id BIGINT UNSIGNED NOT NULL,
  amount DECIMAL(10,3) NOT NULL,
  unit ENUM('g','kg','ml','l','db') NOT NULL,

  PRIMARY KEY (food_id, ingredient_id),
  CONSTRAINT fk_recipe_food FOREIGN KEY (food_id) REFERENCES foods(id) ON DELETE CASCADE,
  CONSTRAINT fk_recipe_ingredient FOREIGN KEY (ingredient_id) REFERENCES ingredients(id) ON DELETE RESTRICT,

  CHECK (amount > 0)
) ENGINE=InnoDB;

CREATE TABLE menus (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  code CHAR(1) NOT NULL,
  price_ft INT UNSIGNED NOT NULL,
  is_active TINYINT(1) NOT NULL DEFAULT 1,

  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT uq_menus_code UNIQUE (code),
  CHECK (price_ft > 0)
) ENGINE=InnoDB;

CREATE TABLE menu_items (
  menu_id BIGINT UNSIGNED NOT NULL,
  food_id BIGINT UNSIGNED NOT NULL,
  course_order TINYINT UNSIGNED NOT NULL,
  PRIMARY KEY (menu_id, food_id),

  CONSTRAINT fk_menu_items_menu FOREIGN KEY (menu_id) REFERENCES menus(id) ON DELETE CASCADE,
  CONSTRAINT fk_menu_items_food FOREIGN KEY (food_id) REFERENCES foods(id) ON DELETE RESTRICT,

  INDEX ix_menu_items_menu (menu_id),
  INDEX ix_menu_items_food (food_id),
  CHECK (course_order >= 1)
) ENGINE=InnoDB;

CREATE TABLE daily_menus (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  menu_date DATE NOT NULL,
  note VARCHAR(200) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT uq_daily_menus_date UNIQUE (menu_date)
) ENGINE=InnoDB;

CREATE TABLE menu_availability (
  daily_menu_id BIGINT UNSIGNED NOT NULL,
  menu_id BIGINT UNSIGNED NOT NULL,
  max_qty INT UNSIGNED NULL,
  PRIMARY KEY (daily_menu_id, menu_id),

  CONSTRAINT fk_menu_avail_daily FOREIGN KEY (daily_menu_id) REFERENCES daily_menus(id) ON DELETE CASCADE,
  CONSTRAINT fk_menu_avail_menu FOREIGN KEY (menu_id) REFERENCES menus(id) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE orders (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  order_date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  status ENUM('draft','placed','preparing','ready','cancelled') NOT NULL DEFAULT 'placed',
  comment VARCHAR(200) NULL,

  CONSTRAINT fk_orders_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
  INDEX ix_orders_user_date (user_id, order_date),
  INDEX ix_orders_status (status)
) ENGINE=InnoDB;

CREATE TABLE order_items (
  order_id BIGINT UNSIGNED NOT NULL,
  menu_id BIGINT UNSIGNED NOT NULL,
  qty INT UNSIGNED NOT NULL DEFAULT 1,
  unit_price_ft INT UNSIGNED NOT NULL,
  PRIMARY KEY (order_id, menu_id),

  CONSTRAINT fk_order_items_order FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
  CONSTRAINT fk_order_items_menu FOREIGN KEY (menu_id) REFERENCES menus(id) ON DELETE RESTRICT,

  CHECK (qty > 0),
  CHECK (unit_price_ft > 0)
) ENGINE=InnoDB;

CREATE TABLE ticket_types (
  id SMALLINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(80) NOT NULL,
  CONSTRAINT uq_ticket_types_name UNIQUE (name)
) ENGINE=InnoDB;

CREATE TABLE tickets (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  ticket_type_id SMALLINT UNSIGNED NOT NULL,
  description TEXT NOT NULL,
  status ENUM('open','in_progress','closed') NOT NULL DEFAULT 'open',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT fk_tickets_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
  CONSTRAINT fk_tickets_type FOREIGN KEY (ticket_type_id) REFERENCES ticket_types(id) ON DELETE RESTRICT,

  INDEX ix_tickets_user_status (user_id, status),
  INDEX ix_tickets_type_status (ticket_type_id, status)
) ENGINE=InnoDB;
```
