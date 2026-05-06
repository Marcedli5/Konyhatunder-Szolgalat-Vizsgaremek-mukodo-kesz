-- Schema variant that relies on ASP.NET Identity tables for users and roles.
-- If AspNetUsers.Id uses a different SQL type in your project, adjust orders.user_id
-- and tickets.user_id to match it before importing.

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE TABLE `allergens` (
  `id` smallint(5) UNSIGNED NOT NULL,
  `name` varchar(120) NOT NULL,
  `description` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `categories` (
  `id` int(10) UNSIGNED NOT NULL,
  `name` varchar(40) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `daily_menus` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `menu_date` date NOT NULL,
  `note` varchar(200) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `foods` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(200) NOT NULL,
  `category_id` int(10) UNSIGNED NOT NULL,
  `kcal` decimal(7,2) DEFAULT NULL,
  `protein_g` decimal(7,2) DEFAULT NULL,
  `fat_g` decimal(7,2) DEFAULT NULL,
  `carbs_g` decimal(7,2) DEFAULT NULL,
  `salt_g` decimal(7,2) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `food_allergens` (
  `food_id` bigint(20) UNSIGNED NOT NULL,
  `allergen_id` smallint(5) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `ingredients` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `name` varchar(120) NOT NULL,
  `base_unit` enum('g','kg','ml','l','db') NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `menus` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `code` char(1) NOT NULL,
  `price_ft` int(10) UNSIGNED NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `menu_availability` (
  `daily_menu_id` bigint(20) UNSIGNED NOT NULL,
  `menu_id` bigint(20) UNSIGNED NOT NULL,
  `max_qty` int(10) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `menu_items` (
  `menu_id` bigint(20) UNSIGNED NOT NULL,
  `food_id` bigint(20) UNSIGNED NOT NULL,
  `course_order` tinyint(3) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `orders` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` varchar(255) NOT NULL,
  `order_date` datetime NOT NULL DEFAULT current_timestamp(),
  `status` enum('draft','placed','preparing','ready','cancelled') NOT NULL DEFAULT 'placed',
  `comment` varchar(200) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `order_items` (
  `order_id` bigint(20) UNSIGNED NOT NULL,
  `menu_id` bigint(20) UNSIGNED NOT NULL,
  `qty` int(10) UNSIGNED NOT NULL DEFAULT 1,
  `unit_price_ft` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `recipe_items` (
  `food_id` bigint(20) UNSIGNED NOT NULL,
  `ingredient_id` bigint(20) UNSIGNED NOT NULL,
  `amount` decimal(10,3) NOT NULL,
  `unit` enum('g','kg','ml','l','db') NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `tickets` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `user_id` varchar(255) NOT NULL,
  `ticket_type_id` smallint(5) UNSIGNED NOT NULL,
  `description` text NOT NULL,
  `status` enum('open','in_progress','closed') NOT NULL DEFAULT 'open',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

CREATE TABLE `ticket_types` (
  `id` smallint(5) UNSIGNED NOT NULL,
  `name` varchar(80) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_hungarian_ci;

ALTER TABLE `allergens`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_allergens_name` (`name`);

ALTER TABLE `categories`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_categories_name` (`name`);

ALTER TABLE `daily_menus`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_daily_menus_date` (`menu_date`);

ALTER TABLE `foods`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_foods_category` (`category_id`),
  ADD KEY `ix_foods_name` (`name`);

ALTER TABLE `food_allergens`
  ADD PRIMARY KEY (`food_id`,`allergen_id`),
  ADD KEY `ix_food_allergens_allergen` (`allergen_id`);

ALTER TABLE `ingredients`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_ingredients_name` (`name`);

ALTER TABLE `menus`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_menus_code` (`code`);

ALTER TABLE `menu_availability`
  ADD PRIMARY KEY (`daily_menu_id`,`menu_id`),
  ADD KEY `fk_menu_avail_menu` (`menu_id`);

ALTER TABLE `menu_items`
  ADD PRIMARY KEY (`menu_id`,`food_id`),
  ADD KEY `ix_menu_items_menu` (`menu_id`),
  ADD KEY `ix_menu_items_food` (`food_id`);

ALTER TABLE `orders`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_orders_user_date` (`user_id`,`order_date`),
  ADD KEY `ix_orders_status` (`status`);

ALTER TABLE `order_items`
  ADD PRIMARY KEY (`order_id`,`menu_id`),
  ADD KEY `fk_order_items_menu` (`menu_id`);

ALTER TABLE `recipe_items`
  ADD PRIMARY KEY (`food_id`,`ingredient_id`),
  ADD KEY `fk_recipe_ingredient` (`ingredient_id`);

ALTER TABLE `tickets`
  ADD PRIMARY KEY (`id`),
  ADD KEY `ix_tickets_user_status` (`user_id`,`status`),
  ADD KEY `ix_tickets_type_status` (`ticket_type_id`,`status`);

ALTER TABLE `ticket_types`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_ticket_types_name` (`name`);

ALTER TABLE `allergens`
  MODIFY `id` smallint(5) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `categories`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `daily_menus`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `foods`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `ingredients`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `menus`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `orders`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `tickets`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `ticket_types`
  MODIFY `id` smallint(5) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `foods`
  ADD CONSTRAINT `fk_foods_category` FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`);

ALTER TABLE `food_allergens`
  ADD CONSTRAINT `fk_food_allergen_allergen` FOREIGN KEY (`allergen_id`) REFERENCES `allergens` (`id`),
  ADD CONSTRAINT `fk_food_allergen_food` FOREIGN KEY (`food_id`) REFERENCES `foods` (`id`) ON DELETE CASCADE;

ALTER TABLE `menu_availability`
  ADD CONSTRAINT `fk_menu_avail_daily` FOREIGN KEY (`daily_menu_id`) REFERENCES `daily_menus` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_menu_avail_menu` FOREIGN KEY (`menu_id`) REFERENCES `menus` (`id`);

ALTER TABLE `menu_items`
  ADD CONSTRAINT `fk_menu_items_food` FOREIGN KEY (`food_id`) REFERENCES `foods` (`id`),
  ADD CONSTRAINT `fk_menu_items_menu` FOREIGN KEY (`menu_id`) REFERENCES `menus` (`id`) ON DELETE CASCADE;

ALTER TABLE `orders`
  ADD CONSTRAINT `fk_orders_user` FOREIGN KEY (`user_id`) REFERENCES `AspNetUsers` (`Id`);

ALTER TABLE `order_items`
  ADD CONSTRAINT `fk_order_items_menu` FOREIGN KEY (`menu_id`) REFERENCES `menus` (`id`),
  ADD CONSTRAINT `fk_order_items_order` FOREIGN KEY (`order_id`) REFERENCES `orders` (`id`) ON DELETE CASCADE;

ALTER TABLE `recipe_items`
  ADD CONSTRAINT `fk_recipe_food` FOREIGN KEY (`food_id`) REFERENCES `foods` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_recipe_ingredient` FOREIGN KEY (`ingredient_id`) REFERENCES `ingredients` (`id`);

ALTER TABLE `tickets`
  ADD CONSTRAINT `fk_tickets_type` FOREIGN KEY (`ticket_type_id`) REFERENCES `ticket_types` (`id`),
  ADD CONSTRAINT `fk_tickets_user` FOREIGN KEY (`user_id`) REFERENCES `AspNetUsers` (`Id`);

COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
