import { type UseQueryResult, useQuery } from "@tanstack/react-query";
import { Button, Card, Flex, Skeleton, Tag, Typography } from "antd";
import { type ReactNode, useState } from "react";
import { useNavigate, useParams } from "react-router";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { UnitHrModal } from "../org-structure/UnitHrModal";
import { UnitPositionsModal } from "../org-structure/UnitPositionsModal";
import { useBlockUser } from "../users/useBlockUser";
import { AccountModal } from "./AccountModal";
import { AssignHeadModal } from "./AssignHeadModal";
import { type UnitSetup, unitSetupQuery } from "./deskApi";
import { deskPaths } from "./deskTexts";
import { LinkButton } from "./LinkButton";
import { CompactAlert, ContextLine } from "./ScenarioContext";
import { LoadFailure, ScenarioPage } from "./ScenarioPage";
import {
  type InvitationResend,
  useResendInvitation,
} from "./useResendInvitation";

type Row = UnitSetup["rows"][number];
type Account = NonNullable<UnitSetup["headAccount"]>;
type AccountRow = "HeadAccount" | "HrAccount";
type Dialog = "head" | "headAccount" | "hr" | "hrAccount" | "positions" | null;

const ACCOUNT_DIALOGS: Record<AccountRow, Dialog> = {
  HeadAccount: "headAccount",
  HrAccount: "hrAccount",
};

interface RowContent {
  value: ReactNode;
  note?: string;
  action?: ReactNode;
}

const ROW_TITLES: Record<Row["row"], string> = {
  Head: "Руководитель",
  HeadAccount: "Учётная запись руководителя",
  Hr: "Ответственный HR",
  HrAccount: "Учётная запись HR",
  Positions: "Перечень должностей",
};

const WAITING_TAGS: Partial<Record<Row["row"], string>> = {
  HeadAccount: "Ждёт руководителя",
  HrAccount: "Ждёт HR",
};

const WAITING_TEXTS: Partial<Record<Row["row"], string>> = {
  HeadAccount: "Появится после назначения руководителя.",
  HrAccount: "Появится после закрепления HR.",
};

const MISSING_TEXTS: Record<Row["row"], string> = {
  Head: "руководителя",
  HeadAccount: "учётной записи руководителя",
  Hr: "ответственного HR",
  HrAccount: "учётной записи HR",
  Positions: "перечня должностей",
};

/** «Нет руководителя, ответственного HR и перечня должностей.» for the rows that lack something. */
function missingText(rows: readonly Row[]): string {
  const names = rows
    .filter((row) => row.status === "Missing")
    .map((row) => MISSING_TEXTS[row.row]);
  const last = names.pop();
  return `Нет ${names.length > 0 ? `${names.join(", ")} и ` : ""}${last}.`;
}

const NO_CANDIDATES_NOTE =
  "некого закрепить: нужен работающий сотрудник с действующей учётной записью и ролью «HR». Выдайте роль в разделе «Пользователи», проверив привязку записи к сотруднику; пробел закроется после этого.";

const HR_INACTIVE_CAUSES: Record<Account["state"], string> = {
  None: "у него нет учётной записи",
  Invited: "его учётная запись не приняла приглашение",
  Blocked: "его учётная запись заблокирована",
  NoHrRole: "у его учётной записи нет роли «HR»",
  Active: "его учётная запись не может действовать",
};

const ACCOUNT_STATES: Record<Account["state"], string> = {
  None: "нет учётной записи",
  Invited: "Приглашена: пароль не задан, вход невозможен",
  Blocked: "Заблокирована: вход невозможен",
  NoHrRole: "Нет роли «HR»: закрепление не действует",
  Active: "Действует",
};

/** «Подключить подразделение»: what the unit lacks to work, row by row, and the action that closes each gap on the spot. */
export function ConnectUnitPage() {
  const { unitId } = useParams();
  const navigate = useNavigate();
  const resend = useResendInvitation();
  const setup = useQuery({
    ...unitSetupQuery(unitId ?? ""),
    enabled: unitId !== undefined,
  });
  return (
    <ScenarioPage title="Подключить подразделение">
      <ContextLine>
        <Typography.Text strong id="connect-unit-label">
          Подразделение
        </Typography.Text>
        <div className="context-select">
          <OrgUnitSelect
            value={unitId}
            placeholder="Выберите подразделение"
            disabled={resend.sendingAccountId !== null}
            onChange={(id) => id && void navigate(deskPaths.connectUnitOf(id))}
          />
        </div>
        {setup.data && (
          <Typography.Text type="secondary">
            {setup.data.unit.map((part) => part.name).join(" → ")}
          </Typography.Text>
        )}
      </ContextLine>
      {resend.sendingAccountId !== null && (
        <Typography.Text type="secondary">
          Пока идёт отправка письма, выбор подразделения и повторная отправка в
          других строках недоступны.
        </Typography.Text>
      )}
      {unitId ? (
        <SetupScreen
          key={unitId}
          unitId={unitId}
          setup={setup}
          resend={resend}
        />
      ) : (
        <Typography.Text type="secondary">
          Выберите подразделение, чтобы увидеть, чего ему не хватает.
        </Typography.Text>
      )}
    </ScenarioPage>
  );
}

interface SetupScreenProps {
  unitId: string;
  setup: UseQueryResult<UnitSetup>;
  resend: InvitationResend;
}

function SetupScreen({ unitId, setup, resend }: SetupScreenProps) {
  if (setup.error) {
    return (
      <LoadFailure
        title="Не удалось загрузить подразделение"
        error={setup.error}
        onRetry={() => void setup.refetch()}
      />
    );
  }
  if (!setup.data) return <Skeleton active />;
  return <SetupRows unitId={unitId} data={setup.data} resend={resend} />;
}

interface SetupRowsProps {
  unitId: string;
  data: UnitSetup;
  resend: InvitationResend;
}

function SetupRows({ unitId, data, resend }: SetupRowsProps) {
  const [dialog, setDialog] = useState<Dialog>(null);
  const confirmBlock = useBlockUser();
  const unit = { id: unitId, name: data.unit.at(-1)?.name ?? "" };
  const gaps = data.rows.filter((row) => row.status === "Missing").length;
  const close = () => setDialog(null);
  const open = (target: Dialog, label: string) => (
    <Button onClick={() => setDialog(target)}>{label}</Button>
  );

  function accountAction(row: AccountRow, account: Account | null): ReactNode {
    if (!account) return null;
    if (account.state === "None") {
      const owner = row === "HeadAccount" ? data.head : data.hr;
      return (
        owner && open(ACCOUNT_DIALOGS[row], "Создать и отправить приглашение")
      );
    }
    if (account.id === null || account.version === null) return null;
    const { id, email, version } = account;
    if (account.state === "Invited") {
      return (
        <Button
          disabled={resend.sendingAccountId !== null}
          loading={resend.sendingAccountId === id}
          onClick={() => resend.send({ accountId: id, version })}
        >
          Отправить приглашение повторно
        </Button>
      );
    }
    if (account.state === "Blocked") {
      return (
        <Button
          onClick={() =>
            confirmBlock({ id, email: email ?? "", version, isBlocked: true })
          }
        >
          Разблокировать
        </Button>
      );
    }
    if (account.state === "NoHrRole") {
      return <LinkButton to={deskPaths.users}>Пользователи</LinkButton>;
    }
    return null;
  }

  function describeHr(row: Row): RowContent {
    const { hr } = data;
    if (!hr) {
      if (row.reason === "NoHrCandidates") {
        return {
          value: "Не назначен",
          note: NO_CANDIDATES_NOTE,
          action: <LinkButton to={deskPaths.users}>Пользователи</LinkButton>,
        };
      }
      return {
        value: "Не назначен: ни своего, ни унаследованного закрепления",
        action: open("hr", "Закрепить HR"),
      };
    }
    const value =
      hr.orgUnitId === unitId
        ? hr.name
        : `${hr.name} (унаследован из «${hr.orgUnitName}»)`;
    if (row.status === "Ready") return { value };
    const why = HR_INACTIVE_CAUSES[hr.account?.state ?? "None"];
    return {
      value: `Закреплён: ${value}, но ${why} и действовать не может, поэтому ответственного HR нет`,
      action: open("hr", "Закрепить другого HR"),
    };
  }

  function describePositions(row: Row): RowContent {
    const { own, inherited } = data.positions;
    if (row.status === "Ready") {
      return {
        value: `Своих: ${own.length} (${own.map((position) => position.name).join(", ") || "—"}) · унаследованных: ${inherited.length}`,
        action: open("positions", "Изменить перечень"),
      };
    }
    return {
      value: `Пуст: ни своих, ни унаследованных должностей, а сотрудников ${data.employees}`,
      note:
        data.employees === 0
          ? "В подразделении нет сотрудников, поэтому на рабочем столе этого пункта нет."
          : undefined,
      action: open("positions", "Добавить должность"),
    };
  }

  function describe(row: Row): RowContent {
    switch (row.row) {
      case "Head":
        return data.head
          ? { value: data.head.fullName }
          : {
              value: "Не назначен",
              note: `Руководителем может быть только работающий сотрудник этого подразделения. Сотрудников: ${data.employees}.`,
              action: open("head", "Назначить руководителя"),
            };
      case "HeadAccount":
      case "HrAccount": {
        const account =
          row.row === "HeadAccount" ? data.headAccount : data.hr?.account;
        return {
          value: account?.email
            ? `${account.email} — ${ACCOUNT_STATES[account.state]}`
            : "Нет учётной записи",
          action: accountAction(row.row, account ?? null),
        };
      }
      case "Hr":
        return describeHr(row);
      case "Positions":
        return describePositions(row);
    }
  }

  return (
    <Flex vertical gap="middle">
      {gaps === 0 ? (
        <CompactAlert
          type="success"
          lead="Подразделение подключено."
          text="Пробелов нет: руководитель, ответственный HR, должности и действующие учётные записи на месте."
        />
      ) : (
        <CompactAlert
          type="warning"
          lead={`Не хватает: ${gaps} из ${data.rows.length}.`}
          text={missingText(data.rows)}
        />
      )}
      <Card size="small">
        <Flex vertical>
          {data.rows.map((row, index) => {
            const { value, note, action } = describe(row);
            const isWaiting = row.status === "Waiting";
            return (
              <Flex
                key={row.row}
                vertical
                gap="small"
                style={{
                  paddingBlock: "var(--ant-padding-xs)",
                  borderTop:
                    index === 0
                      ? undefined
                      : "var(--ant-line-width) var(--ant-line-type) var(--ant-color-border-secondary)",
                }}
              >
                <Flex justify="space-between" align="center" gap="small" wrap>
                  <Flex align="center" gap="small" wrap>
                    <Typography.Text strong>
                      {ROW_TITLES[row.row]}
                    </Typography.Text>
                    <StatusTag row={row} />
                  </Flex>
                  {!isWaiting && action}
                </Flex>
                <Typography.Text>
                  {isWaiting
                    ? WAITING_TEXTS[row.row]
                    : [value, note].filter(Boolean).join(" · ")}
                </Typography.Text>
              </Flex>
            );
          })}
        </Flex>
      </Card>
      {dialog === "head" && <AssignHeadModal unitId={unitId} onClose={close} />}
      {dialog === "hr" && <UnitHrModal unit={unit} onClose={close} />}
      {dialog === "positions" && (
        <UnitPositionsModal unit={unit} onClose={close} />
      )}
      {dialog === "headAccount" && data.head && (
        <AccountModal employee={data.head} addressee="head" onClose={close} />
      )}
      {dialog === "hrAccount" && data.hr && (
        <AccountModal
          employee={{ id: data.hr.employeeId, fullName: data.hr.name }}
          addressee="hr"
          onClose={close}
        />
      )}
    </Flex>
  );
}

function StatusTag({ row }: { row: Row }) {
  switch (row.status) {
    case "Ready":
      return <Tag color="success">Готово</Tag>;
    case "Missing":
      return <Tag color="warning">Не хватает</Tag>;
    case "Waiting":
      return <Tag>{WAITING_TAGS[row.row] ?? "Ждёт"}</Tag>;
  }
}
