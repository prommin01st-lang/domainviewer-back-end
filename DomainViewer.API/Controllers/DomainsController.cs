using DomainViewer.API.Common;
using DomainViewer.API.DTOs;
using DomainViewer.Core.Entities;
using DomainViewer.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace DomainViewer.API.Controllers;

public class DomainsController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public DomainsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("export")]
    [Authorize]
    public async Task<IActionResult> ExportCsv()
    {
        var domains = await _context.Domains
            .Where(d => d.IsActive)
            .OrderBy(d => d.ExpirationDate)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Name,Registrar,Registrant,RegistrationDate,ExpirationDate,Description");

        foreach (var d in domains)
        {
            csv.AppendLine($"{EscapeCsv(d.Name)},{EscapeCsv(d.Registrar ?? "")},{EscapeCsv(d.Registrant ?? "")},{(d.RegistrationDate.HasValue ? d.RegistrationDate.Value.ToString("yyyy-MM-dd") : "")},{d.ExpirationDate:yyyy-MM-dd},{EscapeCsv(d.Description ?? "")}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"domains-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("import")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return ApiBadRequest("กรุณาเลือกไฟล์ CSV", ErrorCodes.ValidationFailed);

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var header = await reader.ReadLineAsync();
        if (header == null)
            return ApiBadRequest("ไฟล์ว่างเปล่า", ErrorCodes.ValidationFailed);

        var imported = 0;
        var errors = new List<string>();
        var lineNum = 1;
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line);
            if (parts.Length < 5)
            {
                errors.Add($"บรรทัด {lineNum}: ข้อมูลไม่ครบ");
                continue;
            }

            var name = parts[0].Trim();
            var registrar = parts[1].Trim();
            var registrant = parts[2].Trim();
            var regDateStr = parts[3].Trim();
            var expDateStr = parts[4].Trim();
            var description = parts.Length > 5 ? parts[5].Trim() : null;

            if (string.IsNullOrWhiteSpace(name) || !name.Contains("."))
            {
                errors.Add($"บรรทัด {lineNum}: ชื่อ Domain ไม่ถูกต้อง");
                continue;
            }

            DateTime? regDate = null;
            if (!string.IsNullOrWhiteSpace(regDateStr))
            {
                if (!DateTime.TryParse(regDateStr, out var rd))
                {
                    errors.Add($"บรรทัด {lineNum}: วันที่จดทะเบียนไม่ถูกต้อง");
                    continue;
                }
                regDate = rd;
            }

            if (!DateTime.TryParse(expDateStr, out var expDate))
            {
                errors.Add($"บรรทัด {lineNum}: วันหมดอายุไม่ถูกต้อง");
                continue;
            }

            var domain = new Domain
            {
                Name = name,
                Registrar = string.IsNullOrWhiteSpace(registrar) ? null : registrar,
                Registrant = string.IsNullOrWhiteSpace(registrant) ? null : registrant,
                RegistrationDate = regDate.HasValue ? DateTime.SpecifyKind(regDate.Value, DateTimeKind.Utc) : null,
                ExpirationDate = DateTime.SpecifyKind(expDate, DateTimeKind.Utc),
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Domains.Add(domain);
            imported++;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ApiOk(new { imported, errors }, $"นำเข้าสำเร็จ {imported} รายการ" + (errors.Count > 0 ? $" (ข้อผิดพลาด {errors.Count} รายการ)" : ""));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var sb = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetDomains(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "expirationDate",
        [FromQuery] string? sortOrder = "asc")
    {
        pageSize = Math.Min(pageSize, 100);

        var query = _context.Domains
            .Include(d => d.Creator)
            .Where(d => d.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(keyword));
        }

        query = sortBy?.ToLower() switch
        {
            "name" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            "created" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            _ => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(d => d.ExpirationDate) : query.OrderBy(d => d.ExpirationDate),
        };

        var projected = query.Select(d => new DomainResponse(
            d.Id,
            d.Name,
            d.Description,
            d.RegistrationDate,
            d.ExpirationDate,
            d.Registrant,
            d.Registrar,
            d.ImageUrl,
            d.IsActive,
            d.CreatedBy,
            d.Creator != null ? d.Creator.Name : null,
            d.CreatedAt,
            d.UpdatedAt,
            (d.ExpirationDate - DateTime.UtcNow).Days
        ));

        var pagedList = await PagedList<DomainResponse>.CreateAsync(projected, page, pageSize);
        return ApiOk(pagedList);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetDomain(Guid id)
    {
        var domain = await _context.Domains
            .Include(d => d.Creator)
            .Where(d => d.Id == id)
            .Select(d => new DomainResponse(
                d.Id,
                d.Name,
                d.Description,
                d.RegistrationDate,
                d.ExpirationDate,
                d.Registrant,
                d.Registrar,
                d.ImageUrl,
                d.IsActive,
                d.CreatedBy,
                d.Creator != null ? d.Creator.Name : null,
                d.CreatedAt,
                d.UpdatedAt,
                (d.ExpirationDate - DateTime.UtcNow).Days
            ))
            .FirstOrDefaultAsync();

        if (domain == null)
            return ApiNotFound("ไม่พบ Domain");

        return ApiOk(domain);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateDomain([FromBody] CreateDomainRequest request)
    {
        if (!ModelState.IsValid)
            return ApiBadRequest("ข้อมูลไม่ถูกต้อง", ErrorCodes.ValidationFailed);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var domain = new Domain
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            RegistrationDate = request.RegistrationDate.HasValue ? DateTime.SpecifyKind(request.RegistrationDate.Value, DateTimeKind.Utc) : null,
            ExpirationDate = DateTime.SpecifyKind(request.ExpirationDate, DateTimeKind.Utc),
            Registrant = request.Registrant?.Trim(),
            Registrar = request.Registrar?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            CreatedBy = string.IsNullOrEmpty(userId) ? Guid.Empty : Guid.Parse(userId),
            CreatedAt = DateTime.UtcNow
        };

        _context.Domains.Add(domain);
        await _context.SaveChangesAsync();

        return ApiOk(new DomainResponse(
            domain.Id,
            domain.Name,
            domain.Description,
            domain.RegistrationDate,
            domain.ExpirationDate,
            domain.Registrant,
            domain.Registrar,
            domain.ImageUrl,
            domain.IsActive,
            domain.CreatedBy,
            null,
            domain.CreatedAt,
            domain.UpdatedAt,
            (domain.ExpirationDate - DateTime.UtcNow).Days
        ), "สร้าง Domain สำเร็จ");
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateDomain(Guid id, [FromBody] UpdateDomainRequest request)
    {
        if (!ModelState.IsValid)
            return ApiBadRequest("ข้อมูลไม่ถูกต้อง", ErrorCodes.ValidationFailed);

        var domain = await _context.Domains.FindAsync(id);
        if (domain == null)
            return ApiNotFound("ไม่พบ Domain");

        domain.Name = request.Name.Trim();
        domain.Description = request.Description?.Trim();
        domain.RegistrationDate = request.RegistrationDate.HasValue ? DateTime.SpecifyKind(request.RegistrationDate.Value, DateTimeKind.Utc) : null;
        domain.ExpirationDate = DateTime.SpecifyKind(request.ExpirationDate, DateTimeKind.Utc);
        domain.Registrant = request.Registrant?.Trim();
        domain.Registrar = request.Registrar?.Trim();
        domain.ImageUrl = request.ImageUrl?.Trim();
        domain.IsActive = request.IsActive;
        domain.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ApiOk(new DomainResponse(
            domain.Id,
            domain.Name,
            domain.Description,
            domain.RegistrationDate,
            domain.ExpirationDate,
            domain.Registrant,
            domain.Registrar,
            domain.ImageUrl,
            domain.IsActive,
            domain.CreatedBy,
            null,
            domain.CreatedAt,
            domain.UpdatedAt,
            (domain.ExpirationDate - DateTime.UtcNow).Days
        ), "อัปเดต Domain สำเร็จ");
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> DeleteDomain(Guid id)
    {
        var domain = await _context.Domains.FindAsync(id);
        if (domain == null)
            return ApiNotFound("ไม่พบ Domain");

        // Soft delete
        domain.IsActive = false;
        domain.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return ApiOk("ลบ Domain สำเร็จ");
    }
}
