import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Button, Table, Tag, Typography } from "antd";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { useBlocker, useSearchParams } from "react-router";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
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
import { ActionBar, CompactAlert, FormColumn } from "./ScenarioContext";
import { ScenarioPage } from "./ScenarioPage";
import { StepRuler } from "./StepRuler";

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

const STEPS = ["Сотрудник", "Учётная запись", "Итог"] as const;
const EMPLOYEE_STEP = 1;
const ACCOUNT_STEP = 2;
const SUMMARY_STEP = 3;

const UNSAVED_MESSAGE =
  "Есть несохранённые правки. Сохраните их, чтобы перейти на другой экран.";

/** «Принять сотрудника»: the employee card, then an account with an invitation if wanted, then what was created. */
export function OnboardEmployeePage() {
  const [searchParams] = useSearchParams();
  const unitId = searchParams.get("unitId") ?? undefined;
  const { message } = App.useApp();
  const [outcome, setOutcome] = useState<Outcome>();
  const [step, setStep] = useState(EMPLOYEE_STEP);
  const [round, setRound] = useState(0);
  const [isUnsaved, setIsUnsaved] = useState(false);
  const blocker = useBlocker(() => isUnsaved);
  useEffect(() => {
    if (blocker.state === "blocked") {
      message.warning(UNSAVED_MESSAGE);
      blocker.reset();
    }
  }, [blocker, message]);
  const reached = outcome?.account === undefined ? EMPLOYEE_STEP : SUMMARY_STEP;

  function goTo(next: number) {
    setIsUnsaved(false);
    setStep(next);
  }

  return (
    <ScenarioPage title="Принять сотрудника">
      <StepRuler
        steps={STEPS}
        current={step}
        reached={reached}
        isLocked={isUnsaved}
        onChange={goTo}
      />
      {step === EMPLOYEE_STEP && (
        <EmployeeStep
          key={round}
          unitId={unitId}
          outcome={outcome}
          onSaved={(saved) => {
            setOutcome(saved);
            goTo(ACCOUNT_STEP);
          }}
          onUnsavedChange={setIsUnsaved}
        />
      )}
      {step === ACCOUNT_STEP && outcome && (
        <AccountStep
          outcome={outcome}
          onDone={(done) => {
            setOutcome(done);
            goTo(SUMMARY_STEP);
          }}
          onBack={() => goTo(EMPLOYEE_STEP)}
          onUnsavedChange={setIsUnsaved}
        />
      )}
      {step === SUMMARY_STEP && outcome && (
        <SummaryStep
          outcome={outcome}
          onBack={() => goTo(ACCOUNT_STEP)}
          onAnother={() => {
            setOutcome(undefined);
            setRound((count) => count + 1);
            goTo(EMPLOYEE_STEP);
          }}
        />
      )}
    </ScenarioPage>
  );
}

function isChanged(employee: Employee, input: EmployeeInput): boolean {
  return (
    employee.lastName !== input.lastName ||
    employee.firstName !== input.firstName ||
    (employee.middleName ?? "") !== (input.middleName ?? "") ||
    employee.orgUnitId !== input.orgUnitId ||
    employee.positionId !== input.positionId
  );
}

interface EmployeeStepProps {
  unitId?: string;
  /** The employee already created; the step then shows their saved values and saves a change to the same card. */
  outcome?: Outcome;
  onSaved: (outcome: Outcome) => void;
  onUnsavedChange: (isUnsaved: boolean) => void;
}

function EmployeeStep({
  unitId,
  outcome,
  onSaved,
  onUnsavedChange,
}: EmployeeStepProps) {
  const queryClient = useQueryClient();
  const added = useRef<AddedPosition>(undefined);
  const [isDirty, setIsDirty] = useState(false);
  const save = useMutation({
    mutationFn: async (input: EmployeeInput) => {
      if (!outcome) {
        return unwrap(await api.POST("/api/v1/employees", { body: input }));
      }
      const { employee } = outcome;
      if (!isChanged(employee, input)) return employee;
      return unwrap(
        await api.PUT("/api/v1/employees/{id}", {
          params: {
            path: { id: employee.id },
            header: { "If-Match": ifMatchOf(employee.version ?? 0) },
          },
          body: input,
        }),
      );
    },
    onSettled: () => invalidateOrgStructure(queryClient),
  });
  useEffect(
    () => onUnsavedChange(isDirty || save.isPending),
    [isDirty, save.isPending, onUnsavedChange],
  );

  const employee = outcome?.employee;
  const positionOf = (positionId: string) =>
    added.current?.id === positionId
      ? added.current
      : outcome?.position?.id === positionId
        ? outcome.position
        : undefined;

  return (
    <FormColumn>
      {employee && (
        <CompactAlert
          type="info"
          lead="Сотрудник уже создан:"
          text="«Сохранить и далее» обновит его карточку, второго сотрудника не будет."
        />
      )}
      {save.isError && (
        <ErrorAlert
          title={
            employee
              ? "Карточка сотрудника не обновлена"
              : "Сотрудник не создан"
          }
          error={save.error}
        />
      )}
      <EmployeeForm
        initialValues={
          employee
            ? {
                lastName: employee.lastName,
                firstName: employee.firstName,
                middleName: employee.middleName ?? undefined,
                orgUnitId: employee.orgUnitId,
                positionId: employee.positionId,
              }
            : { orgUnitId: unitId }
        }
        position={
          employee && { id: employee.positionId, name: employee.position }
        }
        onDirtyChange={setIsDirty}
        onPositionAdded={(position) => {
          added.current = position;
        }}
        submitLabel={
          employee ? "Сохранить и далее" : "Создать сотрудника и продолжить"
        }
        onSubmit={async (input) => {
          const saved = await save.mutateAsync(input);
          onSaved({
            employee: saved,
            position: positionOf(input.positionId),
            account: outcome?.account,
          });
        }}
        scenarioActions={{
          end: employee && isDirty && !save.isPending && (
            <Typography.Text type="secondary">
              Есть несохранённые правки. Сохраните их, чтобы перейти на другой
              шаг.
            </Typography.Text>
          ),
        }}
      />
    </FormColumn>
  );
}

interface AccountStepProps {
  outcome: Outcome;
  onDone: (outcome: Outcome) => void;
  onBack: () => void;
  onUnsavedChange: (isUnsaved: boolean) => void;
}

function AccountStep({
  outcome,
  onDone,
  onBack,
  onUnsavedChange,
}: AccountStepProps) {
  const { employee, account } = outcome;
  const [isDirty, setIsDirty] = useState(false);
  useEffect(() => onUnsavedChange(isDirty), [isDirty, onUnsavedChange]);

  return (
    <FormColumn>
      <CompactAlert
        type="success"
        lead="Сотрудник создан:"
        text={`${employee.fullName}, ${employee.orgUnitName}, ${employee.position}. Выход из сценария его не отменяет.`}
      />
      {account ? (
        <>
          <ResultTable rows={accountRows(account)} />
          <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
            <Button
              type="primary"
              onClick={() => onDone({ ...outcome, account })}
            >
              Далее
            </Button>
          </ActionBar>
        </>
      ) : (
        <CreateAccountForm
          employeeId={employee.id}
          submitLabel="Создать учётную запись и отправить приглашение"
          onDirtyChange={setIsDirty}
          onCreated={(created) => onDone({ ...outcome, account: created })}
          scenarioActions={{
            start: (
              <Button disabled={isDirty} onClick={onBack}>
                Назад
              </Button>
            ),
            end: (
              <>
                {isDirty && (
                  <Typography.Text type="secondary">
                    Есть несохранённые правки. Создайте учётную запись или
                    пропустите шаг, чтобы вернуться.
                  </Typography.Text>
                )}
                <Button onClick={() => onDone({ ...outcome, account: null })}>
                  Пропустить
                </Button>
              </>
            ),
          }}
        />
      )}
    </FormColumn>
  );
}

interface ResultRow {
  key: string;
  tag: ReactNode;
  text: string;
}

function ResultTable({ rows }: { rows: ResultRow[] }) {
  return (
    <Table
      size="small"
      pagination={false}
      showHeader={false}
      rowKey="key"
      dataSource={rows}
      columns={[{ dataIndex: "tag", width: 160 }, { dataIndex: "text" }]}
    />
  );
}

function accountRows(account: UserResponse): ResultRow[] {
  return [
    {
      key: "account",
      tag: <Tag color="success">Создана</Tag>,
      text: `Учётная запись: ${account.email}`,
    },
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
  ];
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
  return [...rows, ...accountRows(account)];
}

interface SummaryStepProps {
  outcome: Outcome;
  onBack: () => void;
  onAnother: () => void;
}

function SummaryStep({ outcome, onBack, onAnother }: SummaryStepProps) {
  return (
    <FormColumn>
      <ResultTable rows={resultRows(outcome)} />
      <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
        <Button onClick={onAnother}>Принять ещё одного сотрудника</Button>
        <LinkButton to={deskPaths.desk} type="primary">
          К рабочему столу
        </LinkButton>
      </ActionBar>
    </FormColumn>
  );
}
