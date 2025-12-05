using Microsoft.Data.SqlClient;
using Azure.Core;
using Azure.Identity;
using ExpenseManagementApp.Models;

namespace ExpenseManagementApp.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;
    private string? _errorMessage;

    public string? ErrorMessage => _errorMessage;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    private async Task<SqlConnection> GetConnectionAsync()
    {
        try
        {
            var connection = new SqlConnection(_connectionString);
            
            // Check if using Managed Identity authentication
            if (_connectionString.Contains("Authentication=Active Directory"))
            {
                // Use DefaultAzureCredential for managed identity (works in Azure)
                // or Azure CLI credential (works locally)
                var credential = new DefaultAzureCredential();
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://database.windows.net/.default" }));
                connection.AccessToken = token.Token;
            }
            
            await connection.OpenAsync();
            _errorMessage = null;
            return connection;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Database connection error at DatabaseService.GetConnectionAsync (Line 42): {ex.Message}";
            _logger.LogError(ex, "Failed to connect to database");
            throw;
        }
    }

    public async Task<List<Expense>> GetAllExpensesAsync(int? userId = null, int? statusId = null)
    {
        try
        {
            var expenses = new List<Expense>();
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_GetAllExpenses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            _errorMessage = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error retrieving expenses at DatabaseService.GetAllExpensesAsync (Line 71): {ex.Message}";
            _logger.LogError(ex, "Failed to get expenses");
            return new List<Expense>();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_GetExpenseById", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _errorMessage = null;
                return MapExpense(reader);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error retrieving expense at DatabaseService.GetExpenseByIdAsync (Line 101): {ex.Message}";
            _logger.LogError(ex, "Failed to get expense by id");
            return null;
        }
    }

    public async Task<List<Expense>> GetPendingExpensesAsync()
    {
        try
        {
            var expenses = new List<Expense>();
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_GetPendingExpenses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            _errorMessage = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error retrieving pending expenses at DatabaseService.GetPendingExpensesAsync (Line 131): {ex.Message}";
            _logger.LogError(ex, "Failed to get pending expenses");
            return new List<Expense>();
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_CreateExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.AmountGBP * 100));
            command.Parameters.AddWithValue("@Currency", "GBP");
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            _errorMessage = null;
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error creating expense at DatabaseService.CreateExpenseAsync (Line 164): {ex.Message}";
            _logger.LogError(ex, "Failed to create expense");
            throw;
        }
    }

    public async Task<int> UpdateExpenseAsync(int expenseId, UpdateExpenseRequest request)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_UpdateExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.AmountGBP * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _errorMessage = null;
                return reader.GetInt32(0);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error updating expense at DatabaseService.UpdateExpenseAsync (Line 199): {ex.Message}";
            _logger.LogError(ex, "Failed to update expense");
            throw;
        }
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_SubmitExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _errorMessage = null;
                return reader.GetInt32(0);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error submitting expense at DatabaseService.SubmitExpenseAsync (Line 229): {ex.Message}";
            _logger.LogError(ex, "Failed to submit expense");
            throw;
        }
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_ApproveExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _errorMessage = null;
                return reader.GetInt32(0);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error approving expense at DatabaseService.ApproveExpenseAsync (Line 259): {ex.Message}";
            _logger.LogError(ex, "Failed to approve expense");
            throw;
        }
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_RejectExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _errorMessage = null;
                return reader.GetInt32(0);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error rejecting expense at DatabaseService.RejectExpenseAsync (Line 289): {ex.Message}";
            _logger.LogError(ex, "Failed to reject expense");
            throw;
        }
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_DeleteExpense", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _errorMessage = null;
                return reader.GetInt32(0);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error deleting expense at DatabaseService.DeleteExpenseAsync (Line 319): {ex.Message}";
            _logger.LogError(ex, "Failed to delete expense");
            throw;
        }
    }

    public async Task<List<ExpenseCategory>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = new List<ExpenseCategory>();
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_GetAllCategories", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            
            _errorMessage = null;
            return categories;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error retrieving categories at DatabaseService.GetAllCategoriesAsync (Line 353): {ex.Message}";
            _logger.LogError(ex, "Failed to get categories");
            return new List<ExpenseCategory>();
        }
    }

    public async Task<List<ExpenseStatus>> GetAllStatusesAsync()
    {
        try
        {
            var statuses = new List<ExpenseStatus>();
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_GetAllStatuses", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }
            
            _errorMessage = null;
            return statuses;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error retrieving statuses at DatabaseService.GetAllStatusesAsync (Line 386): {ex.Message}";
            _logger.LogError(ex, "Failed to get statuses");
            return new List<ExpenseStatus>();
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var users = new List<User>();
            await using var connection = await GetConnectionAsync();
            await using var command = new SqlCommand("dbo.usp_GetAllUsers", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleName = reader.GetString(3),
                    ManagerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    ManagerName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsActive = reader.GetBoolean(6),
                    CreatedAt = reader.GetDateTime(7)
                });
            }
            
            _errorMessage = null;
            return users;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error retrieving users at DatabaseService.GetAllUsersAsync (Line 424): {ex.Message}";
            _logger.LogError(ex, "Failed to get users");
            return new List<User>();
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            AmountGBP = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedByName = reader.IsDBNull(reader.GetOrdinal("ReviewedByName")) ? null : reader.GetString(reader.GetOrdinal("ReviewedByName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
