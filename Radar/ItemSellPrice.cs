using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using SPT.Common.Http;
using SPT.Reflection.Utils;

internal static class TraderClassExtensions
{
    private static ISession _Session;
    private static ISession Session => _Session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();

    private static readonly FieldInfo SupplyDataField =
        typeof(TraderClass).GetField("supplyData_0", BindingFlags.NonPublic | BindingFlags.Instance);

    public static SupplyData GetSupplyData(this TraderClass trader) =>
        SupplyDataField.GetValue(trader) as SupplyData;

    public static void SetSupplyData(this TraderClass trader, SupplyData supplyData) =>
        SupplyDataField.SetValue(trader, supplyData);

    public static async void UpdateSupplyData(this TraderClass trader)
    {
        Result<SupplyData> result = await Session.GetSupplyData(trader.Id);
        if (result.Succeed)
            trader.SetSupplyData(result.Value);
        else
            UnityEngine.Debug.LogError("Failed to download supply data");
    }
}

class ItemExtensions
{
    public static ISession Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
    static Dictionary<string, int> fleaCache = new Dictionary<string, int>();
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

    public static TraderOffer? GetTraderOffer(Item item, TraderClass trader)
    {
        var result = trader.GetUserItemPrice(item);
        return result is null ? null : new(
            trader.LocalizedName,
            result.Value.Amount,
            trader.GetSupplyData().CurrencyCourses[result.Value.CurrencyId],
            item.StackObjectsCount
        );
    }

    public static IEnumerable<TraderOffer?>? GetAllTraderOffers(Item item)
    {
        if (!Session.Profile.Examined(item))
            return null;
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
    public class FleaPriceRequest
    {
        public string templateId;
        public FleaPriceRequest(string templateId) => this.templateId = templateId;
    }

    public static int GetFleaPrice(Item item)
    {
        ISession Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();
        if (!Session.RagFair.Available)
        {
            return 0;
        }

        if (fleaCache.ContainsKey(item.Name))
        {
            return fleaCache[item.Name];
        }

        var response = RequestHandler.PostJson("/LootValue/GetItemLowestFleaPrice", JsonConvert.SerializeObject(new FleaPriceRequest(item.TemplateId)));
        bool hasPlayerFleaPrice = !(string.IsNullOrEmpty(response) || response == "null");
        var price = 0;
        if (hasPlayerFleaPrice)
        {
            try
            {
                price = int.Parse(response);
            }
            catch (FormatException) { }
        }
        fleaCache[item.Name] = price;
        return price;
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
        }
        traderCache[item.Name] = price;
        return price;
    }
        

    public static int GetBestPrice(Item item)
    {                                                                                 
        var fleaPrice = GetFleaPrice(item);
        var traderPrice = GetBestTraderPrice(item);
        return fleaPrice > traderPrice ? fleaPrice : traderPrice;
    }
}