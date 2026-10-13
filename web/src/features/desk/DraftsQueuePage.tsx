import { WarningOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
  Alert,
  Button,
  Descriptions,
  Flex,
  Skeleton,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { type RefObject, useState } from "react";
import { Link, useLocation, useParams } from "react-router";
import { NotFoundPage } from "../../app/NotFoundPage";
import { useProcessRoles } from "../career/careerApi";
import {
  type CatalogDraft,
  type CatalogKind,
  draftsQuery,
  type EntryMatrix,
  entryMatricesQuery,
  publicationPreviewQuery,
  SEGMENT_OF_KIND,
} from "../career/catalogApi";
import { KIND_LABELS } from "../career/DraftsSection";
import { VERSION_STATUS_LABELS } from "../career/matrixLabels";
import { useCatalogRights } from "../career/useCatalogRights";
import {
  RefusalContent,
  recordsInstrumental,
  usePublishEntry,
} from "../career/usePublishEntry";
import { CascadeTable } from "./CascadeTable";
import { deskPaths, formatDate, relativeTime, shortName } from "./deskTexts";
import { EditDraftModal } from "./EditDraftModal";
import {
  QueueLayout,
  useQueueSelection,
  useRecordFocus,
  useShownCount,
} from "./QueueLayout";
import { returnState } from "./returnLink";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";

const FIRST_COUNT = 20;
const MORE_COUNT = 20;

const KIND_OF_URL: Record<string, CatalogKind> = {
  direction: "Direction",
  track: "Track",
  category: "Category",
  competency: "Competency",
  scale: "Scale",
};

const KIND_OPTIONS = Object.entries(KIND_LABELS).map(([value, label]) => ({
  value,
  label,
}));

const keyOf = (draft: CatalogDraft) => `${draft.kind}:${draft.id}`;

/** What stops a draft track from being approved: no levels, or levels no active position is linked to. */
function earlyReasons(draft: CatalogDraft): string[] {
  const { readiness } = draft;
  if (draft.kind !== "Track" || !readiness) return [];
  return readiness.hasLevels
    ? readiness.levelsWithoutActivePosition.map(
        (level) => `Уровень «${level.name}»: нет действующей должности`,
      )
    : ["У трека нет уровней"];
}

/** «Утвердить черновые записи»: the drafts the user may approve, one record at a time. */
export function DraftsQueuePage() {
  const { kind, entryId } = useParams();
  const roles = useProcessRoles();
  const rights = useCatalogRights();
  const [filterKind, setFilterKind] = useState<CatalogKind>();
  const { shown, showMore } = useShownCount(FIRST_COUNT, MORE_COUNT);
  const list = useQuery({
    ...draftsQuery({ kind: filterKind }),
    enabled: rights.isHr,
  });
  const requestedKey =
    kind && entryId && KIND_OF_URL[kind]
      ? `${KIND_OF_URL[kind]}:${entryId}`
      : undefined;
  const drafts = list.data ?? [];
  const selection = useQueueSelection(
    drafts.map(keyOf),
    list.data !== undefined,
    requestedKey,
  );
  const current = drafts.find(
    (draft) => keyOf(draft) === selection.selectedKey,
  );
  const headingRef = useRecordFocus(selection.selectedKey);

  if (roles.isSuccess && !rights.isHr) return <NotFoundPage />;

  return (
    <ScenarioPage title="Утвердить черновые записи">
      {list.error ? (
        <LoadFailure
          title="Не удалось загрузить очередь"
          error={list.error}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <>
          {list.data && !selection.wasRequestedFound && (
            <Alert
              type="info"
              showIcon
              title="Запись уже утверждена другим пользователем или недоступна вам."
            />
          )}
          <QueueLayout
            items={drafts.slice(0, shown)}
            total={drafts.length}
            getKey={keyOf}
            selectedKey={selection.selectedKey}
            onSelect={selection.select}
            renderItem={(draft) => <DraftRow draft={draft} />}
            filter={{
              value: filterKind,
              options: KIND_OPTIONS,
              onChange: (value) => setFilterKind(value as CatalogKind),
            }}
            onShowMore={showMore}
            moreCount={MORE_COUNT}
            isLoading={list.isPending}
            emptyText="Черновых записей, которые вы вправе утвердить, нет. Новые появятся здесь и на рабочем столе."
          >
            {list.isPending ? (
              <Skeleton active />
            ) : current ? (
              <DraftRecord
                key={keyOf(current)}
                draft={current}
                position={drafts.indexOf(current) + 1}
                total={drafts.length}
                headingRef={headingRef}
              />
            ) : (
              <Typography.Paragraph>
                Записей этого вида в очереди нет.
              </Typography.Paragraph>
            )}
          </QueueLayout>
        </>
      )}
    </ScenarioPage>
  );
}

function DraftRow({ draft }: { draft: CatalogDraft }) {
  const isEarly = earlyReasons(draft).length > 0;
  return (
    <Flex vertical gap={2}>
      <Flex align="center" gap="small" wrap>
        <Tag>{KIND_LABELS[draft.kind]}</Tag>
        <Typography.Text strong>{draft.name}</Typography.Text>
        {isEarly && (
          <Tag icon={<WarningOutlined />} color="warning">
            Утверждать рано
          </Tag>
        )}
      </Flex>
      <Typography.Text type="secondary">
        {[
          draft.ownerOrgUnitName ?? "Общая",
          draft.authorName && shortName(draft.authorName),
          relativeTime(draft.createdAt),
        ]
          .filter(Boolean)
          .join(" · ")}
      </Typography.Text>
    </Flex>
  );
}

const MATRIX_COLUMNS: TableColumnsType<EntryMatrix> = [
  {
    title: "Матрица",
    key: "matrix",
    render: (_, matrix) => (
      <Link to={`/career/matrices/${matrix.matrixId}`}>
        {`${matrix.trackName} — ${matrix.orgUnitName}`}
      </Link>
    ),
  },
  { title: "Версия", dataIndex: "number" },
  {
    title: "Статус",
    dataIndex: "status",
    render: (status: EntryMatrix["status"]) => VERSION_STATUS_LABELS[status],
  },
];

interface DraftRecordProps {
  draft: CatalogDraft;
  position: number;
  total: number;
  headingRef: RefObject<HTMLHeadingElement | null>;
}

function DraftRecord({ draft, position, total, headingRef }: DraftRecordProps) {
  const location = useLocation();
  const rights = useCatalogRights();
  const [isEditing, setIsEditing] = useState(false);
  const segment = SEGMENT_OF_KIND[draft.kind];
  const target = { segment, entry: draft };
  const preview = useQuery(publicationPreviewQuery(segment, draft));
  const matrices = useQuery(entryMatricesQuery(segment, draft.id));
  const { approveShown, publish, isBusy } = usePublishEntry();
  const reasons = earlyReasons(draft);
  const isEarly = reasons.length > 0;
  const canApprove = rights.canManage(draft.ownerOrgUnitId ?? undefined);
  const entries = preview.data?.entries ?? [];
  const label =
    entries.length > 0
      ? `Утвердить вместе с ${recordsInstrumental(entries.length)}`
      : "Утвердить";
  const back = {
    to: location.pathname + location.search,
    label: "К очереди черновиков",
  };

  return (
    <Flex vertical gap="middle">
      <Typography.Title
        level={2}
        tabIndex={-1}
        ref={headingRef}
        style={{ margin: 0 }}
      >
        {`${KIND_LABELS[draft.kind]} ${draft.name}`}
      </Typography.Title>
      <Typography.Text type="secondary">{`Запись ${position} из ${total}`}</Typography.Text>
      {isEarly && (
        <Alert
          type="warning"
          showIcon
          title="Утверждать рано"
          description={
            <>
              <p>
                Трек нельзя сделать действующим, пока у его уровней нет
                связанных действующих должностей.
              </p>
              <ul>
                {reasons.map((reason) => (
                  <li key={reason}>{reason}</li>
                ))}
              </ul>
            </>
          }
        />
      )}
      <Descriptions
        column={1}
        size="small"
        items={[
          {
            key: "author",
            label: "Автор",
            children: draft.authorName ?? "—",
          },
          {
            key: "unit",
            label: "Подразделение",
            children: draft.ownerOrgUnitName ?? "Общая (без подразделения)",
          },
          {
            key: "created",
            label: "Создана",
            children: `${formatDate(draft.createdAt)}, ${relativeTime(draft.createdAt)}`,
          },
        ]}
      />
      <section>
        <Typography.Title level={4}>Используют матрицы</Typography.Title>
        {matrices.error ? (
          <LoadFailure
            title="Не удалось загрузить матрицы"
            error={matrices.error}
            onRetry={() => void matrices.refetch()}
          />
        ) : (
          <>
            <Table<EntryMatrix>
              size="small"
              rowKey={(matrix) => matrix.versionId}
              columns={MATRIX_COLUMNS}
              dataSource={matrices.data}
              loading={matrices.isFetching}
              pagination={false}
              scroll={{ x: "max-content" }}
              locale={{
                emptyText:
                  "Записью пока не пользуется ни одна матрица, доступная вам.",
              }}
            />
            <Typography.Text type="secondary">
              Показаны матрицы и версии, которые вы вправе читать.
            </Typography.Text>
          </>
        )}
      </section>
      <section>
        <Typography.Title level={4}>
          Вместе с записью будет опубликовано
        </Typography.Title>
        {preview.error ? (
          <LoadFailure
            title="Не удалось загрузить перечень публикуемого"
            error={preview.error}
            onRetry={() => void preview.refetch()}
          />
        ) : !preview.data ? (
          <Skeleton active />
        ) : (
          <Flex vertical gap="small">
            {entries.length > 0 ? (
              <CascadeTable entries={entries} />
            ) : (
              <Typography.Paragraph>
                Других черновых записей публикация не затронет.
              </Typography.Paragraph>
            )}
            {(preview.data.blockers.length > 0 ||
              preview.data.moreCount > 0) && (
              <Alert
                type="warning"
                showIcon
                title="Часть записей утвердить нельзя"
                description={
                  <RefusalContent
                    target={target}
                    blockers={preview.data.blockers}
                    moreCount={preview.data.moreCount}
                  />
                }
              />
            )}
          </Flex>
        )}
      </section>
      {canApprove && (
        <Flex gap="small" align="center" wrap>
          <Button
            type="primary"
            loading={isBusy}
            disabled={!preview.data || isEarly || isBusy}
            onClick={() =>
              preview.data && void approveShown(target, preview.data)
            }
          >
            {label}
          </Button>
          <Button disabled={isBusy} onClick={() => setIsEditing(true)}>
            Поправить и утвердить
          </Button>
          {isEarly &&
            (draft.readiness?.hasLevels ? (
              <Link
                to={deskPaths.trackPositionsOf(draft.id)}
                state={returnState(back)}
              >
                Связать должности с треком
              </Link>
            ) : (
              <Link to="/career">Править трек</Link>
            ))}
          {isEarly && (
            <Typography.Text type="secondary">
              {draft.readiness?.hasLevels
                ? "Утвердить недоступно: у трека есть уровни без действующих должностей. Свяжите должности — и вернитесь в очередь."
                : "Утвердить недоступно: у трека нет уровней. Добавьте их — и вернитесь в очередь."}
            </Typography.Text>
          )}
        </Flex>
      )}
      {isEditing && (
        <EditDraftModal
          draft={draft}
          onSaved={(saved) =>
            void publish({ segment, entry: saved }, "approve")
          }
          onClose={() => setIsEditing(false)}
        />
      )}
    </Flex>
  );
}
