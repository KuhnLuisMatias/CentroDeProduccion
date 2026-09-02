"use client";

import { getToken } from "./api";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api";

export type ExportFormat = "excel" | "pdf";

/**
 * Downloads a report export from the backend as a file.
 * Uses the auth token and the same query params the report view uses.
 * The export endpoint returns the raw binary (xlsx or pdf), so this bypasses
 * apiClient (which parses JSON) and triggers a programmatic download.
 */
export async function downloadReportExport(
  type: string,
  format: ExportFormat,
  params: Record<string, string>,
): Promise<void> {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== "" && value != null) {
      query.append(key, value);
    }
  }

  const token = getToken();
  const response = await fetch(`${API_URL}/reports/${type}/export/${format}?${query.toString()}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    cache: "no-store",
  });

  if (!response.ok) {
    let message = `Error ${response.status}`;
    try {
      const payload = await response.json();
      if (payload && typeof payload === "object") {
        const obj = payload as Record<string, unknown>;
        if (typeof obj.detail === "string" && obj.detail) message = obj.detail;
        else if (typeof obj.title === "string" && obj.title) message = obj.title;
        else if (typeof obj.message === "string" && obj.message) message = obj.message;
      }
    } catch {
      // not JSON
    }
    throw new Error(message);
  }

  const blob = await response.blob();
  const extension = format === "excel" ? "xlsx" : "pdf";
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = `${type}-${new Date().toISOString().slice(0, 10)}.${extension}`;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}
