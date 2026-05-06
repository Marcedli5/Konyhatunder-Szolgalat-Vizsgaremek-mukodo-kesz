namespace Konyhatunder_Szolgalat_Vizsgaremek.Models;

public partial class Allergen
{
    public ushort Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Food> Foods { get; set; } = [];
}

public partial class Category
{
    public uint Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Food> Foods { get; set; } = [];
}

public partial class DailyMenu
{
    public ulong Id { get; set; }

    public DateOnly MenuDate { get; set; }

    public string? Note { get; set; }

    public virtual ICollection<MenuAvailability> MenuAvailabilities { get; set; } = [];
}

public partial class Food
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public uint CategoryId { get; set; }

    public int? Kcal { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Allergen> Allergens { get; set; } = [];

    public virtual ICollection<MenuItem> MenuItems { get; set; } = [];

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = [];
}

public partial class Ingredient
{
    public ulong Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string BaseUnit { get; set; } = string.Empty;

    public virtual ICollection<RecipeItem> RecipeItems { get; set; } = [];
}

public partial class Menu
{
    public ulong Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public uint PriceFt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<MenuAvailability> MenuAvailabilities { get; set; } = [];

    public virtual ICollection<MenuItem> MenuItems { get; set; } = [];

    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
}

public partial class MenuAvailability
{
    public ulong DailyMenuId { get; set; }

    public ulong MenuId { get; set; }

    public uint? MaxQty { get; set; }

    public virtual DailyMenu DailyMenu { get; set; } = null!;

    public virtual Menu Menu { get; set; } = null!;
}

public partial class MenuItem
{
    public ulong MenuId { get; set; }

    public ulong FoodId { get; set; }

    public byte CourseOrder { get; set; }

    public virtual Food Food { get; set; } = null!;

    public virtual Menu Menu { get; set; } = null!;
}

public partial class Order
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public DateTime OrderDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
}

public partial class OrderItem
{
    public ulong OrderId { get; set; }

    public ulong MenuId { get; set; }

    public uint Qty { get; set; }

    public uint UnitPriceFt { get; set; }

    public virtual Menu Menu { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}

public partial class RecipeItem
{
    public ulong FoodId { get; set; }

    public ulong IngredientId { get; set; }

    public decimal Amount { get; set; }

    public string Unit { get; set; } = string.Empty;

    public virtual Food Food { get; set; } = null!;

    public virtual Ingredient Ingredient { get; set; } = null!;
}

public partial class Role
{
    public ushort Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}

public partial class UserRole
{
    public ulong UserId { get; set; }

    public ushort RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

public partial class Ticket
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public ushort TicketTypeId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public virtual TicketType TicketType { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}

public partial class TicketType
{
    public ushort Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Ticket> Tickets { get; set; } = [];
}

public partial class User
{
    public ulong Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public bool? IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = [];

    public virtual ICollection<Ticket> Tickets { get; set; } = [];

    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}
