using Konyhatunder_Szolgalat_Vizsgaremek.Models;
using Microsoft.EntityFrameworkCore;
using LegacyMenuItem = Konyhatunder_Szolgalat_Vizsgaremek.Models.MenuItem;

namespace Backend_ASP.Services
{
    public static class LegacyPortalInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<VizsgaremekEtlapContext>();

            await context.Database.EnsureCreatedAsync();
            await SeedCategoriesAsync(context);
            await SeedRolesAsync(context);
            await SeedUsersAsync(context);
            await SeedUserRolesAsync(context);
            await SeedAllergensAsync(context);
            await SeedIngredientsAsync(context);
            await SeedFoodsAsync(context);
            await SeedFoodAllergensAsync(context);
            await SeedMenusAsync(context);
            await SeedMenuItemsAsync(context);
            await SeedRecipeItemsAsync(context);
            await SeedTicketTypesAsync(context);
            await SeedOrdersAsync(context);
            await SeedTicketsAsync(context);
            await SeedMenuAvailabilityAsync(context);
        }

        private static async Task SeedCategoriesAsync(VizsgaremekEtlapContext context)
        {
            foreach (var name in new[] { "Leves", "Főétel", "Köret", "Desszert", "Saláta", "Ital", "Feltét" })
            {
                if (!await context.Categories.AnyAsync(category => category.Name == name))
                {
                    context.Categories.Add(new Category { Name = name });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedRolesAsync(VizsgaremekEtlapContext context)
        {
            foreach (var name in new[] { "Admin", "Customer", "Kitchen" })
            {
                if (!await context.Roles.AnyAsync(role => role.Name == name))
                {
                    context.Roles.Add(new Role { Name = name });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedUsersAsync(VizsgaremekEtlapContext context)
        {
            var users = new[]
            {
                new User { FullName = "Admin Felhasználó", Email = "admin@admin.com", Phone = "+36201234567", Address = "Budapest, Admin utca 1.", PasswordHash = "seed-admin", IsActive = true },
                new User { FullName = "Teszt Elek", Email = "teszt.elek@example.com", Phone = "+36301234567", Address = "Budapest, Minta utca 12.", PasswordHash = "seed-customer", IsActive = true },
                new User { FullName = "Kiss Anna", Email = "kiss.anna@example.com", Phone = "+36701234567", Address = "Veszprém, Konyha tér 4.", PasswordHash = "seed-customer", IsActive = true },
                new User { FullName = "WPF-Admin", Email = "wpf-admin@local", Phone = "000000000", Address = "AdminFelulet", PasswordHash = "wpf-admin-system-user", IsActive = true }
            };

            foreach (var user in users)
            {
                if (!await context.Users.AnyAsync(existing => existing.Email == user.Email))
                {
                    context.Users.Add(user);
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedUserRolesAsync(VizsgaremekEtlapContext context)
        {
            var users = await context.Users.ToDictionaryAsync(user => user.Email);
            var roles = await context.Roles.ToDictionaryAsync(role => role.Name);
            var links = new[] { ("admin@admin.com", "Admin"), ("wpf-admin@local", "Admin"), ("teszt.elek@example.com", "Customer"), ("kiss.anna@example.com", "Customer"), ("admin@admin.com", "Kitchen") };

            foreach (var (email, roleName) in links)
            {
                if (!users.TryGetValue(email, out var user) || !roles.TryGetValue(roleName, out var role))
                {
                    continue;
                }

                if (!await context.UserRoles.AnyAsync(link => link.UserId == user.Id && link.RoleId == role.Id))
                {
                    context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedAllergensAsync(VizsgaremekEtlapContext context)
        {
            foreach (var name in new[] { "Glutén", "Tojás", "Tej", "Szója", "Diófélék", "Mustár", "Szezámmag" })
            {
                if (!await context.Allergens.AnyAsync(allergen => allergen.Name == name))
                {
                    context.Allergens.Add(new Allergen { Name = name });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedIngredientsAsync(VizsgaremekEtlapContext context)
        {
            foreach (var name in new[] { "Csirkemell", "Burgonya", "Rizs", "Paradicsom", "Túró", "Liszt", "Tojás", "Tej", "Uborka", "Lencse", "Paprika", "Tejföl" })
            {
                if (!await context.Ingredients.AnyAsync(ingredient => ingredient.Name == name))
                {
                    context.Ingredients.Add(new Ingredient { Name = name, BaseUnit = "db" });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedFoodsAsync(VizsgaremekEtlapContext context)
        {
            var categories = await context.Categories.ToDictionaryAsync(category => category.Name, category => category.Id);
            var foods = new[]
            {
                new Food { Name = "Húsleves", CategoryId = categories["Leves"], Kcal = 180 },
                new Food { Name = "Gulyásleves", CategoryId = categories["Leves"], Kcal = 240 },
                new Food { Name = "Zöldségleves", CategoryId = categories["Leves"], Kcal = 160 },
                new Food { Name = "Lencseleves", CategoryId = categories["Leves"], Kcal = 260 },
                new Food { Name = "Rántott csirkemell", CategoryId = categories["Főétel"], Kcal = 520 },
                new Food { Name = "Sült csirkecomb", CategoryId = categories["Főétel"], Kcal = 490 },
                new Food { Name = "Bakonyi sertésszelet", CategoryId = categories["Főétel"], Kcal = 610 },
                new Food { Name = "Rakott karfiol", CategoryId = categories["Főétel"], Kcal = 470 },
                new Food { Name = "Csirkepaprikás", CategoryId = categories["Főétel"], Kcal = 560 },
                new Food { Name = "Petrezselymes burgonya", CategoryId = categories["Köret"], Kcal = 230 },
                new Food { Name = "Párolt rizs", CategoryId = categories["Köret"], Kcal = 210 },
                new Food { Name = "Burgonyapüré", CategoryId = categories["Köret"], Kcal = 250 },
                new Food { Name = "Cézár saláta", CategoryId = categories["Saláta"], Kcal = 170 },
                new Food { Name = "Vegyes saláta", CategoryId = categories["Saláta"], Kcal = 140 },
                new Food { Name = "Káposztasaláta", CategoryId = categories["Saláta"], Kcal = 120 },
                new Food { Name = "Uborkasaláta", CategoryId = categories["Saláta"], Kcal = 90 },
                new Food { Name = "Túrógombóc", CategoryId = categories["Desszert"], Kcal = 380 },
                new Food { Name = "Mákos guba", CategoryId = categories["Desszert"], Kcal = 410 },
                new Food { Name = "Somlói galuska", CategoryId = categories["Desszert"], Kcal = 430 },
                new Food { Name = "Gesztenyepüré", CategoryId = categories["Desszert"], Kcal = 360 },
                new Food { Name = "Házi limonádé", CategoryId = categories["Ital"], Kcal = 120 },
                new Food { Name = "Pirított hagyma", CategoryId = categories["Feltét"], Kcal = 180 }
            };

            foreach (var food in foods)
            {
                if (!await context.Foods.AnyAsync(existing => existing.Name == food.Name))
                {
                    context.Foods.Add(food);
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedFoodAllergensAsync(VizsgaremekEtlapContext context)
        {
            var foods = await context.Foods.Include(food => food.Allergens).ToDictionaryAsync(food => food.Name);
            var allergens = await context.Allergens.ToDictionaryAsync(allergen => allergen.Name);
            var links = new Dictionary<string, string[]>
            {
                ["Rántott csirkemell"] = ["Glutén", "Tojás"],
                ["Burgonyapüré"] = ["Tej"],
                ["Túrógombóc"] = ["Glutén", "Tojás", "Tej"],
                ["Mákos guba"] = ["Glutén", "Tej"],
                ["Csirkepaprikás"] = ["Tej"],
                ["Cézár saláta"] = ["Tojás", "Mustár"]
            };

            foreach (var (foodName, allergenNames) in links)
            {
                if (!foods.TryGetValue(foodName, out var food))
                {
                    continue;
                }

                foreach (var allergenName in allergenNames)
                {
                    if (allergens.TryGetValue(allergenName, out var allergen) && food.Allergens.All(item => item.Id != allergen.Id))
                    {
                        food.Allergens.Add(allergen);
                    }
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedMenusAsync(VizsgaremekEtlapContext context)
        {
            var menus = new[]
            {
                new Menu { Code = "A", PriceFt = 1890, IsActive = true },
                new Menu { Code = "B", PriceFt = 2090, IsActive = true },
                new Menu { Code = "C", PriceFt = 2290, IsActive = true },
                new Menu { Code = "P", PriceFt = 990, IsActive = true }
            };

            foreach (var menu in menus)
            {
                if (!await context.Menus.AnyAsync(existing => existing.Code == menu.Code))
                {
                    context.Menus.Add(menu);
                }
            }

            foreach (var unsupportedMenu in await context.Menus.Where(menu => menu.Code == "D" || menu.Code == "E").ToListAsync())
            {
                unsupportedMenu.IsActive = false;
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedMenuItemsAsync(VizsgaremekEtlapContext context)
        {
            var foods = await context.Foods.ToDictionaryAsync(food => food.Name, food => food.Id);
            var menus = await context.Menus.ToDictionaryAsync(menu => menu.Code, menu => menu.Id);
            var items = new[] { ("A", "Húsleves", 1), ("A", "Rántott csirkemell", 2), ("A", "Petrezselymes burgonya", 3), ("B", "Gulyásleves", 1), ("B", "Bakonyi sertésszelet", 2), ("B", "Párolt rizs", 3), ("C", "Zöldségleves", 1), ("C", "Rakott karfiol", 2), ("C", "Vegyes saláta", 3), ("P", "Túrógombóc", 1) };

            foreach (var (menuCode, foodName, order) in items)
            {
                if (!menus.TryGetValue(menuCode, out var menuId) || !foods.TryGetValue(foodName, out var foodId))
                {
                    continue;
                }

                if (!await context.MenuItems.AnyAsync(item => item.MenuId == menuId && item.FoodId == foodId))
                {
                    context.MenuItems.Add(new LegacyMenuItem { MenuId = menuId, FoodId = foodId, CourseOrder = (byte)order });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedRecipeItemsAsync(VizsgaremekEtlapContext context)
        {
            var foods = await context.Foods.ToDictionaryAsync(food => food.Name, food => food.Id);
            var ingredients = await context.Ingredients.ToDictionaryAsync(ingredient => ingredient.Name, ingredient => ingredient.Id);
            var recipeItems = new[] { ("Rántott csirkemell", "Csirkemell", 1.00m), ("Rántott csirkemell", "Liszt", 1.00m), ("Rántott csirkemell", "Tojás", 1.00m), ("Burgonyapüré", "Burgonya", 1.00m), ("Burgonyapüré", "Tej", 1.00m), ("Csirkepaprikás", "Csirkemell", 1.00m), ("Csirkepaprikás", "Paprika", 1.00m), ("Csirkepaprikás", "Tejföl", 1.00m), ("Uborkasaláta", "Uborka", 1.00m), ("Lencseleves", "Lencse", 1.00m), ("Túrógombóc", "Túró", 1.00m), ("Túrógombóc", "Tojás", 1.00m) };

            foreach (var (foodName, ingredientName, amount) in recipeItems)
            {
                if (!foods.TryGetValue(foodName, out var foodId) || !ingredients.TryGetValue(ingredientName, out var ingredientId))
                {
                    continue;
                }

                if (!await context.RecipeItems.AnyAsync(item => item.FoodId == foodId && item.IngredientId == ingredientId))
                {
                    context.RecipeItems.Add(new RecipeItem { FoodId = foodId, IngredientId = ingredientId, Amount = amount, Unit = "db" });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedTicketTypesAsync(VizsgaremekEtlapContext context)
        {
            foreach (var name in new[] { "Hibabejelentés", "WPF-issue", "Szállítási kérdés", "Étlap és allergén" })
            {
                if (!await context.TicketTypes.AnyAsync(ticketType => ticketType.Name == name))
                {
                    context.TicketTypes.Add(new TicketType { Name = name });
                }
            }

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedOrdersAsync(VizsgaremekEtlapContext context)
        {
            if (await context.Orders.AnyAsync(order => order.Comment != null && order.Comment.StartsWith("Seed rendelés")))
            {
                return;
            }

            var users = await context.Users.ToDictionaryAsync(user => user.Email, user => user.Id);
            var menus = await context.Menus.ToDictionaryAsync(menu => menu.Code);
            var today = DateOnly.FromDateTime(DateTime.Today).AddDays(1).ToDateTime(TimeOnly.MinValue);

            if (!users.TryGetValue("teszt.elek@example.com", out var firstUserId) || !users.TryGetValue("kiss.anna@example.com", out var secondUserId) || !menus.TryGetValue("A", out var menuA) || !menus.TryGetValue("B", out var menuB) || !menus.TryGetValue("C", out var menuC))
            {
                return;
            }

            var firstOrder = new Order { UserId = firstUserId, OrderDate = today, Status = "placed", Comment = "Seed rendelés - webes kosár" };
            var secondOrder = new Order { UserId = secondUserId, OrderDate = today.AddDays(1), Status = "preparing", Comment = "Seed rendelés - admin ellenőrzés" };
            context.Orders.AddRange(firstOrder, secondOrder);
            await SaveSeedChangesAsync(context);

            context.OrderItems.AddRange(
                new OrderItem { OrderId = firstOrder.Id, MenuId = menuA.Id, Qty = 2, UnitPriceFt = menuA.PriceFt },
                new OrderItem { OrderId = firstOrder.Id, MenuId = menuB.Id, Qty = 1, UnitPriceFt = menuB.PriceFt },
                new OrderItem { OrderId = secondOrder.Id, MenuId = menuC.Id, Qty = 3, UnitPriceFt = menuC.PriceFt });

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedTicketsAsync(VizsgaremekEtlapContext context)
        {
            if (await context.Tickets.AnyAsync(ticket => ticket.Description.StartsWith("Seed hibajegy")))
            {
                return;
            }

            var users = await context.Users.ToDictionaryAsync(user => user.Email, user => user.Id);
            var ticketTypes = await context.TicketTypes.ToDictionaryAsync(ticketType => ticketType.Name, ticketType => ticketType.Id);

            if (!users.TryGetValue("teszt.elek@example.com", out var firstUserId) || !users.TryGetValue("kiss.anna@example.com", out var secondUserId) || !ticketTypes.TryGetValue("Hibabejelentés", out var issueTypeId) || !ticketTypes.TryGetValue("Szállítási kérdés", out var deliveryTypeId))
            {
                return;
            }

            context.Tickets.AddRange(
                new Ticket { UserId = firstUserId, TicketTypeId = issueTypeId, Description = "Seed hibajegy: hidegen érkezett az ebéd.", Status = "open" },
                new Ticket { UserId = secondUserId, TicketTypeId = deliveryTypeId, Description = "Seed hibajegy: pontosítás a szállítási címhez.", Status = "in_progress" });

            await SaveSeedChangesAsync(context);
        }

        private static async Task SeedMenuAvailabilityAsync(VizsgaremekEtlapContext context)
        {
            var menus = await context.Menus
                .Include(menu => menu.MenuItems)
                .Where(menu => menu.IsActive != false)
                .ToListAsync();
            if (menus.Count == 0)
            {
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var horizon = today.AddDays(14);

            for (var date = today; date <= horizon; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                var dailyMenu = await context.DailyMenus.Include(item => item.MenuAvailabilities).FirstOrDefaultAsync(item => item.MenuDate == date);
                if (dailyMenu is null)
                {
                    dailyMenu = new DailyMenu { MenuDate = date, Note = "Automatikusan előkészített napi kínálat." };
                    context.DailyMenus.Add(dailyMenu);
                    await SaveSeedChangesAsync(context);
                    dailyMenu = await context.DailyMenus.Include(item => item.MenuAvailabilities).FirstOrDefaultAsync(item => item.MenuDate == date);
                    if (dailyMenu is null)
                    {
                        continue;
                    }
                }

                foreach (var menu in menus)
                {
                    var shouldBeAvailable = menu.Code != "P" || date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
                    var existingAvailability = await context.MenuAvailabilities.FindAsync(dailyMenu.Id, menu.Id);

                    if (!shouldBeAvailable)
                    {
                        if (existingAvailability is not null)
                        {
                            context.MenuAvailabilities.Remove(existingAvailability);
                        }

                        continue;
                    }

                    if (existingAvailability is null)
                    {
                        context.MenuAvailabilities.Add(new MenuAvailability { DailyMenuId = dailyMenu.Id, MenuId = menu.Id, MaxQty = 50 });
                    }
                }

                await SaveSeedChangesAsync(context);
            }
        }

        private static async Task SaveSeedChangesAsync(VizsgaremekEtlapContext context)
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                context.ChangeTracker.Clear();
            }
        }
    }
}
