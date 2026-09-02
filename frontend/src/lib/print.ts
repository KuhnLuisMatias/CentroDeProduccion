"use client";

export function openHtmlInNewTab(html: string): void {
  const blob = new Blob([html], { type: "text/html" });
  const url = URL.createObjectURL(blob);
  const win = window.open(url, "_blank");
  if (win) {
    win.addEventListener("load", () => URL.revokeObjectURL(url), { once: true });
  } else {
    URL.revokeObjectURL(url);
  }
}
