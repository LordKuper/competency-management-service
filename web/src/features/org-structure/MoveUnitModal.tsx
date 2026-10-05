import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Button, Form, Modal, Space } from "antd";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { showFieldErrors } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { ifMatchOf } from "../users/usersApi";
import { OrgUnitSelect } from "./OrgUnitSelect";
import { invalidateOrgStructure, type OrgUnit } from "./orgStructureApi";

interface MoveFormValues {
  parentId?: string;
}

interface MoveUnitModalProps {
  unit: OrgUnit;
  onClose: () => void;
}

/**
 * Dialog that moves a unit with everything below it under another unit, or makes it a root.
 * The unit and its descendants are not offered as the new parent; the server still refuses a cycle. Mount it only while it is shown.
 */
export function MoveUnitModal({ unit, onClose }: MoveUnitModalProps) {
  const [form] = Form.useForm<MoveFormValues>();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const move = useMutation({
    mutationFn: async (parentId: string | null) =>
      unwrap(
        await api.POST("/api/v1/org-units/{id}/move", {
          params: {
            path: { id: unit.id },
            header: { "If-Match": ifMatchOf(unit.version) },
          },
          body: { parentId },
        }),
      ).data,
    onSuccess: () => {
      message.success("Подразделение перенесено");
      onClose();
    },
    onError: (error) => showFieldErrors(form, error),
    onSettled: () => invalidateOrgStructure(queryClient),
  });

  return (
    <Modal open title="Перенос подразделения" onCancel={onClose} footer={null}>
      <Form
        form={form}
        name="move-org-unit"
        layout="vertical"
        initialValues={{ parentId: unit.parentId ?? undefined }}
        onFinish={({ parentId }) => move.mutate(parentId ?? null)}
      >
        <Space orientation="vertical" size="large" style={{ display: "flex" }}>
          {move.isError && (
            <ErrorAlert
              title="Не удалось перенести подразделение"
              error={move.error}
            />
          )}
          <Form.Item
            name="parentId"
            label="Новое родительское подразделение"
            extra="Подразделение переносится вместе со всеми дочерними. Очистите поле, чтобы сделать его корневым."
          >
            <OrgUnitSelect
              allowClear
              isActiveRequired
              excludedSubtreeRoot={unit.id}
              placeholder="Без родителя: корневое подразделение"
            />
          </Form.Item>
          <Space>
            <Button type="primary" htmlType="submit" loading={move.isPending}>
              Перенести
            </Button>
            <Button onClick={onClose}>Отмена</Button>
          </Space>
        </Space>
      </Form>
    </Modal>
  );
}
