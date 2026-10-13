import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, App, Button, Flex, Skeleton, Tag, Typography } from "antd";
import { type RefObject, useState } from "react";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import { NotFoundPage } from "../../app/NotFoundPage";
import { useProcessRoles } from "../career/careerApi";
import {
  type CatalogKind,
  changeCatalogStatus,
  deleteEntry,
  invalidateCatalogs,
  SEGMENT_OF_KIND,
  type UnusedEntry,
  unusedEntriesQuery,
} from "../career/catalogApi";
import { KIND_LABELS } from "../career/DraftsSection";
import { useCatalogRights } from "../career/useCatalogRights";
import { useConfirmDeletion } from "../career/useConfirmDeletion";
import { KIND_ACCUSATIVE } from "../career/usePublishEntry";
import {
  QueueLayout,
  useQueueSelection,
  useRecordFocus,
  useShownCount,
} from "./QueueLayout";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";

const FIRST_COUNT = 20;
const MORE_COUNT = 20;
const STALE_STATUS = 412;

const KIND_OPTIONS = (
  ["Competency", "Scale", "Category", "Direction"] as const
).map((kind) => ({ value: kind, label: KIND_LABELS[kind] }));

const REASONS: Partial<Record<CatalogKind, string>> = {
  Competency: "Компетенция нигде не используется",
  Scale: "Шкала нигде не используется",
  Category: "Категория вне любых версий матриц и компетенций",
  Direction: "У направления нет треков",
};

const keyOf = (entry: UnusedEntry) => `${entry.kind}:${entry.id}`;

/** «Привести каталог в порядок»: entries nothing uses, each deleted or deactivated on the spot. */
export function CatalogCleanupPage() {
  const roles = useProcessRoles();
  const rights = useCatalogRights();
  const [filterKind, setFilterKind] = useState<CatalogKind>();
  const { shown, showMore } = useShownCount(FIRST_COUNT, MORE_COUNT);
  const list = useQuery({
    ...unusedEntriesQuery(shown, filterKind),
    enabled: rights.isHr,
  });
  const entries = list.data?.items ?? [];
  const selection = useQueueSelection(
    entries.map(keyOf),
    list.data !== undefined,
  );
  const current = entries.find(
    (entry) => keyOf(entry) === selection.selectedKey,
  );
  const headingRef = useRecordFocus(selection.selectedKey);

  if (roles.isSuccess && !rights.isHr) return <NotFoundPage />;

  return (
    <ScenarioPage title="Привести каталог в порядок">
      {list.error ? (
        <LoadFailure
          title="Не удалось загрузить очередь"
          error={list.error}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <QueueLayout
          items={entries}
          total={list.data?.total ?? 0}
          getKey={keyOf}
          selectedKey={selection.selectedKey}
          onSelect={selection.select}
          renderItem={(entry) => (
            <Flex vertical gap={2}>
              <Flex align="center" gap="small" wrap>
                <Tag>{KIND_LABELS[entry.kind]}</Tag>
                <Typography.Text strong>{entry.name}</Typography.Text>
              </Flex>
              <Typography.Text type="secondary">
                {REASONS[entry.kind]}
              </Typography.Text>
            </Flex>
          )}
          filter={{
            value: filterKind,
            options: KIND_OPTIONS,
            onChange: (value) => setFilterKind(value as CatalogKind),
          }}
          onShowMore={showMore}
          moreCount={MORE_COUNT}
          isLoading={list.isPending}
          emptyText="Неиспользуемых записей каталога нет: всё, что есть, где-то используется. Если появятся, они будут на рабочем столе в группе «К сведению»."
        >
          {list.isPending ? (
            <Skeleton active />
          ) : current ? (
            <UnusedRecord
              key={keyOf(current)}
              entry={current}
              headingRef={headingRef}
            />
          ) : (
            <Typography.Paragraph>
              Записей этого вида в очереди нет.
            </Typography.Paragraph>
          )}
        </QueueLayout>
      )}
    </ScenarioPage>
  );
}

interface UnusedRecordProps {
  entry: UnusedEntry;
  headingRef: RefObject<HTMLHeadingElement | null>;
}

function UnusedRecord({ entry, headingRef }: UnusedRecordProps) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const confirmDeletion = useConfirmDeletion();
  const segment = SEGMENT_OF_KIND[entry.kind];
  const deactivate = useMutation({
    mutationFn: () => changeCatalogStatus(segment, entry, "deactivate"),
    onSuccess: () => message.success(`Запись «${entry.name}» деактивирована`),
    onError: (failure) =>
      message.warning(
        failure instanceof ApiError && failure.status === STALE_STATUS
          ? "Запись уже используется или изменена другим пользователем"
          : describeApiError(failure),
      ),
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <Flex vertical gap="middle">
      <Typography.Title
        level={2}
        tabIndex={-1}
        ref={headingRef}
        style={{ margin: 0 }}
      >
        {`${KIND_LABELS[entry.kind]} ${entry.name}`}
      </Typography.Title>
      <Alert
        type="info"
        showIcon
        title={REASONS[entry.kind]}
        description="Можно удалить запись или деактивировать её, чтобы она не предлагалась при построении матриц."
      />
      <Flex gap="small" wrap>
        <Button
          loading={deactivate.isPending}
          onClick={() => deactivate.mutate()}
        >
          Деактивировать
        </Button>
        <Button
          danger
          disabled={deactivate.isPending}
          onClick={() =>
            confirmDeletion({
              kind: KIND_ACCUSATIVE[segment],
              name: entry.name,
              content:
                "Запись и её история будут удалены без возможности восстановления.",
              run: () => deleteEntry(segment, entry),
              onDeleted: () =>
                message.success(`Запись «${entry.name}» удалена`),
            })
          }
        >
          Удалить
        </Button>
      </Flex>
    </Flex>
  );
}
