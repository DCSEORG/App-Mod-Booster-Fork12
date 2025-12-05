using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly ILogger<ApproveExpensesModel> _logger;
    private readonly DatabaseService _databaseService;

    public List<Expense> PendingExpenses { get; set; } = new();

    public ApproveExpensesModel(ILogger<ApproveExpensesModel> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }

    public async Task OnGetAsync()
    {
        PendingExpenses = await _databaseService.GetPendingExpensesAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        try
        {
            await _databaseService.ApproveExpenseAsync(expenseId, 2); // Bob Manager
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        try
        {
            await _databaseService.RejectExpenseAsync(expenseId, 2); // Bob Manager
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            return Page();
        }
    }
}
