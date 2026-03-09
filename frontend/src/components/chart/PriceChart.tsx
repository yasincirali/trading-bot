import { useEffect, useState } from 'react';
import {
  ResponsiveContainer, ComposedChart, Line, Area, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend,
} from 'recharts';
import { PriceCandle } from '../../types';
import { symbolsApi } from '../../api/trading';
import { LoadingSpinner } from '../common/LoadingSpinner';

interface Props {
  ticker: string;
}

interface ChartPoint {
  time: string;
  close: number;
  high: number;
  low: number;
  upper?: number;
  middle?: number;
  lower?: number;
}

function calcSMA(data: number[], period: number): number[] {
  return data.map((_, i) => {
    if (i < period - 1) return NaN;
    return data.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period;
  });
}

function calcBollinger(closes: number[], period = 20, mult = 2) {
  const sma = calcSMA(closes, period);
  return sma.map((avg, i) => {
    if (isNaN(avg) || i < period - 1) return { upper: NaN, middle: NaN, lower: NaN };
    const slice = closes.slice(i - period + 1, i + 1);
    const std = Math.sqrt(slice.reduce((s, v) => s + Math.pow(v - avg, 2), 0) / period);
    return { upper: avg + mult * std, middle: avg, lower: avg - mult * std };
  });
}

export function PriceChart({ ticker }: Props) {
  const [data, setData] = useState<ChartPoint[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    symbolsApi.getCandles(ticker, 100).then((candles: PriceCandle[]) => {
      const closes = candles.map(c => c.close);
      const bands = calcBollinger(closes);

      const points: ChartPoint[] = candles.map((c, i) => ({
        time: new Date(c.timestamp).toLocaleDateString('tr-TR', { month: 'short', day: 'numeric' }),
        close: parseFloat(c.close.toFixed(2)),
        high: parseFloat(c.high.toFixed(2)),
        low: parseFloat(c.low.toFixed(2)),
        upper: isNaN(bands[i].upper) ? undefined : parseFloat(bands[i].upper.toFixed(2)),
        middle: isNaN(bands[i].middle) ? undefined : parseFloat(bands[i].middle.toFixed(2)),
        lower: isNaN(bands[i].lower) ? undefined : parseFloat(bands[i].lower.toFixed(2)),
      }));
      setData(points);
      setLoading(false);
    }).catch(() => setLoading(false));
  }, [ticker]);

  if (loading) return <div className="h-64 flex items-center justify-center"><LoadingSpinner /></div>;

  return (
    <ResponsiveContainer width="100%" height={280}>
      <ComposedChart data={data} margin={{ top: 5, right: 10, left: 0, bottom: 5 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
        <XAxis dataKey="time" tick={{ fill: '#6b7280', fontSize: 11 }} tickLine={false} />
        <YAxis
          domain={['auto', 'auto']}
          tick={{ fill: '#6b7280', fontSize: 11 }}
          tickLine={false}
          tickFormatter={(v: number) => v.toFixed(0)}
          width={55}
        />
        <Tooltip
          contentStyle={{ backgroundColor: '#111827', border: '1px solid #374151', borderRadius: 8 }}
          labelStyle={{ color: '#9ca3af', fontSize: 12 }}
          itemStyle={{ color: '#e5e7eb', fontSize: 12 }}
        />
        <Legend wrapperStyle={{ fontSize: 11, color: '#6b7280' }} />
        <Area type="monotone" dataKey="upper" stroke="#374151" fill="#1f2937" strokeWidth={1} name="BB Upper" dot={false} />
        <Area type="monotone" dataKey="lower" stroke="#374151" fill="#111827" strokeWidth={1} name="BB Lower" dot={false} />
        <Line type="monotone" dataKey="middle" stroke="#4b5563" strokeWidth={1} dot={false} name="BB Mid" strokeDasharray="4 2" />
        <Line type="monotone" dataKey="close" stroke="#3b82f6" strokeWidth={2} dot={false} name="Close" />
      </ComposedChart>
    </ResponsiveContainer>
  );
}
