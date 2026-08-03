using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SPT.Common.Http;
using SPT.Reflection.Utils;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Radar
{
    /// <summary>Lazily resolved handle to the client's backend session.</summary>
    internal static class EftSession
    {
        private static IEftSession? _session;

        public static IEftSession Current => _session ??= ClientAppUtils.GetMainApp().GetClientBackEndSession();
    }

    internal static class TraderExtensions
    {
        public static async Task UpdateSupplyData(this EFT.Trading.Trader trader)
        {
            try
            {
                Result<SupplyData> result = await EftSession.Current.GetSupplyData(trader.Id);
                if (result.Succeed)
                    trader._supplyData = result.Value;
                else
                    RadarPlugin.Log.LogWarning($"Failed to download supply data for trader {trader.Id}");
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Error updating supply data for trader {trader.Id}: {e}");
            }
        }
    }

    /// <summary>
    /// Live flea prices served by the optional LootValue server mod.
    /// </summary>
    /// <remarks>
    /// Nothing here ever blocks the main thread: the availability probe and every price lookup run in
    /// the background, and callers get whatever is already cached. Missing prices fall back to the
    /// ragfair price table in <see cref="ItemPricing"/>.
    /// </remarks>
    internal static class FleaPriceCache
    {
        private const string PricePath = "/LootValue/GetItemLowestFleaPrice";

        /// <summary>Cheap, always-present template used to decide whether the endpoint answers at all.</summary>
        private const string ProbeTemplateId = "5c06782b86f77426df5407d2";

        private const double CacheLifetimeSeconds = 300;

        private const int ProbePending = 0;
        private const int ProbeRunning = 1;
        private const int ProbeSucceeded = 2;
        private const int ProbeFailed = 3;

        private static readonly ConcurrentDictionary<string, CachedPrice> Cache =
            new ConcurrentDictionary<string, CachedPrice>();

        private static int _probeState = ProbePending;

        /// <summary>
        /// Whether the LootValue endpoint is answering. Kicks off the probe on first use and reports
        /// false until it lands, so the first loot scan is never stalled on the network.
        /// </summary>
        public static bool EndpointAvailable
        {
            get
            {
                if (Interlocked.CompareExchange(ref _probeState, ProbeRunning, ProbePending) == ProbePending)
                    _ = RunProbeAsync();

                return Volatile.Read(ref _probeState) == ProbeSucceeded;
            }
        }

        /// <summary>
        /// The cached price for a template, refreshing it in the background once stale.
        /// Returns null while a price has yet to arrive.
        /// </summary>
        public static double? FetchPrice(string templateId)
        {
            if (!EftSession.Current.RagFair.Available)
                return null;

            if (!Cache.TryGetValue(templateId, out CachedPrice cached))
            {
                _ = QueryAndCacheAsync(templateId);
                return null;
            }

            if ((DateTime.UtcNow - cached.LastUpdate).TotalSeconds > CacheLifetimeSeconds)
                _ = QueryAndCacheAsync(templateId);

            return cached.Price;
        }

        private static async Task RunProbeAsync()
        {
            double? price = await QueryAndCacheAsync(ProbeTemplateId);
            Volatile.Write(ref _probeState, price != null ? ProbeSucceeded : ProbeFailed);
        }

        private static async Task<double?> QueryAndCacheAsync(string templateId)
        {
            string response;
            try
            {
                string json = JsonConvert.SerializeObject(new FleaPriceRequest(templateId));
                byte[] reply = await RequestHandler.HttpClient.PostAsync(PricePath, Encoding.UTF8.GetBytes(json));
                response = Encoding.UTF8.GetString(reply);
            }
            catch (Exception)
            {
                // LootValue is optional; a missing route is the normal case, not an error worth logging.
                return null;
            }

            if (string.IsNullOrEmpty(response) || response == "null")
                return null;

            if (!double.TryParse(response, NumberStyles.Float, CultureInfo.InvariantCulture, out double price))
                return null;

            // LootValue reports a negative price for items it refuses to value.
            if (price < 0)
            {
                Cache.TryRemove(templateId, out _);
                return null;
            }

            Cache[templateId] = new CachedPrice(price);
            return price;
        }

        private sealed class FleaPriceRequest
        {
            public string templateId;

            public FleaPriceRequest(string templateId) => this.templateId = templateId;
        }

        private readonly struct CachedPrice
        {
            public double Price { get; }
            public DateTime LastUpdate { get; }

            public CachedPrice(double price)
            {
                Price = price;
                LastUpdate = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Works out what an item is worth, taking the better of the flea price and the best trader offer.
    /// </summary>
    internal static class ItemPricing
    {
        /// <summary>Ragfair price table, downloaded once per raid.</summary>
        private static Dictionary<string, float>? _fleaPrices;

        /// <summary>Best trader offer per item name; trader prices do not move during a raid.</summary>
        private static readonly Dictionary<string, int> TraderPrices = new Dictionary<string, int>();

        private static bool _initStarted;

        internal static bool Ready { get; private set; }

        /// <summary>Downloads trader supply data and the ragfair price table in the background.</summary>
        public static void Init(MonoBehaviour coroutineRunner)
        {
            if (_initStarted) return;

            _initStarted = true;
            coroutineRunner.StartCoroutine(InitCoroutine());
        }

        public static int GetBestPrice(Item item) => Mathf.Max(GetFleaPrice(item), GetBestTraderPrice(item));

        private static IEnumerator InitCoroutine()
        {
            var traderTasks = EftSession.Current.Traders.Select(trader => trader.UpdateSupplyData()).ToList();

            bool fleaPricesCompleted = false;
            EftSession.Current.RagfairGetPrices(result =>
            {
                if (result.Succeed && result.Value != null)
                    _fleaPrices = result.Value;
                else
                    RadarPlugin.Log.LogWarning("Failed to get ragfair price table");

                fleaPricesCompleted = true;
            });

            while (traderTasks.Any(task => !task.IsCompleted))
                yield return null;

            while (!fleaPricesCompleted)
                yield return null;

            Ready = true;
        }

        private static int GetFleaPrice(Item item)
        {
            string templateId = item.TemplateId;

            if (FleaPriceCache.EndpointAvailable)
            {
                double? price = FleaPriceCache.FetchPrice(templateId);
                if (price != null)
                    return (int)price.Value;
            }

            if (_fleaPrices != null && _fleaPrices.TryGetValue(templateId, out float tablePrice))
                return (int)tablePrice;

            return -1;
        }

        private static int GetBestTraderPrice(Item item)
        {
            if (TraderPrices.TryGetValue(item.Name, out int cached))
                return cached;

            TraderOffer? offer = GetAllTraderOffers(item)?.FirstOrDefault();
            if (offer == null)
                return 0;

            TraderPrices[item.Name] = offer.Price;
            return offer.Price;
        }

        private static IEnumerable<TraderOffer?>? GetAllTraderOffers(Item item)
        {
            switch (item.Owner?.OwnerType)
            {
                case EOwnerType.RagFair:
                case EOwnerType.Trader:
                    // Trader and flea stock is priced per stack; value a single unit instead.
                    if (item.StackObjectsCount > 1 || item.UnlimitedCount)
                    {
                        item = item.CloneItem();
                        item.StackObjectsCount = 1;
                        item.UnlimitedCount = false;
                    }
                    break;
            }

            return EftSession.Current.Traders
                .Select(trader => GetTraderOffer(item, trader))
                .Where(offer => offer != null)
                .OrderByDescending(offer => offer?.Price * offer?.Course);
        }

        private static TraderOffer? GetTraderOffer(Item item, EFT.Trading.Trader trader)
        {
            if (trader._supplyData == null)
                return null;

            var result = trader.GetUserItemPrice(item);
            return result == null
                ? null
                : new TraderOffer(
                    trader.LocalizedName,
                    result.Value.Amount,
                    trader.CurrencyCourses[result.Value.CurrencyId],
                    item.StackObjectsCount);
        }

        public sealed class TraderOffer
        {
            public string Name { get; }
            public int Price { get; }
            public double Course { get; }
            public int Count { get; }

            public TraderOffer(string name, int price, double course, int count)
            {
                Name = name;
                Price = price;
                Course = course;
                Count = count;
            }
        }
    }
}
