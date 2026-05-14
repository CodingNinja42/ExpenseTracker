using ExpenseTracker.Data;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Controllers;

public class TransactionController : Controller
{
    private readonly ApplicationDbContext _context;

    public TransactionController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Transaction
    public async Task<IActionResult> Index()
    {
        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .OrderByDescending(t => t.Date)
            .ToListAsync();
        return View(transactions);
    }

    // GET: Transaction/AddOrEdit
    // GET: Transaction/AddOrEdit/5
    public async Task<IActionResult> AddOrEdit(int id = 0)
    {
        PopulateCategories();

        if (id == 0)
            return View(new Transaction());
        else
            return View(await _context.Transactions.FindAsync(id));
    }

    // POST: Transaction/AddOrEdit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
    {
        if (ModelState.IsValid)
        {
            if (transaction.TransactionId == 0)
                _context.Add(transaction);
            else
                _context.Update(transaction);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        PopulateCategories();
        return View(transaction);
    }

    // POST: Transaction/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
            _context.Transactions.Remove(transaction);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [NonAction]
    public void PopulateCategories()
    {
        var categories = _context.Categories.ToList();
        Category defaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
        categories.Insert(0, defaultCategory);
        ViewBag.Categories = categories;
    }
}
