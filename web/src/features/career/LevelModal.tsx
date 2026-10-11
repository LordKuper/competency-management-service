import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Checkbox, Form, Input, InputNumber, Select, Skeleton } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CareerLevel,
  invalidateCatalogs,
  type LevelSalary,
  levelSalaryQuery,
  levelsQuery,
} from "./catalogApi";
import { useCatalogRights } from "./useCatalogRights";

const NAME_MAX_LENGTH = 200;
const FIELDS = [
  "name",
  "code",
  "previousLevelId",
  "nextLevelIds",
  "validFrom",
  "validTo",
  "roleDescription",
  "salaryFrom",
  "salaryTo",
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
  salaryFrom?: number | null;
  salaryTo?: number | null;
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

/** Saves the salary range under the level's current version when it differs from `current`. */
async function saveSalary(
  saved: { id: string; version: number },
  values: LevelValues,
  current?: LevelSalary,
) {
  const salaryFrom = values.salaryFrom ?? null;
  const salaryTo = values.salaryTo ?? null;
  if (
    salaryFrom === (current?.salaryFrom ?? null) &&
    salaryTo === (current?.salaryTo ?? null)
  ) {
    return;
  }
  unwrap(
    await api.PUT("/api/v1/career/levels/{id}/salary", {
      params: {
        path: { id: saved.id },
        header: { "If-Match": ifMatchOf(saved.version) },
      },
      body: { salaryFrom, salaryTo },
    }),
  );
}

/** Creates a level of the track, then its salary range when one is given. */
export async function createLevel(trackId: string, values: LevelValues) {
  const saved = unwrap(
    await api.POST("/api/v1/career/levels", {
      body: { ...levelBody(values), trackId },
    }),
  );
  await saveSalary(saved, values);
  return saved;
}

interface LevelModalProps {
  /** Unit of the level's track, which decides who may read the salary range. */
  ownerOrgUnitId: string;
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

/**
 * Dialog that creates a level of a track or edits one. The salary range has endpoints of its own and is shown and saved
 * only to those who may read it: the form saves the level first, then the range under the level's new version.
 */
export function LevelModal({
  ownerOrgUnitId,
  trackId,
  level: latest,
  draft,
  onCollect,
  onClose,
}: LevelModalProps) {
  const queryClient = useQueryClient();
  const rights = useCatalogRights();
  const { base: level, noteSaveFailure } = useEditBase(latest);
  const canReadSalary = rights.keeps(ownerOrgUnitId);
  const mayReadSalary = !!level && canReadSalary;
  const salaryRead = useQuery({
    ...levelSalaryQuery(level?.id ?? ""),
    enabled: mayReadSalary,
  });
  const salary = salaryRead.data;
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
      const saved = unwrap(
        await api.PUT("/api/v1/career/levels/{id}", {
          params: {
            path: { id: level.id },
            header: { "If-Match": ifMatchOf(level.version) },
          },
          body: levelBody(values),
        }),
      );
      await saveSalary(saved, values, salary);
    },
    onSuccess: onClose,
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });
  const isLoadingSalary = mayReadSalary && salaryRead.isPending;

  return (
    <CatalogModal
      title={level || draft ? "Править" : "Добавить уровень"}
      error={save.error}
      fields={FIELDS}
      onClose={onClose}
    >
      {isLoadingSalary ? (
        <Skeleton active />
      ) : (
        <LevelForm
          key={level?.version}
          level={level}
          draft={draft}
          salary={salary}
          canReadSalary={canReadSalary}
          onSubmit={(values) => save.mutateAsync(values)}
          onCancel={onClose}
        />
      )}
    </CatalogModal>
  );
}

interface LevelFormProps {
  level?: CareerLevel;
  draft?: LevelValues;
  /** The range as the server returned it; absent when the user may not read it. */
  salary?: LevelSalary;
  /** Whether the user may read and set salary ranges of the track's levels. */
  canReadSalary: boolean;
  onSubmit: (values: LevelValues) => Promise<unknown>;
  onCancel: () => void;
}

function LevelForm({
  level,
  draft,
  salary,
  canReadSalary,
  onSubmit,
  onCancel,
}: LevelFormProps) {
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
  const showSalary = level ? salary !== undefined : canReadSalary;

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
          salaryFrom: salary?.salaryFrom ?? undefined,
          salaryTo: salary?.salaryTo ?? undefined,
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
      {showSalary && (
        <>
          <Form.Item name="salaryFrom" label="Оклад от">
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
          <Form.Item name="salaryTo" label="Оклад до">
            <InputNumber min={0} style={{ width: "100%" }} />
          </Form.Item>
        </>
      )}
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
