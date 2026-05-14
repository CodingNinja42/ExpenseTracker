using System.Diagnostics;
using System.Globalization;
using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Last 7 days
        DateTime startDate = DateTime.Today.AddDays(-6);
        DateTime endDate = DateTime.Today;

        List<Transaction> selectedTransactions = await _context.Transactions
            .Include(x => x.Category)
            .Where(y => y.Date >= startDate && y.Date <= endDate)
            .ToListAsync();

        // Total Income
        int totalIncome = selectedTransactions
            .Where(i => i.Category!.Type == "Income")
            .Sum(j => j.Amount);
        ViewBag.TotalIncome = totalIncome.ToString("C0", CultureInfo.CreateSpecificCulture("en-IN"));

        // Total Expense
        int totalExpense = selectedTransactions
            .Where(i => i.Category!.Type == "Expense")
            .Sum(j => j.Amount);
        ViewBag.TotalExpense = totalExpense.ToString("C0", CultureInfo.CreateSpecificCulture("en-IN"));

        // Balance
        int balance = totalIncome - totalExpense;
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-IN");
        culture.NumberFormat.CurrencyNegativePattern = 1;
        ViewBag.Balance = balance.ToString("C0", culture);

        // Doughnut Chart - Expense By Category
        ViewBag.DoughnutChartData = selectedTransactions
            .Where(i => i.Category!.Type == "Expense")
            .GroupBy(j => j.Category!.CategoryId)
            .Select(k => new
            {
                categoryTitleWithIcon = k.First().Category!.Icon + " " + k.First().Category!.Title,
                amount = k.Sum(j => j.Amount),
                formattedAmount = k.Sum(j => j.Amount).ToString("C0", CultureInfo.CreateSpecificCulture("en-IN")),
            })
            .OrderByDescending(l => l.amount)
            .ToList();

        // Spline Chart - Income vs Expense (last 7 days)
        // Income
        List<SplineChartData> incomeSummary = selectedTransactions
            .Where(i => i.Category!.Type == "Income")
            .GroupBy(j => j.Date)
            .Select(k => new SplineChartData()
            {
                day = k.First().Date.ToString("dd-MMM"),
                income = k.Sum(l => l.Amount)
            })
            .ToList();

        // Expense
        List<SplineChartData> expenseSummary = selectedTransactions
            .Where(i => i.Category!.Type == "Expense")
            .GroupBy(j => j.Date)
            .Select(k => new SplineChartData()
            {
                day = k.First().Date.ToString("dd-MMM"),
                expense = k.Sum(l => l.Amount)
            })
            .ToList();

        // Combine Income & Expense
        string[] last7Days = Enumerable.Range(0, 7)
            .Select(i => startDate.AddDays(i).ToString("dd-MMM"))
            .ToArray();

        ViewBag.SplineChartData = from day in last7Days
                                  join income in incomeSummary on day equals income.day into dayIncomeJoined
                                  from income in dayIncomeJoined.DefaultIfEmpty()
                                  join expense in expenseSummary on day equals expense.day into expenseJoined
                                  from expense in expenseJoined.DefaultIfEmpty()
                                  select new
                                  {
                                      day = day,
                                      income = income == null ? 0 : income.income,
                                      expense = expense == null ? 0 : expense.expense,
                                  };

        // Recent Transactions
        ViewBag.RecentTransactions = await _context.Transactions
            .Include(i => i.Category)
            .OrderByDescending(j => j.Date)
            .Take(5)
            .ToListAsync();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class SplineChartData
{
    public string day { get; set; } = "";
    public int income { get; set; }
    public int expense { get; set; }
}
