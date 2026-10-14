import { Space, Tabs, Typography } from "antd";
import { ApprovalsSection } from "./ApprovalsSection";
import { BlocksTab } from "./BlocksTab";
import { CategoriesTab } from "./CategoriesTab";
import { CompetenciesTab } from "./CompetenciesTab";
import { useCanApprove } from "./careerApi";
import { DirectionsTracksTab } from "./DirectionsTracksTab";
import { DraftsSection } from "./DraftsSection";
import { MatricesTab } from "./MatricesTab";
import { ScalesTab } from "./ScalesTab";

const TABS = [
  { key: "scales", label: "Шкалы", children: <ScalesTab /> },
  { key: "categories", label: "Категории", children: <CategoriesTab /> },
  {
    key: "tracks",
    label: "Области и треки",
    children: <DirectionsTracksTab />,
  },
  { key: "competencies", label: "Компетенции", children: <CompetenciesTab /> },
  { key: "blocks", label: "Общие блоки", children: <BlocksTab /> },
  { key: "matrices", label: "Матрицы", children: <MatricesTab /> },
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
          children: (
            <Space
              orientation="vertical"
              size="large"
              style={{ display: "flex" }}
            >
              <ApprovalsSection />
              <DraftsSection />
            </Space>
          ),
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
