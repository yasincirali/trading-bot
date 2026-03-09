import { IndicatorResult } from '../types';

export function calculateRSI(prices: number[], period = 14): number {
  if (prices.length < period + 1) return 50;

  let avgGain = 0;
  let avgLoss = 0;

  for (let i = 1; i <= period; i++) {
    const diff = prices[i] - prices[i - 1];
    if (diff > 0) avgGain += diff;
    else avgLoss += Math.abs(diff);
  }

  avgGain /= period;
  avgLoss /= period;

  for (let i = period + 1; i < prices.length; i++) {
    const diff = prices[i] - prices[i - 1];
    const gain = diff > 0 ? diff : 0;
    const loss = diff < 0 ? Math.abs(diff) : 0;
    avgGain = (avgGain * (period - 1) + gain) / period;
    avgLoss = (avgLoss * (period - 1) + loss) / period;
  }

  if (avgLoss === 0) return 100;
  const rs = avgGain / avgLoss;
  return 100 - 100 / (1 + rs);
}

export function rsiIndicator(prices: number[], period = 14): IndicatorResult {
  if (prices.length < period + 1) {
    return { signal: 'NEUTRAL', strength: 0, value: 50 };
  }

  const rsi = calculateRSI(prices, period);

  if (rsi < 30) {
    const strength = Math.min((30 - rsi) / 30, 1);
    return { signal: 'BUY', strength, value: rsi };
  }
  if (rsi > 70) {
    const strength = Math.min((rsi - 70) / 30, 1);
    return { signal: 'SELL', strength, value: rsi };
  }

  return { signal: 'NEUTRAL', strength: 0, value: rsi };
}
