import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Alert,
  App,
  Button,
  Flex,
  Form,
  Select,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { AppModal } from "../../app/AppModal";
import { describeApiError } from "../../app/apiErrors";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useFormSubmit } from "../../app/useFormSubmit";
import { useIsAdmin } from "../auth/useCurrentUser";
import {
  careerQueryKey,
  type OrgUnitHrPerson,
  unitHrQuery,
} from "../career/careerApi";
import { employeesQueryKey, type OrgUnit } from "./orgStructureApi";

const PICKER_PAGE_SIZE = 20;

const USERS_PATH = "/users";

const CANDIDATES_QUERY_KEY = [...employeesQueryKey, "hr-candidates"] as const;

interface UnitHrModalProps {
  /** The unit; only its identifier and name are used. */
  unit: Pick<OrgUnit, "id" | "name">;
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
    mutationFn: async (employeeId: string) =>
      unwrap(
        await api.PUT("/api/v1/career/org-units/{id}/hr", {
          params: { path: { id: unit.id } },
          body: { employeeId },
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
      ...destructiveConfirm(true),
      cancelText: "Отмена",
      onOk: () =>
        remove().then(
          () => {
            message.success("Закрепление HR снято");
            onClose();
          },
          (failure) => message.error(describeApiError(failure)),
        ),
    });
  }

  return (
    <AppModal
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
          {isAdmin && data.assigned && (
            <InactiveAssignment assigned={data.assigned} />
          )}
          {isAdmin && (
            <>
              <AssignHrForm
                onAssign={async (employeeId) => {
                  await assign(employeeId);
                  message.success("HR закреплён");
                  onClose();
                }}
              />
              {data.assigned && (
                <Button danger onClick={confirmRemoval}>
                  Снять закрепление
                </Button>
              )}
            </>
          )}
        </Space>
      )}
    </AppModal>
  );
}

const INACTIVE_CAUSES: Record<
  Exclude<NonNullable<OrgUnitHrPerson["account"]>["state"], "Active">,
  (email: string | null) => string
> = {
  None: () => "У сотрудника нет учётной записи",
  Invited: (email) => `Учётная запись ${email} не завершила регистрацию`,
  Blocked: (email) => `Учётная запись ${email} заблокирована`,
  NoHrRole: (email) => `У учётной записи ${email} нет роли «HR»`,
};

/** Warning for administrators when the assigned employee cannot act, with the cause and the next step. */
function InactiveAssignment({ assigned }: { assigned: OrgUnitHrPerson }) {
  const account = assigned.account;
  if (!account || account.state === "Active") return null;
  return (
    <Alert
      type="warning"
      showIcon
      title="Закрепление не действует"
      description={
        <>
          {INACTIVE_CAUSES[account.state](account.email)}, поэтому у
          подразделения нет ответственного HR. Исправьте запись в разделе{" "}
          <Link to={USERS_PATH}>Пользователи</Link> или закрепите другого
          сотрудника.
        </>
      }
    />
  );
}

interface AssignHrValues {
  employeeId: string;
}

/** The first page of working employees whose account can be assigned as HR, matching the text by name or e-mail. */
function hrCandidatesQuery(text: string) {
  return {
    queryKey: [...CANDIDATES_QUERY_KEY, text],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/employees", {
          params: {
            query: {
              hrCandidate: true,
              q: text || undefined,
              page: 1,
              pageSize: PICKER_PAGE_SIZE,
            },
          },
        }),
      ),
  };
}

/**
 * Remote-search choice of an employee and the button that assigns them as the unit's HR; a refusal naming the employee
 * shows under the field, any other failure as a message, and either rereads the list. With no candidates at all the field gives way to a pointer to the account screen.
 */
function AssignHrForm({
  onAssign,
}: {
  onAssign: (employeeId: string) => Promise<unknown>;
}) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const [form] = Form.useForm<AssignHrValues>();
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    try {
      await onAssign(values.employeeId);
    } catch (failure) {
      await queryClient.invalidateQueries({ queryKey: CANDIDATES_QUERY_KEY });
      if (!(failure instanceof ApiError && failure.errors?.employeeId)) {
        message.error(describeApiError(failure));
      }
      throw failure;
    }
  });
  const [search, setSearch] = useState("");
  const text = useDebouncedValue(search.trim());
  const { data, error, isFetching } = useQuery({
    ...hrCandidatesQuery(text),
    placeholderData: keepPreviousData,
  });
  if (data && data.items.length === 0 && !text && !isFetching) {
    return (
      <Alert
        type="info"
        showIcon
        title="Некого закрепить"
        description={
          <>
            Нужен работающий сотрудник с действующей учётной записью и ролью
            «HR», а таких в системе нет. Отметьте роль «HR» у нужной учётной
            записи в разделе <Link to={USERS_PATH}>Пользователи</Link> и
            убедитесь, что запись привязана к сотруднику, не заблокирована и
            завершила регистрацию.
          </>
        }
      />
    );
  }
  const options = (data?.items ?? []).map((employee) => ({
    value: employee.id,
    label: employee.fullName,
    detail: `${employee.orgUnitName}, ${employee.position}${employee.email ? ` · ${employee.email}` : ""}`,
  }));

  return (
    <Form form={form} name="unit-hr" layout="vertical" onFinish={submit}>
      <Form.Item
        name="employeeId"
        label="Сотрудник"
        rules={[{ required: true, message: "Выберите сотрудника" }]}
      >
        <Select
          showSearch={{ filterOption: false, onSearch: setSearch }}
          placeholder="Начните вводить ФИО"
          options={options}
          optionRender={(option) => (
            <Flex vertical>
              <span className="option-truncate" title={String(option.label)}>
                {option.label}
              </span>
              <Typography.Text
                type="secondary"
                className="option-truncate"
                title={option.data.detail}
              >
                {option.data.detail}
              </Typography.Text>
            </Flex>
          )}
          loading={isFetching}
          notFoundContent={
            error
              ? describeApiError(error)
              : "Никого не найдено. Измените запрос."
          }
        />
      </Form.Item>
      <Button htmlType="submit" loading={isSubmitting}>
        Закрепить
      </Button>
    </Form>
  );
}
