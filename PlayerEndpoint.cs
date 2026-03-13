// ============================================================
// ADD THIS FILE to your backend project root
// Then in Program.cs, add:  app.MapSpotifyPlayer();
// ============================================================

namespace Spotilove;

public static class PlayerEndpoint
{
    // Stores Spotify access tokens per user (in-memory, lives as long as server runs)
    private static readonly Dictionary<Guid, string> _userTokens = new();

    // Called from SpotifyService after OAuth — stores the real Spotify access token
    public static void StoreToken(Guid userId, string spotifyAccessToken)
    {
        _userTokens[userId] = spotifyAccessToken;
    }

    public static string? GetToken(Guid userId)
    {
        return _userTokens.TryGetValue(userId, out var token) ? token : null;
    }

    public static void MapSpotifyPlayer(this WebApplication app)
    {
        // Returns the stored Spotify access token for a user
        app.MapGet("/player/token/{userId:guid}", (Guid userId) =>
        {
            var token = GetToken(userId);
            if (token == null)
                return Results.NotFound(new { success = false, message = "No Spotify token found. Please log in with Spotify." });

            return Results.Ok(new { success = true, token });
        })
        .WithName("GetPlayerToken")
        .WithSummary("Get Spotify access token for Web Playback SDK");

        // Serves the HTML player page
        app.MapGet("/player", (string? token) =>
        {
            var html = BuildPlayerHtml(token ?? "");
            return Results.Content(html, "text/html");
        })
        .WithName("SpotifyPlayer")
        .WithSummary("Spotify Web Playback SDK HTML player");
    }

    private static string BuildPlayerHtml(string token) => $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>SpotiLove Player</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ background: transparent; overflow: hidden; }}
    </style>
</head>
<body>
<script src='https://sdk.scdn.co/spotify-player.js'></script>
<script>
    let player;
    let deviceId;
    let currentToken = '{token}';

    // ── JS → C# bridge ──────────────────────────────────────
    // Uses a hidden iframe with a custom scheme to fire MAUI's Navigating event
    // without navigating the main page away.
    function sendToNative(event, data) {{
        try {{
            const params = new URLSearchParams({{ event, data: JSON.stringify(data) }});
            const iframe = document.createElement('iframe');
            iframe.style.display = 'none';
            iframe.src = 'spotilove-event://' + event + '?' + params.toString();
            document.body.appendChild(iframe);
            setTimeout(() => iframe.remove(), 500);
        }} catch(e) {{
            console.error('sendToNative error:', e);
        }}
    }}

    // ── SDK init ─────────────────────────────────────────────
    window.onSpotifyWebPlaybackSDKReady = () => {{
        player = new Spotify.Player({{
            name: 'SpotiLove',
            getOAuthToken: cb => cb(currentToken),
            volume: 0.8
        }});

        player.addListener('ready', ({{ device_id }}) => {{
            deviceId = device_id;
            console.log('Player ready, device:', device_id);
            sendToNative('ready', {{ deviceId: device_id }});
        }});

        player.addListener('not_ready', ({{ device_id }}) => {{
            console.log('Player not ready:', device_id);
            sendToNative('not_ready', {{ deviceId: device_id }});
        }});

        player.addListener('player_state_changed', state => {{
            if (!state) return;
            sendToNative('state_changed', {{
                paused: state.paused,
                position: state.position,
                duration: state.duration,
                trackId: state.track_window?.current_track?.id,
                trackName: state.track_window?.current_track?.name
            }});
        }});

        player.addListener('initialization_error', ({{ message }}) => {{
            sendToNative('error', {{ type: 'init', message }});
        }});

        player.addListener('authentication_error', ({{ message }}) => {{
            sendToNative('error', {{ type: 'auth', message }});
        }});

        player.addListener('account_error', ({{ message }}) => {{
            sendToNative('error', {{ type: 'account', message }});
        }});

        player.addListener('playback_error', ({{ message }}) => {{
            sendToNative('error', {{ type: 'playback', message }});
        }});

        player.connect().then(success => {{
            console.log('Connect result:', success);
            sendToNative('connect', {{ success }});
        }});
    }};

    // ── C# → JS commands ─────────────────────────────────────
    window.playTrack = function(spotifyUri) {{
        if (!deviceId) {{
            sendToNative('error', {{ type: 'no_device', message: 'Player not ready yet' }});
            return;
        }}
        fetch('https://api.spotify.com/v1/me/player/play?device_id=' + deviceId, {{
            method: 'PUT',
            body: JSON.stringify({{ uris: [spotifyUri] }}),
            headers: {{
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + currentToken
            }}
        }}).then(r => {{
            if (!r.ok) r.text().then(t => sendToNative('error', {{ type: 'play_failed', message: t }}));
        }}).catch(e => sendToNative('error', {{ type: 'fetch', message: e.message }}));
    }};

    window.pauseTrack = function() {{ player?.pause(); }};
    window.resumeTrack = function() {{ player?.resume(); }};
    window.stopTrack = function() {{ player?.pause(); }};

    window.updateToken = function(newToken) {{
        currentToken = newToken;
    }};

    window.setVolume = function(vol) {{
        player?.setVolume(vol);
    }};
</script>
</body>
</html>";
}
