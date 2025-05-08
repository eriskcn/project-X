using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.DTOs;
using ProjectX.Helpers;
using ProjectX.Models;
using ProjectX.Services.Notifications;

namespace ProjectX.Controllers;

[ApiController]
[Route("capablanca/api/v0/posts")]
[Authorize]
public class PostController(
    ApplicationDbContext context,
    IWebHostEnvironment env,
    INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetPosts(
        [FromQuery] string? search,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        List<LikeResponse>? likes = null;
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            likes = await context.Likes
                .Where(l => l.UserId == Guid.Parse(userId))
                .Select(l => new LikeResponse
                {
                    Id = l.Id,
                    PostId = l.PostId,
                    IsLike = l.IsLike
                })
                .ToListAsync();
        }

        var query = context.Posts
            .Include(p => p.User)
            .ThenInclude(u => u.CompanyDetail)
            .Where(p => p.ParentId == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var lowCaseSearch = search.ToLower();
            query = query.Where(p => p.Content.ToLower().Contains(lowCaseSearch));
        }

        if (startDate.HasValue)
        {
            query = query.Where(p => p.Created >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => p.Created <= endDate.Value);
        }

        query = query.OrderByDescending(p => p.Created);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var postIds = posts.Select(p => p.Id).ToList();

        var likeCounts = await context.Likes
            .Where(l => postIds.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Sum(l => l.IsLike ? 1 : -1) })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var commentCounts = await context.Posts
            .Where(p => p.ParentId.HasValue && postIds.Contains(p.ParentId.Value))
            .GroupBy(p => p.ParentId!.Value)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var attachedFiles = await context.AttachedFiles
            .Where(f => f.Type == FileType.PostAttachment && postIds.Contains(f.TargetId))
            .ToListAsync();

        var items = posts.Select(p =>
        {
            var postLikes = likes?.SingleOrDefault(l => l.PostId == p.Id);
            return new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                Liked = postLikes?.IsLike,
                IsEdited = p.IsEdited,
                Edited = p.Edited,
                User = new UserResponse
                {
                    Id = p.User.Id,
                    Name = p.User.CompanyDetail != null ? p.User.CompanyDetail.CompanyName : p.User.FullName,
                    ProfilePicture = p.User.CompanyDetail != null ? p.User.CompanyDetail.Logo : p.User.ProfilePicture
                },
                LikesCount = likeCounts.GetValueOrDefault(p.Id, 0),
                CommentsCount = commentCounts.GetValueOrDefault(p.Id, 0),
                AttachedFile = attachedFiles
                    .Where(f => f.TargetId == p.Id)
                    .Select(f => new FileResponse
                    {
                        Id = f.Id,
                        TargetId = f.TargetId,
                        Name = f.Name,
                        Path = f.Path,
                        Uploaded = f.Uploaded
                    })
                    .SingleOrDefault() ?? new FileResponse
                {
                    Id = Guid.Empty,
                    Name = "No attached file",
                    Path = string.Empty,
                    Uploaded = DateTime.UtcNow
                },
                Created = p.Created
            };
        }).ToList();

        return Ok(new
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            First = page == 1,
            Last = page == totalPages,
            PageNumber = page,
            PageSize = pageSize
        });
    }


    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPost(
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page number and page size must be greater than zero." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool? userLikeRootPost = null;
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await context.Users.FindAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            userLikeRootPost = await context.Likes
                .Where(l => l.UserId == Guid.Parse(userId) && l.PostId == id)
                .Select(l => (bool?)l.IsLike)
                .SingleOrDefaultAsync();
        }

        var post = await context.Posts
            .Include(p => p.User)
            .ThenInclude(u => u.CompanyDetail)
            .SingleOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound(new { Message = "Post not found." });
        }

        var likeCount = await context.Likes
            .Where(l => l.PostId == post.Id)
            .SumAsync(l => l.IsLike ? 1 : -1);

        var attachedFile = await context.AttachedFiles
            .Where(f => f.Type == FileType.PostAttachment && f.TargetId == post.Id)
            .Select(f => new FileResponse
            {
                Id = f.Id,
                TargetId = f.TargetId,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .SingleOrDefaultAsync();

        var userLikeParentPost = await context.Likes
            .Where(l => userId != null && l.UserId == Guid.Parse(userId) && l.PostId == post.ParentId)
            .Select(l => (bool?)l.IsLike)
            .SingleOrDefaultAsync();

        var parentPostResponse = await context.Posts
            .Include(p => p.User)
            .ThenInclude(u => u.CompanyDetail)
            .Where(p => p.Id == post.ParentId)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                Liked = userLikeParentPost,
                IsEdited = p.IsEdited,
                Edited = p.Edited,
                Created = p.Created,
                User = new UserResponse
                {
                    Id = p.User.Id,
                    Name = p.User.CompanyDetail != null ? p.User.CompanyDetail.CompanyName : p.User.FullName,
                    ProfilePicture = p.User.CompanyDetail != null ? p.User.CompanyDetail.Logo : p.User.ProfilePicture
                },
                LikesCount = context.Likes.Where(l => l.PostId == p.Id).Sum(l => l.IsLike ? 1 : -1),
                CommentsCount = context.Posts.Count(c => c.ParentId == p.Id),
                AttachedFile = context.AttachedFiles
                                   .Where(f => f.Type == FileType.PostAttachment && f.TargetId == post.ParentId)
                                   .Select(f => new FileResponse
                                   {
                                       Id = f.Id,
                                       TargetId = f.TargetId,
                                       Name = f.Name,
                                       Path = f.Path,
                                       Uploaded = f.Uploaded
                                   })
                                   .SingleOrDefault()
                               ?? new FileResponse
                               {
                                   Id = Guid.Empty,
                                   Name = "No attached file",
                                   Path = string.Empty,
                                   Uploaded = DateTime.UtcNow
                               }
            })
            .SingleOrDefaultAsync();

        var totalComments = await context.Posts
            .Where(c => c.ParentId == post.Id)
            .CountAsync();

        var totalPages = (int)Math.Ceiling(totalComments / (double)pageSize);

        var comments = await context.Posts
            .Where(c => c.ParentId == post.Id)
            .Include(c => c.User)
            .ThenInclude(u => u.CompanyDetail)
            .OrderBy(c => c.Created)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var commentIds = comments.Select(c => c.Id).ToList();

        var commentLikes = await context.Likes
            .Where(l => commentIds.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Sum(l => l.IsLike ? 1 : -1) })
            .ToDictionaryAsync(g => g.PostId, g => g.Count);

        var userCommentLikes = !string.IsNullOrEmpty(userId)
            ? await context.Likes
                .Where(l => l.UserId == Guid.Parse(userId) && commentIds.Contains(l.PostId))
                .Select(l => new { l.PostId, l.IsLike })
                .ToDictionaryAsync(l => l.PostId, l => (bool?)l.IsLike)
            : new Dictionary<Guid, bool?>();

        // Query attached files for comments
        var commentAttachedFiles = await context.AttachedFiles
            .Where(f => f.Type == FileType.PostAttachment && commentIds.Contains(f.TargetId))
            .Select(f => new FileResponse
            {
                Id = f.Id,
                TargetId = f.TargetId,
                Name = f.Name,
                Path = f.Path,
                Uploaded = f.Uploaded
            })
            .ToListAsync();

        var commentResponses = comments.Select(c => new PostResponse
        {
            Id = c.Id,
            Content = c.Content,
            Liked = userCommentLikes.GetValueOrDefault(c.Id),
            IsEdited = c.IsEdited,
            Edited = c.Edited,
            Created = c.Created,
            User = new UserResponse
            {
                Id = c.User.Id,
                Name = c.User.CompanyDetail != null ? c.User.CompanyDetail.CompanyName : c.User.FullName,
                ProfilePicture = c.User.CompanyDetail != null ? c.User.CompanyDetail.Logo : c.User.ProfilePicture
            },
            LikesCount = commentLikes.GetValueOrDefault(c.Id, 0),
            CommentsCount = 0,
            AttachedFile = commentAttachedFiles
                .SingleOrDefault(f => f.TargetId == c.Id) ?? new FileResponse
            {
                Id = Guid.Empty,
                Name = "No attached file",
                Path = string.Empty,
                Uploaded = DateTime.UtcNow
            }
        }).ToList();

        var response = new
        {
            Post = new PostResponse
            {
                Id = post.Id,
                Content = post.Content,
                Liked = userLikeRootPost,
                ParentPost = parentPostResponse,
                IsEdited = post.IsEdited,
                Edited = post.Edited,
                Created = post.Created,
                User = new UserResponse
                {
                    Id = post.User.Id,
                    Name = post.User.CompanyDetail != null ? post.User.CompanyDetail.CompanyName : post.User.FullName,
                    ProfilePicture = post.User.CompanyDetail != null
                        ? post.User.CompanyDetail.Logo
                        : post.User.ProfilePicture
                },
                LikesCount = likeCount,
                CommentsCount = totalComments,
                Comments = commentResponses,
                AttachedFile = attachedFile ?? new FileResponse
                {
                    Id = Guid.Empty,
                    TargetId = Guid.Empty,
                    Name = "No attached file",
                    Path = string.Empty,
                    Uploaded = DateTime.UtcNow
                }
            },
            CommentPagination = new
            {
                TotalItems = totalComments,
                TotalPages = totalPages,
                PageNumber = page,
                PageSize = pageSize,
                First = page == 1,
                Last = page == totalPages
            }
        };

        return Ok(response);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<PostResponse>> CommentPost([FromRoute] Guid id, [FromForm] PostRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        var parentPost = await context.Posts.FindAsync(id);
        if (parentPost == null)
            return NotFound("Parent post not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var comment = new Post
            {
                Content = request.Content,
                UserId = user.Id,
                ParentId = id,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            context.Posts.Add(comment);

            if (request.AttachedFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file extension. Only image files are allowed.");
                }

                if (request.AttachedFile.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds the 10MB limit.");
                }

                var postAttachmentsFolder = Path.Combine(env.WebRootPath, "postAttachments");
                if (!Directory.Exists(postAttachmentsFolder))
                {
                    Directory.CreateDirectory(postAttachmentsFolder);
                }

                var cleanFileName = PathHelper.GetCleanFileName(request.AttachedFile.FileName);
                var displayFileName = Path.GetFileName(cleanFileName);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(postAttachmentsFolder, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.AttachedFile.CopyToAsync(stream);
                var attachedFile = new AttachedFile
                {
                    Name = displayFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = FileType.PostAttachment,
                    TargetId = comment.Id,
                    Uploaded = DateTime.UtcNow,
                    UploadedById = Guid.Parse(userId),
                };

                context.AttachedFiles.Add(attachedFile);
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            await notificationService.SendNotificationAsync(NotificationType.NewComment, parentPost.UserId, comment.Id);
            return Ok(new { Message = "Comment successfully." });
        }
        catch (Exception ex)
        {
            // Rollback transaction
            await transaction.RollbackAsync();

            // Log error (implement proper logging)
            return StatusCode(500, new { Message = "Error creating comment.", Error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<PostResponse>> LikePost([FromRoute] Guid id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        var post = await context.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var existingLike = await context.Likes
                .IgnoreSoftDelete()
                .SingleOrDefaultAsync(l => l.UserId == user.Id && l.PostId == id);

            if (existingLike != null)
            {
                if (existingLike is { IsDeleted: false, IsLike: true })
                {
                    existingLike.IsDeleted = true;
                }
                else
                {
                    existingLike.IsDeleted = false;
                    existingLike.IsLike = true;
                }
            }
            else
            {
                var newLike = new Like
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PostId = id,
                    IsLike = true,
                    IsDeleted = false
                };
                context.Likes.Add(newLike);
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            await notificationService.SendNotificationAsync(NotificationType.NewReactToPost, post.UserId, post.Id);

            return Ok(new { Message = "Like post successfully." });
        }
        catch (Exception ex)
        {
            // Rollback transaction
            await transaction.RollbackAsync();

            // Log error (implement proper logging)
            return StatusCode(500, new { Message = "Error processing like.", Error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/dislike")]
    public async Task<ActionResult<PostResponse>> DislikePost([FromRoute] Guid id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        var post = await context.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var existingLike = await context.Likes
                .IgnoreSoftDelete()
                .SingleOrDefaultAsync(l => l.UserId == user.Id && l.PostId == id);

            if (existingLike != null)
            {
                if (existingLike is { IsDeleted: false, IsLike: false })
                {
                    existingLike.IsDeleted = true;
                }
                else
                {
                    existingLike.IsDeleted = false;
                    existingLike.IsLike = false;
                }
            }
            else
            {
                var newLike = new Like
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    PostId = id,
                    IsLike = false,
                    IsDeleted = false
                };
                context.Likes.Add(newLike);
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            await notificationService.SendNotificationAsync(NotificationType.NewReactToPost, post.UserId, post.Id);
            return Ok(new { Message = "Dislike post successfully." });
        }
        catch (Exception ex)
        {
            // Rollback transaction
            await transaction.RollbackAsync();

            // Log error (implement proper logging)
            return StatusCode(500, new { Message = "Error processing dislike.", Error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromForm] PostRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var post = new Post
            {
                Content = request.Content,
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            context.Posts.Add(post);

            if (request.AttachedFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file extension. Only image files are allowed.");
                }

                if (request.AttachedFile.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds the 10MB limit.");
                }

                var postAttachmentsFolder = Path.Combine(env.WebRootPath, "postAttachments");
                if (!Directory.Exists(postAttachmentsFolder))
                {
                    Directory.CreateDirectory(postAttachmentsFolder);
                }

                var cleanFileName = PathHelper.GetCleanFileName(request.AttachedFile.FileName);
                var displayFileName = Path.GetFileName(cleanFileName);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(postAttachmentsFolder, fileName);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.AttachedFile.CopyToAsync(stream);
                var attachedFile = new AttachedFile
                {
                    Name = displayFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = FileType.PostAttachment,
                    TargetId = post.Id,
                    Uploaded = DateTime.UtcNow,
                    UploadedById = Guid.Parse(userId)
                };

                context.AttachedFiles.Add(attachedFile);
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new { Message = "Create post successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Error creating post.", Error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdatePost([FromRoute] Guid id, [FromForm] UpdatePostRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        var post = await context.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        if (post.UserId != Guid.Parse(userId))
            return Forbid("You are not authorized to edit this post.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            if (!string.IsNullOrEmpty(request.Content))
            {
                post.Content = request.Content;
                post.IsEdited = true;
                post.Edited = DateTime.UtcNow;
            }

            if (request.AttachedFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Invalid file extension. Only image files are allowed.");

                if (request.AttachedFile.Length > 10 * 1024 * 1024)
                    return BadRequest("File size exceeds the 10MB limit.");

                var postAttachmentsFolder = Path.Combine(env.WebRootPath, "postAttachments");
                if (!Directory.Exists(postAttachmentsFolder))
                    Directory.CreateDirectory(postAttachmentsFolder);

                var cleanFileName = PathHelper.GetCleanFileName(request.AttachedFile.FileName);
                var displayFileName = Path.GetFileName(cleanFileName);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(postAttachmentsFolder, fileName);

                var existingFile = await context.AttachedFiles
                    .SingleOrDefaultAsync(f => f.Type == FileType.PostAttachment && f.TargetId == post.Id);

                if (existingFile != null)
                    context.AttachedFiles.Remove(existingFile);

                var attachedFile = new AttachedFile
                {
                    Name = displayFileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = FileType.PostAttachment,
                    TargetId = post.Id,
                    Uploaded = DateTime.UtcNow
                };

                context.AttachedFiles.Add(attachedFile);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.AttachedFile.CopyToAsync(stream);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { Message = "Post updated successfully." });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { Message = "Error updating post.", Error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost([FromRoute] Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        var post = await context.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        if (post.UserId != Guid.Parse(userId))
            return Forbid("You are not authorized to delete this post.");

        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        return Ok(new { Message = "Post deleted successfully." });
    }
}