// OpportunityPricing.cs
//
// How to run:
//   .NET 9+:  dotnet run OpportunityPricing.cs
//   .NET 6-8: drop into any console project (`dotnet new console`), replace
//             Program.cs with this file, then `dotnet run`.
//   Or paste into dotnetfiddle.net / Replit and run.
//
// ============================================================================
//  H1 — Refactor the brittle pricing function
// ============================================================================
//
//  This is legacy code from our CRM. It calculates the total value of an
//  Opportunity from its line items. Sales ops has flagged that totals
//  occasionally don't match what they see in the quoting tool, especially on
//  large opportunities and on LATAM deals.
//
//  Your task:
//    1) Read it. Tell us what's wrong — bugs and code smells both.
//    2) Refactor it so it's correct and testable. Ask us about anything
//       ambiguous before assuming a business rule.
//    3) Write at least one unit test. Cover a line item with a discount.
//
//  You can use any libraries and AI tools. Please narrate when you reach for
//  AI — what you asked, what you took, what you rejected.
//
// ============================================================================

using System;
using System.Collections.Generic;

public class LineItem
{
    public string Sku { get; set; }
    public string Name { get; set; }
    public double UnitPrice { get; set; }
    public int Quantity { get; set; }
    public double? DiscountPercent { get; set; }  // 0-100, sometimes null
    public double? DiscountAmount { get; set; }   // flat amount off, sometimes null
    public bool IsTaxable { get; set; }
}

public class Opportunity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<LineItem> LineItems { get; set; }
    public string CustomerRegion { get; set; }  // "US", "EU", "LATAM"
}

public class OpportunityPricer
{
    public string CalculateTotal(Opportunity opp)
    {
        double subtotal = 0;
        double taxTotal = 0;

        foreach (var line in opp.LineItems)
        {
            double lineTotal = line.UnitPrice * line.Quantity;

            if (line.DiscountPercent > 0)
            {
                if (opp.CustomerRegion == "LATAM")
                {
                    // LATAM gets an extra 5% on top of any other discount
                    lineTotal = lineTotal - (lineTotal * (line.DiscountPercent.Value / 100));
                    lineTotal = lineTotal - (lineTotal * 0.05);
                }
                else
                {
                    lineTotal = lineTotal - (lineTotal * (line.DiscountPercent.Value / 100));
                }
            }

            if (line.DiscountAmount > 0)
            {
                lineTotal = lineTotal - line.DiscountAmount.Value;
            }

            if (line.IsTaxable)
            {
                if (opp.CustomerRegion == "US")
                {
                    taxTotal = taxTotal + (lineTotal * 0.0875);
                }
                else if (opp.CustomerRegion == "EU")
                {
                    taxTotal = taxTotal + (lineTotal * 0.20);
                }
                else
                {
                    taxTotal = taxTotal + (lineTotal * 0.0875);
                }
            }

            subtotal = subtotal + lineTotal;
        }

        double grandTotal = subtotal + taxTotal;

        return "$" + grandTotal.ToString("F2");
    }
}

public class Program
{
    public static void Main()
    {
        var opp = new Opportunity
        {
            Id = "OPP-558",
            Name = "Acme Q2 Renewal",
            CustomerRegion = "LATAM",
            LineItems = new List<LineItem>
            {
                new LineItem {
                    Sku = "SKU-001", Name = "Pro License",
                    UnitPrice = 1200.00, Quantity = 10,
                    DiscountPercent = 10, IsTaxable = true
                },
                new LineItem {
                    Sku = "SKU-002", Name = "Support Hours",
                    UnitPrice = 175.00, Quantity = 40,
                    DiscountAmount = 500, IsTaxable = false
                },
                new LineItem {
                    Sku = "SKU-003", Name = "Implementation Fee",
                    UnitPrice = 0.10, Quantity = 3,
                    IsTaxable = true
                },
            }
        };

        var pricer = new OpportunityPricer();
        Console.WriteLine($"Total: {pricer.CalculateTotal(opp)}");
    }
}
