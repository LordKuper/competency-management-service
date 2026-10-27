import { useQuery } from "@tanstack/react-query";
import { Flex, Skeleton, Tag, Typography } from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { type DeskGroup, type DeskItem, orgQueueQuery } from "./deskApi";
import { reasonLines, typeLabelOf } from "./deskTexts";
import { useIsFindingBusy } from "./findingBusy";
import { OrgFinding } from "./OrgFinding";
import {
  QueueLayout,
  useQueueSelection,
  useRecordFocus,
  useShownCount,
} from "./QueueLayout";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";

const FIRST_COUNT = 20;
const MORE_COUNT = 20;
const SHOWN_REASONS = 2;

const KIND_OPTIONS: ReadonlyArray<{ value: DeskGroup; label: string }> = [
  { value: "OrgCleanup", label: "Расхождения" },
  { value: "UnitsToConnect", label: "Подразделение не подключено" },
  { value: "InactivePositions", label: "Неактивная должность занята" },
  { value: "UnusedPositions", label: "Неиспользуемая должность" },
  { value: "EmptyUnits", label: "Пустое подразделение" },
];

const keyOf = (item: DeskItem) => `${item.object.type}:${item.object.id}`;

/** The reasons of a finding in a row of the queue: the first two, the rest by number. */
function summary(item: DeskItem): string {
  const lines = reasonLines(item).map((line) => line.text);
  const more = lines.length - SHOWN_REASONS;
  return [
    ...lines.slice(0, SHOWN_REASONS),
    ...(more > 0 ? [`ещё ${more}`] : []),
  ].join(" · ");
}

/** «Навести порядок в оргструктуре»: the findings of the organisation structure, each put right on the spot. */
export function OrgCleanupPage() {
  const [group, setGroup] = useState<DeskGroup>();
  const { shown, showMore } = useShownCount(FIRST_COUNT, MORE_COUNT);
  const list = useQuery(orgQueueQuery(shown, group));
  const items = list.data?.items ?? [];
  const selection = useQueueSelection(
    items.map(keyOf),
    list.data !== undefined,
  );
  const current = items.find((item) => keyOf(item) === selection.selectedKey);
  const headingRef = useRecordFocus(selection.selectedKey);
  const isBusy = useIsFindingBusy();
  const { confirmLeave } = useLeaveGuard();

  return (
    <ScenarioPage title="Навести порядок в оргструктуре">
      {list.error ? (
        <LoadFailure
          title="Не удалось загрузить очередь"
          error={list.error}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <QueueLayout
          items={items}
          total={list.data?.total ?? 0}
          getKey={keyOf}
          selectedKey={selection.selectedKey}
          onSelect={(key) => confirmLeave(() => selection.select(key))}
          renderItem={(item) => (
            <Flex vertical gap="var(--ant-margin-xxs)">
              <Flex align="center" gap="small" wrap>
                <Tag>{typeLabelOf(item) ?? "Подразделение"}</Tag>
                <Typography.Text strong>{item.object.name}</Typography.Text>
                {item.isReference && <Tag>Справочно</Tag>}
              </Flex>
              <Typography.Text type="secondary">
                {summary(item)}
              </Typography.Text>
            </Flex>
          )}
          filter={{
            value: group,
            options: KIND_OPTIONS,
            onChange: (value) =>
              confirmLeave(() => setGroup(value as DeskGroup | undefined)),
          }}
          onShowMore={showMore}
          moreCount={MORE_COUNT}
          isLoading={list.isPending}
          lockedReason={
            isBusy
              ? "Идёт сохранение: другую находку можно выбрать после него."
              : undefined
          }
          emptyText="Расхождений в оргструктуре нет: должности, подразделения и учётные записи согласованы."
        >
          {list.isPending ? (
            <Skeleton active />
          ) : current ? (
            <OrgFinding
              key={keyOf(current)}
              item={current}
              headingRef={headingRef}
            />
          ) : (
            <Typography.Paragraph>
              Находок этого вида в очереди нет.
            </Typography.Paragraph>
          )}
        </QueueLayout>
      )}
    </ScenarioPage>
  );
}
