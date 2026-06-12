import type { APIRequestContext } from '@playwright/test';
import { API_BASE } from './qa-seed.js';

export function extractTokenFromEmailBody(htmlBody: string): string {
  const match = htmlBody.match(/<strong>([^<]+)<\/strong>/);
  if (!match?.[1]) {
    throw new Error('Token not found in dev email body');
  }
  return match[1];
}

export async function fetchLatestDevEmailToken(
  request: APIRequestContext,
  recipient: string,
): Promise<string> {
  const res = await request.get(`${API_BASE}/dev/sent-emails`, {
    params: { to: recipient },
  });
  if (res.status() !== 200) {
    throw new Error(`Dev sent-emails failed (${res.status()}): ${await res.text()}`);
  }
  const emails = (await res.json()) as { htmlBody: string }[];
  if (emails.length === 0) {
    throw new Error(`No dev emails found for ${recipient}`);
  }
  return extractTokenFromEmailBody(emails[0].htmlBody);
}
