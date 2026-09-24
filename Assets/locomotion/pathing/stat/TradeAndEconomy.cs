using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TradeCommodityShare
{
    public string commodityKey;
    [Range(0f, 1f)] public float produceShare01;
    [Range(0f, 1f)] public float consumeShare01;
    public float softPrice = 1f;
}

public enum TradeSettlementMode
{
    BarterOnly = 0,
    LedgerOnly = 1,
    BarterOrLedger = 2,
    ContractStanding = 3
}

public enum TradeRelationKind
{
    Commodity = 0,
    AllergyAvoid = 1,
    AllergySensitize = 2,
    ChemicalBond = 3,
    ChemicalRelease = 4
}

[Serializable]
public sealed class TradeEdge
{
    public string edgeId;
    public string fromRetinueId;
    public string toRetinueId;
    public string commodityKey;
    [Range(0f, 1f)] public float affinity01 = 0.5f;
    [Range(0f, 1f)] public float volumeShare01 = 0.1f;
    public float unitPrice = -1f;
    [CronExpr] public string openCron = "* * * * *";
    public bool bidirectional;
    public TradeSettlementMode settlement = TradeSettlementMode.BarterOrLedger;
    public string brokerPersonaKey;
    public int minPeckingOrder = 100;
    public TradeRelationKind relationKind = TradeRelationKind.Commodity;
    public string allergenOrBondKey;
    [Range(0f, 1f)] public float sensitization01;
    [Range(0f, 1f)] public float bondStrength01;
    public bool irreversible;
    public string metaboliteKey;
}

[Serializable]
public sealed class TradeDemographics
{
    public string scopeId;
    public List<TradeCommodityShare> commodities = new List<TradeCommodityShare>();
    public List<TradeEdge> edges = new List<TradeEdge>();
    [Range(0f, 1f)] public float slack01 = 0.08f;

    public static TradeDemographics FromSocietyFeatures(
        IReadOnlyDictionary<string, float> societyFeatures,
        string scopeId = "city")
    {
        var d = new TradeDemographics { scopeId = scopeId ?? "city" };
        float commercial = 0.5f;
        if (societyFeatures != null)
        {
            if (societyFeatures.TryGetValue("commercial_activity", out float c) ||
                societyFeatures.TryGetValue("commercialActivity", out c))
                commercial = Mathf.Clamp01(c);
        }
        d.commodities.Add(new TradeCommodityShare
        {
            commodityKey = "generic",
            produceShare01 = commercial,
            consumeShare01 = 1f - commercial * 0.5f,
            softPrice = 1f
        });
        return d;
    }

    public TradeEdge BetweenRetinues(
        string fromId, string toId, string commodity, float volumeShare01, float affinity01)
    {
        if (edges == null) edges = new List<TradeEdge>();
        string id = $"{fromId}->{toId}:{commodity}";
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i] != null && edges[i].edgeId == id)
            {
                edges[i].volumeShare01 = Mathf.Clamp01(volumeShare01);
                edges[i].affinity01 = Mathf.Clamp01(affinity01);
                return edges[i];
            }
        }
        var edge = new TradeEdge
        {
            edgeId = id,
            fromRetinueId = fromId,
            toRetinueId = toId,
            commodityKey = commodity,
            volumeShare01 = Mathf.Clamp01(volumeShare01),
            affinity01 = Mathf.Clamp01(affinity01)
        };
        edges.Add(edge);
        return edge;
    }

    public TradeEdge SamplePartner(string fromId, string commodity, int seed)
    {
        if (edges == null || edges.Count == 0) return null;
        float sum = 0f;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e == null) continue;
            if (e.relationKind == TradeRelationKind.AllergyAvoid) continue;
            if (!string.Equals(e.fromRetinueId, fromId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(commodity) &&
                !string.Equals(e.commodityKey, commodity, StringComparison.OrdinalIgnoreCase)) continue;
            float aff = e.affinity01;
            if (e.relationKind == TradeRelationKind.ChemicalBond)
                aff *= Mathf.Max(0.01f, e.bondStrength01);
            sum += Mathf.Max(0.01f, aff * e.volumeShare01);
        }
        if (sum <= 0f) return null;
        var rng = new System.Random(seed);
        float pick = (float)rng.NextDouble() * sum;
        float acc = 0f;
        for (int i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (e == null) continue;
            if (e.relationKind == TradeRelationKind.AllergyAvoid) continue;
            if (!string.Equals(e.fromRetinueId, fromId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(commodity) &&
                !string.Equals(e.commodityKey, commodity, StringComparison.OrdinalIgnoreCase)) continue;
            float aff = e.affinity01;
            if (e.relationKind == TradeRelationKind.ChemicalBond)
                aff *= Mathf.Max(0.01f, e.bondStrength01);
            acc += Mathf.Max(0.01f, aff * e.volumeShare01);
            if (pick <= acc) return e;
        }
        return null;
    }

    public TradeEdge EnsureRelation(
        string fromId, string toId, string commodity, TradeRelationKind kind,
        float affinity01 = 0.5f, float bondOrSens01 = 0.5f)
    {
        if (edges == null) edges = new List<TradeEdge>();
        string id = $"{fromId}->{toId}:{commodity}:{kind}";
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i] != null && edges[i].edgeId == id)
            {
                edges[i].relationKind = kind;
                edges[i].affinity01 = Mathf.Clamp01(affinity01);
                if (kind == TradeRelationKind.ChemicalBond || kind == TradeRelationKind.ChemicalRelease)
                    edges[i].bondStrength01 = Mathf.Clamp01(bondOrSens01);
                else
                    edges[i].sensitization01 = Mathf.Clamp01(bondOrSens01);
                edges[i].allergenOrBondKey = commodity;
                return edges[i];
            }
        }
        var edge = new TradeEdge
        {
            edgeId = id,
            fromRetinueId = fromId,
            toRetinueId = toId,
            commodityKey = commodity,
            allergenOrBondKey = commodity,
            relationKind = kind,
            affinity01 = Mathf.Clamp01(affinity01),
            volumeShare01 = 0.1f
        };
        if (kind == TradeRelationKind.ChemicalBond || kind == TradeRelationKind.ChemicalRelease)
            edge.bondStrength01 = Mathf.Clamp01(bondOrSens01);
        else
            edge.sensitization01 = Mathf.Clamp01(bondOrSens01);
        edges.Add(edge);
        return edge;
    }

    public void Reconcile()
    {
        if (commodities == null) return;
        for (int i = 0; i < commodities.Count; i++)
        {
            if (commodities[i] == null) continue;
            commodities[i].produceShare01 = Mathf.Clamp01(commodities[i].produceShare01);
            commodities[i].consumeShare01 = Mathf.Clamp01(commodities[i].consumeShare01);
        }
        if (edges == null) return;
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i] == null) continue;
            edges[i].volumeShare01 = Mathf.Clamp01(edges[i].volumeShare01);
            edges[i].affinity01 = Mathf.Clamp01(edges[i].affinity01);
        }
    }
}

public struct TradeDealRequest
{
    public string fromRetinueId;
    public string toRetinueId;
    public string commodityKey;
    public string brokerPersonaKey;
    public float quantity;
    public DateTime utcNow;
}

public struct TradeDealResult
{
    public bool allowed;
    public TradeEdge edge;
    public string reason;
}

[Serializable]
public sealed class LedgerBalance
{
    public string commodityKey;
    public float quantity;
    public float reserved;
}

[Serializable]
public sealed class EconomicOverlay
{
    public string retinueId;
    public string companyId;
    public bool enabled;
    [CronExpr] public string marketHoursCron = "* 8-20 * * *";
    [Range(0f, 1f)] public float liquidity01 = 0.5f;
    [Range(0f, 1f)] public float velocity01;
    public List<LedgerBalance> balances = new List<LedgerBalance>();
    public List<string> exportCommodityKeys = new List<string>();
    public List<string> importCommodityKeys = new List<string>();
    public float priceIndex = 1f;

    public static EconomicOverlay FromRetinue(string retinueId, in StatSeed seed)
    {
        return new EconomicOverlay
        {
            retinueId = retinueId ?? seed.retinueId ?? "retinue",
            companyId = seed.companyId,
            enabled = true,
            liquidity01 = 0.5f
        };
    }

    public static EconomicOverlay FromCompany(CompanyRegistration company, StoreBase store, in StatSeed seed)
    {
        var o = FromRetinue(company != null ? company.companyId : seed.retinueId, seed);
        if (company != null)
        {
            o.companyId = company.companyId;
            float sum = 0f;
            if (company.fundingSources != null)
                for (int i = 0; i < company.fundingSources.Count; i++)
                    if (company.fundingSources[i] != null)
                        sum += Mathf.Max(0f, company.fundingSources[i].share01);
            if (sum > 1e-5f)
                o.liquidity01 = Mathf.Clamp01(sum);
            SyncFundingToCredit(o, company);
        }
        if (store?.shelves != null)
            SyncShelves(o, store);
        return o;
    }

    static void SyncFundingToCredit(EconomicOverlay o, CompanyRegistration company)
    {
        if (o.balances == null) o.balances = new List<LedgerBalance>();
        float credit = 0f;
        if (company.fundingSources != null)
            for (int i = 0; i < company.fundingSources.Count; i++)
                if (company.fundingSources[i] != null)
                    credit += company.fundingSources[i].share01 * 100f;
        UpsertBalance(o, "credit", credit);
    }

    public static void SyncShelves(EconomicOverlay o, StoreBase store)
    {
        if (o == null || store?.shelves == null) return;
        for (int i = 0; i < store.shelves.Count; i++)
        {
            var s = store.shelves[i];
            if (s == null || string.IsNullOrEmpty(s.commodityKey)) continue;
            UpsertBalance(o, s.commodityKey, s.quantity);
        }
    }

    public static void UpsertBalance(EconomicOverlay o, string key, float qty)
    {
        if (o.balances == null) o.balances = new List<LedgerBalance>();
        for (int i = 0; i < o.balances.Count; i++)
        {
            if (o.balances[i] != null &&
                string.Equals(o.balances[i].commodityKey, key, StringComparison.OrdinalIgnoreCase))
            {
                o.balances[i].quantity = qty;
                return;
            }
        }
        o.balances.Add(new LedgerBalance { commodityKey = key, quantity = qty });
    }

    public bool TryGetBalance(string key, out float qty)
    {
        qty = 0f;
        if (balances == null) return false;
        for (int i = 0; i < balances.Count; i++)
        {
            if (balances[i] != null &&
                string.Equals(balances[i].commodityKey, key, StringComparison.OrdinalIgnoreCase))
            {
                qty = balances[i].quantity - balances[i].reserved;
                return true;
            }
        }
        return false;
    }

    public void AdjustBalance(string key, float delta)
    {
        if (balances == null) balances = new List<LedgerBalance>();
        for (int i = 0; i < balances.Count; i++)
        {
            if (balances[i] != null &&
                string.Equals(balances[i].commodityKey, key, StringComparison.OrdinalIgnoreCase))
            {
                balances[i].quantity = Mathf.Max(0f, balances[i].quantity + delta);
                return;
            }
        }
        if (delta > 0f)
            balances.Add(new LedgerBalance { commodityKey = key, quantity = delta });
    }
}
