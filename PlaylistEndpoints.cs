using Microsoft.EntityFrameworkCore;

namespace Spotilove;

public static class PlaylistEndpoints
{
    // POST /matches/{userId}/create-playlist/{matchedUserId}
    // Creates a shared Spotify playlist from both users' favorite songs
    public static async Task<IResult> CreateMatchPlaylist(
        AppDbContext db,
        SpotifyService spotify,
        Guid userId,
        Guid matchedUserId)
    {
        try
        {
            Console.WriteLine($"  Creating shared playlist for users {userId} & {matchedUserId}");

            // 1. Verify these users are actually a match
            var isMatch = await db.Likes
                .AnyAsync(l => l.FromUserId == userId && l.ToUserId == matchedUserId && l.IsLike == true) &&
                await db.Likes
                .AnyAsync(l => l.FromUserId == matchedUserId && l.ToUserId == userId && l.IsLike == true);

            if (!isMatch)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Users are not matched"
                });
            }

            // 2. Fetch both users' profiles
            var user1 = await db.Users
                .Include(u => u.MusicProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var user2 = await db.Users
                .Include(u => u.MusicProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == matchedUserId);

            if (user1 == null || user2 == null)
            {
                return Results.NotFound(new { success = false, message = "One or both users not found" });
            }

            // 3. Get song lists from both profiles
            var songs1 = user1.MusicProfile?.FavoriteSongs ?? new List<string>();
            var songs2 = user2.MusicProfile?.FavoriteSongs ?? new List<string>();

            // 4. Interleave songs from both users for a mixed feel
            var mergedSongs = InterleaveSongs(songs1, songs2);

            if (!mergedSongs.Any())
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "No songs found in either user's music profile"
                });
            }

            // 5. Search Spotify for track URIs
            Console.WriteLine($"  Searching Spotify for {mergedSongs.Count} songs...");
            var trackUris = await spotify.SearchTrackUrisAsync(mergedSongs);

            if (!trackUris.Any())
            {
                return Results.Problem("Could not find any tracks on Spotify for these users' songs.");
            }

            // 6. Create the playlist
            string playlistName = $"SpotiLove: {user1.Name} & {user2.Name}";
            string description =
                $"A musical match made on SpotiLove 💚 " +
                $"Songs from {user1.Name} and {user2.Name}'s favorite lists.";

            Console.WriteLine($"  Creating playlist: '{playlistName}'");
            var (playlistId, playlistUrl) = await spotify.CreateCollaborativePlaylistAsync(playlistName, description);

            if (string.IsNullOrEmpty(playlistId))
            {
                return Results.Problem("Failed to create Spotify playlist. Check the owner refresh token.");
            }

            // 7. Add tracks (up to 50 to keep it concise)
            var tracksToAdd = trackUris.Take(50).ToList();
            await spotify.AddTracksToPlaylistAsync(playlistId, tracksToAdd);
            Console.WriteLine($"  Added {tracksToAdd.Count} tracks to playlist {playlistId}");

            // 8. Set playlist cover image (SpotiLove logo as base64)
            await spotify.SetPlaylistCoverImageAsync(playlistId);

            return Results.Ok(new
            {
                success = true,
                playlistId,
                playlistUrl,
                trackCount = tracksToAdd.Count,
                message = $"Playlist '{playlistName}' created with {tracksToAdd.Count} tracks!"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating playlist: {ex.Message}");
            return Results.Problem(detail: ex.Message, title: "Failed to create playlist");
        }
    }

    // GET /matches/{userId}/playlist-status/{matchedUserId}
    // Returns whether a playlist already exists between these users
    // (You could store this in DB; for now we just confirm match status)
    public static async Task<IResult> GetPlaylistStatus(
        AppDbContext db,
        Guid userId,
        Guid matchedUserId)
    {
        var isMatch = await db.Likes
            .AnyAsync(l => l.FromUserId == userId && l.ToUserId == matchedUserId && l.IsLike == true) &&
            await db.Likes
            .AnyAsync(l => l.FromUserId == matchedUserId && l.ToUserId == userId && l.IsLike == true);

        return Results.Ok(new
        {
            success = true,
            isMatch,
            canCreatePlaylist = isMatch
        });
    }

    // Interleave songs from two lists (A, B, A, B, ...) for a balanced mix
    private static List<string> InterleaveSongs(List<string> list1, List<string> list2)
    {
        var result = new List<string>();
        int max = Math.Max(list1.Count, list2.Count);

        for (int i = 0; i < max; i++)
        {
            if (i < list1.Count) result.Add(list1[i]);
            if (i < list2.Count) result.Add(list2[i]);
        }

        return result.Distinct().ToList();
    }
}
