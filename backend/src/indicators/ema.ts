import { IndicatorResult, SignalType } from '../types';

export function calculateEMA(prices: number[], period: number): number[] {
  if (prices.length === 0) return [];
  const k = 2 / (period + 1);
  const ema: number[] = [prices[0]];
  for (let i = 1; i < prices.length; i++) {
    ema.push(prices[i] * k + ema[i - 1] * (1 - k));
  }
  return ema;
}

export function emaIndicator(prices: number[], shortPeriod = 9, longPeriod = 21): IndicatorResult {
  if (prices.length < longPeriod + 1) {
    return { signal: 'NEUTRAL', strength: 0, value: null };
  }

  const shortEMA = calculateEMA(prices, shortPeriod);
  const longEMA = calculateEMA(prices, longPeriod);

  const lastShort = shortEMA[shortEMA.length - 1];
  const lastLong = longEMA[longEMA.length - 1];
  const prevShort = shortEMA[shortEMA.length - 2];
  const prevLong = longEMA[longEMA.length - 2];

  const diff = (lastShort - lastLong) / lastLong;
  const strength = Math.min(Math.abs(diff) * 20, 1);

  let signal: SignalType;
  if (lastShort > lastLong) {
    // Bullish crossover bonus
    signal = prevShort <= prevLong ? 'BUY' : 'BUY';
    return { signal: 'BUY', strength, value: { shortEMA: lastShort, longEMA: lastLong, diff } };
  } else if (lastShort < lastLong) {
    signal = prevShort >= prevLong ? 'SELL' : 'SELL';
    return { signal: 'SELL', strength, value: { shortEMA: lastShort, longEMA: lastLong, diff } };
  }

  return { signal: 'NEUTRAL', strength: 0, value: { shortEMA: lastShort, longEMA: lastLong, diff } };
}
