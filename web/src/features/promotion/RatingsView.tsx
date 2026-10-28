import { Empty, Table, type TableColumnsType, Tag } from "antd";
import { Link } from "react-router";
import type { RatingTask } from "./promotionApi";
import { promotionPaths } from "./routes";

function actionLabel(task: RatingTask) {
  if (!task.canRate) return "Посмотреть";
  return task.isRated ? "Изменить оценку" : "Оценить";
}

const columns: TableColumnsType<RatingTask> = [
  { title: "Кандидат", dataIndex: "candidateName" },
  {
    title: "Оценка",
    key: "state",
    render: (_, task) =>
      task.isRated ? (
        <Tag color="success">Оценено</Tag>
      ) : (
        <Tag color={task.canRate ? "processing" : "default"}>
          {task.canRate ? "Ждёт оценки" : "Не оценено"}
        </Tag>
      ),
  },
  {
    title: "Действия",
    key: "actions",
    render: (_, task) => (
      <Link
        to={promotionPaths.rating(task.applicationId)}
        aria-label={`${actionLabel(task)}: ${task.candidateName}`}
      >
        {actionLabel(task)}
      </Link>
    ),
  },
];

/** The candidates the viewer is asked to rate, with whether they have rated them. */
export function RatingsView({ tasks }: { tasks: readonly RatingTask[] }) {
  return (
    <Table<RatingTask>
      rowKey="applicationId"
      columns={columns}
      dataSource={[...tasks]}
      pagination={false}
      scroll={{ x: "max-content" }}
      locale={{
        emptyText: (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description="Вас пока не просили оценивать кандидатов."
          />
        ),
      }}
    />
  );
}
