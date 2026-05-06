using Microsoft.EntityFrameworkCore;

namespace Konyhatunder_Szolgalat_Vizsgaremek.Models;

public partial class VizsgaremekEtlapContext : DbContext
{
    public VizsgaremekEtlapContext()
    {
    }

    public VizsgaremekEtlapContext(DbContextOptions<VizsgaremekEtlapContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Allergen> Allergens { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<DailyMenu> DailyMenus { get; set; }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Menu> Menus { get; set; }

    public virtual DbSet<MenuAvailability> MenuAvailabilities { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<RecipeItem> RecipeItems { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TicketType> TicketTypes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allergen>(entity =>
        {
            entity.ToTable("allergens");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
        });

        modelBuilder.Entity<DailyMenu>(entity =>
        {
            entity.ToTable("daily_menus");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.MenuDate).HasColumnName("menu_date");
            entity.Property(item => item.Note).HasColumnName("note");
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.ToTable("foods");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.CategoryId).HasColumnName("category_id");
            entity.Property(item => item.Kcal).HasColumnName("kcal");

            entity.HasOne(item => item.Category)
                .WithMany(item => item.Foods)
                .HasForeignKey(item => item.CategoryId);

            entity.HasMany(food => food.Allergens)
                .WithMany(allergen => allergen.Foods)
                .UsingEntity<Dictionary<string, object>>(
                    "FoodAllergen",
                    right => right.HasOne<Allergen>().WithMany().HasForeignKey("AllergenId"),
                    left => left.HasOne<Food>().WithMany().HasForeignKey("FoodId"),
                    join =>
                    {
                        join.ToTable("food_allergens");
                        join.HasKey("FoodId", "AllergenId");
                        join.Property<ulong>("FoodId").HasColumnName("food_id");
                        join.Property<ushort>("AllergenId").HasColumnName("allergen_id");
                    });
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.ToTable("ingredients");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.BaseUnit).HasColumnName("base_unit");
        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("menus");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Code).HasColumnName("code");
            entity.Property(item => item.PriceFt).HasColumnName("price_ft");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<MenuAvailability>(entity =>
        {
            entity.ToTable("menu_availability");
            entity.HasKey(item => new { item.DailyMenuId, item.MenuId });
            entity.Property(item => item.DailyMenuId).HasColumnName("daily_menu_id");
            entity.Property(item => item.MenuId).HasColumnName("menu_id");
            entity.Property(item => item.MaxQty).HasColumnName("max_qty");

            entity.HasOne(item => item.DailyMenu)
                .WithMany(item => item.MenuAvailabilities)
                .HasForeignKey(item => item.DailyMenuId);

            entity.HasOne(item => item.Menu)
                .WithMany(item => item.MenuAvailabilities)
                .HasForeignKey(item => item.MenuId);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.HasKey(item => new { item.MenuId, item.FoodId });
            entity.Property(item => item.MenuId).HasColumnName("menu_id");
            entity.Property(item => item.FoodId).HasColumnName("food_id");
            entity.Property(item => item.CourseOrder).HasColumnName("course_order");

            entity.HasOne(item => item.Menu)
                .WithMany(item => item.MenuItems)
                .HasForeignKey(item => item.MenuId);

            entity.HasOne(item => item.Food)
                .WithMany(item => item.MenuItems)
                .HasForeignKey(item => item.FoodId);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.OrderDate).HasColumnName("order_date");
            entity.Property(item => item.Status).HasColumnName("status");
            entity.Property(item => item.Comment).HasColumnName("comment");

            entity.HasOne(item => item.User)
                .WithMany(item => item.Orders)
                .HasForeignKey(item => item.UserId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(item => new { item.OrderId, item.MenuId });
            entity.Property(item => item.OrderId).HasColumnName("order_id");
            entity.Property(item => item.MenuId).HasColumnName("menu_id");
            entity.Property(item => item.Qty).HasColumnName("qty");
            entity.Property(item => item.UnitPriceFt).HasColumnName("unit_price_ft");

            entity.HasOne(item => item.Order)
                .WithMany(item => item.OrderItems)
                .HasForeignKey(item => item.OrderId);

            entity.HasOne(item => item.Menu)
                .WithMany(item => item.OrderItems)
                .HasForeignKey(item => item.MenuId);
        });

        modelBuilder.Entity<RecipeItem>(entity =>
        {
            entity.ToTable("recipe_items");
            entity.HasKey(item => new { item.FoodId, item.IngredientId });
            entity.Property(item => item.FoodId).HasColumnName("food_id");
            entity.Property(item => item.IngredientId).HasColumnName("ingredient_id");
            entity.Property(item => item.Amount).HasColumnName("amount");
            entity.Property(item => item.Unit).HasColumnName("unit");

            entity.HasOne(item => item.Food)
                .WithMany(item => item.RecipeItems)
                .HasForeignKey(item => item.FoodId);

            entity.HasOne(item => item.Ingredient)
                .WithMany(item => item.RecipeItems)
                .HasForeignKey(item => item.IngredientId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(item => new { item.UserId, item.RoleId });
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.RoleId).HasColumnName("role_id");

            entity.HasOne(item => item.User)
                .WithMany(item => item.UserRoles)
                .HasForeignKey(item => item.UserId);

            entity.HasOne(item => item.Role)
                .WithMany(item => item.UserRoles)
                .HasForeignKey(item => item.RoleId);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.UserId).HasColumnName("user_id");
            entity.Property(item => item.TicketTypeId).HasColumnName("ticket_type_id");
            entity.Property(item => item.Description).HasColumnName("description");
            entity.Property(item => item.Status).HasColumnName("status");

            entity.HasOne(item => item.User)
                .WithMany(item => item.Tickets)
                .HasForeignKey(item => item.UserId);

            entity.HasOne(item => item.TicketType)
                .WithMany(item => item.Tickets)
                .HasForeignKey(item => item.TicketTypeId);
        });

        modelBuilder.Entity<TicketType>(entity =>
        {
            entity.ToTable("ticket_types");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Name).HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.FullName).HasColumnName("full_name");
            entity.Property(item => item.Email).HasColumnName("email");
            entity.Property(item => item.Phone).HasColumnName("phone");
            entity.Property(item => item.Address).HasColumnName("address");
            entity.Property(item => item.PasswordHash).HasColumnName("password_hash");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
