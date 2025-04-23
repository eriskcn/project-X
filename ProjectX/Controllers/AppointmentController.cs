using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Models;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/appointments")]
[Authorize]
public class AppointmentController(ApplicationDbContext context) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Business, FreelanceRecruiter", Policy = "RecruiterVerifiedOnly")]
    public async Task<IActionResult> CreateAppointment([FromBody] AppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return BadRequest(new { Message = "User ID not found in claims." });
        }

        var application = await context.Applications
            .Include(a => a.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .SingleOrDefaultAsync(a => a.Id == request.ApplicationId && a.Status != ApplicationStatus.Draft);

        if (application == null)
        {
            return NotFound(new { Message = "Application not found." });
        }

        if (application.Job.Campaign.RecruiterId != Guid.Parse(userId))
        {
            return Forbid("You are not authorized to create an appointment for this application.");
        }

        if (application.Process != ApplicationProcess.Interviewing)
        {
            return BadRequest(new
                { Message = "You can only create an appointment for applications in the Interviewing process." });
        }

        var appointment = new Appointment
        {
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ApplicationId = request.ApplicationId,
            Note = request.Note,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Appointment created successfully.", AppointmentId = appointment.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentResponse>> GetAppointment(Guid id)
    {
        // Early validation of user ID
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return BadRequest(new { Message = "Invalid or missing user ID in claims." });
        }

        // Optimized query with minimal Includes
        var appointment = await context.Appointments
            .AsNoTracking()
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .Include(a => a.Application)
            .ThenInclude(app => app.Candidate)
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.Major)
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.Location)
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.Skills)
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.ContractTypes)
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.JobLevels)
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.JobTypes)
            .Select(a => new
            {
                Appointment = a,
                JobDescription = context.AttachedFiles
                    .FirstOrDefault(f => f.Type == TargetType.JobDescription && f.TargetId == a.Application.Job.Id),
                Resume = context.AttachedFiles
                    .FirstOrDefault(f => f.Type == TargetType.Application && f.TargetId == a.Application.Id)
            })
            .FirstOrDefaultAsync(a => a.Appointment.Id == id);

        if (appointment?.Appointment == null)
        {
            return NotFound(new { Message = "Appointment not found." });
        }

        var app = appointment.Appointment;

        // Authorization check
        if (app.Application.Job.Campaign.RecruiterId != userId && app.Application.CandidateId != userId)
        {
            return Forbid("You are not authorized to view this appointment.");
        }

        var isRecruiter = app.Application.CandidateId != userId;

        // Map to response (consider using AutoMapper for larger projects)
        var response = new AppointmentResponse
        {
            Id = app.Id,
            StartTime = app.StartTime,
            EndTime = app.EndTime,
            Application = new ApplicationResponseForAppointment
            {
                Id = app.Application.Id,
                FullName = app.Application.FullName,
                Email = app.Application.Email,
                PhoneNumber = app.Application.PhoneNumber,
                Introduction = app.Application.Introduction,
                Job = new JobResponseForAppointment
                {
                    Id = app.Application.Job.Id,
                    Title = app.Application.Job.Title,
                    Description = app.Application.Job.Description,
                    Quantity = app.Application.Job.Quantity,
                    OfficeAddress = app.Application.Job.OfficeAddress,
                    EducationLevelRequire = app.Application.Job.EducationLevelRequire,
                    YearOfExperience = app.Application.Job.YearOfExperience,
                    MinSalary = app.Application.Job.MinSalary,
                    MaxSalary = app.Application.Job.MaxSalary,
                    Major = new MajorResponse
                    {
                        Id = app.Application.Job.Major.Id,
                        Name = app.Application.Job.Major.Name
                    },
                    Location = new LocationResponse
                    {
                        Id = app.Application.Job.Location.Id,
                        Name = app.Application.Job.Location.Name,
                        Region = app.Application.Job.Location.Region
                    },
                    JobDescription = appointment.JobDescription != null
                        ? new FileResponse
                        {
                            Id = appointment.JobDescription.Id,
                            TargetId = appointment.JobDescription.TargetId,
                            Name = appointment.JobDescription.Name,
                            Path = appointment.JobDescription.Path,
                            Uploaded = appointment.JobDescription.Uploaded
                        }
                        : null,
                    Skills = app.Application.Job.Skills.Select(s => new SkillResponse
                    {
                        Id = s.Id,
                        Name = s.Name
                    }).ToList(),
                    ContractTypes = app.Application.Job.ContractTypes.Select(ct => new ContractTypeResponse
                    {
                        Id = ct.Id,
                        Name = ct.Name
                    }).ToList(),
                    JobLevels = app.Application.Job.JobLevels.Select(jl => new JobLevelResponse
                    {
                        Id = jl.Id,
                        Name = jl.Name
                    }).ToList(),
                    JobTypes = app.Application.Job.JobTypes.Select(jt => new JobTypeResponse
                    {
                        Id = jt.Id,
                        Name = jt.Name
                    }).ToList(),
                    Created = app.Application.Job.Created,
                    Modified = app.Application.Job.Modified
                },
                Resume = appointment.Resume != null
                    ? new FileResponse
                    {
                        Id = appointment.Resume.Id,
                        TargetId = appointment.Resume.TargetId,
                        Name = appointment.Resume.Name,
                        Path = appointment.Resume.Path,
                        Uploaded = appointment.Resume.Uploaded
                    }
                    : null,
                Status = app.Application.Status,
                Process = app.Application.Process,
                Applied = app.Application.Applied,
                Created = app.Application.Created,
                Modified = app.Application.Modified
            },
            Participant = new UserResponse
            {
                Id = isRecruiter ? app.Application.Candidate.Id : app.Application.Job.Campaign.Recruiter.Id,
                Name = isRecruiter
                    ? app.Application.Candidate.FullName
                    : app.Application.Job.Campaign.Recruiter.CompanyDetail?.CompanyName
                      ?? app.Application.Job.Campaign.Recruiter.FullName,
                ProfilePicture = isRecruiter
                    ? app.Application.Candidate.ProfilePicture
                    : app.Application.Job.Campaign.Recruiter.CompanyDetail?.Logo
                      ?? app.Application.Job.Campaign.Recruiter.ProfilePicture
            },
            Note = app.Note,
            Created = app.Created
        };

        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentShortResponse>>> GetOwnAppointments(
        [FromQuery] string? search,
        [FromQuery] bool thisWeek = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return BadRequest(new { Message = "Invalid or missing user ID in claims." });
        }

        var query = context.Appointments
            .AsNoTracking()
            .Include(a => a.Application)
            .ThenInclude(app => app.Job)
            .ThenInclude(j => j.Campaign)
            .ThenInclude(c => c.Recruiter)
            .ThenInclude(r => r.CompanyDetail)
            .Include(a => a.Application)
            .ThenInclude(app => app.Candidate)
            .Where(a => a.Application.CandidateId == userId || a.Application.Job.Campaign.RecruiterId == userId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a => a.Application.FullName.Contains(search) ||
                                     a.Application.Job.Title.Contains(search));
        }

        if (thisWeek)
        {
            var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);
            query = query.Where(a => a.StartTime >= startOfWeek && a.StartTime < endOfWeek);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var appointments = await query
            .OrderBy(a => a.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AppointmentShortResponse
            {
                Id = a.Id,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Participant = new UserResponse
                {
                    Id = a.Application.CandidateId != userId
                        ? a.Application.Candidate.Id
                        : a.Application.Job.Campaign.Recruiter.Id,
                    Name = a.Application.CandidateId != userId
                        ? a.Application.Candidate.FullName
                        : a.Application.Job.Campaign.Recruiter.CompanyDetail != null
                            ? a.Application.Job.Campaign.Recruiter.CompanyDetail!.CompanyName
                            : a.Application.Job.Campaign.Recruiter.FullName,
                    ProfilePicture = a.Application.CandidateId != userId
                        ? a.Application.Candidate.ProfilePicture
                        : a.Application.Job.Campaign.Recruiter.CompanyDetail != null
                            ? a.Application.Job.Campaign.Recruiter.CompanyDetail!.Logo
                            : a.Application.Job.Campaign.Recruiter.ProfilePicture
                },
                Note = a.Note,
                Created = a.Created
            })
            .ToListAsync();

        return Ok(new
        {
            Items = appointments,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }
}