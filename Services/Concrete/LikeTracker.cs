using System.Text;

namespace Server.Services.Concrete;

public class LikeTracker
{
    private readonly Dictionary<string, int> _likers = new();
    private readonly Dictionary<string, int> _liked = new();
    private int LikeCount = 0;

    public void RegisterLike(string liker, string liked)
    {
        lock (_likers)
        {
            if (!_likers.TryAdd(liker, 1))
            {
                _likers[liker] += 1;
            }
        }
        
        lock (liked)
        {
            if (!_liked.TryAdd(liked, 1))
            {
                _liked[liked] += 1;
            }
        }

        LikeCount++;
    }

    public string GetStats()
    {
        var sb = new StringBuilder();
        if (_likers.Count == 0 || _liked.Count == 0)
        {
            return "No likes yet";
        }
        
        lock (_likers)
        {
            var topliker = _likers.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First();
            sb.Append($"Top liker: {topliker.Key} ({topliker.Value})\n");
        }
        
        lock (_liked)
        {
            var topliked = _liked.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First();
            sb.Append($"Top liked: {topliked.Key} ({topliked.Value})\n");
        }

        sb.Append($"All likes: {LikeCount}");
        
        return sb.ToString();
    }
}