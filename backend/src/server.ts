import 'dotenv/config';
import express from 'express';
import { createServer } from 'http';
import { Server } from 'socket.io';
import cors from 'cors';
import { PrismaClient } from '@prisma/client';
import jwt from 'jsonwebtoken';
import routes from './routes';
import { errorHandler, notFound } from './middleware/errorHandler';
import { JwtPayload } from './types';
import { isBotRunning, startBot } from './bot/tradingEngine';

const app = express();
const httpServer = createServer(app);
const prisma = new PrismaClient();

const io = new Server(httpServer, {
  cors: {
    origin: process.env.FRONTEND_URL ?? 'http://localhost:5173',
    credentials: true,
  },
});

// ── Middleware ────────────────────────────────────────────────────────────────
app.use(cors({ origin: process.env.FRONTEND_URL ?? 'http://localhost:5173', credentials: true }));
app.use(express.json());

// Make io available to route handlers
app.set('io', io);

// ── Routes ────────────────────────────────────────────────────────────────────
app.use('/api', routes);
app.use(notFound);
app.use(errorHandler);

// ── Socket.io ─────────────────────────────────────────────────────────────────
io.use((socket, next) => {
  const token = socket.handshake.auth.token as string | undefined;
  if (!token) {
    next(new Error('Authentication required'));
    return;
  }
  try {
    const payload = jwt.verify(token, process.env.JWT_SECRET ?? '') as JwtPayload;
    socket.data.user = payload;
    next();
  } catch {
    next(new Error('Invalid token'));
  }
});

io.on('connection', socket => {
  const user = socket.data.user as JwtPayload;
  console.log(`[Socket] Connected: ${user.email} (${socket.id})`);

  // Join personal room
  socket.join(`user:${user.userId}`);

  socket.on('subscribe:symbol', (ticker: string) => {
    socket.join(`symbol:${ticker}`);
  });

  socket.on('unsubscribe:symbol', (ticker: string) => {
    socket.leave(`symbol:${ticker}`);
  });

  socket.on('disconnect', () => {
    console.log(`[Socket] Disconnected: ${user.email}`);
  });
});

// ── Bootstrap ─────────────────────────────────────────────────────────────────
async function bootstrap() {
  try {
    await prisma.$connect();
    console.log('[DB] Connected to PostgreSQL');

    // Resume any previously enabled bots
    const enabledConfigs = await prisma.botConfig.findMany({ where: { enabled: true } });
    for (const config of enabledConfigs) {
      if (!isBotRunning(config.userId)) {
        startBot(config.userId, io);
        console.log(`[Bootstrap] Resumed bot for user ${config.userId}`);
      }
    }

    const port = parseInt(process.env.PORT ?? '3001');
    httpServer.listen(port, () => {
      console.log(`[Server] Running on http://localhost:${port}`);
      console.log(`[Server] PAPER_TRADING_MODE=${process.env.PAPER_TRADING_MODE ?? 'true'}`);
      console.log(`[Server] BANK_MODE=${process.env.BANK_MODE ?? 'mock'}`);
    });
  } catch (err) {
    console.error('[Server] Failed to start:', err);
    process.exit(1);
  }
}

bootstrap();
