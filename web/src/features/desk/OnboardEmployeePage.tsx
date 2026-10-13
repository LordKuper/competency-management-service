import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, App, Button, Flex, Steps, Table, Tag, Typography } from "antd";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { useBlocker, useNavigate, useSearchParams } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EmployeeForm } from "../org-structure/EmployeeForm";
import {
  type Employee,
  type EmployeeInput,
  invalidateOrgStructure,
} from "../org-structure/orgStructureApi";
import type { UserResponse } from "../users/usersApi";
import { CreateAccountForm } from "./CreateAccountForm";
import { deskPaths } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import { useReturnLink } from "./returnLink";
import { ScenarioPage } from "./ScenarioPage";

interface AddedPosition {
  id: string;
  name: string;
}

/** What the scenario has done so far: the employee, the position added for them, and the account (null when skipped). */
interface Outcome {
  employee: Employee;
  position?: AddedPosition;
  account?: UserResponse | null;
}

const SAVING_MESSAGE =
  "Идёт сохранение сотрудника. Дождитесь ответа, чтобы перейти на другой экран.";

/** «Принять сотрудника»: the employee card, then an account with an invitation if wanted, then what was created. */
export function OnboardEmployeePage() {
  const [searchParams] = useSearchParams();
  const unitId = searchParams.get("unitId") ?? undefined;
  const [outcome, setOutcome] = useState<Outcome>();
  const [round, setRound] = useState(0);
  const step = !outcome ? 0 : outcome.account === undefined ? 1 : 2;

  return (
    <ScenarioPage title="Принять сотрудника">
      <Steps
        current={step}
        items={[
          { title: "Сотрудник" },
          { title: "Учётная запись" },
          { title: "Итог" },
        ]}
      />
      {!outcome && (
        <EmployeeStep key={round} unitId={unitId} onCreated={setOutcome} />
      )}
      {outcome && outcome.account === undefined && (
        <AccountStep outcome={outcome} onDone={setOutcome} />
      )}
      {outcome && outcome.account !== undefined && (
        <SummaryStep
          outcome={outcome}
          onAnother={() => {
            setOutcome(undefined);
            setRound((count) => count + 1);
          }}
        />
      )}
    </ScenarioPage>
  );
}

interface EmployeeStepProps {
  unitId?: string;
  onCreated: (outcome: Outcome) => void;
}

function EmployeeStep({ unitId, onCreated }: EmployeeStepProps) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { message } = App.useApp();
  const added = useRef<AddedPosition>(undefined);
  const create = useMutation({
    mutationFn: async (input: EmployeeInput) =>
      unwrap(await api.POST("/api/v1/employees", { body: input })),
    onSettled: () => invalidateOrgStructure(queryClient),
  });
  const blocker = useBlocker(create.isPending);
  useEffect(() => {
    if (blocker.state === "blocked") {
      message.warning(SAVING_MESSAGE);
      blocker.reset();
    }
  }, [blocker, message]);

  return (
    <Flex vertical gap="middle">
      {create.isError && (
        <ErrorAlert title="Сотрудник не создан" error={create.error} />
      )}
      <EmployeeForm
        initialValues={{ orgUnitId: unitId }}
        onPositionAdded={(position) => {
          added.current = position;
        }}
        submitLabel="Создать сотрудника и продолжить"
        onSubmit={async (input) => {
          const employee = await create.mutateAsync(input);
          onCreated({
            employee,
            position:
              added.current?.id === input.positionId
                ? added.current
                : undefined,
          });
        }}
        onCancel={() => void navigate(deskPaths.desk)}
      />
    </Flex>
  );
}

function AccountStep({
  outcome,
  onDone,
}: {
  outcome: Outcome;
  onDone: (outcome: Outcome) => void;
}) {
  const { employee } = outcome;
  return (
    <Flex vertical gap="middle">
      <Alert
        type="success"
        showIcon
        title={`Сотрудник создан: ${employee.fullName}, ${employee.orgUnitName}, ${employee.position}.`}
        description="Выход из сценария сотрудника не отменяет: он уже в оргструктуре."
      />
      <Typography.Paragraph>
        Учётная запись нужна, чтобы сотрудник входил в систему. Роль —
        «Пользователь»; роль администратора назначается в разделе
        «Пользователи». Шаг можно пропустить и создать запись позже.
      </Typography.Paragraph>
      <CreateAccountForm
        employeeId={employee.id}
        submitLabel="Создать учётную запись и отправить приглашение"
        cancelLabel="Пропустить"
        onCreated={(account) => onDone({ ...outcome, account })}
        onCancel={() => onDone({ ...outcome, account: null })}
      />
    </Flex>
  );
}

interface ResultRow {
  key: string;
  tag: ReactNode;
  text: string;
}

function resultRows({ employee, position, account }: Outcome): ResultRow[] {
  const rows: ResultRow[] = [
    {
      key: "employee",
      tag: <Tag color="success">Создан</Tag>,
      text: `Сотрудник: ${employee.fullName}, ${employee.orgUnitName}`,
    },
  ];
  if (position) {
    rows.push({
      key: "position",
      tag: <Tag color="success">Добавлена</Tag>,
      text: `Должность «${position.name}» в перечне подразделения`,
    });
  }
  if (!account) {
    rows.push({
      key: "account",
      tag: <Tag>Не создана</Tag>,
      text: "Учётная запись: её можно создать позже в «Администрирование → Пользователи»",
    });
    return rows;
  }
  rows.push({
    key: "account",
    tag: <Tag color="success">Создана</Tag>,
    text: `Учётная запись: ${account.email}`,
  });
  rows.push(
    account.mailSent === false
      ? {
          key: "mail",
          tag: <Tag color="warning">Не отправлено</Tag>,
          text: "Почтовый сервер не принял письмо. Сотрудник приглашения не получил. Отправьте приглашение повторно из «Администрирование → Пользователи» или с рабочего стола, когда там появится пункт «Приглашение не принято».",
        }
      : {
          key: "mail",
          tag: <Tag color="success">Отправлено</Tag>,
          text: `Письмо с приглашением ушло на ${account.email}`,
        },
  );
  return rows;
}

function SummaryStep({
  outcome,
  onAnother,
}: {
  outcome: Outcome;
  onAnother: () => void;
}) {
  const back = useReturnLink();
  return (
    <Flex vertical gap="middle">
      <Typography.Title level={2} style={{ margin: 0 }}>
        Итог
      </Typography.Title>
      <Table
        size="small"
        pagination={false}
        showHeader={false}
        rowKey="key"
        dataSource={resultRows(outcome)}
        columns={[{ dataIndex: "tag", width: 160 }, { dataIndex: "text" }]}
      />
      <Flex gap="small" wrap>
        <LinkButton to={deskPaths.desk} type="primary">
          К рабочему столу
        </LinkButton>
        {back && <LinkButton to={back.to}>{back.label}</LinkButton>}
        <Button onClick={onAnother}>Принять ещё одного сотрудника</Button>
      </Flex>
    </Flex>
  );
}
