"use client";

import type { ReactNode } from "react";
import { Bar, Line, Pie, Doughnut } from "react-chartjs-2";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  ArcElement,
  Tooltip,
  Legend,
} from "chart.js";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { Chart as ChartData } from "@/lib/types";

ChartJS.register(
  CategoryScale,
  LinearScale,
  BarElement,
  LineElement,
  PointElement,
  ArcElement,
  Tooltip,
  Legend,
);

export default function ChartCard({ chart }: { chart: ChartData }) {
  const data = {
    labels: chart.labels,
    datasets: chart.datasets.map((ds) => ({
      label: ds.label,
      data: ds.data,
      backgroundColor: ds.backgroundColor ?? "#2563eb",
      borderColor: ds.backgroundColor ?? "#2563eb",
    })),
  };

  const type = (chart.type ?? "bar").toLowerCase();
  const commonProps = { data };

  let chartElement: ReactNode;
  if (type === "line") {
    chartElement = <Line {...commonProps} />;
  } else if (type === "pie") {
    chartElement = <Pie {...commonProps} />;
  } else if (type === "doughnut") {
    chartElement = <Doughnut {...commonProps} />;
  } else if (type === "horizontalBar") {
    chartElement = <Bar {...commonProps} options={{ indexAxis: "y" }} />;
  } else {
    chartElement = <Bar {...commonProps} />;
  }

  return (
    <Card className="gap-4 py-5">
      <CardHeader className="px-5">
        <CardTitle className="text-base">{chart.title}</CardTitle>
      </CardHeader>
      <CardContent className="relative h-[300px] px-5">{chartElement}</CardContent>
    </Card>
  );
}
