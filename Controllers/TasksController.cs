using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

[Authorize] // Exige autenticação JWT
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        // Obtém o ID do usuário do token JWT
        var userId = int.Parse(User.FindFirst("userId")!.Value);

        var tasks = await _context.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = int.Parse(User.FindFirst("userId")!.Value);

        var task = new Task
        {
            Title = request.Title,
            IsCompleted = false,
            UserId = userId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return Ok(task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        task.Title = request.Title;
        task.IsCompleted = request.IsCompleted;

        await _context.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
            return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Tarefa excluída." });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        Console.WriteLine("Entrou no endpoint /api/auth/profile");
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null)
        {
            Console.WriteLine("Claim userId não encontrada!");
            return Unauthorized("Token inválido.");
        }
        var userId = int.Parse(userIdClaim.Value);
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Email })
            .FirstOrDefaultAsync();
        
        if (user == null)
            return NotFound("Usuário não encontrado.");
        
        return Ok(user);
    }
}

// DTOs
public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
}

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}