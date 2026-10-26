import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AutoComplete, Form } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CareerLevel,
  invalidateCatalogs,
  levelNamesQuery,
} from "./catalogApi";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name"] as const;

export interface LevelValues {
  name: string;
}

/** Creates a level of the track; the server puts it after the track's other levels. */
export async function createLevel(trackId: string, values: LevelValues) {
  return unwrap(
    await api.POST("/api/v1/career/levels", {
      body: { name: values.name, trackId },
    }),
  );
}

interface LevelModalProps {
  /** Level being edited as the cache holds it now; omitted when one is created. */
  level?: CareerLevel;
  /** Values of a level that waits for its track; set with `onCollect`. */
  draft?: LevelValues;
  /** Takes the values instead of saving the level, for a track that is not saved yet. */
  onCollect?: (values: LevelValues) => void;
  onClose: () => void;
}

/** Dialog that takes the name of a level the track will save, or renames a saved level. */
export function LevelModal({
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
      if (!level) return;
      unwrap(
        await api.PUT("/api/v1/career/levels/{id}", {
          params: {
            path: { id: level.id },
            header: { "If-Match": ifMatchOf(level.version) },
          },
          body: { name: values.name },
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
        initialName={draft?.name ?? level?.name ?? ""}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface LevelFormProps {
  initialName: string;
  onSubmit: (values: LevelValues) => Promise<unknown>;
  onCancel: () => void;
}

function LevelForm({ initialName, onSubmit, onCancel }: LevelFormProps) {
  const [form] = Form.useForm<LevelValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);
  const typed = useDebouncedValue(Form.useWatch("name", form)?.trim() ?? "");
  const suggestions = useQuery(levelNamesQuery(typed));

  return (
    <Form
      form={form}
      name="career-level"
      disabled={isSubmitting}
      layout="vertical"
      initialValues={{ name: initialName }}
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
        <AutoComplete
          options={suggestions.data?.map((name) => ({ value: name }))}
        />
      </Form.Item>
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
