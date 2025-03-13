using System.Security.Claims;
        using Microsoft.AspNetCore.Authorization;
        using Microsoft.AspNetCore.Mvc;
        using Microsoft.EntityFrameworkCore;
        using ProjectX.Data;
        using ProjectX.DTOs;
        using ProjectX.Models;
        
        namespace ProjectX.Controllers;
        
        [ApiController]
        [Route("capablanca/api/v0/applications")]
        [Authorize]
        public class ApplicationController(ApplicationDbContext context, IWebHostEnvironment env) : ControllerBase
        {
            [HttpPost]
            [Authorize(Roles = "Candidate")]
            public async Task<ActionResult<ApplicationResponse>> CreateApplication([FromBody] ApplicationRequest request)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User ID not found in access token." });
                }
        
                var application = new Application
                {
                    Id = Guid.NewGuid(),
                    CandidateId = Guid.Parse(userId),
                    JobId = request.JobId,
                    FullName = request.FullName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    Introduction = request.Introduction,
                    Process = ApplicationProcess.Pending,
                    Status = request.Status,
                    Created = DateTime.UtcNow
                };
        
                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
        
                var resumeFileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Resume.FileName)}";
                await using (var stream = new FileStream(Path.Combine(uploadsFolder, resumeFileName), FileMode.Create))
                {
                    await request.Resume.CopyToAsync(stream);
                }
        
                var resumeUrl = $"/uploads/{resumeFileName}";
        
                var resumeFile = new AttachedFile
                {
                    Name = resumeFileName,
                    Path = resumeUrl,
                    Type = TargetType.Application,
                    TargetId = application.Id,
                    UploadedById = Guid.Parse(userId)
                };
        
                context.Applications.Add(application);
                context.AttachedFiles.Add(resumeFile);
                await context.SaveChangesAsync();
        
                var resume = context.AttachedFiles
                    .Where(f => f.Type == TargetType.Application && f.TargetId == application.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded
                    })
                    .SingleOrDefault();
        
                if (resume == null)
                {
                    throw new InvalidOperationException();
                }
        
                var response = new ApplicationResponse
                {
                    Id = application.Id,
                    JobId = application.JobId,
                    FullName = application.FullName,
                    Email = application.Email,
                    PhoneNumber = application.PhoneNumber,
                    Resume = resume,
                    Introduction = application.Introduction,
                    Status = application.Status,
                    Created = application.Created
                };
        
                return CreatedAtAction(nameof(GetApplication), new { id = application.Id }, response);
            }
        
            [HttpGet]
            public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetApplications()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User ID not found in access token." });
                }
        
                var applications = await context.Applications
                    .Where(a => a.CandidateId == Guid.Parse(userId))
                    .Select(a => new ApplicationResponse
                    {
                        Id = a.Id,
                        JobId = a.JobId,
                        FullName = a.FullName,
                        Email = a.Email,
                        PhoneNumber = a.PhoneNumber,
                        Resume = context.AttachedFiles
                            .Where(f => f.Type == TargetType.Application && f.TargetId == a.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded
                            })
                            .SingleOrDefault(),
                        Introduction = a.Introduction,
                        Status = a.Status,
                        Created = a.Created
                    })
                    .ToListAsync();
                return Ok(applications);
            }
        
            [HttpGet("{id:guid}")]
            public async Task<ActionResult<ApplicationResponse>> GetApplication(Guid id)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User ID not found in access token." });
                }
        
                var application = await context.Applications
                    .Where(a => a.CandidateId == Guid.Parse(userId) && a.Id == id)
                    .Select(a => new ApplicationResponse
                    {
                        Id = a.Id,
                        JobId = a.JobId,
                        FullName = a.FullName,
                        Email = a.Email,
                        PhoneNumber = a.PhoneNumber,
                        Resume = context.AttachedFiles
                            .Where(f => f.Type == TargetType.Application && f.TargetId == a.Id)
                            .Select(f => new FileResponse
                            {
                                Id = f.Id, Name = f.Name, Path = f.Path, UploadedById = f.UploadedById, Uploaded = f.Uploaded
                            })
                            .SingleOrDefault(),
                        Introduction = a.Introduction,
                        Status = a.Status,
                        Created = a.Created
                    })
                    .SingleOrDefaultAsync();
        
                if (application == null)
                {
                    return NotFound(new { Message = "Application not found." });
                }
                return Ok(application);
            }
        }