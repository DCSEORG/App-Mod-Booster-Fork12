using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly DatabaseService _databaseService;

    public List<Expense> Expenses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool UsingDummyData { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public IndexModel(ILogger<IndexModel> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Expenses = await _databaseService.GetAllExpensesAsync();
            
            // Apply filter if provided
            if (!string.IsNullOrWhiteSpace(Filter))
            {
                Expenses = Expenses.Where(e =>
                    e.CategoryName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true ||
                    e.StatusName?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true ||
                    e.Description?.Contains(Filter, StringComparison.OrdinalIgnoreCase) == true
                ).ToList();
            }
            
            ErrorMessage = _databaseService.ErrorMessage;
            UsingDummyData = !string.IsNullOrEmpty(ErrorMessage) && !Expenses.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            ErrorMessage = $"Error loading expenses: {ex.Message}";
            UsingDummyData = true;
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        try
        {
            await _databaseService.SubmitExpenseAsync(expenseId);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense");
            ErrorMessage = $"Error submitting expense: {ex.Message}";
            return Page();
        }
    }
}
