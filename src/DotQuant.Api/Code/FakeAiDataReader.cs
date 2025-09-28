using AiHedgeFund.Contracts.Model;

namespace DotQuant.Api.Code;

public class FakeAiDataReader : AiHedgeFund.Contracts.IDataReader
{
    public bool TryGetPrices(string ticker, DateTime startDate, DateTime endDate, out IEnumerable<Price>? prices)
    {
        prices = null;
        if (string.IsNullOrWhiteSpace(ticker) || startDate > endDate)
            return false;

        var random = new Random();
        var result = new List<Price>();
        var currentDate = startDate;

        while (currentDate <= endDate)
        {
            result.Add(new Price
            {
                Date = currentDate,
                Open = (decimal)(random.NextDouble() * 100),
                High = (decimal)(random.NextDouble() * 100),
                Low = (decimal)(random.NextDouble() * 100),
                Close = (decimal)(random.NextDouble() * 100),
                Volume = (decimal)random.Next(1000, 100000)
            });
            currentDate = currentDate.AddDays(1);
        }

        prices = result;
        return true;
    }

    public bool TryGetFinancialMetrics(string ticker, DateTime endDate, string period, int limit,
        out IEnumerable<FinancialMetrics>? metrics)
    {
        metrics = null;
        if (string.IsNullOrWhiteSpace(ticker) || limit <= 0)
            return false;

        var result = new List<FinancialMetrics>();
        var random = new Random();

        for (var i = 0; i < limit; i++)
        {
            result.Add(new FinancialMetrics
            {
                Ticker = ticker,
                Industry = "Tech",
                ReportPeriod = endDate.AddMonths(-i).ToString("yyyy-MM"),
                Period = period,
                EndDate = endDate.AddMonths(-i),
                Currency = "USD",
                MarketCap = (decimal?)(random.NextDouble() * 1_000_000_000),
                PriceToEarningsRatio = (decimal?)(random.NextDouble() * 50),
                RevenueGrowth = (decimal?)(random.NextDouble()),
                // Add more fields as needed
            });
        }

        metrics = result;
        return true;
    }

    public bool TryGetFinancialLineItems(string ticker, DateTime endDate, string period, int limit,
        out IEnumerable<FinancialLineItem>? financialLineItems)
    {
        financialLineItems = null;
        if (string.IsNullOrWhiteSpace(ticker) || limit <= 0)
            return false;

        var result = new List<FinancialLineItem>();
        var random = new Random();

        for (var i = 0; i < limit; i++)
        {
            var extras = new Dictionary<string, dynamic>
            {
                { "LineItemValue", random.NextDouble() * 100 }
            };
            result.Add(new FinancialLineItem(
                ticker,
                endDate.AddMonths(-i),
                period,
                "USD",
                extras
            ));
        }

        financialLineItems = result;
        return true;
    }

    public bool TryGetCompanyNews(string ticker, out IEnumerable<NewsSentiment>? newsSentiments)
    {
        newsSentiments = null;
        if (string.IsNullOrWhiteSpace(ticker))
            return false;

        var random = new Random();
        var result = new List<NewsSentiment>();
        for (var i = 0; i < 5; i++)
        {
            result.Add(new NewsSentiment
            {
                Title = $"Sample headline {i + 1} for {ticker}",
                Url = $"https://news.example.com/{ticker}/{i + 1}",
                PublishedAt = DateTime.UtcNow.AddDays(-i),
                OverallSentimentScore = (decimal)(random.NextDouble() * 2 - 1), // -1 to 1
                OverallSentimentLabel = random.NextDouble() > 0.5 ? "Positive" : "Negative",
                TickerSentiments = new List<TickerSentiment>
                {
                    new TickerSentiment
                    {
                        Ticker = ticker,
                        RelevanceScore = (decimal)random.NextDouble(),
                        SentimentScore = (decimal)(random.NextDouble() * 2 - 1),
                        SentimentLabel = random.NextDouble() > 0.5 ? "Bullish" : "Bearish"
                    }
                }
            });
        }

        newsSentiments = result;
        return true;
    }
}