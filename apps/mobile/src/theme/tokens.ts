/**
 * Qaniva design tokens — the single source of visual truth for the RN shell.
 * BRAND STATUS: MVP / PROVISIONAL (see docs/audits/QANIVA_BRAND_PRODUCT_SHELL_AUDIT.md).
 *
 * Identity intent: modern, clinically credible, calm, mobile-first. Deep ink
 * surfaces with a restrained clinical-teal brand accent — deliberately not
 * hospital-blue-only, not a game HUD, no gradients/neon. Clinical meaning is
 * NEVER communicated by color alone (badges/sections always carry text).
 */
export const colors = {
  // surfaces
  background: '#0e1116',
  surface: '#171c23',
  surfaceAlt: '#1f2630',
  border: '#2a323d',

  // brand
  brand: '#3ec6b4', // Qaniva teal — primary accent + CTAs
  brandText: '#06231e', // text on brand surfaces
  brandDim: '#2a8779',

  // text
  text: '#edf1f5',
  textMuted: '#98a4b1',
  textFaint: '#6b7684',

  // legacy alias kept for nav theme etc.
  primary: '#3ec6b4',
  primaryText: '#06231e',

  // semantic (each ALWAYS paired with a text label in UI)
  success: '#46c98b', // completed / on time
  harmful: '#e5484d', // safety-relevant penalty
  warning: '#e8a33d', // delayed / timing loss
  unnecessary: '#8fa3b0', // efficiency penalty — visually neutral, not red
  info: '#5b9cf5', // informational accents
  danger: '#e5484d',
  disabled: '#55606b',
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
  pill: 999,
} as const;

export const sizes = {
  touchTarget: 44,
  buttonHeight: 52,
  tabIcon: 24,
} as const;

/**
 * Type hierarchy. Body text never below 14; captions only for metadata.
 */
export const typography = {
  display: { fontSize: 32, fontWeight: '800' as const, letterSpacing: 0.2 },
  screenTitle: { fontSize: 24, fontWeight: '700' as const },
  sectionTitle: { fontSize: 17, fontWeight: '700' as const },
  cardTitle: { fontSize: 16, fontWeight: '600' as const },
  body: { fontSize: 15, fontWeight: '400' as const, lineHeight: 21 },
  bodySecondary: { fontSize: 14, fontWeight: '400' as const, lineHeight: 20 },
  caption: { fontSize: 12, fontWeight: '500' as const },
  button: { fontSize: 16, fontWeight: '600' as const },
  numeric: { fontSize: 30, fontWeight: '800' as const },
} as const;

/** Semantic tone → color + REQUIRED default label (never color-only). */
export const tones = {
  success: { color: colors.success },
  harmful: { color: colors.harmful },
  warning: { color: colors.warning },
  unnecessary: { color: colors.unnecessary },
  info: { color: colors.info },
  neutral: { color: colors.textMuted },
} as const;

export type Tone = keyof typeof tones;
