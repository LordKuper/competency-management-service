import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Button, Form, Skeleton, Space, Typography } from "antd";
import { Link } from "react-router";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { AppModal, CancelButton } from "../../app/AppModal";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useFormSubmit } from "../../app/useFormSubmit";
import {
  invalidateOrgStructure,
  orgUnitQuery,
} from "../org-structure/orgStructureApi";
import { EmployeePicker } from "../users/EmployeePicker";
import { deskPaths } from "./deskTexts";
import { returnState } from "./returnLink";

interface AssignHeadModalProps {
  unitId: string;
  onClose: () => void;
}

/**
 * Dialog that names a working employee of the unit its head. The unit is read when the dialog opens and again after a
 * refusal, so the next save sends the version the form shows. Mount it only while it is shown.
 */
export function AssignHeadModal({ unitId, onClose }: AssignHeadModalProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { data: unit, error, refetch } = useQuery(orgUnitQuery(unitId));
  const [form] = Form.useForm<{ headEmployeeId?: string }>();
  const save = useMutation({
    mutationFn: async (headEmployeeId: string) => {
      if (!unit) throw new Error("The unit is not read yet");
      return unwrap(
        await api.PUT("/api/v1/org-units/{id}", {
          params: {
            path: { id: unit.id },
            header: { "If-Match": ifMatchOf(unit.version) },
          },
          body: { name: unit.name, headEmployeeId },
        }),
      );
    },
    onSuccess: async () => {
      await invalidateOrgStructure(queryClient);
      message.success("Руководитель назначен");
      onClose();
    },
    onError: () => invalidateOrgStructure(queryClient),
  });
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    if (values.headEmployeeId) await save.mutateAsync(values.headEmployeeId);
  });

  return (
    <AppModal
      open
      title="Назначить руководителя"
      onCancel={onClose}
      footer={null}
      closable={!isSubmitting}
      mask={{ closable: !isSubmitting }}
      keyboard={!isSubmitting}
    >
      {error && (
        <ErrorAlert
          title="Не удалось загрузить подразделение"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {!unit && !error && <Skeleton active />}
      {unit && (
        <Space orientation="vertical" size="large" style={{ display: "flex" }}>
          {save.isError && (
            <ErrorAlert
              title="Не удалось назначить руководителя"
              error={save.error}
            />
          )}
          <Form
            key={unit.version}
            form={form}
            name="unit-head"
            layout="vertical"
            disabled={isSubmitting}
            onFinish={submit}
          >
            <Form.Item
              name="headEmployeeId"
              label="Сотрудник"
              extra="Только работающие сотрудники этого подразделения."
              rules={[{ required: true, message: "Выберите сотрудника" }]}
            >
              <EmployeePicker orgUnitId={unit.id} />
            </Form.Item>
            <Typography.Paragraph type="secondary">
              Если сотрудников нет, сначала{" "}
              <Link
                to={deskPaths.onboardEmployeeWith({ unitId: unit.id })}
                state={returnState({
                  to: deskPaths.connectUnitOf(unit.id),
                  crumb: "Подключить подразделение",
                })}
              >
                примите сотрудника
              </Link>
              .
            </Typography.Paragraph>
            <Space>
              <Button type="primary" htmlType="submit" loading={isSubmitting}>
                Назначить
              </Button>
              <CancelButton disabled={isSubmitting} onClose={onClose} />
            </Space>
          </Form>
        </Space>
      )}
    </AppModal>
  );
}
