namespace TradingBot.Indicators;

public static class RsiIndicator
{
    public static double Calculate(double[] prices, int period = 14)
    {
        if (prices.Length < period + 1) return 50;

        double avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            double diff = prices[i] - prices[i - 1];
            if (diff > 0) avgGain += diff;
            else avgLoss += Math.Abs(diff);
        }
        avgGain /= period;
        avgLoss /= period;

        for (int i = period + 1; i < prices.Length; i++)
        {
            double diff = prices[i] - prices[i - 1];
            double gain = diff > 0 ? diff : 0;
            double loss = diff < 0 ? Math.Abs(diff) : 0;
            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
        }

        if (avgLoss == 0) return 100;
        double rs = avgGain / avgLoss;
        return 100 - 100 / (1 + rs);
    }

    public static IndicatorResult Compute(double[] prices, int period = 14)
    {
        if (prices.Length < period + 1)
            return new IndicatorResult(SignalType.NEUTRAL, 0, 50.0);

        double rsi = Calculate(prices, period);

        if (rsi < 30)
            return new IndicatorResult(SignalType.BUY, Math.Min((30 - rsi) / 30.0, 1.0), rsi);
        if (rsi > 70)
            return new IndicatorResult(SignalType.SELL, Math.Min((rsi - 70) / 30.0, 1.0), rsi);

        return new IndicatorResult(SignalType.NEUTRAL, 0, rsi);
    }
}
