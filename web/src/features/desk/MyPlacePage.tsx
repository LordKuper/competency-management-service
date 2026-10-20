import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  theme as antdTheme,
  Button,
  Card,
  Col,
  Descriptions,
  Empty,
  Flex,
  Row,
  Skeleton,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { COMPETENCY_TYPES } from "../career/competencyType";
import { matrixVersionQuery } from "../career/matrixApi";
import { chipLevel } from "../career/matrixContent";
import {
  OUTSIDE_LABEL,
  scaleOfCompetency,
  sharedBlockOf,
} from "../career/matrixGroups";
import "../career/matrix.css";
import { type MyPlace, myPlaceQuery } from "./deskApi";
import { deskPaths, formatDate } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import { CompactAlert } from "./ScenarioContext";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";

type TrackView = MyPlace["tracks"][number];
type LevelView = TrackView["nextLevels"][number];
type Requirement = NonNullable<LevelView["requirements"]>[number];
type MatrixView = MyPlace["publishedMatrices"][number];
type PositionMatrix = NonNullable<MyPlace["positionMatrix"]>;
type PositionRequirement = PositionMatrix["requirements"][number];

const matrixPath = (matrixId: string) => `/career/matrices/${matrixId}`;

/** «Моё место в компании»: the unit, position, head and HR, the tracks of the position and what the levels above ask. */
export function MyPlacePage() {
  const place = useQuery(myPlaceQuery);
  return (
    <ScenarioPage title="Моё место в компании">
      {place.error ? (
        <LoadFailure
          title="Не удалось загрузить данные о вашем месте в компании"
          error={place.error}
          onRetry={() => void place.refetch()}
        />
      ) : !place.data ? (
        <Skeleton active />
      ) : (
        <PlaceContent place={place.data} />
      )}
    </ScenarioPage>
  );
}

function PlaceContent({ place }: { place: MyPlace }) {
  const { token } = antdTheme.useToken();
  if (!place.employee) {
    return (
      <Empty
        description={
          <>
            <Typography.Title level={4}>
              Нет данных о вашем месте в компании
            </Typography.Title>
            <Typography.Paragraph>
              Ваша учётная запись не привязана к карточке сотрудника, поэтому
              подразделение, должность и треки показать нельзя. Обратитесь к
              администратору, чтобы вас привязали.
            </Typography.Paragraph>
          </>
        }
      >
        <LinkButton to={deskPaths.desk} type="primary">
          К рабочему столу
        </LinkButton>
      </Empty>
    );
  }
  const unitName = place.unit.at(-1)?.name ?? "";
  const hr = place.responsibleHr;
  return (
    <Flex vertical gap="middle">
      <section>
        <Typography.Title level={4}>Подразделение и должность</Typography.Title>
        <Row gutter={[token.margin, 0]}>
          <Col xs={24} lg={12}>
            <Descriptions
              column={1}
              size="small"
              items={[
                {
                  key: "unit",
                  label: "Подразделение",
                  children: place.unit.map((part) => part.name).join(" → "),
                },
                {
                  key: "position",
                  label: "Должность",
                  children: place.position?.name ?? "—",
                },
              ]}
            />
          </Col>
          <Col xs={24} lg={12}>
            <Descriptions
              column={1}
              size="small"
              items={[
                {
                  key: "head",
                  label: "Руководитель",
                  children: place.head?.fullName ?? "Не назначен",
                },
                {
                  key: "hr",
                  label: "Ответственный HR",
                  children: hr
                    ? `${hr.name} · ${hr.orgUnitName === unitName ? "закрепление за вашим подразделением" : `унаследовано из «${hr.orgUnitName}»`}`
                    : "Не назначен",
                },
              ]}
            />
          </Col>
        </Row>
      </section>
      {place.positionMatrix ? (
        <PositionMatrixPanel
          matrix={place.positionMatrix}
          unitName={unitName}
        />
      ) : (
        <Flex vertical gap="middle">
          <Typography.Title level={4} style={{ margin: 0 }}>
            Мои карьерные треки
          </Typography.Title>
          {place.noTrack ? (
            <CompactAlert
              type="info"
              lead="Для вашей должности нет карьерного трека."
              text={`Должность «${place.position?.name ?? ""}» не связана ни с одним уровнем действующего трека, поэтому уровень не определён.${place.head ? ` Обратитесь к руководителю: ${place.head.fullName}.` : ""}`}
            />
          ) : (
            place.tracks.map((track) => (
              <TrackPanel
                key={track.track.id}
                track={track}
                positionName={place.position?.name ?? ""}
                unitName={unitName}
              />
            ))
          )}
        </Flex>
      )}
      <PublishedMatrices
        matrices={place.publishedMatrices}
        unitName={unitName}
      />
    </Flex>
  );
}

/** The requirements of the published position matrix of the user's unit for their position, as one column of scores. */
function PositionMatrixPanel({
  matrix,
  unitName,
}: {
  matrix: PositionMatrix;
  unitName: string;
}) {
  const version = useQuery(matrixVersionQuery(matrix.versionId));
  const blockOf = (competencyId: string) => {
    const shared = version.data && sharedBlockOf(version.data, competencyId);
    if (shared) return `Общий блок «${shared.name}»`;
    return version.data ? OUTSIDE_LABEL : undefined;
  };
  const examplesOf = (competencyId: string) =>
    version.data?.entries.competencies.find((item) => item.id === competencyId)
      ?.evidenceExamples;
  const columns: TableColumnsType<PositionRequirement> = [
    {
      title: "Компетенция",
      key: "name",
      render: (_, item) => (
        <Flex vertical>
          {item.competencyName}
          {examplesOf(item.competencyId) && (
            <Typography.Text type="secondary">
              {`Примеры подтверждений: ${examplesOf(item.competencyId)}`}
            </Typography.Text>
          )}
        </Flex>
      ),
    },
    {
      title: "Требуемый балл",
      key: "score",
      render: (_, item) => {
        if (item.requiredScore === null) return "—";
        const scale =
          version.data && scaleOfCompetency(version.data, item.competencyId);
        return (
          <Flex align="center" gap="small" wrap>
            <span
              className={`rating-chip rating-chip--${scale ? chipLevel(item.requiredScore, scale.points) : 0}`}
            >
              {item.requiredScore}
            </span>
            {[item.scoreLabel, scale && `шкала ${scale.name}`]
              .filter(Boolean)
              .join(" · ")}
          </Flex>
        );
      },
    },
    {
      title: "Тип в матрице",
      key: "type",
      render: (_, item) =>
        item.type === "Critical" ? (
          <Tag color="warning">{COMPETENCY_TYPES.Critical}</Tag>
        ) : (
          COMPETENCY_TYPES[item.type]
        ),
    },
    {
      title: "Блок",
      key: "block",
      render: (_, item) => blockOf(item.competencyId),
    },
  ];
  return (
    <Flex vertical gap="middle">
      <Typography.Title level={4} style={{ margin: 0 }}>
        Требования моей должности
      </Typography.Title>
      <Card
        size="small"
        title={
          <Flex align="center" gap="small" wrap>
            <span>{matrix.positionName}</span>
            <Tag>Матрица должности</Tag>
          </Flex>
        }
      >
        <Flex vertical gap="small">
          <Typography.Text>
            {`Требования взяты из опубликованной матрицы должности подразделения «${unitName}», версия ${matrix.number}. `}
            <Link to={matrixPath(matrix.matrixId)}>Смотреть матрицу</Link>
          </Typography.Text>
          <Table
            size="small"
            rowKey="competencyId"
            pagination={false}
            columns={columns}
            dataSource={[...matrix.requirements]}
          />
        </Flex>
      </Card>
    </Flex>
  );
}

function TrackPanel({
  track,
  positionName,
  unitName,
}: {
  track: TrackView;
  positionName: string;
  unitName: string;
}) {
  const [expanded, setExpanded] = useState<string[]>([]);
  const toggle = (id: string) =>
    setExpanded((keys) =>
      keys.includes(id) ? keys.filter((key) => key !== id) : [...keys, id],
    );
  const columns: TableColumnsType<LevelView> = [
    { title: "Следующий уровень", dataIndex: "name" },
    {
      title: "Требования",
      key: "requirements",
      render: (_, level) =>
        level.requirements
          ? `Компетенций: ${level.requirements.length} · критических: ${level.requirements.filter((item) => item.type === "Critical").length}`
          : "Нет данных",
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, level) =>
        level.requirements && (
          <Button
            type="link"
            size="small"
            aria-expanded={expanded.includes(level.id)}
            onClick={() => toggle(level.id)}
          >
            {expanded.includes(level.id)
              ? "Скрыть требования"
              : "Показать требования"}
          </Button>
        ),
    },
  ];
  return (
    <Card
      size="small"
      title={
        <Flex align="center" gap="small" wrap>
          <span>{track.track.name}</span>
          <Tag>{`Область: ${track.direction.name}`}</Tag>
        </Flex>
      }
    >
      <Flex vertical gap="small">
        <Flex align="center" gap="small" wrap>
          <Typography.Text strong>Мой уровень:</Typography.Text>
          <Tag color="processing">{track.currentLevel.name}</Tag>
          <Typography.Text type="secondary">
            {`определён по вашей должности «${positionName}»`}
          </Typography.Text>
        </Flex>
        {track.matrix ? (
          <Typography.Text>
            {`Требования взяты из опубликованной матрицы подразделения «${unitName}», версия ${track.matrix.number}. `}
            <Link to={matrixPath(track.matrix.matrixId)}>Смотреть матрицу</Link>
          </Typography.Text>
        ) : (
          <Alert
            type="info"
            showIcon
            title="Для вашего подразделения нет опубликованной матрицы по этому треку: следующие уровни показаны без требований."
          />
        )}
        {track.nextLevels.length === 0 ? (
          <Typography.Text type="secondary">
            Это высший уровень трека: следующих уровней нет.
          </Typography.Text>
        ) : (
          <Table
            size="small"
            rowKey="id"
            pagination={false}
            columns={columns}
            dataSource={track.nextLevels}
            expandable={{
              showExpandColumn: false,
              expandedRowKeys: expanded,
              expandedRowRender: (level) => (
                <RequirementsTable requirements={level.requirements ?? []} />
              ),
            }}
          />
        )}
      </Flex>
    </Card>
  );
}

function RequirementsTable({
  requirements,
}: {
  requirements: readonly Requirement[];
}) {
  return (
    <Table
      size="small"
      rowKey="competencyId"
      pagination={false}
      dataSource={[...requirements]}
      columns={[
        { title: "Компетенция", dataIndex: "competencyName" },
        {
          title: "Требуемый уровень",
          key: "score",
          render: (_, item: Requirement) =>
            item.requiredScore === null
              ? "—"
              : [item.requiredScore, item.scoreLabel].filter(Boolean).join(" "),
        },
        {
          title: "Тип в матрице",
          key: "type",
          render: (_, item: Requirement) =>
            item.type === "Critical" ? (
              <Tag color="warning">{COMPETENCY_TYPES.Critical}</Tag>
            ) : (
              COMPETENCY_TYPES[item.type]
            ),
        },
      ]}
    />
  );
}

function PublishedMatrices({
  matrices,
  unitName,
}: {
  matrices: readonly MatrixView[];
  unitName: string;
}) {
  return (
    <Flex vertical gap="middle">
      <Typography.Title level={4} style={{ margin: 0 }}>
        Опубликованные матрицы моего подразделения
      </Typography.Title>
      {matrices.length === 0 ? (
        <CompactAlert
          type="info"
          lead="Опубликованных матриц пока нет."
          text={`Для подразделения «${unitName}» ещё не опубликована ни одна матрица, поэтому требований к уровням нет. Матрицу готовит руководитель подразделения, публикует её ответственный HR при согласовании.`}
        />
      ) : (
        <Table
          size="small"
          rowKey="matrixId"
          pagination={false}
          dataSource={[...matrices]}
          columns={[
            { title: "Трек", dataIndex: "trackName" },
            { title: "Версия", dataIndex: "number" },
            {
              title: "Опубликована",
              key: "publishedAt",
              render: (_, matrix: MatrixView) =>
                matrix.publishedAt ? formatDate(matrix.publishedAt) : "—",
            },
            {
              title: "Действия",
              key: "actions",
              render: (_, matrix: MatrixView) => (
                <Link to={matrixPath(matrix.matrixId)}>
                  Смотреть требования
                </Link>
              ),
            },
          ]}
        />
      )}
    </Flex>
  );
}
