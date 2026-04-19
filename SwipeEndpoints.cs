using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Spotilove;

public static class SwipeEndpoints
{
    public static async Task<IResult> GetPotentialMatches(SwipeService swipeService, Guid userId, int count = 10)
    {
        try
        {
            var suggestions = await swipeService.GetPotentialMatchesAsync(userId, count);
            return Results.Ok(new
            {
                Users = suggestions,
                Count = suggestions.Count(),
                Message = suggestions.Any() ? "Potential matches found" : "No more users to show"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Error fetching potential matches");
        }
    }
    public static async Task<IResult> SwipeOnUser(SwipeService swipeService, SwipeDto swipeDto)
    {
        try
        {
            var result = await swipeService.SwipeAsync(swipeDto.FromUserId, swipeDto.ToUserId, swipeDto.IsLike);

            return Results.Ok(new ResponseMessage
            {
                Success = true
            });
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new ResponseMessage { Success = false });
        }
        catch (InvalidOperationException)
        {
            return Results.Conflict(new ResponseMessage { Success = false });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Error processing swipe");
        }
    }

    public static async Task<IResult> GetUserMatches(SwipeService swipeService, Guid userId)
    {
        try
        {
            var matches = await swipeService.GetMatchesAsync(userId);
            return Results.Ok(new
            {
                Matches = matches,
                Count = matches.Count(),
                Message = matches.Any() ? "Your matches" : "No matches yet"
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Error fetching matches");
        }
    }
    public static async Task<IResult> GetSwipeStats(AppDbContext db, Guid userId)
    {
        try
        {
            var swipes = await db.Likes
                .Where(l => l.FromUserId == userId)
                .ToListAsync();

            var totalSwipes = swipes.Count;
            var likes = swipes.Count(l => l.IsLike);
            var passes = totalSwipes - likes;
            var matches = swipes.Count(l => l.IsMatch);
            var likeRate = totalSwipes > 0 ? Math.Round((double)likes / totalSwipes * 100, 1) : 0.0;

            return Results.Ok(new
            {
                TotalSwipes = totalSwipes,
                Likes = likes,
                Passes = passes,
                Matches = matches,
                LikeRate = likeRate
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, title: "Error fetching swipe stats");
        }
    }

    public static async Task<IResult> LikeUser(SwipeService swipeService, Guid fromUserId, Guid toUserId)
    {
        var swipeDto = new SwipeDto(fromUserId, toUserId, true);
        return await SwipeOnUser(swipeService, swipeDto);
    }

    public static async Task<IResult> PassUser(SwipeService swipeService, Guid fromUserId, Guid toUserId)
    {
        var swipeDto = new SwipeDto(fromUserId, toUserId, false);
        return await SwipeOnUser(swipeService, swipeDto);
    }
}