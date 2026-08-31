/** RFC-4122-shaped random id; good enough for offline attempt ids. */
export function cryptoRandomId(): string {
  const hex = Array.from({ length: 32 }, () => Math.floor(Math.random() * 16).toString(16)).join(
    '',
  );
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-8${hex.slice(17, 20)}-${hex.slice(20, 32)}`;
}

export function randomSeed(): number {
  return Math.floor(Math.random() * 2 ** 31);
}
