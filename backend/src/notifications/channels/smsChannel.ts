import twilio from 'twilio';

let client: ReturnType<typeof twilio> | null = null;

function getClient() {
  if (!client) {
    const sid = process.env.TWILIO_ACCOUNT_SID;
    const token = process.env.TWILIO_AUTH_TOKEN;
    if (!sid || !token || sid.startsWith('ACxx')) {
      console.warn('[SMS] Twilio not configured — SMS will be logged only');
      return null;
    }
    client = twilio(sid, token);
  }
  return client;
}

export async function sendSms(to: string, message: string): Promise<boolean> {
  // Ensure Turkish number has +90 prefix
  const normalized = to.startsWith('+') ? to : `+90${to.replace(/^0/, '')}`;

  const twilioClient = getClient();
  if (!twilioClient) {
    console.log(`[SMS] (Mock) To: ${normalized} | Message: ${message}`);
    return true;
  }

  try {
    const result = await twilioClient.messages.create({
      body: message,
      from: process.env.TWILIO_PHONE_FROM ?? '',
      to: normalized,
    });
    console.log(`[SMS] Sent to ${normalized}, SID: ${result.sid}`);
    return true;
  } catch (err) {
    console.error(`[SMS] Failed to send to ${normalized}:`, err);
    return false;
  }
}
