import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Button, Form, Input, Modal, Space } from "antd";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useEditBase } from "../../app/useEditBase";
import { useFormSubmit } from "../../app/useFormSubmit";
import { invalidatePositions, type Position } from "./positionsApi";

const NAME_MAX_LENGTH = 200;

interface PositionNameValues {
  name: string;
}

interface PositionNameModalProps {
  /** Position being renamed as the cache holds it now; omitted when a position is created. */
  position?: Position;
  onClose: () => void;
}

/** A taken name is shown under the field, so an alert would only repeat it. */
function isShownUnderField(error: unknown): boolean {
  return error instanceof ApiError && error.errors !== undefined;
}

/**
 * Dialog that creates a position or renames one. Mount it only while it is shown, so every opening starts clean.
 * The form keeps the position it was opened with and takes the reread one only after a save is refused for a stale version.
 */
export function PositionNameModal({
  position: latest,
  onClose,
}: PositionNameModalProps) {
  const queryClient = useQueryClient();
  const { base: position, noteSaveFailure } = useEditBase(latest);
  const save = useMutation({
    mutationFn: async (body: PositionNameValues) =>
      unwrap(
        position
          ? await api.PUT("/api/v1/positions/{id}", {
              params: {
                path: { id: position.id },
                header: { "If-Match": ifMatchOf(position.version) },
              },
              body,
            })
          : await api.POST("/api/v1/positions", { body }),
      ),
    onSuccess: onClose,
    onError: noteSaveFailure,
    onSettled: () => invalidatePositions(queryClient),
  });

  return (
    <Modal
      open
      title={position ? "Переименование должности" : "Новая должность"}
      onCancel={onClose}
      footer={null}
    >
      <Space orientation="vertical" size="large" style={{ display: "flex" }}>
        {save.isError && !isShownUnderField(save.error) && (
          <ErrorAlert
            title="Не удалось сохранить должность"
            error={save.error}
          />
        )}
        <PositionNameForm
          key={position?.version}
          name={position?.name ?? ""}
          onSubmit={(values) => save.mutateAsync(values)}
          onCancel={onClose}
        />
      </Space>
    </Modal>
  );
}

interface PositionNameFormProps {
  name: string;
  /** Saves the name; a rejection is shown under the field it names and by the caller. */
  onSubmit: (values: PositionNameValues) => Promise<unknown>;
  onCancel: () => void;
}

function PositionNameForm({ name, onSubmit, onCancel }: PositionNameFormProps) {
  const [form] = Form.useForm<PositionNameValues>();
  const { isSubmitting, submit } = useFormSubmit(form, onSubmit);

  return (
    <Form
      form={form}
      name="position"
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
      <Space>
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          Сохранить
        </Button>
        <Button onClick={onCancel}>Отмена</Button>
      </Space>
    </Form>
  );
}
