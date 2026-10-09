import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Form, Input } from "antd";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { CatalogModal, FormActions } from "./CatalogModal";
import { OwnerFormItem } from "./CatalogParts";
import {
  type CatalogEntry,
  invalidateCatalogs,
  type NamedEntryValues,
  saveNamedEntry,
} from "./catalogApi";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name", "ownerOrgUnitId"] as const;

interface NameModalProps {
  segment: "directions" | "tracks" | "categories";
  title: string;
  /** Entry being renamed as the cache holds it now; omitted when one is created. */
  entry?: CatalogEntry;
  /** Direction of a track being created. */
  directionId?: string;
  /** Unit offered first for a new entry, the one its parent belongs to. */
  defaultOwner?: string | null;
  onClose: () => void;
}

/** Dialog that creates a direction, track or category, or renames one. */
export function NameModal({
  segment,
  title,
  entry: latest,
  directionId,
  defaultOwner,
  onClose,
}: NameModalProps) {
  const queryClient = useQueryClient();
  const { base: entry, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: (values: NamedEntryValues) =>
      saveNamedEntry(segment, { ...values, directionId }, entry),
    onSuccess: onClose,
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
        isNew={!entry}
        defaultOwner={defaultOwner}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface NameFormProps {
  name: string;
  isNew: boolean;
  defaultOwner?: string | null;
  onSubmit: (values: NamedEntryValues) => Promise<unknown>;
  onCancel: () => void;
}

function NameForm({
  name,
  isNew,
  defaultOwner,
  onSubmit,
  onCancel,
}: NameFormProps) {
  const [form] = Form.useForm<NamedEntryValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);

  return (
    <Form
      form={form}
      name="catalog-name"
      layout="vertical"
      initialValues={{ name, ownerOrgUnitId: defaultOwner ?? undefined }}
      onFinish={submit}
    >
      {isNew && <OwnerFormItem />}
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
