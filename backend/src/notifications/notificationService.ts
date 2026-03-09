import { PrismaClient, NotificationChannel } from '@prisma/client';
import { sendSms } from './channels/smsChannel';
import { sendEmail } from './channels/emailChannel';

const prisma = new PrismaClient();

export async function sendNotification(
  userId: string,
  channel: NotificationChannel,
  message: string,
  extra?: { email?: string; phone?: string; subject?: string }
): Promise<void> {
  const notification = await prisma.notification.create({
    data: { userId, channel, message, status: 'PENDING' },
  });

  let success = false;

  try {
    if (channel === 'SMS') {
      const user = await prisma.user.findUnique({ where: { id: userId } });
      const phone = extra?.phone ?? user?.phone ?? '';
      if (!phone) throw new Error('No phone number available');
      success = await sendSms(phone, message);
    } else if (channel === 'EMAIL') {
      const user = await prisma.user.findUnique({ where: { id: userId } });
      const email = extra?.email ?? user?.email ?? '';
      if (!email) throw new Error('No email available');
      const subject = extra?.subject ?? 'BIST Trading Bot — Notification';
      const html = `<div style="font-family:sans-serif"><p>${message.replace(/\n/g, '<br>')}</p></div>`;
      success = await sendEmail(email, subject, html);
    }

    await prisma.notification.update({
      where: { id: notification.id },
      data: { status: success ? 'SENT' : 'FAILED', sentAt: success ? new Date() : undefined },
    });
  } catch (err) {
    console.error(`[NotificationService] Failed to send ${channel} notification:`, err);
    await prisma.notification.update({
      where: { id: notification.id },
      data: { status: 'FAILED' },
    });
  }
}

export async function notifyOrderFilled(
  userId: string,
  ticker: string,
  type: string,
  quantity: number,
  price: number
): Promise<void> {
  const message = `BIST Bot: ${type} emri gerçekleşti\nHisse: ${ticker}\nAdet: ${quantity}\nFiyat: ${price.toFixed(2)} TRY\nToplam: ${(quantity * price).toFixed(2)} TRY`;
  await sendNotification(userId, 'SMS', message);
}
