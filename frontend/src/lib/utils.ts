import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

// ── Currency formatting ──────────────────────────────────────────────────────

/** Shared ARS currency formatter — $ symbol, 2 decimals, Argentine locale. */
export const MONEY = new Intl.NumberFormat("es-AR", {
  style: "currency",
  currency: "ARS",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** Format a number as currency string (e.g. "$ 1.234,56"). */
export function formatCurrency(value: number): string {
  return MONEY.format(value);
}

/** Parse a currency-formatted string back to a number. */
export function parseCurrencyInput(value: string): number {
  // Strip everything except digits, dots, commas, hyphens
  let cleaned = value.replace(/[^0-9.,-]/g, "");
  // Remove thousand-separator dots (e.g. "300.000" → "300000")
  cleaned = cleaned.replace(/\./g, "");
  // Replace comma decimal separator with dot for parseFloat
  cleaned = cleaned.replace(",", ".");
  const num = parseFloat(cleaned);
  return Number.isNaN(num) ? 0 : num;
}
