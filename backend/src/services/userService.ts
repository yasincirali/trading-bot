import { PrismaClient } from '@prisma/client';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import { JwtPayload } from '../types';
import { createError } from '../middleware/errorHandler';

const prisma = new PrismaClient();

export async function registerUser(email: string, password: string, name: string, phone?: string) {
  const existing = await prisma.user.findUnique({ where: { email } });
  if (existing) throw createError('Email already registered', 409);

  if (password.length < 6) throw createError('Password must be at least 6 characters', 400);

  const passwordHash = await bcrypt.hash(password, 10);
  const user = await prisma.user.create({
    data: { email, passwordHash, name, phone },
    select: { id: true, email: true, name: true, phone: true, role: true, createdAt: true },
  });

  await prisma.botConfig.create({
    data: {
      userId: user.id,
      confidenceThreshold: parseFloat(process.env.DEFAULT_CONFIDENCE_THRESHOLD ?? '0.65'),
      maxOrderSizeTry: parseFloat(process.env.DEFAULT_MAX_ORDER_SIZE_TRY ?? '10000'),
      dailyLossLimitTry: parseFloat(process.env.DEFAULT_DAILY_LOSS_LIMIT_TRY ?? '5000'),
      tickIntervalMs: parseInt(process.env.BOT_TICK_INTERVAL_MS ?? '5000'),
      watchlist: ['THYAO', 'GARAN', 'AKBNK'],
    },
  });

  return { user, tokens: generateTokens(user) };
}

export async function loginUser(email: string, password: string) {
  const user = await prisma.user.findUnique({ where: { email } });
  if (!user) throw createError('Invalid email or password', 401);

  const valid = await bcrypt.compare(password, user.passwordHash);
  if (!valid) throw createError('Invalid email or password', 401);

  const { passwordHash: _, ...safeUser } = user;
  return { user: safeUser, tokens: generateTokens(safeUser) };
}

export async function getUserById(userId: string) {
  const user = await prisma.user.findUnique({
    where: { id: userId },
    select: { id: true, email: true, name: true, phone: true, role: true, createdAt: true },
  });
  if (!user) throw createError('User not found', 404);
  return user;
}

function generateTokens(user: { id: string; email: string; role: string }) {
  const secret = process.env.JWT_SECRET ?? 'fallback-secret';
  const payload: JwtPayload = { userId: user.id, email: user.email, role: user.role };

  const accessToken = jwt.sign(payload, secret, { expiresIn: '7d' });
  const refreshToken = jwt.sign(payload, secret, { expiresIn: '30d' });

  return { accessToken, refreshToken };
}

export async function refreshTokens(refreshToken: string) {
  const secret = process.env.JWT_SECRET ?? 'fallback-secret';
  try {
    const payload = jwt.verify(refreshToken, secret) as JwtPayload;
    const user = await getUserById(payload.userId);
    return generateTokens(user);
  } catch (err) {
    console.error('[UserService] Refresh token invalid:', err);
    throw createError('Invalid refresh token', 401);
  }
}
