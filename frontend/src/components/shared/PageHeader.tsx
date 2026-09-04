import type { ReactNode } from "react";

interface PageHeaderProps {
  actions?: ReactNode;
}

export default function PageHeader({ actions }: PageHeaderProps) {
  if (!actions) return null;
  return (
    <div className="mb-5">
      <div className="flex items-center gap-2">{actions}</div>
    </div>
  );
}
