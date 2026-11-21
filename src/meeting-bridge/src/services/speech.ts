import { DefaultAzureCredential } from '@azure/identity';

/** Turns text into speech for the demo cast. */
export interface SpeechSynthesizer {
  /**
   * Synthesises text.
   * @param voice - Azure Speech voice name.
   * @param text - What to say.
   * @returns PCM16, 24 kHz, mono audio.
   */
  synthesize(voice: string, text: string): Promise<Buffer>;
}

/**
 * Azure AI Speech over REST with an Entra token (no keys). Raw PCM16 24 kHz mono is requested so the
 * audio needs no resampling anywhere (SPEC 8). Results are cached so a re-run of the demo is instant.
 * VERIFY: https://learn.microsoft.com/azure/ai-services/speech-service/rest-text-to-speech
 */
export class AzureSpeechSynthesizer implements SpeechSynthesizer {
  private readonly credential: DefaultAzureCredential;
  private readonly cache = new Map<string, Promise<Buffer>>();

  /**
   * @param endpoint - Resource endpoint, for example `https://<resource>.cognitiveservices.azure.com`.
   * @param tenantId - Optional tenant to sign in to.
   */
  constructor(
    private readonly endpoint: string,
    tenantId?: string,
  ) {
    this.credential = new DefaultAzureCredential({ tenantId });
  }

  /** @inheritdoc */
  synthesize(voice: string, text: string): Promise<Buffer> {
    const key = `${voice}|${text}`;
    let pending = this.cache.get(key);
    if (!pending) {
      pending = this.request(voice, text).catch((error: unknown) => {
        this.cache.delete(key);
        throw error;
      });
      this.cache.set(key, pending);
    }
    return pending;
  }

  private async request(voice: string, text: string): Promise<Buffer> {
    const token = await this.credential.getToken('https://cognitiveservices.azure.com/.default');
    const ssml = `<speak version="1.0" xml:lang="en-US"><voice name="${escapeXml(voice)}">${escapeXml(text)}</voice></speak>`;
    const response = await fetch(`${this.endpoint}/tts/cognitiveservices/v1`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token.token}`,
        'Content-Type': 'application/ssml+xml',
        'X-Microsoft-OutputFormat': 'raw-24khz-16bit-mono-pcm',
      },
      body: ssml,
    });
    if (!response.ok)
      throw new Error(
        `Speech synthesis failed (${response.status}): ${(await response.text()).slice(0, 200)}`,
      );
    return Buffer.from(await response.arrayBuffer());
  }
}

function escapeXml(value: string): string {
  return value.replace(
    /[<>&'"]/g,
    (c) => ({ '<': '&lt;', '>': '&gt;', '&': '&amp;', "'": '&apos;', '"': '&quot;' })[c] ?? c,
  );
}
