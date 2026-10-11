import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Checkbox, Form, Input, Select } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CareerLevel,
  invalidateCatalogs,
  levelsQuery,
} from "./catalogApi";

const NAME_MAX_LENGTH = 200;
const FIELDS = [
  "name",
  "code",
  "previousLevelId",
  "nextLevelIds",
  "validFrom",
  "validTo",
  "roleDescription",
] as const;

export interface LevelValues {
  name: string;
  code: string;
  previousLevelId?: string | null;
  nextLevelIds?: string[];
  isManagerial?: boolean;
  validFrom?: string | null;
  validTo?: string | null;
  roleDescription?: string | null;
}

function levelBody(values: LevelValues) {
  return {
    name: values.name,
    code: values.code,
    isManagerial: values.isManagerial ?? false,
    validFrom: values.validFrom || null,
    validTo: values.validTo || null,
    roleDescription: values.roleDescription?.trim() || null,
    previousLevelId: values.previousLevelId ?? null,
    nextLevelIds: values.nextLevelIds ?? [],
  };
}

/** Creates a level of the track. */
export async function createLevel(trackId: string, values: LevelValues) {
  return unwrap(
    await api.POST("/api/v1/career/levels", {
      body: { ...levelBody(values), trackId },
    }),
  );
}

interface LevelModalProps {
  /** Track of a level being created and saved at once; omitted when the level waits for its track to be saved. */
  trackId?: string;
  /** Level being edited as the cache holds it now; omitted when one is created. */
  level?: CareerLevel;
  /** Values of a level that waits for its track; set with `onCollect`. */
  draft?: LevelValues;
  /** Takes the values instead of saving the level, for a track that is not saved yet. */
  onCollect?: (values: LevelValues) => void;
  onClose: () => void;
}

/** Dialog that creates a level of a track or edits one. */
export function LevelModal({
  trackId,
  level: latest,
  draft,
  onCollect,
  onClose,
}: LevelModalProps) {
  const queryClient = useQueryClient();
  const { base: level, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: async (values: LevelValues) => {
      if (onCollect) {
        onCollect(values);
        return;
      }
      if (!level) {
        if (trackId) await createLevel(trackId, values);
        return;
      }
      unwrap(
        await api.PUT("/api/v1/career/levels/{id}", {
          params: {
            path: { id: level.id },
            header: { "If-Match": ifMatchOf(level.version) },
          },
          body: levelBody(values),
        }),
      );
    },
    onSuccess: onClose,
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <CatalogModal
      title={level || draft ? "Изменение уровня" : "Новый уровень"}
      error={save.error}
      fields={FIELDS}
      onClose={onClose}
    >
      <LevelForm
        key={level?.version}
        level={level}
        draft={draft}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface LevelFormProps {
  level?: CareerLevel;
  draft?: LevelValues;
  onSubmit: (values: LevelValues) => Promise<unknown>;
  onCancel: () => void;
}

function LevelForm({ level, draft, onSubmit, onCancel }: LevelFormProps) {
  const [form] = Form.useForm<LevelValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);
  const { data: levels, isFetching } = useQuery(levelsQuery());
  const chosen = new Set([
    level?.previousLevelId,
    ...(level?.nextLevelIds ?? []),
  ]);
  const options = (levels?.items ?? [])
    .filter(
      (other) =>
        other.id !== level?.id &&
        (other.status !== "Inactive" || chosen.has(other.id)),
    )
    .map((other) => ({
      value: other.id,
      label: `${other.name} (${other.code})`,
    }));

  return (
    <Form
      form={form}
      name="career-level"
      layout="vertical"
      initialValues={
        draft ?? {
          name: level?.name ?? "",
          code: level?.code ?? "",
          previousLevelId: level?.previousLevelId ?? undefined,
          nextLevelIds: level?.nextLevelIds ?? [],
          isManagerial: level?.isManagerial ?? false,
          validFrom: level?.validFrom ?? "",
          validTo: level?.validTo ?? "",
          roleDescription: level?.roleDescription ?? "",
        }
      }
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
      <Form.Item name="code" label="Код уровня">
        <Input autoComplete="off" />
      </Form.Item>
      <Form.Item name="previousLevelId" label="Предыдущий уровень">
        <Select
          allowClear
          showSearch={{ optionFilterProp: "label" }}
          loading={isFetching}
          options={options}
        />
      </Form.Item>
      <Form.Item name="nextLevelIds" label="Следующие уровни">
        <Select
          mode="multiple"
          showSearch={{ optionFilterProp: "label" }}
          loading={isFetching}
          options={options}
        />
      </Form.Item>
      <Form.Item name="isManagerial" valuePropName="checked">
        <Checkbox>Управленческий уровень</Checkbox>
      </Form.Item>
      <Form.Item name="validFrom" label="Действует с">
        <Input type="date" />
      </Form.Item>
      <Form.Item name="validTo" label="Действует по">
        <Input type="date" />
      </Form.Item>
      <Form.Item name="roleDescription" label="Описание роли">
        <Input.TextArea autoSize={{ minRows: 3 }} />
      </Form.Item>
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
