import { useQueries, useQuery } from "@tanstack/react-query";
import {
  Button,
  Empty,
  Flex,
  Input,
  List,
  Space,
  Table,
  Tag,
  Typography,
  theme,
} from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { CatalogModal } from "../career/CatalogModal";
import { CatalogStatusTag } from "../career/CatalogParts";
import { COMPETENCY_TYPES } from "../career/CompetencyModal";
import {
  blocksQuery,
  type CompetencyBlock,
  competencyQuery,
  MAX_PAGE_SIZE,
} from "../career/catalogApi";
import { useCompetencies } from "../career/MatrixEditor";
import type { MatrixVersion } from "../career/matrixApi";
import { competenciesText, scaleNameOf } from "../career/matrixGroups";

interface BlockPreviewProps {
  block: CompetencyBlock;
  entries: MatrixVersion["entries"];
}

/** The block with the composition as it stands today. */
function BlockPreview({ block, entries }: BlockPreviewProps) {
  const competencies = useCompetencies(
    entries,
    block.competencies.map((item) => item.id),
  );
  return (
    <Space orientation="vertical" size="small" style={{ display: "flex" }}>
      <Space size="small" wrap>
        <Typography.Text strong>{block.name}</Typography.Text>
        {block.status === "Draft" && <CatalogStatusTag status="Draft" />}
      </Space>
      <Typography.Text type="secondary">
        {`Действует для: ${block.scopeUnitName ?? "Для всех"} · состав на сегодня; в мастере он не правится.`}
      </Typography.Text>
      <Table
        size="small"
        rowKey="id"
        pagination={false}
        dataSource={[...block.competencies]}
        columns={[
          { title: "Компетенция", dataIndex: "name" },
          {
            title: "Категория",
            key: "category",
            render: (_, item) => competencies.get(item.id)?.categoryName,
          },
          {
            title: "Шкала",
            key: "scale",
            render: (_, item) =>
              scaleNameOf({ entries }, item.id, competencies.get(item.id)),
          },
          {
            title: "Тип",
            key: "type",
            render: (_, item) => {
              const type = competencies.get(item.id)?.type;
              return type && COMPETENCY_TYPES[type];
            },
          },
        ]}
      />
    </Space>
  );
}

interface AddBlockModalProps {
  /** The version's entries, which name the competencies of a block shown. */
  entries: MatrixVersion["entries"];
  /** Why a block cannot be added for what the version holds already, in words; undefined for one that can. */
  reasonOf: (block: CompetencyBlock) => string | undefined;
  /** The unit of the matrix: only blocks that apply to it are offered. */
  availableFor?: string;
  /** In a track matrix, the competencies of its direction or of none; a block with another one is not offered. */
  allowedIds?: ReadonlySet<string>;
  onPick: (block: CompetencyBlock) => void;
  onClose: () => void;
}

/**
 * Window that picks one shared block to include in the version by reference: the active and the draft blocks the user may
 * see, searched on the server, with the chosen block's scale and composition beside the list.
 */
export function AddBlockModal({
  entries,
  reasonOf,
  allowedIds,
  availableFor,
  onPick,
  onClose,
}: AddBlockModalProps) {
  const { token } = theme.useToken();
  const [search, setSearch] = useState("");
  const [chosenId, setChosenId] = useState<string>();
  const q = useDebouncedValue(search.trim());
  const list = useQuery(
    blocksQuery({
      q: q || undefined,
      inWork: true,
      availableFor,
      page: 1,
      pageSize: MAX_PAGE_SIZE,
    }),
  );
  const items = list.data?.items ?? [];
  const chosen = items.find((block) => block.id === chosenId);
  const foreignOf = (block: CompetencyBlock) =>
    allowedIds && block.competencies.find((item) => !allowedIds.has(item.id));
  const foreignIds = [
    ...new Set(
      items.flatMap((block) => {
        const foreign = foreignOf(block);
        return foreign ? [foreign.id] : [];
      }),
    ),
  ];
  const foreignReads = useQueries({ queries: foreignIds.map(competencyQuery) });
  const directionOf = new Map(
    foreignReads.flatMap((read) =>
      read.data ? [[read.data.id, read.data.directionName] as const] : [],
    ),
  );
  const unavailableBecause = (block: CompetencyBlock) => {
    const foreign = foreignOf(block);
    if (!foreign) return reasonOf(block);
    const direction = directionOf.get(foreign.id);
    return (
      reasonOf(block) ??
      `в составе компетенции другой области${direction ? ` — «${direction}»` : ""}`
    );
  };

  return (
    <CatalogModal
      title="Добавить общий блок"
      error={null}
      fields={[]}
      width={token.sizeXXL * 14}
      onClose={onClose}
    >
      <Flex vertical gap="middle">
        <Input.Search
          aria-label="Найти блок"
          placeholder="Найти блок"
          allowClear
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        {list.error ? (
          <ErrorAlert
            title="Не удалось загрузить общие блоки"
            error={list.error}
            onRetry={() => void list.refetch()}
          />
        ) : (
          <List
            size="small"
            bordered
            loading={list.isFetching}
            dataSource={items}
            locale={{
              emptyText: (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description={
                    q
                      ? `Блок «${q}» не найден`
                      : "Общих блоков пока нет: их ведёт справочник «Общие блоки»"
                  }
                />
              ),
            }}
            renderItem={(block) => {
              const reason = unavailableBecause(block);
              return (
                <List.Item>
                  <Button
                    type="text"
                    block
                    disabled={reason !== undefined}
                    aria-pressed={block.id === chosenId}
                    style={{ height: "auto", textAlign: "start" }}
                    onClick={() => setChosenId(block.id)}
                  >
                    <Flex vertical>
                      <Space size="small" wrap>
                        <Typography.Text strong={block.id === chosenId}>
                          {block.name}
                        </Typography.Text>
                        {block.status === "Draft" && (
                          <CatalogStatusTag status="Draft" />
                        )}
                      </Space>
                      <Typography.Text type="secondary">
                        {reason
                          ? `Недоступен: ${reason}`
                          : `${competenciesText(block.competencies.length)}${block.status === "Draft" ? " · опубликуется вместе с версией" : ""}`}
                      </Typography.Text>
                    </Flex>
                  </Button>
                </List.Item>
              );
            }}
          />
        )}
        {chosen ? (
          <BlockPreview block={chosen} entries={entries} />
        ) : (
          <Tag>Выберите блок в списке, чтобы увидеть его состав</Tag>
        )}
        <Space>
          <Button
            type="primary"
            disabled={!chosen}
            onClick={() => chosen && onPick(chosen)}
          >
            Добавить в версию
          </Button>
          <Button onClick={onClose}>Отмена</Button>
        </Space>
      </Flex>
    </CatalogModal>
  );
}
