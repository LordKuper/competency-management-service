import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Form, Input, Select } from "antd";
import { useEffect } from "react";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type Competency,
  type CompetencyType,
  invalidateCatalogs,
  scalesQuery,
} from "./catalogApi";
import { DirectionSelect } from "./DirectionSelect";
import { ScaleSelect } from "./ScaleSelect";
import { usePublishEntry } from "./usePublishEntry";

const NAME_MAX_LENGTH = 200;
const FIELDS = [
  "name",
  "description",
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
  directionId?: string | null;
  type: CompetencyType;
  scaleId?: string;
}

interface CompetencyModalProps {
  /** Competency being edited as the cache holds it now; omitted when one is created. */
  competency?: Competency;
  /** Called with the competency a save produced, as the dialog closes. */
  onSaved?: (competency: Competency) => void;
  onClose: () => void;
}

/** Dialog that creates a competency or edits one; every saved change becomes a new version. */
export function CompetencyModal({
  competency: latest,
  onSaved,
  onClose,
}: CompetencyModalProps) {
  const queryClient = useQueryClient();
  const { offerApproval } = usePublishEntry();
  const { base: competency, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: async (values: CompetencyValues) => {
      const body = {
        name: values.name,
        description: values.description?.trim() || null,
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
    onSuccess: (saved) => {
      onClose();
      onSaved?.(saved);
      if (!competency) {
        void offerApproval(
          { segment: "competencies", entry: saved },
          saved.status,
        );
      }
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <CatalogModal
      title={competency ? "Изменение компетенции" : "Новая компетенция"}
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

/**
 * Fields of a competency. The default scale arrives after the form opens, so a new competency gets it then, unless the
 * user has chosen a scale first.
 */
function CompetencyForm({
  competency,
  onSubmit,
  onCancel,
}: CompetencyFormProps) {
  const [form] = Form.useForm<CompetencyValues>();
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    try {
      return await onSubmit(values);
    } catch (error) {
      const field =
        error instanceof ApiError &&
        error.errors &&
        Object.keys(error.errors)[0];
      // The messages are put under the fields right after this rethrow, so the focus waits for them.
      if (field)
        queueMicrotask(() => form.scrollToField(field, { focus: true }));
      throw error;
    }
  });
  const defaultScale = useQuery({
    ...scalesQuery({ page: 1, pageSize: 1, isDefault: true }),
    enabled: !competency,
  });
  const defaultScaleId = defaultScale.data?.items[0]?.id;

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
      <Form.Item name="directionId" label="Область">
        <DirectionSelect allowClear placeholder="Общая" />
      </Form.Item>
      <Form.Item name="type" label="Тип" rules={[{ required: true }]}>
        <Select options={TYPE_OPTIONS} />
      </Form.Item>
      <Form.Item name="scaleId" label="Шкала" rules={[{ required: true }]}>
        <ScaleSelect />
      </Form.Item>
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
