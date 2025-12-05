using Microsoft.AspNetCore.Mvc;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(DatabaseService databaseService, ILogger<CategoriesController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseCategory>>> GetAllCategories()
    {
        try
        {
            var categories = await _databaseService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, new { error = "Failed to retrieve categories", details = ex.Message });
        }
    }
}
