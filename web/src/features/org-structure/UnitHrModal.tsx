import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Form,
  Modal,
  Select,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useState } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useFormSubmit } from "../../app/useFormSubmit";
import { useIsAdmin } from "../auth/useCurrentUser";
import { careerQueryKey, unitHrQuery } from "../career/careerApi";
import { userListQuery } from "../users/usersApi";
import type { OrgUnit } from "./orgStructureApi";

const PICKER_PAGE_SIZE = 20;

interface UnitHrModalProps {
  unit: OrgUnit;
  onClose: () => void;
}

/**
 * Dialog of the HR who answers for a unit: the assigned and the inherited one, for everyone; administrators also assign
 * an account and remove the assignment. Mount it only while it is shown.
 */
export function UnitHrModal({ unit, onClose }: UnitHrModalProps) {
  const isAdmin = useIsAdmin();
  const queryClient = useQueryClient();
  const { modal, message } = App.useApp();
  const { data, error, isPending, refetch } = useQuery(unitHrQuery(unit.id));
  const { mutateAsync: assign } = useMutation({
    mutationFn: async (userId: string) =>
      unwrap(
        await api.PUT("/api/v1/career/org-units/{id}/hr", {
          params: { path: { id: unit.id } },
          body: { userId },
        }),
      ),
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: careerQueryKey }),
  });
  const { mutateAsync: remove } = useMutation({
    mutationFn: async () =>
      unwrap(
        await api.DELETE("/api/v1/career/org-units/{id}/hr", {
          params: { path: { id: unit.id } },
        }),
      ),
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: careerQueryKey }),
  });

  function confirmRemoval() {
    modal.confirm({
      title: "Снять закрепление HR?",
      content:
        "Ответственным станет HR вышестоящего подразделения, если он назначен.",
      okText: "Снять закрепление",
      okButtonProps: { danger: true },
      cancelText: "Отмена",
      onOk: () =>
        remove().then(
          () => undefined,
          (failure) => message.error(describeApiError(failure)),
        ),
    });
  }

  return (
    <Modal
      open
      title={`HR подразделения «${unit.name}»`}
      onCancel={onClose}
      footer={null}
    >
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить HR подразделения"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && (
        <Space orientation="vertical" size="large" style={{ display: "flex" }}>
          <div>
            {data.assigned && (
              <Typography.Paragraph>
                Закреплён: {data.assigned.name}
              </Typography.Paragraph>
            )}
            {data.inherited && (
              <Typography.Paragraph>
                Унаследован из «{data.inherited.orgUnitName}»:{" "}
                {data.inherited.name}
              </Typography.Paragraph>
            )}
            {!data.assigned && !data.inherited && (
              <Typography.Text type="secondary">HR не назначен</Typography.Text>
            )}
          </div>
          {isAdmin && (
            <>
              <AssignHrForm onAssign={assign} />
              {data.assigned && (
                <Button danger onClick={confirmRemoval}>
                  Снять закрепление
                </Button>
              )}
            </>
          )}
        </Space>
      )}
    </Modal>
  );
}

interface AssignHrValues {
  userId: string;
}

/** Remote-search choice of an account and the button that assigns it as the unit's HR; a refusal shows under the field. */
function AssignHrForm({
  onAssign,
}: {
  onAssign: (userId: string) => Promise<unknown>;
}) {
  const [form] = Form.useForm<AssignHrValues>();
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onAssign(values.userId);
    form.resetFields();
  });
  const [search, setSearch] = useState("");
  const text = useDebouncedValue(search.trim());
  const { data, error, isFetching } = useQuery(
    userListQuery({
      q: text || undefined,
      page: 1,
      pageSize: PICKER_PAGE_SIZE,
    }),
  );
  const options = (data?.items ?? []).map((user) => ({
    value: user.id,
    label: user.employeeName
      ? `${user.employeeName} (${user.email})`
      : user.email,
  }));

  return (
    <Form form={form} name="unit-hr" layout="vertical" onFinish={submit}>
      <Form.Item name="userId" label="Пользователь">
        <Select
          showSearch={{ filterOption: false, onSearch: setSearch }}
          options={options}
          loading={isFetching}
          notFoundContent={error ? describeApiError(error) : undefined}
        />
      </Form.Item>
      <Button htmlType="submit" loading={isSubmitting}>
        Закрепить
      </Button>
    </Form>
  );
}
