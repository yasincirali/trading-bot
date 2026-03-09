import { IndicatorResult } from '../types';

export function bollingerBandsIndicator(
  prices: number[],
  period = 20,
  stdDevMultiplier = 2
): IndicatorResult {
  if (prices.length < period) {
    return { signal: 'NEUTRAL', strength: 0, value: null };
  }

  const recent = prices.slice(-period);
  const sma = recent.reduce((a, b) => a + b, 0) / period;
  const variance = recent.reduce((sum, p) => sum + Math.pow(p - sma, 2), 0) / period;
  const stdDev = Math.sqrt(variance);

  const upper = sma + stdDevMultiplier * stdDev;
  const lower = sma - stdDevMultiplier * stdDev;
  const currentPrice = prices[prices.length - 1];
  const bandWidth = upper - lower;

  const value = { upper, middle: sma, lower, currentPrice, bandWidth };

  if (currentPrice <= lower) {
    const strength = Math.min((lower - currentPrice) / stdDev + 0.1, 1);
    return { signal: 'BUY', strength, value };
  }

  if (currentPrice >= upper) {
    const strength = Math.min((currentPrice - upper) / stdDev + 0.1, 1);
    return { signal: 'SELL', strength, value };
  }

  // Near lower band: mild buy; near upper band: mild sell
  const position = (currentPrice - lower) / bandWidth; // 0 = at lower, 1 = at upper
  if (position < 0.3) {
    return { signal: 'BUY', strength: (0.3 - position) * 0.5, value };
  }
  if (position > 0.7) {
    return { signal: 'SELL', strength: (position - 0.7) * 0.5, value };
  }

  return { signal: 'NEUTRAL', strength: 0, value };
}
