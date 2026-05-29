using BugraLife.Models;
using Microsoft.EntityFrameworkCore;

namespace BugraLife.DBContext
{
    public class BugraLifeDBContext:DbContext
    {
        public BugraLifeDBContext(DbContextOptions<BugraLifeDBContext> options) : base(options)
        {

        }

        public DbSet<WebSitePassword> WebSitePasswords { get; set; }
        public DbSet<WebSite> WebSites { get; set; }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Daily> Dailies { get; set; }
        public DbSet<Debtor> Debtors { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseType> ExpenseTypes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<PaymentType> PaymentTypes { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<IncomeType> IncomeTypes { get; set; }
        public DbSet<LoginUser> LoginUser { get; set; }
        public DbSet<PlannedToDo> PlannedToDos { get; set; }
        public DbSet<UnPlannedToDo> UnPlannedToDos { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Movement> Movements { get; set; }
        public DbSet<FixedExpense> FixedExpenses { get; set; }
        public DbSet<ActivityDefinition> ActivityDefinitions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<PracticalNote> PracticalNotes { get; set; }
        public DbSet<FileShared> FileShareds { get; set; }


    }
}
