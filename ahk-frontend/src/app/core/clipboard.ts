/**
 * Copies text to the clipboard, resolving to whether it worked.
 *
 * The async Clipboard API is the path that runs — the app is HTTPS in development and production alike — but it
 * is also refused outright when the document is not focused or the browser withholds permission, and an invite
 * link the instructor believes they copied is worse than one they know they did not. The textarea fallback
 * covers those cases; the caller uses the result to decide what to say.
 */
export async function copyToClipboard(text: string): Promise<boolean> {
  if (navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch {
      // Permission refused or the document is not focused — fall through.
    }
  }

  const area = document.createElement('textarea');
  area.value = text;

  // Off-screen rather than hidden: an element with display:none cannot be selected.
  area.style.position = 'fixed';
  area.style.left = '-9999px';
  document.body.appendChild(area);

  try {
    area.select();
    return document.execCommand('copy');
  } catch {
    return false;
  } finally {
    area.remove();
  }
}
