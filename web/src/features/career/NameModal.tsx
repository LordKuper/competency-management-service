import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Form, Input } from "antd";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CatalogEntry,
  invalidateCatalogs,
  type NamedEntryValues,
  saveNamedEntry,
} from "./catalogApi";
import { usePublishEntry } from "./usePublishEntry";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name"] as const;

interface NameModalProps {
  segment: "directions";
  title: string;
  /** Entry being renamed as the cache holds it now; omitted when one is created. */
  entry?: CatalogEntry;
  /** Called with the entry a save produced, as the dialog closes. */
  onSaved?: (saved: CatalogEntry) => void;
  onClose: () => void;
}

/** Dialog that creates a direction or renames one. */
export function NameModal({
  segment,
  title,
  entry: latest,
  onSaved,
  onClose,
}: NameModalProps) {
  const queryClient = useQueryClient();
  const { offerApproval } = usePublishEntry();
  const { base: entry, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (values: NamedEntryValues) =>
      saveNamedEntry(segment, values, entry),
    onSuccess: (saved) => {
      onClose();
      onSaved?.(saved);
      if (!entry) void offerApproval({ segment, entry: saved }, saved.status);
    },
    onError: noteSaveFailure,
    onSettled: () => invalidateCatalogs(queryClient),
  });

  return (
    <CatalogModal
      title={title}
      error={save.error}
      fields={FIELDS}
      onClose={onClose}
    >
      <NameForm
        key={entry?.version}
        name={entry?.name ?? ""}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface NameFormProps {
  name: string;
  onSubmit: (values: NamedEntryValues) => Promise<unknown>;
  onCancel: () => void;
}

function NameForm({ name, onSubmit, onCancel }: NameFormProps) {
  const [form] = Form.useForm<NamedEntryValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);

  return (
    <Form
      form={form}
      name="catalog-name"
      disabled={isSubmitting}
      layout="vertical"
      initialValues={{ name }}
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
      <FormActions isSubmitting={isSubmitting} onCancel={onCancel} />
    </Form>
  );
}
