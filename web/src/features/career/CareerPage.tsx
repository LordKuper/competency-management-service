import { Tabs, Typography } from "antd";
import { CompetenciesTab } from "./CompetenciesTab";
import { useCanApprove } from "./careerApi";
import { DirectionsTracksTab } from "./DirectionsTracksTab";
import { DraftsSection } from "./DraftsSection";
import { ScalesTab } from "./ScalesTab";

const TABS = [
  { key: "matrices", label: "Матрицы", children: null },
  {
    key: "tracks",
    label: "Направления и треки",
    children: <DirectionsTracksTab />,
  },
  { key: "competencies", label: "Компетенции", children: <CompetenciesTab /> },
  { key: "scales", label: "Шкалы", children: <ScalesTab /> },
];

/** The career model section: one tab per reference and matrix screen; "На согласовании" only for those who approve. */
export function CareerPage() {
  const canApprove = useCanApprove();
  const tabs = canApprove
    ? [
        ...TABS,
        {
          key: "approvals",
          label: "На согласовании",
          children: <DraftsSection />,
        },
      ]
    : TABS;

  return (
    <>
      <Typography.Title level={1}>Карьерная модель</Typography.Title>
      <Tabs items={tabs} />
    </>
  );
}
