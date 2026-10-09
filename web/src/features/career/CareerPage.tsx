import { Tabs, Typography } from "antd";
import { useCanApprove } from "./careerApi";

const TABS = [
  { key: "matrices", label: "Матрицы" },
  { key: "tracks", label: "Направления и треки" },
  { key: "competencies", label: "Компетенции" },
  { key: "scales", label: "Шкалы" },
];

/** The career model section: one tab per reference and matrix screen; "На согласовании" only for those who approve. */
export function CareerPage() {
  const canApprove = useCanApprove();
  const tabs = canApprove
    ? [...TABS, { key: "approvals", label: "На согласовании" }]
    : TABS;

  return (
    <>
      <Typography.Title level={1}>Карьерная модель</Typography.Title>
      <Tabs items={tabs.map((tab) => ({ ...tab, children: null }))} />
    </>
  );
}
