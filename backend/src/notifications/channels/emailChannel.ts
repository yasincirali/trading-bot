import nodemailer from 'nodemailer';

let transporter: nodemailer.Transporter | null = null;

function getTransporter(): nodemailer.Transporter | null {
  if (!transporter) {
    const host = process.env.SMTP_HOST;
    const user = process.env.SMTP_USER;
    if (!host || !user || user === 'your@gmail.com') {
      console.warn('[Email] SMTP not configured — emails will be logged only');
      return null;
    }
    transporter = nodemailer.createTransport({
      host,
      port: parseInt(process.env.SMTP_PORT ?? '587'),
      secure: false,
      auth: {
        user,
        pass: process.env.SMTP_PASS,
      },
    });
  }
  return transporter;
}

export async function sendEmail(to: string, subject: string, htmlBody: string): Promise<boolean> {
  const transport = getTransporter();
  if (!transport) {
    console.log(`[Email] (Mock) To: ${to} | Subject: ${subject}`);
    return true;
  }

  try {
    const info = await transport.sendMail({
      from: process.env.SMTP_FROM ?? 'TradingBot <noreply@tradingbot.tr>',
      to,
      subject,
      html: htmlBody,
    });
    console.log(`[Email] Sent to ${to}, messageId: ${info.messageId}`);
    return true;
  } catch (err) {
    console.error(`[Email] Failed to send to ${to}:`, err);
    return false;
  }
}
