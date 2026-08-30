/**
 * Minimal design tokens. Not a design system — just enough shared constants so the
 * foundation screens are consistent. Expand deliberately, not opportunistically.
 */
export const colors = {
  background: '#0f1216',
  surface: '#181d24',
  surfaceAlt: '#212934',
  primary: '#3ba7ff',
  primaryText: '#0f1216',
  text: '#eef2f6',
  textMuted: '#9aa7b4',
  danger: '#ff5c68',
  success: '#46c98b',
  border: '#2b333d',
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
} as const;

export const radius = {
  sm: 8,
  md: 12,
  lg: 20,
} as const;

export const typography = {
  title: { fontSize: 24, fontWeight: '700' as const },
  heading: { fontSize: 18, fontWeight: '600' as const },
  body: { fontSize: 15, fontWeight: '400' as const },
  caption: { fontSize: 12, fontWeight: '500' as const },
} as const;
