import { Empty, Table, type TableColumnsType, Tag } from "antd";
import { Link } from "react-router";
import type { Wave } from "./promotionApi";
import { formatDate, stageLabel } from "./promotionTexts";
import { promotionPaths } from "./routes";

interface WaveListViewProps {
  waves: readonly Wave[];
  total: number;
  page: number;
  pageSize: number;
  isLoading: boolean;
  onPageChange: (page: number) => void;
}

/** The waves as a table: period, where each stands, its units and who runs it; a wave opens from its period. */
export function WaveListView({
  waves,
  total,
  page,
  pageSize,
  isLoading,
  onPageChange,
}: WaveListViewProps) {
  const columns: TableColumnsType<Wave> = [
    {
      title: "Период",
      key: "period",
      render: (_, wave) => (
        <Link to={promotionPaths.wave(wave.id)}>
          {formatDate(wave.startsOn)} —{" "}
          {formatDate(wave.endsOn.at(-1) ?? wave.startsOn)}
        </Link>
      ),
    },
    {
      title: "Этап",
      key: "stage",
      render: (_, wave) => (
        <Tag color={wave.isFinished ? "default" : "processing"}>
          {stageLabel(wave.stage)}
        </Tag>
      ),
    },
    {
      title: "Подразделения",
      key: "units",
      render: (_, wave) => wave.units.map((unit) => unit.name).join(", "),
    },
    { title: "Ведёт", dataIndex: "authorName" },
  ];
  return (
    <Table<Wave>
      rowKey="id"
      columns={columns}
      dataSource={[...waves]}
      loading={isLoading}
      scroll={{ x: "max-content" }}
      locale={{
        emptyText: (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Волн пока нет. Объявите первую волну."
          />
        ),
      }}
      pagination={{
        current: page,
        pageSize,
        total,
        showSizeChanger: false,
        onChange: onPageChange,
      }}
    />
  );
}
