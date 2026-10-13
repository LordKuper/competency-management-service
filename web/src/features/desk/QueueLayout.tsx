import { Button, Empty, Flex, Select, Typography } from "antd";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { deskPaths } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import "./scenario.css";

interface Selection {
  key: string | undefined;
  index: number;
}

/**
 * Which record of a queue is open. A record that leaves the queue, being decided, gives way to the one that took its
 * place, so the next one opens by itself; before the queue is read the record asked for stays remembered.
 */
export function useQueueSelection(
  keys: readonly string[],
  isReady: boolean,
  requestedKey?: string,
) {
  const [selection, setSelection] = useState<Selection>({
    key: requestedKey,
    index: 0,
  });
  const found = selection.key === undefined ? -1 : keys.indexOf(selection.key);
  const index =
    found >= 0
      ? found
      : Math.max(0, Math.min(selection.index, keys.length - 1));
  const key = isReady ? keys[index] : selection.key;

  useEffect(() => {
    if (isReady && (key !== selection.key || index !== selection.index)) {
      setSelection({ key, index });
    }
  }, [isReady, key, index, selection]);

  return {
    selectedKey: key,
    wasRequestedFound:
      requestedKey === undefined || keys.includes(requestedKey),
    select: (next: string) =>
      setSelection({ key: next, index: Math.max(keys.indexOf(next), 0) }),
  };
}

/** A ref for the heading of the open record, which takes the focus whenever another record opens. */
export function useRecordFocus(recordKey: string | undefined) {
  const ref = useRef<HTMLHeadingElement>(null);
  const isFirst = useRef(true);
  useEffect(() => {
    if (isFirst.current) {
      isFirst.current = false;
      return;
    }
    if (recordKey !== undefined) ref.current?.focus();
  }, [recordKey]);
  return ref;
}

interface QueueLayoutProps<T> {
  items: readonly T[];
  /** How many records the queue holds in all. */
  total: number;
  getKey: (item: T) => string;
  selectedKey: string | undefined;
  onSelect: (key: string) => void;
  renderItem: (item: T) => ReactNode;
  filter: {
    value: string | undefined;
    options: ReadonlyArray<{ value: string; label: string }>;
    onChange: (value: string | undefined) => void;
  };
  /** Reads more of the queue; given only while some records are not shown. */
  onShowMore?: () => void;
  /** How many records each «Показать ещё» adds. */
  moreCount: number;
  isLoading: boolean;
  /** Text of an empty queue: what it means and what comes next. */
  emptyText: string;
  /** The open record. */
  children: ReactNode;
}

/** Two columns: the queue on the left, the open record on the right (one column on a tablet). */
export function QueueLayout<T>({
  items,
  total,
  getKey,
  selectedKey,
  onSelect,
  renderItem,
  filter,
  onShowMore,
  moreCount,
  isLoading,
  emptyText,
  children,
}: QueueLayoutProps<T>) {
  if (!isLoading && total === 0 && filter.value === undefined) {
    return (
      <Empty
        description={
          <>
            <Typography.Title level={3}>Очередь пуста</Typography.Title>
            <Typography.Paragraph>{emptyText}</Typography.Paragraph>
          </>
        }
      >
        <LinkButton to={deskPaths.desk} type="primary">
          К рабочему столу
        </LinkButton>
      </Empty>
    );
  }
  return (
    <div className="queue-columns">
      <Flex vertical gap="small">
        <Flex justify="space-between" align="center" gap="small" wrap>
          <Typography.Text strong>{`В очереди: ${total}`}</Typography.Text>
          <Select
            allowClear
            aria-label="Вид"
            placeholder="Вид"
            style={{ minWidth: 160 }}
            value={filter.value}
            options={[...filter.options]}
            onChange={(value) => filter.onChange(value ?? undefined)}
          />
        </Flex>
        <ul style={{ listStyle: "none", margin: 0, padding: 0 }}>
          {items.map((item) => {
            const key = getKey(item);
            const isSelected = key === selectedKey;
            return (
              <li key={key}>
                <button
                  type="button"
                  className={`queue-row${isSelected ? " queue-row-selected" : ""}`}
                  aria-current={isSelected ? "true" : undefined}
                  onClick={() => onSelect(key)}
                >
                  {renderItem(item)}
                </button>
              </li>
            );
          })}
        </ul>
        {items.length < total && (
          <Flex justify="space-between" align="center">
            <Typography.Text type="secondary">
              {`Показано ${items.length} из ${total}`}
            </Typography.Text>
            {onShowMore && (
              <Button
                onClick={onShowMore}
              >{`Показать ещё ${moreCount}`}</Button>
            )}
          </Flex>
        )}
      </Flex>
      <div>{children}</div>
    </div>
  );
}

/** Keeps the number of records a queue shows: the first page, and each «Показать ещё» adds one more. */
export function useShownCount(firstCount: number, moreCount: number) {
  const [shown, setShown] = useState(firstCount);
  return { shown, showMore: () => setShown((count) => count + moreCount) };
}
