using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


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
            Debug.LogError("Failed to download supply data");
    }
}

class ItemExtensions
{
    public static ISession Session = ClientAppUtils.GetMainApp().GetClientBackEndSession();

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

    public static TraderOffer GetTraderOffer(Item item, TraderClass trader)
    {
        var result = trader.GetUserItemPrice(item);
        return result is null ? null : new(
            trader.LocalizedName,
            result.Value.Amount,
            trader.GetSupplyData().CurrencyCourses[result.Value.CurrencyId],
            item.StackObjectsCount
        );
    }

    public static IEnumerable<TraderOffer> GetAllTraderOffers(Item item)
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
            .Where(offer => offer is not null)
            .OrderByDescending(offer => offer.Price * offer.Course);
    }

    public static TraderOffer GetBestTraderOffer(Item item) =>
        GetAllTraderOffers(item)?.FirstOrDefault() ?? null;
}