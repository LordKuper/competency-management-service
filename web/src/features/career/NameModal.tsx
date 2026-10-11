import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Form, Input, Select } from "antd";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { CatalogModal, FormActions } from "./CatalogModal";
import {
  type CatalogEntry,
  invalidateCatalogs,
  type NamedEntryValues,
  saveNamedEntry,
} from "./catalogApi";
import { useCatalogRights } from "./useCatalogRights";

const NAME_MAX_LENGTH = 200;
const FIELDS = ["name", "ownerOrgUnitId"] as const;

interface NameModalProps {
  segment: "directions" | "tracks" | "categories";
  title: string;
  /** Entry being renamed as the cache holds it now; omitted when one is created. */
  entry?: CatalogEntry;
  /** Direction of a track being created. */
  directionId?: string;
  onClose: () => void;
}

/** Dialog that creates a direction, track or category, or renames one. */
export function NameModal({
  segment,
  title,
  entry: latest,
  directionId,
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
        hasUnit={segment === "tracks"}
        onSubmit={(values) => save.mutateAsync(values)}
        onCancel={onClose}
      />
    </CatalogModal>
  );
}

interface NameFormProps {
  name: string;
  isNew: boolean;
  /** Whether a new entry is put in a unit of the user's choice: tracks are. */
  hasUnit: boolean;
  onSubmit: (values: NamedEntryValues) => Promise<unknown>;
  onCancel: () => void;
}

function NameForm({ name, isNew, hasUnit, onSubmit, onCancel }: NameFormProps) {
  const [form] = Form.useForm<NamedEntryValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);

  return (
    <Form
      form={form}
      name="catalog-name"
      layout="vertical"
      initialValues={{ name }}
      onFinish={submit}
    >
      {isNew && hasUnit && <TrackUnitFormItem />}
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

/** The unit of a new track: administrators pick any unit, heads and HR one of the units they work for. */
function TrackUnitFormItem() {
  const rights = useCatalogRights();
  return (
    <Form.Item
      name="ownerOrgUnitId"
      label="Подразделение"
      rules={[{ required: true }]}
    >
      {rights.isAdmin ? (
        <OrgUnitSelect isActiveRequired />
      ) : (
        <Select
          options={rights.ownerUnits.map((unit) => ({
            value: unit.id,
            label: unit.name,
          }))}
        />
      )}
    </Form.Item>
  );
}
