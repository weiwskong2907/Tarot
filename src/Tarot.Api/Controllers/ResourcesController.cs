using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Tarot.Infrastructure.Data;
using System.Security.Claims;

namespace Tarot.Api.Controllers;

[Authorize(Roles = "SuperAdmin")]
[ApiController]
[Route("api/v1/resources")]
public class ResourcesController(IServiceProvider serviceProvider, AppDbContext dbContext) : ControllerBase
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AppDbContext _dbContext = dbContext;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly ConcurrentDictionary<string, Type> _resourceMap;

    static ResourcesController()
    {
        _resourceMap = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        var entityTypes = typeof(BaseEntity).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseEntity)) && !t.IsAbstract);
        
        foreach (var type in entityTypes)
        {
            // Simple pluralization convention: TypeName + "s"
            _resourceMap[(type.Name + "s").ToLowerInvariant()] = type;
        }
    }

    private object? GetRepository(Type entityType)
    {
        var repoType = typeof(IRepository<>).MakeGenericType(entityType);
        return _serviceProvider.GetService(repoType);
    }

    [HttpGet("schemas")]
    public IActionResult GetSchemas()
    {
        var schemas = _resourceMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != "Id" && p.Name != "CreatedAt" && p.Name != "UpdatedAt" && p.Name != "DeletedAt")
                .Select(p => new 
                { 
                    p.Name, 
                    Type = p.PropertyType.Name,
                    IsNullable = Nullable.GetUnderlyingType(p.PropertyType) != null || !p.PropertyType.IsValueType
                })
        );
        return Ok(schemas);
    }

    [HttpGet("{resource}")]
    public async Task<IActionResult> GetAll(string resource, [FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        if (!_resourceMap.TryGetValue(resource, out var type)) return NotFound("Resource not found");

        var repo = GetRepository(type);
        if (repo == null) return StatusCode(500, "Repository not found");

        // Use reflection to call ListAllReadOnlyAsync
        var method = repo.GetType().GetMethod("ListAllReadOnlyAsync", Type.EmptyTypes);
        if (method == null) return StatusCode(500, "Method not found");

        var task = (Task)method.Invoke(repo, null)!;
        await task.ConfigureAwait(false);
        
        var resultProperty = task.GetType().GetProperty("Result");
        var items = resultProperty?.GetValue(task);

        if (items is IEnumerable<object> enumerable)
        {
            items = enumerable.Skip(skip).Take(take);
        }

        return Ok(items);
    }

    [HttpGet("{resource}/{id:guid}")]
    public async Task<IActionResult> GetById(string resource, Guid id)
    {
        if (!_resourceMap.TryGetValue(resource, out var type)) return NotFound("Resource not found");

        var repo = GetRepository(type);
        if (repo == null) return StatusCode(500, "Repository not found");

        var method = repo.GetType().GetMethod("GetByIdAsync", [typeof(Guid)]);
        if (method == null) return StatusCode(500, "Method not found");

        var task = (Task)method.Invoke(repo, [id])!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var item = resultProperty?.GetValue(task);

        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{resource}")]
    public async Task<IActionResult> Create(string resource, [FromBody] JsonElement body)
    {
        if (!_resourceMap.TryGetValue(resource, out var type)) return NotFound("Resource not found");

        var repo = GetRepository(type);
        if (repo == null) return StatusCode(500, "Repository not found");

        object? entity;
        try
        {
            entity = JsonSerializer.Deserialize(body.GetRawText(), type, _jsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest("Invalid JSON format");
        }

        if (entity == null) return BadRequest("Empty body");

        // Validation
        var validationContext = new ValidationContext(entity);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(entity, validationContext, validationResults, true))
        {
            return BadRequest(validationResults.Select(r => r.ErrorMessage));
        }

        // Set BaseEntity properties
        if (entity is BaseEntity baseEntity)
        {
            if (baseEntity.Id == Guid.Empty) baseEntity.Id = Guid.NewGuid();
            baseEntity.CreatedAt = DateTimeOffset.UtcNow;
            baseEntity.UpdatedAt = null;
            baseEntity.DeletedAt = null;
        }

        var method = repo.GetType().GetMethod("AddAsync", [type]);
        if (method == null) return StatusCode(500, "Method not found");

        var task = (Task)method.Invoke(repo, [entity])!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        var created = resultProperty?.GetValue(task);

        await LogAuditAsync("Create", resource, created);

        return CreatedAtAction(nameof(GetById), new { resource, id = ((BaseEntity)created!).Id }, created);
    }

    [HttpPut("{resource}/{id:guid}")]
    public async Task<IActionResult> Update(string resource, Guid id, [FromBody] JsonElement body)
    {
        if (!_resourceMap.TryGetValue(resource, out var type)) return NotFound("Resource not found");

        var repo = GetRepository(type);
        if (repo == null) return StatusCode(500, "Repository not found");

        // Fetch existing
        var getMethod = repo.GetType().GetMethod("GetByIdAsync", [typeof(Guid)]);
        var getTask = (Task)getMethod!.Invoke(repo, [id])!;
        await getTask.ConfigureAwait(false);
        var existing = getTask.GetType().GetProperty("Result")?.GetValue(getTask);

        if (existing == null) return NotFound();

        // Deserialize update
        object? updateData;
        try
        {
            updateData = JsonSerializer.Deserialize(body.GetRawText(), type, _jsonOptions);
        }
        catch
        {
            return BadRequest("Invalid JSON");
        }

        if (updateData == null) return BadRequest();

        // Copy properties (simple mapper)
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite || prop.Name == "Id" || prop.Name == "CreatedAt") continue;
            
            var value = prop.GetValue(updateData);
            // Ideally we only update fields that were present in JSON, but standard Deserialize overwrites with nulls if missing.
            // For a robust generic patch, we need JsonPatch or checking JsonElement properties.
            // Here we assume PUT replaces the object (except ID/Created).
            // However, deserializing to 'type' will set missing fields to default.
            // This is standard PUT behavior.
            
            prop.SetValue(existing, value);
        }

        if (existing is BaseEntity be)
        {
            be.UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Validate again
        var validationContext = new ValidationContext(existing);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(existing, validationContext, validationResults, true))
        {
            return BadRequest(validationResults.Select(r => r.ErrorMessage));
        }

        var updateMethod = repo.GetType().GetMethod("UpdateAsync", [type]);
        await (Task)updateMethod!.Invoke(repo, [existing])!;

        await LogAuditAsync("Update", resource, existing);

        return Ok(existing);
    }

    [HttpDelete("{resource}/{id:guid}")]
    public async Task<IActionResult> Delete(string resource, Guid id)
    {
        if (!_resourceMap.TryGetValue(resource, out var type)) return NotFound("Resource not found");

        var repo = GetRepository(type);
        if (repo == null) return StatusCode(500, "Repository not found");

        var getMethod = repo.GetType().GetMethod("GetByIdAsync", [typeof(Guid)]);
        var getTask = (Task)getMethod!.Invoke(repo, [id])!;
        await getTask.ConfigureAwait(false);
        var existing = getTask.GetType().GetProperty("Result")?.GetValue(getTask);

        if (existing == null) return NotFound();

        var deleteMethod = repo.GetType().GetMethod("DeleteAsync", [type]);
        await (Task)deleteMethod!.Invoke(repo, [existing])!;

        await LogAuditAsync("Delete", resource, new { Id = id });

        return NoContent();
    }

    private async Task LogAuditAsync(string action, string resource, object? details)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var log = new AuditLog
            {
                ActorId = userId != null ? Guid.Parse(userId) : Guid.Empty,
                Action = $"Dynamic_{action}_{resource}",
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // Fail silent on audit log error to not break the main operation
        }
    }
}
