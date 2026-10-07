import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Button, Form, Input, Modal, Space, Typography } from "antd";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EmployeePicker } from "../users/EmployeePicker";
import { OrgUnitSelect } from "./OrgUnitSelect";
import {
  employeeQuery,
  invalidateOrgStructure,
  type OrgUnit,
} from "./orgStructureApi";

interface UnitFormValues {
  name: string;
  parentId?: string;
  headEmployeeId?: string;
}

interface UnitFormModalProps {
  /** Unit being edited as the cache holds it now; omitted when a unit is created. */
  unit?: OrgUnit;
  /** Parent the unit being created starts under, or null for a root; ignored when editing. */
  parentId?: string | null;
  onClose: () => void;
  /** Receives the saved unit once the server has accepted it. */
  onSaved: (unit: OrgUnit) => void;
}

/**
 * Dialog that creates a unit, under a parent or as a root, or edits the unit's own fields. Mount it only while it is
 * shown, so every opening starts clean. Once a unit exists, its parent and status change through their own operations.
 * The form keeps the unit it was opened with and takes the reread one only after a save is refused for a stale version.
 */
export function UnitFormModal({
  unit: latest,
  parentId = null,
  onClose,
  onSaved,
}: UnitFormModalProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { base: unit, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: async (values: UnitFormValues) =>
      unwrap(
        unit
          ? await api.PUT("/api/v1/org-units/{id}", {
              params: {
                path: { id: unit.id },
                header: { "If-Match": ifMatchOf(unit.version) },
              },
              body: {
                name: values.name,
                headEmployeeId: values.headEmployeeId ?? null,
              },
            })
          : await api.POST("/api/v1/org-units", {
              body: { name: values.name, parentId: values.parentId ?? null },
            }),
      ),
    onSuccess: async (saved) => {
      await invalidateOrgStructure(queryClient);
      message.success(unit ? "Изменения сохранены" : "Подразделение создано");
      onSaved(saved);
    },
    onError: (error) => {
      noteSaveFailure(error);
      return invalidateOrgStructure(queryClient);
    },
  });

  return (
    <Modal
      open
      title={unit ? "Изменение подразделения" : "Новое подразделение"}
      onCancel={onClose}
      footer={null}
    >
      <Space orientation="vertical" size="large" style={{ display: "flex" }}>
        {save.isError && (
          <ErrorAlert
            title="Не удалось сохранить подразделение"
            error={save.error}
          />
        )}
        <UnitForm
          key={unit?.version}
          unit={unit}
          parentId={parentId}
          submitLabel={unit ? "Сохранить" : "Создать"}
          onSubmit={(values) => save.mutateAsync(values)}
          onCancel={onClose}
        />
      </Space>
    </Modal>
  );
}

interface UnitFormProps {
  unit?: OrgUnit;
  parentId: string | null;
  submitLabel: string;
  onSubmit: (values: UnitFormValues) => Promise<unknown>;
  onCancel: () => void;
}

function UnitForm({
  unit,
  parentId,
  submitLabel,
  onSubmit,
  onCancel,
}: UnitFormProps) {
  const [form] = Form.useForm<UnitFormValues>();
  const headId = unit?.headEmployeeId ?? null;
  const { data: head } = useQuery({
    ...employeeQuery(headId ?? ""),
    enabled: headId !== null,
  });
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);

  return (
    <Form
      form={form}
      name="org-unit"
      layout="vertical"
      initialValues={{
        name: unit?.name ?? "",
        parentId: parentId ?? undefined,
        headEmployeeId: unit?.headEmployeeId ?? undefined,
      }}
      onFinish={submit}
    >
      <Form.Item
        name="name"
        label="Название"
        rules={[
          { required: true, whitespace: true, message: "Введите название" },
        ]}
      >
        <Input autoComplete="off" />
      </Form.Item>
      {!unit && (
        <Form.Item
          name="parentId"
          label="Родительское подразделение"
          extra="Оставьте поле пустым, чтобы создать корневое подразделение."
        >
          <OrgUnitSelect
            allowClear
            isActiveRequired
            placeholder="Без родителя: корневое подразделение"
          />
        </Form.Item>
      )}
      {unit ? (
        <Form.Item
          name="headEmployeeId"
          label="Руководитель"
          extra="Руководителем может быть только работающий сотрудник этого подразделения."
        >
          <EmployeePicker
            orgUnitId={unit.id}
            current={head ? { id: head.id, name: head.fullName } : undefined}
          />
        </Form.Item>
      ) : (
        <Form.Item label="Руководитель">
          <Typography.Text type="secondary">
            Руководителя можно назначить после добавления сотрудников.
          </Typography.Text>
        </Form.Item>
      )}
      <Space>
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          {submitLabel}
        </Button>
        <Button onClick={onCancel}>Отмена</Button>
      </Space>
    </Form>
  );
}
