import { useQueries, useQuery } from "@tanstack/react-query";
import {
  Button,
  Checkbox,
  Empty,
  Flex,
  Input,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
  theme,
} from "antd";
import type { FilterDropdownProps } from "antd/es/table/interface";
import { type Dispatch, type SetStateAction, useState } from "react";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { BlockModal } from "./BlockModal";
import { EntryActions, entryColumns } from "./CatalogParts";
import { FILTER_WIDTH_IN_CONTROLS } from "./CatalogSelect";
import {
  type BlockListParams,
  blocksQuery,
  type CompetencyBlock,
  competencyQuery,
  entryMatricesQuery,
  FIRST_PAGE,
} from "./catalogApi";
import { onFilterChange, pageProps } from "./columnFilters";
import { competenciesText } from "./matrixGroups";
import { VERSION_STATUS_LABELS } from "./matrixLabels";
import { useCatalogRights } from "./useCatalogRights";

const SEARCH_LABEL = "Найти по названию";
const FOR_ALL = "Для всех";
const SHOWN_MATRICES = 5;

/** Dialog of the tab: creating (`"create"`) or editing the block with that id; null when closed. */
type Dialog = "create" | string | null;

function Composition({ block }: { block: CompetencyBlock }) {
  const reads = useQueries({
    queries: block.competencies.map((item) => competencyQuery(item.id)),
  });
  return (
    <Table
      size="small"
      rowKey="id"
      pagination={false}
      dataSource={block.competencies.map((item, index) => ({
        ...item,
        categoryName: reads[index]?.data?.categoryName,
      }))}
      columns={[
        { title: "Компетенция", dataIndex: "name" },
        { title: "Категория", dataIndex: "categoryName" },
      ]}
    />
  );
}

function Usage({ block }: { block: CompetencyBlock }) {
  const read = useQuery(entryMatricesQuery("blocks", block.id));
  const matrices = read.data ?? [];
  if (read.error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить, где используется блок"
        error={read.error}
        onRetry={() => void read.refetch()}
      />
    );
  }
  return (
    <Table
      size="small"
      rowKey={(matrix) => matrix.versionId}
      pagination={false}
      loading={read.isFetching}
      dataSource={matrices.slice(0, SHOWN_MATRICES)}
      locale={{ emptyText: "Блок не используется ни в одной матрице" }}
      columns={[
        {
          title: "Матрица",
          key: "matrix",
          render: (_, matrix) => (
            <Link to={`/career/matrices/${matrix.matrixId}`}>
              {matrix.positionName
                ? `Матрица должности «${matrix.positionName}» — ${matrix.orgUnitName}`
                : `Матрица «${matrix.trackName}» — ${matrix.orgUnitName}`}
            </Link>
          ),
        },
        {
          title: "Версия",
          key: "version",
          render: (_, matrix) => (
            <Space size="small">
              {matrix.number}
              <Tag>{VERSION_STATUS_LABELS[matrix.status]}</Tag>
            </Space>
          ),
        },
      ]}
      footer={
        matrices.length > SHOWN_MATRICES
          ? () => `и ${matrices.length - SHOWN_MATRICES} других`
          : undefined
      }
    />
  );
}

interface ScopeFilterProps extends FilterDropdownProps {
  params: BlockListParams;
  setParams: Dispatch<SetStateAction<BlockListParams>>;
}

/** The filter of the «Действует для» column: the blocks of one unit, or the blocks for every unit. */
function ScopeFilter({ params, setParams, confirm }: ScopeFilterProps) {
  const { token } = theme.useToken();
  return (
    <Flex vertical gap="small" style={{ padding: token.paddingXS }}>
      <OrgUnitSelect
        allowClear
        placeholder="Подразделение"
        value={params.scopeUnitId}
        onChange={(unitId) => {
          setParams((previous) => ({
            ...previous,
            scopeUnitId: unitId,
            forAll: undefined,
            page: 1,
          }));
          confirm();
        }}
      />
      <Checkbox
        checked={params.forAll === true}
        style={{ width: token.controlHeight * FILTER_WIDTH_IN_CONTROLS }}
        onChange={(event) => {
          setParams((previous) => ({
            ...previous,
            forAll: event.target.checked || undefined,
            scopeUnitId: undefined,
            page: 1,
          }));
          confirm();
        }}
      >
        {FOR_ALL}
      </Checkbox>
    </Flex>
  );
}

/** The shared blocks of competencies: search, paging, creation, editing and the status actions; a row opens to its composition and where it is used. */
export function BlocksTab() {
  const rights = useCatalogRights();
  const [dialog, setDialog] = useState<Dialog>(null);
  const [params, setParams] = useState<BlockListParams>(FIRST_PAGE);
  const { data, error, isFetching, refetch } = useQuery({
    ...blocksQuery(params),
    placeholderData: (previous) => previous,
  });
  const edited = data?.items.find((block) => block.id === dialog);
  const columns: TableColumnsType<CompetencyBlock> = [
    { title: "Название", dataIndex: "name" },
    {
      title: "Действует для",
      key: "scopeUnitId",
      filtered: params.scopeUnitId !== undefined || params.forAll === true,
      filteredValue:
        params.scopeUnitId === undefined ? null : [params.scopeUnitId],
      filterMultiple: false,
      filterDropdown: (props) => (
        <ScopeFilter {...props} params={params} setParams={setParams} />
      ),
      render: (_, block) => block.scopeUnitName ?? FOR_ALL,
    },
    {
      title: "Состав",
      key: "composition",
      render: (_, block) => competenciesText(block.competencies.length),
    },
    ...entryColumns<CompetencyBlock>(params.status),
    {
      title: "Действия",
      key: "actions",
      render: (_, block) => (
        <EntryActions
          segment="blocks"
          entry={block}
          onEdit={() => setDialog(block.id)}
        />
      ),
    },
  ];

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Input.Search
          allowClear
          aria-label={SEARCH_LABEL}
          placeholder={SEARCH_LABEL}
          onSearch={(text) =>
            setParams((previous) => ({
              ...previous,
              q: text.trim() || undefined,
              page: 1,
            }))
          }
        />
        {rights.canCreate && (
          <Button type="primary" onClick={() => setDialog("create")}>
            Добавить блок
          </Button>
        )}
      </Flex>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить общие блоки"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<CompetencyBlock>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        pagination={pageProps(params, data?.total, setParams)}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Общих блоков пока нет"
            />
          ),
        }}
        expandable={{
          expandedRowRender: (block) => (
            <Flex vertical gap="middle">
              <section>
                <Typography.Title level={5}>Состав</Typography.Title>
                <Composition block={block} />
              </section>
              <section>
                <Typography.Title level={5}>Где используется</Typography.Title>
                <Usage block={block} />
              </section>
            </Flex>
          ),
        }}
      />
      {dialog === "create" && <BlockModal onClose={() => setDialog(null)} />}
      {edited && <BlockModal block={edited} onClose={() => setDialog(null)} />}
    </Space>
  );
}
