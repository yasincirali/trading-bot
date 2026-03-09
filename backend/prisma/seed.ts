import { PrismaClient, Role } from '@prisma/client';
import * as bcrypt from 'bcryptjs';

const prisma = new PrismaClient();

const BIST_SYMBOLS = [
  { ticker: 'THYAO', name: 'Türk Hava Yolları', sector: 'Havacılık', basePrice: 280.5 },
  { ticker: 'EREGL', name: 'Ereğli Demir Çelik', sector: 'Metal', basePrice: 45.2 },
  { ticker: 'GARAN', name: 'Garanti BBVA Bankası', sector: 'Bankacılık', basePrice: 105.8 },
  { ticker: 'ASELS', name: 'Aselsan', sector: 'Savunma', basePrice: 95.4 },
  { ticker: 'SISE', name: 'Şişe ve Cam', sector: 'Cam', basePrice: 52.3 },
  { ticker: 'KCHOL', name: 'Koç Holding', sector: 'Holding', basePrice: 178.6 },
  { ticker: 'BIMAS', name: 'BİM Mağazalar', sector: 'Perakende', basePrice: 495.0 },
  { ticker: 'SAHOL', name: 'Sabancı Holding', sector: 'Holding', basePrice: 88.9 },
  { ticker: 'TUPRS', name: 'Tüpraş', sector: 'Petrol', basePrice: 420.0 },
  { ticker: 'AKBNK', name: 'Akbank', sector: 'Bankacılık', basePrice: 62.7 },
];

function generateCandles(basePrice: number, days: number) {
  const candles = [];
  let price = basePrice;
  const now = new Date();

  for (let i = days; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);
    date.setHours(10, 0, 0, 0);

    // Random walk with slight upward drift
    const change = (Math.random() - 0.48) * price * 0.025;
    price = Math.max(price + change, 1);

    const open = price;
    const volatility = price * 0.015;
    const high = open + Math.random() * volatility;
    const low = open - Math.random() * volatility;
    const close = low + Math.random() * (high - low);
    const volume = Math.floor(100000 + Math.random() * 900000);

    candles.push({ open, high, low: Math.max(low, 0.01), close, volume, timestamp: date });
    price = close;
  }

  return candles;
}

async function main() {
  console.log('Seeding database...');

  // Admin user
  const passwordHash = await bcrypt.hash('Admin123!', 10);
  const admin = await prisma.user.upsert({
    where: { email: 'admin@tradingbot.tr' },
    update: {},
    create: {
      email: 'admin@tradingbot.tr',
      passwordHash,
      name: 'Admin User',
      phone: '+905551234567',
      role: Role.ADMIN,
    },
  });
  console.log(`Admin user: ${admin.email}`);

  // BIST symbols + candles
  for (const sym of BIST_SYMBOLS) {
    const candles = generateCandles(sym.basePrice, 200);
    const lastCandle = candles[candles.length - 1];

    const symbol = await prisma.bistSymbol.upsert({
      where: { ticker: sym.ticker },
      update: { lastPrice: lastCandle.close },
      create: {
        ticker: sym.ticker,
        name: sym.name,
        sector: sym.sector,
        lastPrice: lastCandle.close,
      },
    });

    // Delete old candles and reinsert
    await prisma.priceCandle.deleteMany({ where: { symbolId: symbol.id } });
    await prisma.priceCandle.createMany({
      data: candles.map(c => ({ ...c, symbolId: symbol.id })),
    });

    console.log(`Seeded ${sym.ticker}: ${candles.length} candles, last price: ${lastCandle.close.toFixed(2)} TRY`);
  }

  // Default bot config for admin
  await prisma.botConfig.upsert({
    where: { userId: admin.id },
    update: {},
    create: {
      userId: admin.id,
      enabled: false,
      confidenceThreshold: 0.65,
      maxOrderSizeTry: 10000,
      dailyLossLimitTry: 5000,
      tickIntervalMs: 5000,
      watchlist: ['THYAO', 'GARAN', 'AKBNK', 'EREGL', 'ASELS'],
    },
  });

  console.log('Seeding complete!');
  console.log('Login: admin@tradingbot.tr / Admin123!');
}

main()
  .catch(e => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
