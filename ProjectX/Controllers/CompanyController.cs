using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/companies")]
public class CompanyController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetCompanyProfiles([FromQuery] string? search,
        [FromQuery] bool topCompanyOnly,
        [FromQuery] bool eliteOnly,
        [FromQuery] Guid? majorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize < 0)
            return BadRequest("Page and pageSize must be greater than 0.");

        var query = context.CompanyDetails
            .AsNoTracking()
            .Include(c => c.Majors)
            .Where(c => c.Status == VerifyStatus.Verified);

        if (topCompanyOnly)
        {
            query = query.Where(c => c.AvgRatings >= 4);
        }

        if (eliteOnly)
        {
            query = query.Where(c => c.IsElite);
        }

        if (majorId.HasValue)
        {
            query = query.Where(c => c.Majors.Any(m => m.Id == majorId));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => EF.Functions.Like(c.CompanyName, $"%{search}%"));

        var totalItems = await query.CountAsync();

        if (pageSize > 0)
        {
            pageSize = Math.Min(pageSize, 100);
            query = query.Skip((page - 1) * pageSize).Take(pageSize);
        }

        var items = await query
            .OrderBy(c => c.CompanyName)
            .Select(c => new CompanyProfileResponse
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
                ShortName = c.ShortName,
                TaxCode = c.TaxCode,
                HeadQuarterAddress = c.HeadQuarterAddress,
                Logo = c.Logo,
                Cover = c.Cover,
                ContactEmail = c.ContactEmail,
                ContactPhone = c.ContactPhone,
                Website = c.Website,
                FoundedYear = c.FoundedYear,
                Size = c.Size,
                Introduction = c.Introduction,
                Location = new LocationResponse
                {
                    Id = c.Location.Id,
                    Name = c.Location.Name,
                    Region = c.Location.Region
                },
                Majors = c.Majors.Select(m => new MajorResponse
                {
                    Id = m.Id,
                    Name = m.Name
                }).ToList(),
                IsElite = c.IsElite,
                AvgRatings = c.AvgRatings
            })
            .ToListAsync();

        return Ok(new
        {
            Items = items,
            TotalItems = totalItems,
            First = page == 1,
            Last = pageSize == 0 || page * pageSize >= totalItems,
            PageNumber = page,
            PageSize = pageSize
        });
    }


    [HttpGet("{companyId:guid}")]
    public async Task<ActionResult> GetCompanyProfile(Guid companyId)
    {
        var company = await context.CompanyDetails
            .AsNoTracking()
            .Include(c => c.Majors)
            .Include(c => c.Location)
            .SingleOrDefaultAsync(c => c.Id == companyId
                                       && c.Status == VerifyStatus.Verified);

        if (company == null)
            return NotFound();

        var response = new CompanyProfileResponse
        {
            Id = company.Id,
            CompanyName = company.CompanyName,
            ShortName = company.ShortName,
            TaxCode = company.TaxCode,
            HeadQuarterAddress = company.HeadQuarterAddress,
            Logo = company.Logo,
            Cover = company.Cover,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Website = company.Website,
            FoundedYear = company.FoundedYear,
            Size = company.Size,
            Introduction = company.Introduction,
            Location = new LocationResponse
            {
                Id = company.Location.Id,
                Name = company.Location.Name,
                Region = company.Location.Region
            },
            Majors = company.Majors.Select(m => new MajorResponse
            {
                Id = m.Id,
                Name = m.Name
            }).ToList(),
            IsElite = company.IsElite,
            AvgRatings = company.AvgRatings
        };

        return Ok(response);
    }

    [HttpGet("{companyId:guid}/jobs")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<JobResponse>>> GetCompanyJobs([FromRoute] Guid companyId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Invalid page or pageSize." });
        }

        var query = context.Jobs
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Majors)
            .Include(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .ThenInclude(cd => cd!.Location)
            .Include(j => j.Major)
            .Include(j => j.Location)
            .Include(j => j.Skills)
            .Include(j => j.ContractTypes)
            .Include(j => j.JobLevels)
            .Include(j => j.JobTypes)
            .Where(j => j.Campaign.Recruiter.CompanyDetail!.Id == companyId
                        && j.Campaign.Recruiter.CompanyDetail != null
                        && j.Status == JobStatus.Active
                        && j.Campaign.Status == CampaignStatus.Opened
                        && j.StartDate <= DateTime.UtcNow
                        && j.EndDate >= DateTime.UtcNow)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(searchLower)
                                     || j.Description.ToLower().Contains(searchLower));
        }

        query = query.OrderByDescending(j => j.Created);
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var jobs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobResponse
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                OfficeAddress = j.OfficeAddress,
                Quantity = j.Quantity,
                Status = j.Status,
                EducationLevelRequire = j.EducationLevelRequire,
                YearOfExperience = j.YearOfExperience,
                MinSalary = j.MinSalary,
                MaxSalary = j.MaxSalary,
                IsHighlight = j.IsHighlight,
                IsHot = j.IsHot,
                IsUrgent = j.IsUrgent,
                StartDate = j.StartDate,
                EndDate = j.EndDate,
                Major = new MajorResponse
                {
                    Id = j.Major.Id,
                    Name = j.Major.Name
                },
                Location = new LocationResponse
                {
                    Id = j.Location.Id,
                    Name = j.Location.Name,
                    Region = j.Location.Region
                },
                JobDescription = context.AttachedFiles
                    .Where(f => f.Type == FileType.JobDescription && f.TargetId == j.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Path = f.Path,
                        TargetId = f.TargetId,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault(),
                Skills = j.Skills.Select(s => new SkillResponse
                {
                    Id = s.Id,
                    Name = s.Name
                }).ToList(),
                ContractTypes = j.ContractTypes.Select(ct => new ContractTypeResponse
                {
                    Id = ct.Id,
                    Name = ct.Name
                }).ToList(),
                JobLevels = j.JobLevels.Select(jl => new JobLevelResponse
                {
                    Id = jl.Id,
                    Name = jl.Name
                }).ToList(),
                JobTypes = j.JobTypes.Select(jt => new JobTypeResponse
                {
                    Id = jt.Id,
                    Name = jt.Name
                }).ToList(),
                Created = j.Created,
                Modified = j.Modified
            })
            .ToListAsync();

        return Ok(new
        {
            Items = jobs,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("self")]
    [Authorize(Roles = "Business", Policy = "RecruiterVerifiedOnly")]
    public async Task<ActionResult<CompanyProfileResponse>> GetOwnCompanyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return NotFound(new { Message = "User not found" });

        var user = await context.Users
            .Include(u => u.CompanyDetail)
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(userId));

        if (user?.CompanyDetail == null)
            return NotFound(new { Message = "User not found or you are not a company." });

        var company = await context.CompanyDetails
            .AsNoTracking()
            .Include(c => c.Majors)
            .Include(c => c.Location)
            .SingleOrDefaultAsync(c => c.Id == user.CompanyDetail.Id);

        if (company == null)
            return NotFound(new { Message = "Company not found." });

        var response = new CompanyProfileResponse
        {
            Id = company.Id,
            CompanyName = company.CompanyName,
            ShortName = company.ShortName,
            TaxCode = company.TaxCode,
            HeadQuarterAddress = company.HeadQuarterAddress,
            Logo = company.Logo,
            Cover = company.Cover,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Website = company.Website,
            FoundedYear = company.FoundedYear,
            Size = company.Size,
            Introduction = company.Introduction,
            Location = new LocationResponse
            {
                Id = company.Location.Id,
                Name = company.Location.Name,
                Region = company.Location.Region
            },
            Majors = company.Majors.Select(m => new MajorResponse
            {
                Id = m.Id,
                Name = m.Name
            }).ToList(),
            IsElite = company.IsElite,
            AvgRatings = company.AvgRatings
        };

        return Ok(response);
    }

    [HttpPatch("{companyId:guid}")]
    [Authorize(Roles = "Business", Policy = "RecruiterVerifiedOnly")]
    public async Task<ActionResult<CompanyProfileResponse>> UpdateCompanyProfile(
        [FromRoute] Guid companyId,
        [FromForm] CompanyProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return NotFound(new { Message = "User not found" });

        var user = await context.Users
            .Include(u => u.CompanyDetail)
            .SingleOrDefaultAsync(u =>
                u.Id == Guid.Parse(userId) && u.CompanyDetail != null && u.CompanyDetail.Id == companyId);

        if (user == null)
            return NotFound(new { Message = "User not found or you are not authorized to update this company." });

        var company = await context.CompanyDetails
            .Include(c => c.Majors)
            .Include(c => c.Location)
            .SingleOrDefaultAsync(c => c.Id == companyId);

        if (company == null)
            return NotFound();

        const long maxFileSize = 10 * 1024 * 1024;
        var allowedImageExtensions = new[] { ".png", ".jpg", ".jpeg" };

        if (request.Logo is { Length: > 0 })
        {
            if (request.Logo.Length > maxFileSize)
                return BadRequest("Logo file size must not exceed 10MB.");

            var ext = Path.GetExtension(request.Logo.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(ext))
                return BadRequest("Invalid logo image format.");

            var logoFolder = Path.Combine(env.WebRootPath, "logos");
            Directory.CreateDirectory(logoFolder);

            var logoFileName = $"{Guid.NewGuid()}{ext}";
            var logoPath = Path.Combine(logoFolder, logoFileName);

            await using (var stream = new FileStream(logoPath, FileMode.Create))
            {
                await request.Logo.CopyToAsync(stream);
            }

            company.Logo = PathHelper.GetRelativePathFromAbsolute(logoPath, env.WebRootPath);
        }

        if (request.Cover is { Length: > 0 })
        {
            if (request.Cover.Length > maxFileSize)
                return BadRequest("Cover file size must not exceed 10MB.");

            var ext = Path.GetExtension(request.Cover.FileName).ToLowerInvariant();
            if (!allowedImageExtensions.Contains(ext))
                return BadRequest("Invalid cover image format.");

            var coverFolder = Path.Combine(env.WebRootPath, "covers");
            Directory.CreateDirectory(coverFolder);

            var coverFileName = $"{Guid.NewGuid()}{ext}";
            var coverPath = Path.Combine(coverFolder, coverFileName);

            await using (var stream = new FileStream(coverPath, FileMode.Create))
            {
                await request.Cover.CopyToAsync(stream);
            }

            company.Cover = PathHelper.GetRelativePathFromAbsolute(coverPath, env.WebRootPath);
        }

        company.HeadQuarterAddress = request.HeadQuarterAddress ?? company.HeadQuarterAddress;
        company.ContactEmail = request.ContactEmail ?? company.ContactEmail;
        company.ContactPhone = request.ContactPhone ?? company.ContactPhone;
        company.Website = request.Website ?? company.Website;
        company.Introduction = request.Introduction ?? company.Introduction;
        company.LocationId = request.LocationId ?? company.LocationId;

        if (request.MajorIds.Count > 0)
        {
            var majors = await context.Majors
                .Where(m => request.MajorIds.Contains(m.Id))
                .ToListAsync();

            if (majors.Count != request.MajorIds.Count)
                return BadRequest("Some majors not found.");

            company.Majors.Clear();
            foreach (var major in majors)
                company.Majors.Add(major);
        }

        await context.SaveChangesAsync();

        var response = new CompanyProfileResponse
        {
            Id = company.Id,
            CompanyName = company.CompanyName,
            ShortName = company.ShortName,
            TaxCode = company.TaxCode,
            HeadQuarterAddress = company.HeadQuarterAddress,
            Logo = company.Logo,
            Cover = company.Cover,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Website = company.Website,
            FoundedYear = company.FoundedYear,
            Size = company.Size,
            Introduction = company.Introduction,
            Location = new LocationResponse
            {
                Id = company.Location.Id,
                Name = company.Location.Name,
                Region = company.Location.Region
            },
            Majors = company.Majors.Select(m => new MajorResponse
            {
                Id = m.Id,
                Name = m.Name
            }).ToList(),
            IsElite = company.IsElite,
            AvgRatings = company.AvgRatings
        };

        return Ok(response);
    }

    [HttpPost("{companyId:guid}/ratings")]
    [Authorize(Roles = "Candidate", Policy = "EmailConfirmed")]
    public async Task<ActionResult<RatingResponse>> RatingCompany(
        [FromRoute] Guid companyId,
        [FromBody] RatingRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return NotFound(new { Message = "User not found" });

        var company = await context.CompanyDetails
            .SingleOrDefaultAsync(c => c.Id == companyId);
        if (company == null)
            return NotFound(new { Message = "Company not found." });

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        if (user == null)
            return BadRequest(new { Message = "User not found." });

        var existingRating = await context.Ratings
            .AnyAsync(r => r.CompanyId == companyId && r.CandidateId == user.Id);
        if (existingRating)
            return BadRequest(new { Message = "You have already rated this company." });

        var rating = new Rating
        {
            CompanyId = companyId,
            CandidateId = user.Id,
            Point = request.Point,
            IsAnonymous = request.IsAnonymous,
            Comment = request.Comment,
            Created = DateTime.UtcNow
        };

        context.Ratings.Add(rating);
        await context.SaveChangesAsync();

        company.AvgRatings = await context.Ratings
            .Where(r => r.CompanyId == companyId)
            .AverageAsync(r => r.Point);
        await context.SaveChangesAsync();


        var response = new RatingResponse
        {
            Id = rating.Id,
            Point = rating.Point,
            Comment = rating.Comment,
            IsAnonymous = rating.IsAnonymous,
            Candidate = new UserResponse
            {
                Id = user.Id,
                Name = user.FullName,
                ProfilePicture = user.ProfilePicture
            },
            Created = rating.Created
        };

        return Ok(response);
    }


    [HttpGet("{companyId:guid}/ratings")]
    public async Task<ActionResult<object>> GetCompanyRatings(
        Guid companyId,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest("Page and pageSize must be greater than 0.");

        pageSize = Math.Min(pageSize, 100);

        var company = await context.CompanyDetails
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == companyId);

        if (company == null)
            return NotFound(new { Message = "Company not found." });

        var query = context.Ratings
            .AsNoTracking()
            .Include(r => r.Candidate)
            .Where(r => r.CompanyId == companyId);

        query = sort?.ToLower() switch
        {
            "point_asc" => query.OrderBy(r => r.Point),
            "point_desc" => query.OrderByDescending(r => r.Point),
            _ => query.OrderByDescending(r => r.Created)
        };

        var totalItems = await query.CountAsync();

        var ratings = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RatingResponse
            {
                Id = r.Id,
                Point = r.Point,
                Comment = r.Comment,
                IsAnonymous = r.IsAnonymous,
                Candidate = new UserResponse
                {
                    Id = r.IsAnonymous ? Guid.Empty : r.Candidate.Id,
                    Name = r.IsAnonymous ? "Một ứng viên nào đó" : r.Candidate.FullName,
                    ProfilePicture = r.IsAnonymous ? "/images/default-avatar.jpeg" : r.Candidate.ProfilePicture
                },
                Created = r.Created
            })
            .ToListAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return Ok(new
        {
            Items = ratings,
            TotalItems = totalItems,
            PageNumber = page,
            PageSize = pageSize,
            TotalPage = totalPages,
            First = page == 1,
            Last = page * pageSize >= totalItems
        });
    }
}