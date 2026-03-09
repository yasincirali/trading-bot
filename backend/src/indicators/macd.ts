import { IndicatorResult } from '../types';
import { calculateEMA } from './ema';

export function macdIndicator(
  prices: number[],
  fastPeriod = 12,
  slowPeriod = 26,
  signalPeriod = 9
): IndicatorResult {
  if (prices.length < slowPeriod + signalPeriod) {
    return { signal: 'NEUTRAL', strength: 0, value: null };
  }

  const fastEMA = calculateEMA(prices, fastPeriod);
  const slowEMA = calculateEMA(prices, slowPeriod);

  // MACD line = fast EMA - slow EMA (aligned by index)
  const macdLine = fastEMA.map((v, i) => v - slowEMA[i]);

  // Signal line = EMA of MACD line (use last portion)
  const signalLine = calculateEMA(macdLine, signalPeriod);

  const lastMacd = macdLine[macdLine.length - 1];
  const lastSignal = signalLine[signalLine.length - 1];
  const prevMacd = macdLine[macdLine.length - 2];
  const prevSignal = signalLine[signalLine.length - 2];

  const histogram = lastMacd - lastSignal;
  const prevHistogram = prevMacd - prevSignal;

  const signalRef = Math.abs(lastSignal) || 1;
  const strength = Math.min(Math.abs(histogram) / signalRef, 1);

  const crossedAbove = prevHistogram <= 0 && histogram > 0;
  const crossedBelow = prevHistogram >= 0 && histogram < 0;

  const value = { macd: lastMacd, signal: lastSignal, histogram };

  if (crossedAbove || (histogram > 0 && lastMacd > 0)) {
    return { signal: 'BUY', strength, value };
  }
  if (crossedBelow || (histogram < 0 && lastMacd < 0)) {
    return { signal: 'SELL', strength, value };
  }

  return { signal: 'NEUTRAL', strength: strength * 0.5, value };
}
