using Microsoft.AspNetCore.Mvc;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(DatabaseService databaseService, ILogger<UsersController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        try
        {
            var users = await _databaseService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { error = "Failed to retrieve users", details = ex.Message });
        }
    }
}
