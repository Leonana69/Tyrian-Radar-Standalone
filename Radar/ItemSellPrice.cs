using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Common.Http;
using Newtonsoft.Json;
using System;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using static System.Collections.Specialized.BitVector32;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.UI;
using System.Text;

internal static class TraderClassExtensions
{
    private static ISession Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
    public static bool IsInit = false;
    public static async Task UpdateSupplyData(this TraderClass trader)
    {
        try
        {
            Result<SupplyData> result = await Session.GetSupplyData(trader.Id);
            if (result.Succeed)
            {
                trader.SupplyData_0 = result.Value;
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to download supply data for trader {trader.Id}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error updating supply data for trader {trader.Id}: {ex}");
        }
    }
}

internal static class FleaPriceCache
{
    private static ISession Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
    static Dictionary<string, CachePrice> cache = new Dictionary<string, CachePrice>();
    public static bool? valid;

    public static bool? IsValid()
    {
        if (valid == null)
        {

            double? price = Task.Run(() => QueryAndTryUpsertPrice("5c06782b86f77426df5407d2")).Result;
            if (price != null)
            {
                valid = true;
            }
            else
            {
                valid = false;
            }
        }
        return valid;
    }

    public static double? FetchPrice(string templateId)
    {
        bool fleaAvailable = Session.RagFair.Available;

        if (!fleaAvailable)
            return null;

        if (cache.ContainsKey(templateId))
        {
            double secondsSinceLastUpdate = (DateTime.Now - cache[templateId].lastUpdate).TotalSeconds;
            if (secondsSinceLastUpdate > 300)
                _ = QueryAndTryUpsertPrice(templateId);
            return cache[templateId].price;
        }
        else
        {
            _ = QueryAndTryUpsertPrice(templateId);
            return null;
        }
    }

    public class FleaPriceRequest
    {
        public string templateId;
        public FleaPriceRequest(string templateId) => this.templateId = templateId;
    }

    private static async Task<string> QueryPrice(string templateId)
    {
        string json = JsonConvert.SerializeObject(new FleaPriceRequest(templateId));
        string path = "/LootValue/GetItemLowestFleaPrice";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        byte[] bytes2 = await RequestHandler.HttpClient.PostAsync(path, bytes);
        string @string = Encoding.UTF8.GetString(bytes2);
        return @string;
    }

    private static async Task<double?> QueryAndTryUpsertPrice(string templateId)
    {
        string response;
        try
        {
            response = await QueryPrice(templateId);
        } catch (Exception ex)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(response) && response != "null")
        {
            double price = double.Parse(response);

            if (price < 0)
            {
                cache.Remove(templateId);
                return null;
            }

            cache[templateId] = new CachePrice(price);

            return price;
        }

        return null;
    }
}

internal struct CachePrice
{
    public double price { get; private set; }
    public DateTime lastUpdate { get; private set; }

    public CachePrice(double price)
    {
        this.price = price;
        lastUpdate = DateTime.Now;
    }
}

class ItemExtensions : MonoBehaviour
{
    public static ISession Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
    static Dictionary<string, float>? fleaCache = null;
    static Dictionary<string, int> traderCache = new Dictionary<string, int>();

    public sealed class TraderOffer
    {
        public string Name;
        public int Price;
        public double Course;
        public int Count;

        public TraderOffer(string name, int price, double course, int count)
        {
            Name = name;
            Price = price;
            Course = course;
            Count = count;
        }
    }

    public static void Init(MonoBehaviour coroutineRunner)
    {
        if (TraderClassExtensions.IsInit)
        {
            return;
        }

        coroutineRunner.StartCoroutine(InitCoroutine());
    }

    private static IEnumerator InitCoroutine()
    {
        // Start trader updates
        var traderTasks = new List<Task>();
        foreach (var trader in Session.Traders)
        {
            traderTasks.Add(TraderClassExtensions.UpdateSupplyData(trader));
        }

        // Start flea prices request
        var fleaPricesCompleted = false;
        Session.RagfairGetPrices(result =>
        {
            if (result.Succeed && result.Value != null)
            {
                fleaCache = result.Value;
            }
            else
            {
                UnityEngine.Debug.LogError("[RADAR] Failed to get Ragfair database");
            }
            fleaPricesCompleted = true;
        });

        // Wait for all traders to complete
        while (traderTasks.Any(t => !t.IsCompleted))
        {
            yield return null;
        }

        // Wait for flea prices to complete
        while (!fleaPricesCompleted)
        {
            yield return null;
        }

        TraderClassExtensions.IsInit = true;
    }

    public static TraderOffer? GetTraderOffer(Item item, TraderClass trader)
    {
        if (trader.SupplyData_0 != null)
        {
            var result = trader.GetUserItemPrice(item);
            return result is null ? null : new(
                trader.LocalizedName,
                result.Value.Amount,
                trader.Dictionary_0[result.Value.CurrencyId],
                item.StackObjectsCount
            );
        }
        else
        {
            return null;
        }
    }

    public static IEnumerable<TraderOffer?>? GetAllTraderOffers(Item item)
    {
        //if (!Session.Profile.Examined(item))
        //    return null;
        switch (item.Owner?.OwnerType)
        {
            case EOwnerType.RagFair:
            case EOwnerType.Trader:
                if (item.StackObjectsCount > 1 || item.UnlimitedCount)
                {
                    item = item.CloneItem();
                    item.StackObjectsCount = 1;
                    item.UnlimitedCount = false;
                }
                break;
        }
        return Session.Traders
            .Select(trader => GetTraderOffer(item, trader))
            .Where(offer => offer != null)
            .OrderByDescending(offer => offer?.Price * offer?.Course);
    }

    public static int GetFleaPrice(Item item)
    {
        if (FleaPriceCache.IsValid() == true)
        {
            double? price = FleaPriceCache.FetchPrice(item.TemplateId);

            if (price != null)
                return (int) price;
        }

        if (fleaCache != null && fleaCache.ContainsKey(item.TemplateId))
        {
            return (int) fleaCache[item.TemplateId];
        }
        else
        {
            return -1;
        }
    }

    public static int GetBestTraderPrice(Item item)
    {
        if (traderCache.ContainsKey(item.Name))
        {
            return traderCache[item.Name];
        }
        var offer = GetAllTraderOffers(item)?.FirstOrDefault() ?? null;
        var price = 0;
        if (offer != null)
        {
            price = offer.Price;
            traderCache[item.Name] = price;
        }
        
        return price;
    }

    public static int GetBestPrice(Item item)
    {
        var fleaPrice = GetFleaPrice(item);
        var traderPrice = GetBestTraderPrice(item);
        return fleaPrice > traderPrice ? fleaPrice : traderPrice;
    }
}