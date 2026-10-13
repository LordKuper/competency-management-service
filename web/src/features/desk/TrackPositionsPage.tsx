import "./scenario.css";
import { DownOutlined } from "@ant-design/icons";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Alert,
  App,
  Button,
  Card,
  ConfigProvider,
  Dropdown,
  Flex,
  Input,
  Select,
  Skeleton,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useState } from "react";
import { useNavigate, useParams } from "react-router";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { CatalogStatusTag } from "../career/CatalogParts";
import { useProcessRoles } from "../career/careerApi";
import {
  type AvailablePosition,
  availablePositionsQuery,
  entryQuery,
  invalidateCatalogs,
  linkLevelPosition,
  type TrackPositions,
  trackPositionsQuery,
  trackQuery,
  tracksQuery,
  unlinkLevelPosition,
} from "../career/catalogApi";
import { employees, PositionsError } from "../career/LevelPositionsModal";
import { deskPaths } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import { useReturnLink } from "./returnLink";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";

type Level = TrackPositions["levels"][number];
type LinkedPosition = Level["positions"][number];

const TITLE = "Связать должности с треком";
const TRACKS_PAGE_SIZE = 200;

/** «Связать должности с треком»: the levels of a track with their positions, and the positions that can still be linked. */
export function TrackPositionsPage() {
  const { trackId } = useParams();
  return (
    <ScenarioPage title={TITLE}>
      {trackId ? <TrackScreen trackId={trackId} /> : <TrackChoice />}
    </ScenarioPage>
  );
}

/** Opened from the desk without a track: the tracks the user may edit are offered. */
function TrackChoice() {
  const navigate = useNavigate();
  const roles = useProcessRoles();
  const tracks = useQuery(tracksQuery({ page: 1, pageSize: TRACKS_PAGE_SIZE }));
  if (tracks.error) {
    return (
      <LoadFailure
        title="Не удалось загрузить треки"
        error={tracks.error}
        onRetry={() => void tracks.refetch()}
      />
    );
  }
  const editable = (tracks.data?.items ?? []).filter((track) => track.canEdit);
  return (
    <Flex vertical gap="middle">
      <Typography.Paragraph>
        Выберите трек, чтобы связать его уровни с должностями.
      </Typography.Paragraph>
      <Select
        showSearch={{ optionFilterProp: "label" }}
        aria-label="Трек"
        placeholder="Трек"
        style={{ maxWidth: 480 }}
        loading={tracks.isPending || roles.isPending}
        options={editable.map((track) => ({
          value: track.id,
          label: `${track.name} — ${track.ownerOrgUnitName}`,
        }))}
        notFoundContent="Треков, связи которых вы вправе править, нет."
        onChange={(id) => void navigate(deskPaths.trackPositionsOf(id))}
      />
    </Flex>
  );
}

function TrackScreen({ trackId }: { trackId: string }) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const back = useReturnLink() ?? {
    to: deskPaths.desk,
    label: "К рабочему столу",
  };
  const track = useQuery(trackQuery(trackId));
  const direction = useQuery({
    ...entryQuery("directions", track.data?.directionId ?? ""),
    enabled: track.data !== undefined,
  });
  const read = useQuery(trackPositionsQuery(trackId));
  const change = useMutation({
    mutationFn: ({
      level,
      positionId,
      action,
    }: {
      level: Level;
      positionId: string;
      positionName: string;
      action: "link" | "unlink";
    }) =>
      action === "link"
        ? linkLevelPosition(level, positionId)
        : unlinkLevelPosition(level, positionId),
    onSuccess: (_, { level, positionName, action }) => {
      message.success(
        action === "link"
          ? `Должность «${positionName}» связана с уровнем «${level.name}»`
          : "Связь снята",
      );
    },
    onSettled: () => invalidateCatalogs(queryClient),
  });

  const failure = track.error ?? read.error;
  if (failure) {
    return (
      <LoadFailure
        title="Не удалось загрузить трек"
        error={failure}
        onRetry={() => {
          void track.refetch();
          void read.refetch();
        }}
      />
    );
  }
  if (!track.data || !read.data) return <Skeleton active />;

  const { levels, canEdit } = read.data;
  const missing = levels.filter((level) => !level.hasActivePosition).length;

  return (
    <ConfigProvider componentDisabled={change.isPending}>
      <Flex vertical gap="large">
        <Flex vertical gap="small">
          <Flex align="center" gap="small" wrap>
            <Typography.Title level={2} style={{ margin: 0 }}>
              {`Трек «${track.data.name}»`}
            </Typography.Title>
            <CatalogStatusTag status={track.data.status} />
          </Flex>
          <Typography.Text type="secondary">
            {[
              track.data.ownerOrgUnitName,
              direction.data && `область «${direction.data.name}»`,
            ]
              .filter(Boolean)
              .join(" · ")}
          </Typography.Text>
        </Flex>
        {missing > 0 && (
          <Alert
            type="warning"
            showIcon
            title={`Уровней без действующей должности: ${missing} из ${levels.length}`}
            description="Пока они есть, нельзя отправить на согласование и опубликовать матрицу по этому треку."
          />
        )}
        {!canEdit && (
          <Alert
            type="info"
            showIcon
            title="Связи этого трека вправе менять ответственный HR подразделения и администратор; вы можете их только смотреть."
          />
        )}
        {change.error != null && <PositionsError error={change.error} />}
        <div className="queue-columns">
          <section>
            <Typography.Title level={3}>Уровни трека</Typography.Title>
            <Flex vertical gap="small">
              {levels.map((level, index) => (
                <LevelCard
                  key={level.id}
                  level={level}
                  number={index + 1}
                  canEdit={canEdit}
                  onUnlink={(position) =>
                    change.mutate({
                      level,
                      positionId: position.id,
                      positionName: position.name,
                      action: "unlink",
                    })
                  }
                />
              ))}
            </Flex>
          </section>
          {canEdit && (
            <AvailablePositions
              trackId={trackId}
              levels={levels}
              onLink={(level, position) =>
                change.mutate({
                  level,
                  positionId: position.id,
                  positionName: position.name,
                  action: "link",
                })
              }
            />
          )}
        </div>
        <LinkButton to={back.to}>{back.label}</LinkButton>
      </Flex>
    </ConfigProvider>
  );
}

interface LevelCardProps {
  level: Level;
  number: number;
  canEdit: boolean;
  onUnlink: (position: LinkedPosition) => void;
}

function LevelCard({ level, number, canEdit, onUnlink }: LevelCardProps) {
  const isMissing = !level.hasActivePosition;
  return (
    <Card
      size="small"
      style={
        isMissing ? { background: "var(--ant-color-warning-bg)" } : undefined
      }
    >
      <Flex vertical gap="small">
        <Flex justify="space-between" align="center" gap="small" wrap>
          <Typography.Text strong>{`${number}. ${level.name}`}</Typography.Text>
          {isMissing ? (
            <Tag color="warning">Нет действующей должности</Tag>
          ) : (
            <Typography.Text type="secondary">
              {`Должностей: ${level.positions.length}`}
            </Typography.Text>
          )}
        </Flex>
        {level.positions.length === 0 && (
          <Typography.Text type="secondary">
            {canEdit
              ? "Связанных должностей нет. Выберите должность справа и нажмите «Связать с уровнем»."
              : "Связанных должностей нет."}
          </Typography.Text>
        )}
        {level.positions.map((position) => (
          <PositionLine
            key={position.id}
            position={position}
            canEdit={canEdit}
            onUnlink={() => onUnlink(position)}
          />
        ))}
      </Flex>
    </Card>
  );
}

interface PositionLineProps {
  position: LinkedPosition;
  canEdit: boolean;
  onUnlink: () => void;
}

function PositionLine({ position, canEdit, onUnlink }: PositionLineProps) {
  return (
    <Flex justify="space-between" align="center" gap="small" wrap>
      <Typography.Text>
        {`${position.name} · ${employees(position.employeeCount)}`}
        {!position.isActive && (
          <Tag style={{ marginInlineStart: 8 }}>Неактивна</Tag>
        )}
      </Typography.Text>
      {canEdit &&
        (position.canUnlink ? (
          <Button
            type="link"
            size="small"
            aria-label={`Снять «${position.name}»`}
            onClick={onUnlink}
          >
            Снять
          </Button>
        ) : (
          <Typography.Text type="secondary">
            Последняя действующая должность уровня действующего трека
          </Typography.Text>
        ))}
    </Flex>
  );
}

interface AvailablePositionsProps {
  trackId: string;
  levels: readonly Level[];
  onLink: (level: Level, position: AvailablePosition) => void;
}

function AvailablePositions({
  trackId,
  levels,
  onLink,
}: AvailablePositionsProps) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const q = useDebouncedValue(search.trim());
  const available = useQuery({
    ...availablePositionsQuery(trackId, q, page),
    placeholderData: keepPreviousData,
  });
  const columns: TableColumnsType<AvailablePosition> = [
    { title: "Должность", dataIndex: "name" },
    {
      title: "Сотрудников",
      dataIndex: "employeeCount",
      align: "right",
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, position) => (
        <Dropdown
          trigger={["click"]}
          menu={{
            items: levels.map((level, index) => ({
              key: level.id,
              label: `${index + 1}. ${level.name}`,
              onClick: () => onLink(level, position),
            })),
          }}
        >
          <Button aria-label={`Связать «${position.name}» с уровнем`}>
            Связать с уровнем <DownOutlined />
          </Button>
        </Dropdown>
      ),
    },
  ];
  return (
    <section>
      <Typography.Title level={3}>Доступные должности</Typography.Title>
      <Typography.Paragraph type="secondary">
        Действующие должности, доступные в подразделении трека и дочерних, ещё
        не связанные ни с одним уровнем этого трека.
      </Typography.Paragraph>
      <Flex vertical gap="small">
        <Input.Search
          aria-label="Найти должность"
          placeholder="Найти должность"
          allowClear
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
        />
        {available.error ? (
          <LoadFailure
            title="Не удалось загрузить должности"
            error={available.error}
            onRetry={() => void available.refetch()}
          />
        ) : (
          <Table<AvailablePosition>
            size="small"
            rowKey="id"
            columns={columns}
            dataSource={available.data?.items}
            loading={available.isFetching}
            scroll={{ x: "max-content" }}
            pagination={{
              current: page,
              pageSize: 20,
              total: available.data?.total,
              showSizeChanger: false,
              onChange: setPage,
            }}
            locale={{
              emptyText: q
                ? `Должность «${q}» не найдена среди доступных.`
                : "Доступных должностей нет. Все действующие должности подразделения и дочерних уже связаны с уровнями этого трека либо перечни подразделений пусты. Добавьте должности в перечень подразделения в «Справочники → Оргструктура».",
            }}
          />
        )}
      </Flex>
    </section>
  );
}
