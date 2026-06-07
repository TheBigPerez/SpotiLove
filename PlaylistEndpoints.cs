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

            var isMatch = await db.Likes
                .AnyAsync(l => l.FromUserId == userId && l.ToUserId == matchedUserId && l.IsLike == true) &&
                await db.Likes
                .AnyAsync(l => l.FromUserId == matchedUserId && l.ToUserId == userId && l.IsLike == true);

            if (!isMatch)
                return Results.BadRequest(new { success = false, message = "Users are not matched" });

            var user1 = await db.Users.Include(u => u.MusicProfile).AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var user2 = await db.Users.Include(u => u.MusicProfile).AsNoTracking().FirstOrDefaultAsync(u => u.Id == matchedUserId);

            if (user1 == null || user2 == null)
                return Results.NotFound(new { success = false, message = "One or both users not found" });

            var songs1 = user1.MusicProfile?.FavoriteSongs ?? new List<string>();
            var songs2 = user2.MusicProfile?.FavoriteSongs ?? new List<string>();
            var artists1 = user1.MusicProfile?.FavoriteArtists ?? new List<string>();
            var artists2 = user2.MusicProfile?.FavoriteArtists ?? new List<string>();

            // Step 1: interleave favourite songs, deduplicated
            var mergedSongs = InterleaveSongs(songs1, songs2);

            Console.WriteLine($"  Searching Spotify for {mergedSongs.Count} favourite songs...");
            var trackUris = (await spotify.SearchTrackUrisAsync(mergedSongs))
                .Distinct()
                .ToList();

            Console.WriteLine($"  Found {trackUris.Count} tracks from favourites");

            // Step 2: pad up to 50 using top tracks from their artists
            if (trackUris.Count < 50)
            {
                Console.WriteLine($"  Only {trackUris.Count} tracks — padding with artist top tracks...");

                // Interleave artists from both users so we pull from both equally
                var allArtists = InterleaveLists(artists1, artists2)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var artist in allArtists)
                {
                    if (trackUris.Count >= 50) break;

                    try
                    {
                        var topTracks = await spotify.GetArtistTopTracksAsync(artist, limit: 10);
                        foreach (var track in topTracks)
                        {
                            if (trackUris.Count >= 50) break;
                            if (string.IsNullOrEmpty(track.SpotifyUri)) continue;

                            if (!trackUris.Contains(track.SpotifyUri))
                                trackUris.Add(track.SpotifyUri);
                        }

                        Console.WriteLine($"  After {artist}: {trackUris.Count} tracks");
                        await Task.Delay(120); // respect rate limits
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Failed to get tracks for {artist}: {ex.Message}");
                    }
                }
            }

            if (!trackUris.Any())
                return Results.Problem("Could not find any tracks on Spotify for these users' songs.");

            Console.WriteLine($"  Final track count: {trackUris.Count}");

            string playlistName = $"SpotiLove: {user1.Name} & {user2.Name}";
            string description = $"A musical match made on SpotiLove 💚 Songs from {user1.Name} and {user2.Name}'s favourite lists.";

            Console.WriteLine($"  Creating playlist: '{playlistName}'");
            var (playlistId, playlistUrl) = await spotify.CreateCollaborativePlaylistAsync(playlistName, description);

            if (string.IsNullOrEmpty(playlistId))
                return Results.Problem("Failed to create Spotify playlist. Check the owner refresh token.");

            var tracksToAdd = trackUris.Take(50).ToList();
            await spotify.AddTracksToPlaylistAsync(playlistId, tracksToAdd);
            Console.WriteLine($"  Added {tracksToAdd.Count} tracks to playlist {playlistId}");

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

    private static List<string> InterleaveSongs(List<string> list1, List<string> list2)
    {
        var result = new List<string>();
        int max = Math.Max(list1.Count, list2.Count);

        for (int i = 0; i < max; i++)
        {
            if (i < list1.Count) result.Add(list1[i]);
            if (i < list2.Count) result.Add(list2[i]);
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
    private static List<string> InterleaveLists(List<string> list1, List<string> list2)
    {
        var result = new List<string>();
        int max = Math.Max(list1.Count, list2.Count);

        for (int i = 0; i < max; i++)
        {
            if (i < list1.Count) result.Add(list1[i]);
            if (i < list2.Count) result.Add(list2[i]);
        }

        return result;
    }
}
