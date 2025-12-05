using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class AddExpenseModel : PageModel
{
    private readonly ILogger<AddExpenseModel> _logger;
    private readonly DatabaseService _databaseService;

    [BindProperty]
    public CreateExpenseRequest ExpenseRequest { get; set; } = new();
    
    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public AddExpenseModel(ILogger<AddExpenseModel> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        Categories = await _databaseService.GetAllCategoriesAsync();
        ExpenseRequest.ExpenseDate = DateTime.Today;
        ExpenseRequest.UserId = 1; // Default to first user (Alice Example)
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            ExpenseRequest.UserId = 1; // Default to first user
            var expenseId = await _databaseService.CreateExpenseAsync(ExpenseRequest);
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = $"Error creating expense: {ex.Message}";
            Categories = await _databaseService.GetAllCategoriesAsync();
            return Page();
        }
    }
}
