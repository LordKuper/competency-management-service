import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Button, Skeleton, Table, Tag } from "antd";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { api } from "../../api/client";
import { ifMatchOf } from "../../api/ifMatch";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  type LeaveEdits,
  NO_LEAVE_EDITS,
  useLeaveGuard,
} from "../../app/leaveGuard";
import { isStaleVersion } from "../../app/useEditBase";
import { useIsAdmin } from "../auth/useCurrentUser";
import { EmployeeForm } from "../org-structure/EmployeeForm";
import {
  type Employee,
  type EmployeeInput,
  employeeInputOf,
  employeeQuery,
  invalidateOrgStructure,
  orgUnitTreeQuery,
} from "../org-structure/orgStructureApi";
import {
  invalidatePositions,
  positionQuery,
  unitPositionsQuery,
} from "../positions/positionsApi";
import type { UserResponse } from "../users/usersApi";
import { CreateAccountForm } from "./CreateAccountForm";
import { deskPaths } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import { useReturnLink } from "./returnLink";
import { ActionBar, CompactAlert, FormColumn } from "./ScenarioContext";
import { ScenarioPage } from "./ScenarioPage";
import { StepRuler } from "./StepRuler";

interface AddedPosition {
  id: string;
  name: string;
}

/** The unit and position a finding of the organisation queue opens the scenario with; only still-active ones are kept. */
interface Selection {
  unitId?: string;
  position?: AddedPosition;
}

/** Reads the requested unit and position from the address and keeps what is still active; a refused or unreadable one is dropped silently. */
function useSelection(): Selection & { isPending: boolean } {
  const [searchParams] = useSearchParams();
  const requestedUnit = searchParams.get("unitId") ?? undefined;
  const requestedPosition = searchParams.get("positionId") ?? undefined;
  const tree = useQuery({
    ...orgUnitTreeQuery,
    enabled: requestedUnit !== undefined,
  });
  const position = useQuery({
    ...positionQuery(requestedPosition ?? ""),
    enabled: requestedPosition !== undefined,
  });
  const isUnitActive = tree.data?.some(
    (unit) => unit.id === requestedUnit && unit.isActive,
  );
  return {
    isPending: tree.isLoading || position.isLoading,
    unitId: isUnitActive ? requestedUnit : undefined,
    position: position.data?.isActive
      ? { id: position.data.id, name: position.data.name }
      : undefined,
  };
}

/** What the scenario has done so far: the employee, the position added for them, and the account (null when skipped). */
interface Outcome {
  employee: Employee;
  position?: AddedPosition;
  account?: UserResponse | null;
}

/** The outcome with the employee as last read, when the card changed after the scenario saved it. */
function withLatestEmployee(
  outcome: Outcome,
  read: Employee | undefined,
): Outcome {
  const isNewer =
    read?.id === outcome.employee.id &&
    (read.version ?? 0) > (outcome.employee.version ?? 0);
  return isNewer ? { ...outcome, employee: read } : outcome;
}

const STEPS = ["Сотрудник", "Учётная запись", "Итог"] as const;
const EMPLOYEE_STEP = 1;
const ACCOUNT_STEP = 2;
const SUMMARY_STEP = 3;

/**
 * «Принять сотрудника»: the employee card, then an account with an invitation if wanted, then what was created.
 * The created employee is reread only after a save refused for a stale version, so no background refresh replaces input on step 1.
 */
export function OnboardEmployeePage() {
  const requested = useSelection();
  const [outcome, setOutcome] = useState<Outcome>();
  const [isRefused, setIsRefused] = useState(false);
  const reread = useQuery({
    ...employeeQuery(outcome?.employee.id ?? ""),
    enabled: outcome !== undefined && isRefused,
  });
  const latest = outcome && withLatestEmployee(outcome, reread.data);
  if (isRefused && latest !== outcome) {
    setOutcome(latest);
    setIsRefused(false);
  }
  const [step, setStep] = useState(EMPLOYEE_STEP);
  const [round, setRound] = useState(0);
  const [edits, setEdits] = useState(NO_LEAVE_EDITS);
  const { confirmLeave, leaveAfterSave } = useLeaveGuard(edits);
  const navigate = useNavigate();
  const origin = useReturnLink();
  const selection: Selection = round === 0 ? requested : {};
  const reached = outcome?.account === undefined ? EMPLOYEE_STEP : SUMMARY_STEP;

  function goTo(next: number) {
    setEdits(NO_LEAVE_EDITS);
    setStep(next);
  }

  return (
    <ScenarioPage title="Принять сотрудника">
      <StepRuler
        steps={STEPS}
        current={step}
        reached={reached}
        isLocked={edits.isSaving}
        onChange={(next) => confirmLeave(() => goTo(next))}
      />
      {step === EMPLOYEE_STEP && requested.isPending && <Skeleton active />}
      {step === EMPLOYEE_STEP && !requested.isPending && (
        <EmployeeStep
          key={round}
          selection={selection}
          outcome={outcome}
          onSaved={(next) => {
            setOutcome(next);
            setIsRefused(false);
            goTo(ACCOUNT_STEP);
          }}
          onStaleVersion={() => setIsRefused(true)}
          onEditsChange={setEdits}
          onCancel={() =>
            leaveAfterSave(() => void navigate(origin?.to ?? deskPaths.desk))
          }
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
          onEditsChange={setEdits}
        />
      )}
      {step === SUMMARY_STEP && outcome && (
        <SummaryStep
          outcome={outcome}
          onBack={() => goTo(ACCOUNT_STEP)}
          onAnother={() => {
            setOutcome(undefined);
            setIsRefused(false);
            setRound((count) => count + 1);
            goTo(EMPLOYEE_STEP);
          }}
        />
      )}
    </ScenarioPage>
  );
}

function selectionHints({ unitId, position }: Selection) {
  if (!unitId && !position) return undefined;
  return {
    orgUnit: unitId ? "Выбрано из находки; можно выбрать другое." : undefined,
    position: position
      ? "Выбрана из находки; можно выбрать другую. Если её нет в перечне подразделения, она будет добавлена в него."
      : "Нужной должности нет в перечне подразделения — добавьте её в списке.",
  };
}

/** The input as text that does not depend on the order of the legal entities and of the secondary assignments. */
function inputKey(input: EmployeeInput): string {
  return JSON.stringify([
    input.lastName,
    input.firstName,
    input.middleName ?? "",
    input.orgUnitId,
    input.positionId,
    [...input.legalEntityIds].sort(),
    input.assignments
      .map(({ orgUnitId, positionId, legalEntityIds }) =>
        JSON.stringify([orgUnitId, positionId, [...legalEntityIds].sort()]),
      )
      .sort(),
  ]);
}

function isChanged(employee: Employee, input: EmployeeInput): boolean {
  return inputKey(employeeInputOf(employee)) !== inputKey(input);
}

interface EmployeeStepProps {
  selection: Selection;
  /** The employee already created; the step then shows their saved values and saves a change to the same card. */
  outcome?: Outcome;
  onSaved: (outcome: Outcome) => void;
  /** Told when a save is refused because the card changed since the version it sent. */
  onStaleVersion: () => void;
  onEditsChange: (edits: LeaveEdits) => void;
  /** Explicit discard: leaves the scenario without asking; offered only until the employee is created. */
  onCancel: () => void;
}

function EmployeeStep({
  selection,
  outcome,
  onSaved,
  onStaleVersion,
  onEditsChange,
  onCancel,
}: EmployeeStepProps) {
  const queryClient = useQueryClient();
  const added = useRef<AddedPosition>(undefined);
  const [isDirty, setIsDirty] = useState(false);
  const save = useMutation({
    mutationFn: async (input: EmployeeInput) => {
      const { position } = selection;
      if (position?.id === input.positionId) {
        await allowInUnit(position, input.orgUnitId);
      }
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
    onError: (error) => {
      if (isStaleVersion(error)) onStaleVersion();
    },
    onSettled: () =>
      Promise.all([
        invalidateOrgStructure(queryClient),
        invalidatePositions(queryClient),
      ]),
  });
  async function allowInUnit(position: AddedPosition, orgUnitId: string) {
    const allowed = await queryClient.fetchQuery({
      ...unitPositionsQuery(orgUnitId),
      staleTime: 0,
    });
    if (allowed.some((entry) => entry.positionId === position.id)) return;
    unwrap(
      await api.POST("/api/v1/org-units/{id}/positions", {
        params: { path: { id: orgUnitId } },
        body: { positionId: position.id },
      }),
    );
    added.current = position;
  }
  const isSaving = save.isPending;
  useEffect(
    () => onEditsChange({ isUnsaved: isDirty, isSaving }),
    [isDirty, isSaving, onEditsChange],
  );

  const employee = outcome?.employee;
  const [formBase, setFormBase] = useState(employee?.version);
  if (formBase !== employee?.version) {
    setFormBase(employee?.version);
    setIsDirty(false);
  }
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
        key={employee?.version}
        initialValues={
          employee
            ? employeeInputOf(employee)
            : {
                orgUnitId: selection.unitId,
                positionId: selection.position?.id,
              }
        }
        preselectedPosition={selection.position}
        hints={selectionHints(selection)}
        position={
          employee && { id: employee.positionId, name: employee.position }
        }
        saved={employee}
        onDirtyChange={setIsDirty}
        onCancel={employee ? undefined : onCancel}
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
      />
    </FormColumn>
  );
}

interface AccountStepProps {
  outcome: Outcome;
  onDone: (outcome: Outcome) => void;
  onBack: () => void;
  onEditsChange: (edits: LeaveEdits) => void;
}

function AccountStep({
  outcome,
  onDone,
  onBack,
  onEditsChange,
}: AccountStepProps) {
  const { employee, account } = outcome;
  const isAdmin = useIsAdmin();
  const [edits, setEdits] = useState(NO_LEAVE_EDITS);
  const { confirmLeave } = useLeaveGuard();
  useEffect(() => onEditsChange(edits), [edits, onEditsChange]);

  return (
    <FormColumn>
      <CompactAlert
        type="success"
        lead="Сотрудник создан:"
        text={`${employee.fullName}, ${employee.orgUnitName}, ${employee.position}.`}
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
      ) : !isAdmin ? (
        <>
          <CompactAlert
            type="info"
            lead="Учётную запись создаёт администратор."
            text="Сотрудник уже сохранён: попросите администратора создать для него учётную запись."
          />
          <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
            <Button
              type="primary"
              onClick={() => onDone({ ...outcome, account: null })}
            >
              Далее
            </Button>
          </ActionBar>
        </>
      ) : (
        <CreateAccountForm
          employeeId={employee.id}
          submitLabel="Создать учётную запись и отправить приглашение"
          onEditsChange={setEdits}
          onCreated={(created) => onDone({ ...outcome, account: created })}
          scenarioActions={{
            start: (
              <Button
                disabled={edits.isSaving}
                onClick={() => confirmLeave(onBack)}
              >
                Назад
              </Button>
            ),
            end: (
              <Button
                disabled={edits.isSaving}
                onClick={() =>
                  confirmLeave(() => onDone({ ...outcome, account: null }))
                }
              >
                Пропустить
              </Button>
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
      columns={[
        { dataIndex: "tag", width: "calc(var(--ant-control-height) * 5)" },
        { dataIndex: "text" },
      ]}
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

function resultRows(
  { employee, position, account }: Outcome,
  isAdmin: boolean,
): ResultRow[] {
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
      text: isAdmin
        ? "Учётная запись: её можно создать позже в «Администрирование → Пользователи»"
        : "Учётная запись: её создаёт администратор",
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
  const isAdmin = useIsAdmin();
  return (
    <FormColumn>
      <ResultTable rows={resultRows(outcome, isAdmin)} />
      <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
        <Button onClick={onAnother}>Принять ещё одного сотрудника</Button>
        <LinkButton to={deskPaths.desk} type="primary">
          К рабочему столу
        </LinkButton>
      </ActionBar>
    </FormColumn>
  );
}
