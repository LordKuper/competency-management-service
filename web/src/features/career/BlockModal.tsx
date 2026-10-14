import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Flex,
  Form,
  type FormInstance,
  Input,
  Table,
  type TableColumnsType,
  Typography,
  theme,
} from "antd";
import { useEffect, useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { CatalogStatusTag } from "./CatalogParts";
import {
  type Competency,
  type CompetencyBlock,
  competenciesQuery,
  entryMatricesQuery,
  invalidateCatalogs,
  MAX_PAGE_SIZE,
  saveBlock,
} from "./catalogApi";
import { ScaleSelect } from "./ScaleSelect";
import { useCatalogRights } from "./useCatalogRights";
import { usePublishEntry } from "./usePublishEntry";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name", "scaleId", "competencyIds"] as const;
const LIST_HEIGHT_IN_CONTROLS = 8;
const SCALE_FIRST = "Сначала выберите шкалу";
const SCALE_USED =
  "Шкалу блока используют матрицы — изменить нельзя. Для другой шкалы создайте новый блок.";
const IN_USE_NOTICE =
  "Блок используют матрицы: черновые версии сразу увидят новый состав, опубликованные и архивные не изменятся.";
const MANAGER_NOTICE = "Блок будет создан черновиком: его утвердит HR.";
const DRAFT_MEMBER_NOTICE =
  "В составе есть черновая компетенция: блок будет создан черновиком.";

interface BlockFormValues {
  name: string;
  scaleId?: string;
  competencyIds?: string[];
}

interface BlockModalProps {
  /** Block being edited as the cache holds it now; omitted when one is created. */
  block?: CompetencyBlock;
  /** Called with the block a save produced, as the dialog closes. */
  onSaved?: (saved: CompetencyBlock) => void;
  onClose: () => void;
}

/** Dialog that creates a shared block with its scale and composition or changes one. */
export function BlockModal({
  block: latest,
  onSaved,
  onClose,
}: BlockModalProps) {
  const queryClient = useQueryClient();
  const { offerApproval } = usePublishEntry();
  const { base: block, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (values: BlockFormValues) =>
      saveBlock(
        {
          name: values.name,
          scaleId: values.scaleId ?? "",
          competencyIds: values.competencyIds ?? [],
        },
        block,
      ),
    onSuccess: (saved) => {
      onClose();
      onSaved?.(saved);
      if (!block) {
        void offerApproval({ segment: "blocks", entry: saved }, saved.status);
      }
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <CatalogModal
      title={block ? "Изменение общего блока" : "Новый общий блок"}
      error={save.error}
      fields={FIELDS}
      onClose={onClose}
    >
      <BlockForm
        key={block?.version}
        block={block}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface BlockFormProps {
  block?: CompetencyBlock;
  onSubmit: (values: BlockFormValues) => Promise<unknown>;
  onCancel: () => void;
}

function BlockForm({ block, onSubmit, onCancel }: BlockFormProps) {
  const [form] = Form.useForm<BlockFormValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);
  const rights = useCatalogRights();
  const scaleId = Form.useWatch("scaleId", form);
  const [draftIds, setDraftIds] = useState<ReadonlySet<string>>(new Set());
  const noteDrafts = (ids: readonly string[]) =>
    setDraftIds((previous) =>
      ids.every((id) => previous.has(id))
        ? previous
        : new Set([...previous, ...ids]),
    );
  const using = useQuery({
    ...entryMatricesQuery("blocks", block?.id ?? ""),
    enabled: block !== undefined,
  });
  const isUsed = (using.data?.length ?? 0) > 0;

  return (
    <Form
      form={form}
      name="competency-block"
      layout="vertical"
      initialValues={{
        name: block?.name ?? "",
        scaleId: block?.scaleId,
        competencyIds: block?.competencies.map((item) => item.id) ?? [],
      }}
      onFinish={submit}
    >
      {!block && !rights.isHr && (
        <Alert
          type="info"
          showIcon
          title={MANAGER_NOTICE}
          style={{ marginBottom: "var(--ant-margin)" }}
        />
      )}
      {isUsed && (
        <Alert
          type="warning"
          showIcon
          title={IN_USE_NOTICE}
          style={{ marginBottom: "var(--ant-margin)" }}
        />
      )}
      <Form.Item
        name="name"
        label="Название"
        rules={[
          { required: true, whitespace: true, message: "Укажите название" },
          {
            max: NAME_MAX_LENGTH,
            message: `Не длиннее ${NAME_MAX_LENGTH} символов`,
          },
        ]}
      >
        <Input autoComplete="off" />
      </Form.Item>
      <Form.Item
        name="scaleId"
        label="Шкала"
        rules={[{ required: true, message: "Выберите шкалу" }]}
        extra={
          isUsed
            ? SCALE_USED
            : "Шкала обязательна и определяет, какие компетенции можно включить."
        }
      >
        <ScaleSelect
          disabled={isUsed}
          onChange={() => form.setFieldValue("competencyIds", [])}
        />
      </Form.Item>
      <Form.Item name="competencyIds" label="Состав">
        <CompetencyPicker scaleId={scaleId} onDraftsSeen={noteDrafts} />
      </Form.Item>
      <DraftMemberNotice form={form} draftIds={draftIds} />
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}

function DraftMemberNotice({
  form,
  draftIds,
}: {
  form: FormInstance<BlockFormValues>;
  draftIds: ReadonlySet<string>;
}) {
  const chosen = Form.useWatch("competencyIds", form) ?? [];
  if (!chosen.some((id) => draftIds.has(id))) return null;
  return (
    <Alert
      type="info"
      showIcon
      title={DRAFT_MEMBER_NOTICE}
      style={{ marginBottom: "var(--ant-margin)" }}
    />
  );
}

interface CompetencyPickerProps {
  /** Supplied by the form item. */
  value?: string[];
  onChange?: (value: string[]) => void;
  scaleId?: string;
  /** Told which of the competencies listed are drafts. */
  onDraftsSeen: (ids: readonly string[]) => void;
}

/**
 * The composition of a block: the active and draft competencies of the block's scale, searched on the server, with a
 * checkbox each. What was ticked stays ticked while the search changes.
 */
function CompetencyPicker({
  value = [],
  onChange,
  scaleId,
  onDraftsSeen,
}: CompetencyPickerProps) {
  const { token } = theme.useToken();
  const [search, setSearch] = useState("");
  const q = useDebouncedValue(search.trim());
  const list = useQuery({
    ...competenciesQuery({
      q: q || undefined,
      page: 1,
      pageSize: MAX_PAGE_SIZE,
      scaleId,
      inWork: true,
    }),
    enabled: scaleId !== undefined,
  });
  const items = list.data?.items ?? [];
  useEffect(() => {
    onDraftsSeen(
      (list.data?.items ?? [])
        .filter((item) => item.status === "Draft")
        .map((item) => item.id),
    );
  }, [list.data, onDraftsSeen]);

  function toggle(id: string, isChosen: boolean) {
    onChange?.(
      isChosen ? [...value, id] : value.filter((chosen) => chosen !== id),
    );
  }

  const columns: TableColumnsType<Competency> = [
    { title: "Компетенция", dataIndex: "name" },
    { title: "Категория", dataIndex: "categoryName" },
    {
      title: "Статус",
      dataIndex: "status",
      render: (status: Competency["status"]) => (
        <CatalogStatusTag status={status} />
      ),
    },
  ];
  if (scaleId === undefined) {
    return <Typography.Text type="secondary">{SCALE_FIRST}</Typography.Text>;
  }
  return (
    <Flex vertical gap="small">
      <Flex align="center" gap="middle" wrap>
        <Input.Search
          aria-label="Найти компетенцию"
          placeholder="Найти компетенцию"
          allowClear
          style={{ flex: 1 }}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
        <Typography.Text strong>{`Выбрано: ${value.length}`}</Typography.Text>
      </Flex>
      <Typography.Text type="secondary">
        Показаны действующие и черновые компетенции выбранной шкалы.
      </Typography.Text>
      {list.error ? (
        <ErrorAlert
          title="Не удалось загрузить компетенции"
          error={list.error}
          onRetry={() => void list.refetch()}
        />
      ) : (
        <Table<Competency>
          size="small"
          rowKey="id"
          columns={columns}
          dataSource={items}
          loading={list.isFetching}
          pagination={false}
          scroll={{ y: token.controlHeight * LIST_HEIGHT_IN_CONTROLS }}
          rowSelection={{
            selectedRowKeys: value,
            onSelect: (row, isChosen) => toggle(row.id, isChosen),
            onSelectAll: (isChosen, _rows, changed) => {
              const ids = changed.map((row) => row.id);
              onChange?.(
                isChosen
                  ? [...new Set([...value, ...ids])]
                  : value.filter((id) => !ids.includes(id)),
              );
            },
          }}
          locale={{ emptyText: "Компетенции этой шкалы не найдены" }}
        />
      )}
    </Flex>
  );
}
