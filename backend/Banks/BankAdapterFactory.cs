namespace TradingBot.Banks;

public class BankAdapterFactory
{
    private readonly Dictionary<string, IBankAdapter> _adapters = new()
    {
        ["denizbank"] = new DenizBankAdapter(),
        ["akbank"] = new AkbankAdapter(),
        ["yapikredi"] = new YapiKrediAdapter(),
    };

    public IBankAdapter Get(string? name = null)
    {
        var key = name?.ToLowerInvariant() ?? "denizbank";
        if (key == "mock") key = "denizbank";
        return _adapters.TryGetValue(key, out var adapter) ? adapter : _adapters["denizbank"];
    }

    public IBankAdapter GetDefault() => Get("denizbank");
}
