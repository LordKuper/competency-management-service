import "./scenario.css";
import { DownOutlined } from "@ant-design/icons";
import {
  keepPreviousData,
  useIsMutating,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Alert,
  App,
  Button,
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
import { OptionWithTag } from "../../app/OptionWithTag";
import { TruncationNote } from "../../app/TruncationNote";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { CatalogStatusTag } from "../career/CatalogParts";
import {
  type AvailablePosition,
  availablePositionsQuery,
  entryQuery,
  invalidateCatalogs,
  linkLevelPosition,
  MAX_PAGE_SIZE,
  type TrackPositions,
  trackPositionsQuery,
  trackQuery,
  tracksQuery,
  unlinkLevelPosition,
} from "../career/catalogApi";
import { employees, PositionsError } from "../career/LevelPositionsModal";
import { deskPaths } from "./deskTexts";
import { CompactAlert, ContextLine } from "./ScenarioContext";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";

type Level = TrackPositions["levels"][number];
type LinkedPosition = Level["positions"][number];

const TITLE = "Связать должности с треком";
const LINK_MUTATION = ["desk", "track-positions"] as const;

/**
 * «Связать должности с треком»: the choice of a track stays on screen, searched on the server; below it the levels with
 * their positions and the positions that can still be linked.
 */
export function TrackPositionsPage() {
  const { trackId } = useParams();
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const q = useDebouncedValue(search.trim());
  const tracks = useQuery({
    ...tracksQuery({ q: q || undefined, page: 1, pageSize: MAX_PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });
  const track = useQuery({
    ...trackQuery(trackId ?? ""),
    enabled: trackId !== undefined,
  });
  const direction = useQuery({
    ...entryQuery("directions", track.data?.directionId ?? ""),
    enabled: track.data !== undefined,
  });
  const isChanging = useIsMutating({ mutationKey: LINK_MUTATION }) > 0;
  const listed = (tracks.data?.items ?? []).filter(
    (item) => item.canEdit || item.id === trackId,
  );
  const options =
    track.data && !listed.some((item) => item.id === track.data.id)
      ? [...listed, track.data]
      : listed;

  return (
    <ScenarioPage title={TITLE}>
      <ContextLine>
        <Typography.Text strong>Трек</Typography.Text>
        <div className="context-select">
          <Select
            showSearch={{ filterOption: false, onSearch: setSearch }}
            onOpenChange={(isOpen) => {
              if (!isOpen) setSearch("");
            }}
            aria-label="Трек"
            placeholder="Выберите трек"
            value={trackId}
            disabled={isChanging}
            loading={tracks.isFetching}
            options={options.map((item) => ({
              value: item.id,
              label: `${item.name} — ${item.ownerOrgUnitName}`,
              status: item.status,
            }))}
            optionRender={(option) => (
              <OptionWithTag
                label={String(option.label)}
                tag={<CatalogStatusTag status={option.data.status} />}
              />
            )}
            labelRender={({ label, value }) => {
              const chosen = options.find((item) => item.id === value);
              return chosen ? (
                <OptionWithTag
                  label={String(label)}
                  tag={<CatalogStatusTag status={chosen.status} />}
                />
              ) : (
                label
              );
            }}
            notFoundContent={
              q
                ? `Трек «${q}» не найден среди тех, связи которых вы вправе править.`
                : "Треков, связи которых вы вправе править, нет."
            }
            onChange={(id) => void navigate(deskPaths.trackPositionsOf(id))}
          />
        </div>
        {direction.data && (
          <Typography.Text type="secondary">{`область «${direction.data.name}»`}</Typography.Text>
        )}
      </ContextLine>
      {trackId ? (
        <TrackScreen trackId={trackId} />
      ) : tracks.error ? (
        <LoadFailure
          title="Не удалось загрузить треки"
          error={tracks.error}
          onRetry={() => void tracks.refetch()}
        />
      ) : (
        <Typography.Text type="secondary">
          Выберите трек, чтобы связать его уровни с должностями.
        </Typography.Text>
      )}
    </ScenarioPage>
  );
}

function TrackScreen({ trackId }: { trackId: string }) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const track = useQuery(trackQuery(trackId));
  const read = useQuery(trackPositionsQuery(trackId));
  const change = useMutation({
    mutationKey: LINK_MUTATION,
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
      <Flex vertical gap="middle">
        {missing > 0 && (
          <CompactAlert
            type="warning"
            lead={`Уровней без действующей должности: ${missing} из ${levels.length}.`}
            text="Пока они есть, матрицу по этому треку нельзя отправить и опубликовать."
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
            <Typography.Title level={4}>Уровни трека</Typography.Title>
            <LevelsTable
              levels={levels}
              canEdit={canEdit}
              onUnlink={(level, position) =>
                change.mutate({
                  level,
                  positionId: position.id,
                  positionName: position.name,
                  action: "unlink",
                })
              }
            />
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
      </Flex>
    </ConfigProvider>
  );
}

interface LevelsTableProps {
  levels: readonly Level[];
  canEdit: boolean;
  onUnlink: (level: Level, position: LinkedPosition) => void;
}

function LevelsTable({ levels, canEdit, onUnlink }: LevelsTableProps) {
  const columns: TableColumnsType<Level> = [
    {
      title: "Уровень",
      key: "level",
      render: (_, level, index) => `${index + 1}. ${level.name}`,
    },
    {
      title: "Должности",
      key: "positions",
      render: (_, level) => (
        <Flex vertical gap="small">
          {!level.hasActivePosition && (
            <Flex align="center" gap="small" wrap>
              <Tag color="warning">Нет действующей должности</Tag>
              {canEdit && (
                <Typography.Text type="secondary">
                  Выберите должность справа
                </Typography.Text>
              )}
            </Flex>
          )}
          {level.positions.map((position) => (
            <PositionLine
              key={position.id}
              position={position}
              canEdit={canEdit}
              onUnlink={() => onUnlink(level, position)}
            />
          ))}
        </Flex>
      ),
    },
  ];
  return (
    <Table<Level>
      size="small"
      rowKey="id"
      columns={columns}
      dataSource={[...levels]}
      pagination={false}
      className="desk-table"
      rowClassName={(level) =>
        level.hasActivePosition ? "" : "level-row-missing"
      }
    />
  );
}

interface PositionLineProps {
  position: LinkedPosition;
  canEdit: boolean;
  onUnlink: () => void;
}

function PositionLine({ position, canEdit, onUnlink }: PositionLineProps) {
  return (
    <Flex align="center" gap="small" wrap>
      <Typography.Text>
        {`${position.name} · ${employees(position.employeeCount)}`}
      </Typography.Text>
      {!position.isActive && <Tag>Неактивна</Tag>}
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
  const q = useDebouncedValue(search.trim());
  const available = useQuery({
    ...availablePositionsQuery(trackId, q, 1, MAX_PAGE_SIZE),
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
      <Typography.Title level={4}>Доступные должности</Typography.Title>
      <Flex vertical gap="small">
        <Input.Search
          aria-label="Найти должность"
          placeholder="Найти должность"
          allowClear
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
          }}
        />
        <Typography.Text type="secondary">
          Ещё не связаны ни с одним уровнем этого трека.
        </Typography.Text>
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
            className="desk-table"
            pagination={false}
            locale={{
              emptyText: q
                ? `Должность «${q}» не найдена среди доступных.`
                : "Доступных должностей нет. Все действующие должности подразделения и дочерних уже связаны с уровнями этого трека либо перечни подразделений пусты. Добавьте должности в перечень подразделения в «Справочники → Оргструктура».",
            }}
          />
        )}
        <TruncationNote
          shown={available.data?.items.length ?? 0}
          total={available.data?.total}
        />
      </Flex>
    </section>
  );
}
