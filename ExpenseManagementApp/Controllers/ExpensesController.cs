using Microsoft.AspNetCore.Mvc;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService databaseService, ILogger<ExpensesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by user or status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetAllExpenses(
        [FromQuery] int? userId = null,
        [FromQuery] int? statusId = null)
    {
        try
        {
            var expenses = await _databaseService.GetAllExpensesAsync(userId, statusId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses");
            return StatusCode(500, new { error = "Failed to retrieve expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpenseById(int id)
    {
        try
        {
            var expense = await _databaseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense");
            return StatusCode(500, new { error = "Failed to retrieve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all pending expenses awaiting approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<Expense>>> GetPendingExpenses()
    {
        try
        {
            var expenses = await _databaseService.GetPendingExpensesAsync();
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending expenses");
            return StatusCode(500, new { error = "Failed to retrieve pending expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var expenseId = await _databaseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetExpenseById), new { id = expenseId }, new { expenseId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = "Failed to create expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing expense
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            var rowsAffected = await _databaseService.UpdateExpenseAsync(id, request);
            if (rowsAffected == 0)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(new { message = "Expense updated successfully", rowsAffected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            return StatusCode(500, new { error = "Failed to update expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitExpense(int id)
    {
        try
        {
            var rowsAffected = await _databaseService.SubmitExpenseAsync(id);
            if (rowsAffected == 0)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(new { message = "Expense submitted successfully", rowsAffected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense");
            return StatusCode(500, new { error = "Failed to submit expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveExpense(int id, [FromBody] ReviewRequest request)
    {
        try
        {
            var rowsAffected = await _databaseService.ApproveExpenseAsync(id, request.ReviewedBy);
            if (rowsAffected == 0)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(new { message = "Expense approved successfully", rowsAffected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            return StatusCode(500, new { error = "Failed to approve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult> RejectExpense(int id, [FromBody] ReviewRequest request)
    {
        try
        {
            var rowsAffected = await _databaseService.RejectExpenseAsync(id, request.ReviewedBy);
            if (rowsAffected == 0)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(new { message = "Expense rejected successfully", rowsAffected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            return StatusCode(500, new { error = "Failed to reject expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete an expense
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        try
        {
            var rowsAffected = await _databaseService.DeleteExpenseAsync(id);
            if (rowsAffected == 0)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(new { message = "Expense deleted successfully", rowsAffected });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            return StatusCode(500, new { error = "Failed to delete expense", details = ex.Message });
        }
    }
}

public class ReviewRequest
{
    public int ReviewedBy { get; set; }
}
