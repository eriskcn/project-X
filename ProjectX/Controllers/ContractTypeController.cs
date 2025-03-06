using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[Authorize]
[ApiController]
[Route("capablanca/api/v0/contract-types")]
public class ContractTypeController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractTypeResponse>>> GetContractTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.ContractTypes.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(contractType => contractType.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var contractTypes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(contractType => new ContractTypeResponse
            {
                Id = contractType.Id,
                Name = contractType.Name
            })
            .ToListAsync();

        var response = new
        {
            Items = contractTypes,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContractTypeResponse>> GetContractType(Guid id)
    {
        var contractType = await context.ContractTypes.FindAsync(id);
        if (contractType == null)
        {
            return NotFound(new { Message = "Contract type not found." });
        }

        var response = new ContractTypeResponse
        {
            Id = contractType.Id,
            Name = contractType.Name
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ContractTypeResponse>> CreateContractType([FromBody] ContractTypeRequest request)
    {
        var contractType = new ContractType
        {
            Name = request.Name
        };

        context.ContractTypes.Add(contractType);
        await context.SaveChangesAsync();

        var response = new ContractTypeResponse
        {
            Id = contractType.Id,
            Name = contractType.Name
        };

        return CreatedAtAction(nameof(GetContractType), new { id = contractType.Id }, response);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContractTypeResponse>> UpdateContractType(
        Guid id, [FromBody] UpdateContractTypeRequest request)
    {
        var contractType = await context.ContractTypes.FindAsync(id);
        if (contractType == null)
        {
            return NotFound(new { Message = "Contract type not found." });
        }

        contractType.Name = request.Name;
        await context.SaveChangesAsync();

        var response = new ContractTypeResponse
        {
            Id = contractType.Id,
            Name = contractType.Name
        };

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteContractType(Guid id)
    {
        var contractType = await context.ContractTypes.FindAsync(id);
        if (contractType == null)
        {
            return NotFound(new { Message = "Contract type not found." });
        }

        context.ContractTypes.Remove(contractType);
        await context.SaveChangesAsync();

        return Ok("Delete contract type successfully");
    }

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ContractTypeResponse>>> GetDeletedContractTypes(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var query = context.ContractTypes.IgnoreSoftDelete()
            .Where(contractType => contractType.IsDeleted);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(contractType => contractType.Name.Contains(search));
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var contractTypes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(contractType => new ContractTypeResponse
            {
                Id = contractType.Id,
                Name = contractType.Name
            })
            .ToListAsync();

        var response = new
        {
            TotalItems = totalItems,
            TotalPages = totalPages,
            PageNumber = page,
            PageSize = pageSize,
            Items = contractTypes
        };

        return Ok(response);
    }

    [HttpPatch("restore/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContractTypeResponse>> RestoreContractType(Guid id)
    {
        var contractType = await context.ContractTypes.IgnoreSoftDelete()
            .FirstOrDefaultAsync(contractType => contractType.Id == id);
        if (contractType == null)
        {
            return NotFound(new { Message = "Contract type not found." });
        }

        contractType.IsDeleted = false;
        await context.SaveChangesAsync();

        var response = new ContractTypeResponse
        {
            Id = contractType.Id,
            Name = contractType.Name
        };

        return Ok(response);
    }
}