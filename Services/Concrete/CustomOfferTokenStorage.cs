using Server.Helpers;

namespace Server.Services.Concrete;

public class OfferToken {
    public string UserId { get; set; }
    public int OfferId { get; set; }
}

public class CustomOfferTokenStorage
{
    private readonly TimedDict<string, OfferToken> _storage = new();

    public string Add(int offerId, string userId)
    {
        var guid = Guid.NewGuid().ToString();
        _storage.Set(guid, new OfferToken
        {
            UserId = userId,
            OfferId = offerId
        });
        return guid;
    }

    public OfferToken? Access(string token)
    {
        return _storage.TryGet(token, out var pair) ? pair : null;
    }
}