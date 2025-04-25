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
            .Where(f => f.Type == TargetType.PostAttachment && postIds.Contains(f.TargetId))
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
            .Where(f => f.Type == TargetType.PostAttachment && f.TargetId == post.Id)
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
                                   .Where(f => f.Type == TargetType.PostAttachment && f.TargetId == post.ParentId)
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
            .Where(f => f.Type == TargetType.PostAttachment && commentIds.Contains(f.TargetId))
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
        // Validate model state
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get user ID from claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        // Verify user exists
        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        // Verify parent post exists
        var parentPost = await context.Posts.FindAsync(id);
        if (parentPost == null)
            return NotFound("Parent post not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Create new comment (as a Post with ParentId)
            var comment = new Post
            {
                Content = request.Content,
                UserId = user.Id,
                ParentId = id,
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            // Add comment to context
            context.Posts.Add(comment);

            string? filePath = null;
            AttachedFile? attachedFile = null;

            // Handle file upload if present
            if (request.AttachedFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLowerInvariant();

                // Validate file extension
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file extension. Only image files are allowed.");
                }

                // Validate file size (5MB limit)
                if (request.AttachedFile.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds the 5MB limit.");
                }

                // Prepare upload directory
                var postAttachmentsFolder = Path.Combine(env.WebRootPath, "postAttachments");
                if (!Directory.Exists(postAttachmentsFolder))
                {
                    Directory.CreateDirectory(postAttachmentsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                filePath = Path.Combine(postAttachmentsFolder, fileName);

                attachedFile = new AttachedFile
                {
                    Name = fileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = TargetType.PostAttachment,
                    TargetId = comment.Id,
                    Uploaded = DateTime.UtcNow,
                    UploadedById = Guid.Parse(userId),
                };

                context.AttachedFiles.Add(attachedFile);
            }

            // Save changes to database (comment and attached file record)
            await context.SaveChangesAsync();

            // Only save file to disk after database operations are confirmed
            if (request.AttachedFile != null && filePath != null)
            {
                await using var stream = new FileStream(filePath, FileMode.Create);
                await request.AttachedFile.CopyToAsync(stream);
            }

            // Fetch additional data for response within transaction
            var commentUser = await context.Users
                .Include(u => u.CompanyDetail)
                .Where(u => u.Id == comment.UserId)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = u.CompanyDetail != null ? u.CompanyDetail.CompanyName : u.FullName,
                    ProfilePicture = u.CompanyDetail != null ? u.CompanyDetail.Logo : u.ProfilePicture
                })
                .SingleOrDefaultAsync();

            if (commentUser == null)
            {
                throw new InvalidOperationException("Comment user not found.");
            }

            var parentPostResponse = await context.Posts
                .Include(p => p.User)
                .ThenInclude(u => u.CompanyDetail)
                .Where(p => p.Id == comment.ParentId)
                .Select(p => new PostResponse
                {
                    Id = p.Id,
                    Content = p.Content,
                    User = new UserResponse
                    {
                        Id = p.User.Id,
                        Name = p.User.CompanyDetail != null ? p.User.CompanyDetail.CompanyName : p.User.FullName,
                        ProfilePicture = p.User.CompanyDetail != null
                            ? p.User.CompanyDetail.Logo
                            : p.User.ProfilePicture
                    },
                    LikesCount = context.Likes.Count(l => l.PostId == p.Id),
                    CommentsCount = context.Posts.Count(c => c.ParentId == p.Id),
                    Created = p.Created
                })
                .SingleOrDefaultAsync();

            if (parentPostResponse == null)
            {
                throw new InvalidOperationException("Parent post not found.");
            }

            // Create response
            var response = new PostResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                User = commentUser,
                ParentPost = parentPostResponse,
                LikesCount = context.Likes.Where(l => l.PostId == comment.Id).Sum(l => l.IsLike ? 1 : -1),
                CommentsCount = context.Posts.Count(c => c.ParentId == comment.Id), // Comments for comment
                Created = comment.Created,
                AttachedFile = attachedFile != null
                    ? new FileResponse
                    {
                        Id = attachedFile.Id,
                        TargetId = attachedFile.TargetId,
                        Name = attachedFile.Name,
                        Path = attachedFile.Path,
                        Uploaded = attachedFile.Uploaded
                    }
                    : new FileResponse
                    {
                        Id = Guid.Empty,
                        TargetId = Guid.Empty,
                        Name = "No attached file",
                        Path = "",
                        Uploaded = DateTime.UtcNow
                    }
            };

            // Commit transaction
            await transaction.CommitAsync();

            // Return success response
            return CreatedAtAction(nameof(GetPost), new { id = comment.Id }, response);
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
        // Validate model state
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get user ID from claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        // Verify user exists
        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        // Verify post exists
        var post = await context.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Check if the user has already liked/disliked the post
            var existingLike = await context.Likes
                .IgnoreSoftDelete()
                .SingleOrDefaultAsync(l => l.UserId == user.Id && l.PostId == id);

            bool? isLiked; // Default to null (no interaction)
            if (existingLike != null)
            {
                if (existingLike is { IsDeleted: false, IsLike: true })
                {
                    existingLike.IsDeleted = true;
                    isLiked = null; // Like removed, no interaction
                }
                else
                {
                    existingLike.IsDeleted = false;
                    existingLike.IsLike = true;
                    isLiked = true; // Like applied
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
                isLiked = true; // New like
            }

            // Save changes to database
            await context.SaveChangesAsync();

            // Fetch data for response within transaction
            var postUser = await context.Users
                .Include(u => u.CompanyDetail)
                .Where(u => u.Id == post.UserId)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = u.CompanyDetail != null ? u.CompanyDetail.CompanyName : u.FullName,
                    ProfilePicture = u.CompanyDetail != null ? u.CompanyDetail.Logo : u.ProfilePicture
                })
                .SingleOrDefaultAsync();

            if (postUser == null)
            {
                throw new InvalidOperationException("Post user not found.");
            }

            // Get attached file (if any)
            var attachedFile = await context.AttachedFiles
                .Where(f => f.TargetId == post.Id && f.Type == TargetType.PostAttachment)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync();

            // Check if user liked/disliked the parent post (if applicable)
            bool? parentPostLiked = null;
            if (post.ParentId != null)
            {
                var parentLike = await context.Likes
                    .Where(l => l.UserId == user.Id && l.PostId == post.ParentId && !l.IsDeleted)
                    .Select(l => new { l.IsLike })
                    .SingleOrDefaultAsync();
                parentPostLiked = parentLike?.IsLike;
            }

            // Create response
            var response = new PostResponse
            {
                Id = post.Id,
                Content = post.Content,
                Liked = isLiked, // Set Liked based on the like action
                User = postUser,
                ParentPost = post.ParentId != null
                    ? await context.Posts
                        .Include(p => p.User)
                        .ThenInclude(u => u.CompanyDetail)
                        .Where(p => p.Id == post.ParentId)
                        .Select(p => new PostResponse
                        {
                            Id = p.Id,
                            Content = p.Content,
                            Liked = parentPostLiked, // Set Liked for parent post
                            User = new UserResponse
                            {
                                Id = p.User.Id,
                                Name = p.User.CompanyDetail != null
                                    ? p.User.CompanyDetail.CompanyName
                                    : p.User.FullName,
                                ProfilePicture = p.User.CompanyDetail != null
                                    ? p.User.CompanyDetail.Logo
                                    : p.User.ProfilePicture
                            },
                            LikesCount = context.Likes.Where(l => l.PostId == p.Id && !l.IsDeleted)
                                .Sum(l => l.IsLike ? 1 : -1),
                            CommentsCount = context.Posts.Count(c => c.ParentId == p.Id),
                            Created = p.Created
                        })
                        .SingleOrDefaultAsync()
                    : null,
                LikesCount = context.Likes.Where(l => l.PostId == post.Id && !l.IsDeleted).Sum(l => l.IsLike ? 1 : -1),
                CommentsCount = context.Posts.Count(c => c.ParentId == post.Id),
                Created = post.Created,
                AttachedFile = attachedFile ?? new FileResponse
                {
                    Id = Guid.Empty,
                    TargetId = Guid.Empty,
                    Name = "No attached file",
                    Path = string.Empty,
                    Uploaded = DateTime.UtcNow
                }
            };
            var notification = new NotificationRequest
            {
                Type = NotificationType.NewReactToPost,
                RecipientId = postUser.Id,
                TargetId = post.Id
            };
            // Commit transaction
            await transaction.CommitAsync();
            await notificationService.SendNotificationAsync(notification);
            // Return success response
            return Ok(response);
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
        // Validate model state
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Get user ID from claims
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized("Access token is invalid.");

        // Verify user exists
        var user = await context.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");

        // Verify post exists
        var post = await context.Posts.FindAsync(id);
        if (post == null)
            return NotFound("Post not found.");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // Check if the user has already liked/disliked the post
            var existingLike = await context.Likes
                .IgnoreSoftDelete()
                .SingleOrDefaultAsync(l => l.UserId == user.Id && l.PostId == id);

            bool? isLiked; // Default to null (no interaction)
            if (existingLike != null)
            {
                if (existingLike is { IsDeleted: false, IsLike: false })
                {
                    existingLike.IsDeleted = true;
                    isLiked = null; // Dislike removed, no interaction
                }
                else
                {
                    existingLike.IsDeleted = false;
                    existingLike.IsLike = false;
                    isLiked = false; // Dislike applied
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
                isLiked = false; // New dislike
            }

            // Save changes to database
            await context.SaveChangesAsync();

            // Fetch data for response within transaction
            var postUser = await context.Users
                .Include(u => u.CompanyDetail)
                .Where(u => u.Id == post.UserId)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    Name = u.CompanyDetail != null ? u.CompanyDetail.CompanyName : u.FullName,
                    ProfilePicture = u.CompanyDetail != null ? u.CompanyDetail.Logo : u.ProfilePicture
                })
                .SingleOrDefaultAsync();

            if (postUser == null)
            {
                throw new InvalidOperationException("Post user not found.");
            }

            // Get attached file (if any)
            var attachedFile = await context.AttachedFiles
                .Where(f => f.TargetId == post.Id && f.Type == TargetType.PostAttachment)
                .Select(f => new FileResponse
                {
                    Id = f.Id,
                    TargetId = f.TargetId,
                    Name = f.Name,
                    Path = f.Path,
                    Uploaded = f.Uploaded
                })
                .SingleOrDefaultAsync();

            // Check if user liked/disliked the parent post (if applicable)
            bool? parentPostLiked = null;
            if (post.ParentId != null)
            {
                var parentLike = await context.Likes
                    .Where(l => l.UserId == user.Id && l.PostId == post.ParentId && !l.IsDeleted)
                    .Select(l => new { l.IsLike })
                    .SingleOrDefaultAsync();
                parentPostLiked = parentLike?.IsLike;
            }

            // Create response
            var response = new PostResponse
            {
                Id = post.Id,
                Content = post.Content,
                Liked = isLiked, // Set Liked based on the dislike action
                User = postUser,
                ParentPost = post.ParentId != null
                    ? await context.Posts
                        .Include(p => p.User)
                        .ThenInclude(u => u.CompanyDetail)
                        .Where(p => p.Id == post.ParentId)
                        .Select(p => new PostResponse
                        {
                            Id = p.Id,
                            Content = p.Content,
                            Liked = parentPostLiked, // Set Liked for parent post
                            User = new UserResponse
                            {
                                Id = p.User.Id,
                                Name = p.User.CompanyDetail != null
                                    ? p.User.CompanyDetail.CompanyName
                                    : p.User.FullName,
                                ProfilePicture = p.User.CompanyDetail != null
                                    ? p.User.CompanyDetail.Logo
                                    : p.User.ProfilePicture
                            },
                            LikesCount = context.Likes.Where(l => l.PostId == p.Id && !l.IsDeleted)
                                .Sum(l => l.IsLike ? 1 : -1),
                            CommentsCount = context.Posts.Count(c => c.ParentId == p.Id),
                            Created = p.Created
                        })
                        .SingleOrDefaultAsync()
                    : null,
                LikesCount = context.Likes.Where(l => l.PostId == post.Id && !l.IsDeleted).Sum(l => l.IsLike ? 1 : -1),
                CommentsCount = context.Posts.Count(c => c.ParentId == post.Id),
                Created = post.Created,
                AttachedFile = attachedFile ?? new FileResponse
                {
                    Id = Guid.Empty,
                    TargetId = Guid.Empty,
                    Name = "No attached file",
                    Path = string.Empty,
                    Uploaded = DateTime.UtcNow
                }
            };

            // Commit transaction
            await transaction.CommitAsync();

            // Return success response
            return Ok(response);
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

            string? filePath = null;

            if (request.AttachedFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.AttachedFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file extension. Only image files are allowed.");
                }

                if (request.AttachedFile.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds the 5MB limit.");
                }

                var postAttachmentsFolder = Path.Combine(env.WebRootPath, "postAttachments");
                if (!Directory.Exists(postAttachmentsFolder))
                {
                    Directory.CreateDirectory(postAttachmentsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                filePath = Path.Combine(postAttachmentsFolder, fileName);

                var attachedFile = new AttachedFile
                {
                    Name = request.AttachedFile.FileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = TargetType.PostAttachment,
                    TargetId = post.Id,
                    Uploaded = DateTime.UtcNow,
                    UploadedById = Guid.Parse(userId)
                };

                context.AttachedFiles.Add(attachedFile);
            }

            await context.SaveChangesAsync();

            if (request.AttachedFile != null)
            {
                if (filePath != null)
                {
                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await request.AttachedFile.CopyToAsync(stream);
                }
            }

            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
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

                if (request.AttachedFile.Length > 5 * 1024 * 1024)
                    return BadRequest("File size exceeds the 5MB limit.");

                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                var existingFile = await context.AttachedFiles
                    .SingleOrDefaultAsync(f => f.Type == TargetType.PostAttachment && f.TargetId == post.Id);

                if (existingFile != null)
                    context.AttachedFiles.Remove(existingFile);

                var attachedFile = new AttachedFile
                {
                    Name = request.AttachedFile.FileName,
                    Path = PathHelper.GetRelativePathFromAbsolute(filePath, env.WebRootPath),
                    Type = TargetType.PostAttachment,
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