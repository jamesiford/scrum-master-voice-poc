/**
 * Joins class names, skipping falsy values.
 * @param parts - Class names or falsy placeholders.
 * @returns One class string.
 */
export function cx(...parts: (string | false | null | undefined)[]): string {
  return parts.filter(Boolean).join(' ');
}
