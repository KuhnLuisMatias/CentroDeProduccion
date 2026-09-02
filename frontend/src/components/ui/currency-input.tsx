"use client";

import { forwardRef, useCallback, useRef, useState } from "react";
import { Input } from "@/components/ui/input";
import { MONEY, parseCurrencyInput } from "@/lib/utils";

interface CurrencyInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  min?: number;
  max?: number;
  disabled?: boolean;
  className?: string;
}

/**
 * Input that displays values in currency format ($ 1.234,56) and
 * returns raw number strings on change.
 */
export const CurrencyInput = forwardRef<HTMLInputElement, CurrencyInputProps>(
  (
    {
      value,
      onChange,
      placeholder,
      min,
      max,
      disabled,
      className,
    },
    ref,
  ) => {
    const [focused, setFocused] = useState(false);
    const internalRef = useRef<HTMLInputElement>(null);
    const inputRef = (ref as React.RefObject<HTMLInputElement>) ?? internalRef;

    // When focused, show raw number for easy editing
    // When blurred, show formatted currency
    const displayValue = focused
      ? value
      : value
        ? MONEY.format(parseCurrencyInput(value))
        : "";

    const handleChange = useCallback(
      (e: React.ChangeEvent<HTMLInputElement>) => {
        // Allow only numbers, dots, commas
        const raw = e.target.value.replace(/[^0-9.,-]/g, "");
        onChange(raw);
      },
      [onChange],
    );

    const handleBlur = useCallback(() => {
      setFocused(false);
      // Format the value on blur
      if (value) {
        const num = parseCurrencyInput(value);
        onChange(String(num));
      }
    }, [value, onChange]);

    const handleFocus = useCallback(() => {
      setFocused(true);
    }, []);

    return (
      <Input
        ref={inputRef}
        type="text"
        inputMode="decimal"
        className={className}
        value={displayValue}
        onChange={handleChange}
        onFocus={handleFocus}
        onBlur={handleBlur}
        placeholder={placeholder ?? "0.00"}
        disabled={disabled}
        min={min}
        max={max}
      />
    );
  },
);

CurrencyInput.displayName = "CurrencyInput";
