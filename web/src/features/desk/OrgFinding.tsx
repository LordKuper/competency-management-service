import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  theme as antdTheme,
  Button,
  Col,
  Flex,
  Form,
  Row,
  Table,
  Typography,
} from "antd";
import { type ReactNode, type RefObject, useState } from "react";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import {
  type Employee,
  employeeQuery,
  employeeSearchQuery,
  invalidateOrgStructure,
  orgUnitQuery,
  orgUnitTreeQuery,
} from "../org-structure/orgStructureApi";
import { useUnitActivation } from "../org-structure/useUnitActivation";
import { invalidatePositions, positionQuery } from "../positions/positionsApi";
import { EmployeePicker } from "../users/EmployeePicker";
import { userQuery, usersQueryKey } from "../users/usersApi";
import { ChangePositionForm } from "./ChangePositionForm";
import { DecisionError } from "./DecisionError";
import type { DeskItem } from "./deskApi";
import { deskPaths, objectTitle, reasonLines, typeLabelOf } from "./deskTexts";
import { ORG_FINDING_MUTATION, useIsFindingBusy } from "./findingBusy";
import { LinkButton } from "./LinkButton";
import { returnState } from "./returnLink";
import { CompactAlert } from "./ScenarioContext";

const QUEUE_RETURN = returnState({
  to: deskPaths.orgCleanup,
  crumb: "Навести порядок в оргструктуре",
});

interface OrgFindingProps {
  item: DeskItem;
  headingRef: RefObject<HTMLHeadingElement | null>;
}

/** The open finding of the organisation queue: what is wrong, and the action that puts it right on the spot. */
export function OrgFinding({ item, headingRef }: OrgFindingProps) {
  const lines = reasonLines(item);
  const [first, ...rest] = lines;
  const label = typeLabelOf(item);
  return (
    <Flex vertical gap="middle">
      <Typography.Title
        level={4}
        tabIndex={-1}
        ref={headingRef}
        style={{ margin: 0 }}
      >
        {label ? `${label} ${item.object.name}` : objectTitle(item.object)}
      </Typography.Title>
      {first && (
        <CompactAlert
          type={item.isReference ? "info" : "warning"}
          lead={first.text}
          text={rest.map((line) => line.text).join(" · ") || undefined}
        />
      )}
      <Typography.Title level={4} style={{ margin: 0 }}>
        Как исправить
      </Typography.Title>
      <FindingAction item={item} />
    </Flex>
  );
}

function FindingAction({ item }: { item: DeskItem }) {
  switch (item.reasons[0]?.kind) {
    case "PositionOutsideList":
      return <OutsideList item={item} />;
    case "AccountWithoutEmployee":
      return <LinkAccount accountId={item.object.id} />;
    case "InactivePositionInUse":
      return <InactivePosition item={item} />;
    case "UnusedPosition":
      return <UnusedPosition item={item} />;
    case "EmptyUnit":
      return <EmptyUnit item={item} />;
    default:
      return (
        <LinkButton to={deskPaths.connectUnitOf(item.object.id)} type="primary">
          Открыть «Подключить подразделение»
        </LinkButton>
      );
  }
}

function OutsideList({ item }: { item: DeskItem }) {
  const { token } = antdTheme.useToken();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const [position, unit] = item.reasons[0]?.details?.entries ?? [];
  const employee = useQuery(employeeQuery(item.object.id));
  const add = useMutation({
    mutationKey: ORG_FINDING_MUTATION,
    mutationFn: async () => {
      if (!position || !unit) throw new Error("The finding names no position");
      return unwrap(
        await api.POST("/api/v1/org-units/{id}/positions", {
          params: { path: { id: unit.id } },
          body: { positionId: position.id },
        }),
      );
    },
    onSuccess: () =>
      message.success(
        `Должность «${position?.name}» добавлена в перечень подразделения «${unit?.name}»`,
      ),
    onSettled: () => invalidatePositions(queryClient),
  });

  return (
    <Flex vertical gap="middle">
      {add.isError && (
        <DecisionError title="Должность не добавлена" error={add.error} />
      )}
      <Row gutter={[token.margin, token.margin]}>
        <Col xs={24} lg={12}>
          <Flex
            vertical
            gap="small"
            style={{
              height: "100%",
              padding: "var(--ant-padding-sm)",
              border:
                "var(--ant-line-width) var(--ant-line-type) var(--ant-color-primary)",
              borderRadius: "var(--ant-border-radius)",
              background: "var(--ant-color-primary-bg)",
            }}
          >
            <Typography.Text strong>
              {`Добавить должность «${position?.name}» в перечень подразделения «${unit?.name}»`}
            </Typography.Text>
            <div>
              <Button
                type="primary"
                loading={add.isPending}
                disabled={isBusy && !add.isPending}
                onClick={() => add.mutate()}
              >
                Добавить в перечень
              </Button>
            </div>
          </Flex>
        </Col>
        <Col xs={24} lg={12}>
          <Flex vertical gap="small">
            <Typography.Text strong>
              Сменить должность сотрудника
            </Typography.Text>
            {employee.error ? (
              <ErrorAlert
                title="Не удалось загрузить сотрудника"
                error={employee.error}
                onRetry={() => void employee.refetch()}
              />
            ) : employee.data?.version == null ? (
              <Typography.Text type="secondary">Загрузка…</Typography.Text>
            ) : (
              <ChangePositionForm employee={employee.data} />
            )}
          </Flex>
        </Col>
      </Row>
    </Flex>
  );
}

function LinkAccount({ accountId }: { accountId: string }) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const user = useQuery(userQuery(accountId));
  const [form] = Form.useForm<{ employeeId?: string }>();
  const [isDirty, setIsDirty] = useState(false);
  const link = useMutation({
    mutationKey: ORG_FINDING_MUTATION,
    mutationFn: async (employeeId: string) => {
      const account = user.data;
      if (!account) throw new Error("The account is not read yet");
      return unwrap(
        await api.PUT("/api/v1/users/{id}", {
          params: {
            path: { id: account.id },
            header: { "If-Match": ifMatchOf(account.version) },
          },
          body: { email: account.email, roles: account.roles, employeeId },
        }),
      );
    },
    onSuccess: () => {
      setIsDirty(false);
      message.success("Учётная запись привязана к сотруднику");
    },
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: usersQueryKey }),
        invalidateOrgStructure(queryClient),
      ]),
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: link.isPending });
  const { submit } = useFormSubmit(form, async (values) => {
    if (values.employeeId) await link.mutateAsync(values.employeeId);
  });

  if (user.error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить учётную запись"
        error={user.error}
        onRetry={() => void user.refetch()}
      />
    );
  }
  return (
    <Flex vertical gap="small">
      {link.isError && (
        <DecisionError title="Учётная запись не привязана" error={link.error} />
      )}
      <Form
        key={user.data?.version}
        form={form}
        name="link-account"
        layout="vertical"
        disabled={isBusy}
        onFinish={submit}
        onValuesChange={() => setIsDirty(form.isFieldsTouched())}
      >
        <Form.Item
          name="employeeId"
          label="Сотрудник"
          extra="В списке видны подразделение и должность. После привязки ссылка приглашения перестанет действовать: отправьте приглашение повторно."
          rules={[{ required: true, message: "Выберите сотрудника" }]}
        >
          <EmployeePicker />
        </Form.Item>
        <Button
          type="primary"
          htmlType="submit"
          loading={link.isPending}
          disabled={!user.data}
        >
          Привязать
        </Button>
      </Form>
    </Flex>
  );
}

/**
 * An inactive position still held: restore it, or move its holders to other positions.
 * ponytail: the employee search returns one page of 200; a position held by more employees shows its first 200.
 */
function InactivePosition({ item }: { item: DeskItem }) {
  const holders = useQuery(employeeSearchQuery(item.object.name));
  const rows = (holders.data?.items ?? []).filter(
    (employee) =>
      employee.positionId === item.object.id && employee.isActive !== false,
  );
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const restore = useMutation({
    mutationKey: ORG_FINDING_MUTATION,
    mutationFn: async () => {
      const position = await queryClient.fetchQuery(
        positionQuery(item.object.id),
      );
      return unwrap(
        await api.POST("/api/v1/positions/{id}/activate", {
          params: {
            path: { id: position.id },
            header: { "If-Match": ifMatchOf(position.version) },
          },
        }),
      );
    },
    onSuccess: () =>
      message.success(`Должность «${item.object.name}» восстановлена`),
    onSettled: () => invalidatePositions(queryClient),
  });
  if (holders.error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить сотрудников"
        error={holders.error}
        onRetry={() => void holders.refetch()}
      />
    );
  }
  return (
    <Flex vertical gap="small">
      {restore.isError && (
        <DecisionError
          title="Должность не восстановлена"
          error={restore.error}
        />
      )}
      <div>
        <Button
          type="primary"
          loading={restore.isPending}
          disabled={isBusy && !restore.isPending}
          onClick={() => restore.mutate()}
        >
          Восстановить должность
        </Button>
      </div>
      <Typography.Text type="secondary">
        Должность вернётся в работу, сотрудники останутся на своих местах. Или
        смените должность сотрудникам:
      </Typography.Text>
      <HoldersTable isLoading={holders.isPending} rows={rows} />
    </Flex>
  );
}

function HoldersTable({
  isLoading,
  rows,
}: {
  isLoading: boolean;
  rows: Employee[];
}) {
  return (
    <Table
      size="small"
      rowKey="id"
      pagination={false}
      loading={isLoading}
      dataSource={rows}
      locale={{ emptyText: "Работающих сотрудников с этой должностью нет" }}
      columns={[
        { title: "Сотрудник", dataIndex: "fullName" },
        { title: "Подразделение", dataIndex: "orgUnitName" },
        {
          title: "Новая должность",
          key: "change",
          render: (_, employee) =>
            employee.version == null ? null : (
              <ChangePositionForm employee={employee} isInline />
            ),
        },
      ]}
    />
  );
}

function UnusedPosition({ item }: { item: DeskItem }) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const deactivate = useMutation({
    mutationKey: ORG_FINDING_MUTATION,
    mutationFn: async () => {
      const params = { path: { id: item.object.id } };
      const position = unwrap(
        await api.GET("/api/v1/positions/{id}", { params }),
      );
      return unwrap(
        await api.POST("/api/v1/positions/{id}/deactivate", {
          params: {
            ...params,
            header: { "If-Match": ifMatchOf(position.version) },
          },
        }),
      );
    },
    onSuccess: () =>
      message.success(`Должность «${item.object.name}» деактивирована`),
    onSettled: () => invalidatePositions(queryClient),
  });
  const units = useQuery(orgUnitTreeQuery);
  const canOnboard = units.data?.some((unit) => unit.isActive) ?? false;
  return (
    <ActionWithError
      error={deactivate.isError ? deactivate.error : null}
      title="Должность не деактивирована"
    >
      <Flex vertical gap="small">
        <Flex gap="small" wrap>
          {canOnboard && (
            <LinkButton
              type="primary"
              to={deskPaths.onboardEmployeeWith({ positionId: item.object.id })}
              state={QUEUE_RETURN}
            >
              Принять сотрудника
            </LinkButton>
          )}
          <Button
            loading={deactivate.isPending}
            disabled={isBusy && !deactivate.isPending}
            onClick={() => deactivate.mutate()}
          >
            Деактивировать должность
          </Button>
        </Flex>
        {canOnboard && (
          <Typography.Text type="secondary">
            {`«Принять сотрудника» — откроется сценарий, должность «${item.object.name}» в нём уже выбрана; «Деактивировать» — должность перестанет предлагаться.`}
          </Typography.Text>
        )}
      </Flex>
    </ActionWithError>
  );
}

function EmptyUnit({ item }: { item: DeskItem }) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const isBusy = useIsFindingBusy();
  const confirm = useUnitActivation();
  const read = useMutation({
    mutationFn: () => queryClient.fetchQuery(orgUnitQuery(item.object.id)),
    onSuccess: confirm,
    onError: (failure) => message.error(describeApiError(failure)),
  });
  return (
    <Flex vertical gap="small">
      <Flex gap="small" wrap>
        <LinkButton
          type="primary"
          to={deskPaths.onboardEmployeeWith({ unitId: item.object.id })}
          state={QUEUE_RETURN}
        >
          Принять сотрудника
        </LinkButton>
        <Button
          loading={read.isPending}
          disabled={isBusy}
          onClick={() => read.mutate()}
        >
          Деактивировать подразделение
        </Button>
      </Flex>
      <Typography.Text type="secondary">
        {`«Принять сотрудника» — откроется сценарий, подразделение «${item.object.name}» в нём уже выбрано; «Деактивировать» — подразделение исчезнет из дерева.`}
      </Typography.Text>
    </Flex>
  );
}

function ActionWithError({
  error,
  title,
  children,
}: {
  error: unknown;
  title: string;
  children: ReactNode;
}) {
  return (
    <Flex vertical gap="small">
      {error !== null && <DecisionError title={title} error={error} />}
      <div>{children}</div>
    </Flex>
  );
}
