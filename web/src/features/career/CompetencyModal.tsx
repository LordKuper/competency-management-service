import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Form, Input, Select } from "antd";
import { useEffect } from "react";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { CategorySelect } from "./CategorySelect";
import {
  type Competency,
  type CompetencyType,
  directionsQuery,
  invalidateCatalogs,
  scalesQuery,
  usableEntries,
} from "./catalogApi";

const NAME_MAX_LENGTH = 200;
const FIELDS = [
  "name",
  "description",
  "categoryId",
  "directionId",
  "type",
  "scaleId",
] as const;

export const COMPETENCY_TYPES: Record<CompetencyType, string> = {
  Critical: "Критическая",
  Profile: "Профильная",
  Additional: "Дополнительная",
};

const TYPE_OPTIONS = Object.entries(COMPETENCY_TYPES).map(([value, label]) => ({
  value: value as CompetencyType,
  label,
}));

interface CompetencyValues {
  name: string;
  description?: string | null;
  categoryId: string;
  directionId?: string | null;
  type: CompetencyType;
  scaleId?: string;
}

interface CompetencyModalProps {
  /** Competency being edited as the cache holds it now; omitted when one is created. */
  competency?: Competency;
  onClose: () => void;
}

/** Dialog that creates a competency or edits one; every saved change becomes a new version. */
export function CompetencyModal({
  competency: latest,
  onClose,
}: CompetencyModalProps) {
  const queryClient = useQueryClient();
  const { base: competency, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: async (values: CompetencyValues) => {
      const body = {
        name: values.name,
        description: values.description?.trim() || null,
        categoryId: values.categoryId,
        directionId: values.directionId ?? null,
        type: values.type,
        scaleId: values.scaleId ?? null,
      };
      return unwrap(
        competency
          ? await api.PUT("/api/v1/career/competencies/{id}", {
              params: {
                path: { id: competency.id },
                header: { "If-Match": ifMatchOf(competency.version) },
              },
              body,
            })
          : await api.POST("/api/v1/career/competencies", { body }),
      );
    },
    onSuccess: onClose,
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <CatalogModal
      title={competency ? "Править" : "Добавить компетенцию"}
      error={save.error}
      fields={FIELDS}
      onClose={onClose}
    >
      <CompetencyForm
        key={competency?.version}
        competency={competency}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface CompetencyFormProps {
  competency?: Competency;
  onSubmit: (values: CompetencyValues) => Promise<unknown>;
  onCancel: () => void;
}

function CompetencyForm({
  competency,
  onSubmit,
  onCancel,
}: CompetencyFormProps) {
  const [form] = Form.useForm<CompetencyValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);
  const directions = useQuery(directionsQuery());
  const scales = useQuery(scalesQuery());
  const defaultScaleId = scales.data?.items.find(
    (scale) => scale.isDefault,
  )?.id;

  // The scales arrive after the form opens; a new competency starts with the default one unless the user chose first.
  useEffect(() => {
    if (!competency && defaultScaleId && !form.getFieldValue("scaleId")) {
      form.setFieldValue("scaleId", defaultScaleId);
    }
  }, [competency, defaultScaleId, form]);

  return (
    <Form
      form={form}
      name="competency"
      layout="vertical"
      initialValues={{
        name: competency?.name ?? "",
        description: competency?.description ?? "",
        categoryId: competency?.categoryId,
        directionId: competency?.directionId ?? undefined,
        type: competency?.type,
        scaleId: competency?.scaleId,
      }}
      onFinish={submit}
    >
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
      <Form.Item name="description" label="Описание">
        <Input.TextArea autoSize={{ minRows: 3 }} />
      </Form.Item>
      <Form.Item
        name="categoryId"
        label="Категория"
        rules={[{ required: true }]}
      >
        <CategorySelect />
      </Form.Item>
      <Form.Item name="directionId" label="Направление">
        <Select
          allowClear
          placeholder="Общая"
          showSearch={{ optionFilterProp: "label" }}
          loading={directions.isFetching}
          options={usableEntries(
            directions.data?.items,
            competency?.directionId,
          ).map((direction) => ({
            value: direction.id,
            label: direction.name,
          }))}
        />
      </Form.Item>
      <Form.Item name="type" label="Тип" rules={[{ required: true }]}>
        <Select options={TYPE_OPTIONS} />
      </Form.Item>
      <Form.Item name="scaleId" label="Шкала" rules={[{ required: true }]}>
        <Select
          showSearch={{ optionFilterProp: "label" }}
          loading={scales.isFetching}
          options={usableEntries(scales.data?.items, competency?.scaleId).map(
            (scale) => ({ value: scale.id, label: scale.name }),
          )}
        />
      </Form.Item>
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
