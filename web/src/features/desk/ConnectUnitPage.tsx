import { useQuery } from "@tanstack/react-query";
import { Alert, Button, Card, Flex, Skeleton, Tag, Typography } from "antd";
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
import { LoadFailure, ScenarioPage } from "./ScenarioPage";
import { useResendInvitation } from "./useResendInvitation";

type Row = UnitSetup["rows"][number];
type Account = NonNullable<UnitSetup["headAccount"]>;
type Dialog = "head" | "account" | "hr" | "positions" | null;

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

const ACCOUNT_STATES: Record<Account["state"], string> = {
  None: "нет учётной записи",
  Invited: "Приглашена: пароль не задан, вход невозможен",
  Blocked: "Заблокирована: вход невозможен",
  Active: "Действует",
};

/** «Подключить подразделение»: what the unit lacks to work, row by row, and the action that closes each gap on the spot. */
export function ConnectUnitPage() {
  const { unitId } = useParams();
  const navigate = useNavigate();
  return (
    <ScenarioPage title="Подключить подразделение">
      <Flex vertical gap="small">
        <Typography.Text strong id="connect-unit-label">
          Подразделение
        </Typography.Text>
        <OrgUnitSelect
          value={unitId}
          placeholder="Выберите подразделение"
          onChange={(id) => id && void navigate(deskPaths.connectUnitOf(id))}
        />
      </Flex>
      {unitId ? (
        <SetupScreen key={unitId} unitId={unitId} />
      ) : (
        <Typography.Paragraph type="secondary">
          Выберите подразделение, чтобы увидеть, чего ему не хватает.
        </Typography.Paragraph>
      )}
    </ScenarioPage>
  );
}

function SetupScreen({ unitId }: { unitId: string }) {
  const setup = useQuery(unitSetupQuery(unitId));
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
  return <SetupRows unitId={unitId} data={setup.data} />;
}

function SetupRows({ unitId, data }: { unitId: string; data: UnitSetup }) {
  const [dialog, setDialog] = useState<Dialog>(null);
  const resend = useResendInvitation();
  const confirmBlock = useBlockUser();
  const unit = { id: unitId, name: data.unit.at(-1)?.name ?? "" };
  const gaps = data.rows.filter((row) => row.status === "Missing").length;
  const close = () => setDialog(null);
  const open = (target: Dialog, label: string) => (
    <Button onClick={() => setDialog(target)}>{label}</Button>
  );

  function accountAction(account: Account | null): ReactNode {
    if (!account) return null;
    if (account.state === "None" && data.head) {
      return open("account", "Создать и отправить приглашение");
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
    return null;
  }

  function describeHr(row: Row): RowContent {
    const { hr, hrAccount } = data;
    if (!hr) {
      return {
        value: "Не назначен: ни своего, ни унаследованного закрепления",
        action: open("hr", "Закрепить HR"),
      };
    }
    const place =
      hr.orgUnitId === unitId ? "свой" : `унаследован из «${hr.orgUnitName}»`;
    const value = `${hr.name} (${place})`;
    if (row.status === "Ready") return { value };
    const why =
      hrAccount?.state === "Invited"
        ? "его учётная запись не приняла приглашение"
        : "его учётная запись заблокирована";
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
          row.row === "HeadAccount" ? data.headAccount : data.hrAccount;
        return {
          value: account?.email
            ? `${account.email} — ${ACCOUNT_STATES[account.state]}`
            : "Нет учётной записи",
          action: accountAction(account ?? null),
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
      <Typography.Text type="secondary">
        {data.unit.map((part) => part.name).join(" → ")}
      </Typography.Text>
      {gaps === 0 ? (
        <Alert
          type="success"
          showIcon
          title="Подразделение подключено"
          description={`Пробелов нет: руководитель, ответственный HR${data.hr && data.hr.orgUnitId !== unitId ? ` (унаследованный из «${data.hr.orgUnitName}»: ${data.hr.name})` : ""}, должности и действующие учётные записи на месте.`}
        />
      ) : (
        <Alert
          type="warning"
          showIcon
          title={`Не хватает: ${gaps} из ${data.rows.length}`}
          description="Закройте пробелы ниже: у каждой строки есть действие."
        />
      )}
      <Card>
        <Flex vertical>
          {data.rows.map((row, index) => {
            const { value, note, action } = describe(row);
            const isWaiting = row.status === "Waiting";
            return (
              <Flex
                key={row.row}
                vertical
                gap={4}
                style={{
                  paddingBlock: "var(--ant-padding-sm)",
                  borderTop:
                    index === 0
                      ? undefined
                      : "var(--ant-line-width) var(--ant-line-type) var(--ant-color-border-secondary)",
                }}
              >
                <Flex align="center" gap="small" wrap>
                  <Typography.Text strong>
                    {ROW_TITLES[row.row]}
                  </Typography.Text>
                  <StatusTag row={row} />
                </Flex>
                <Typography.Text>
                  {isWaiting ? WAITING_TEXTS[row.row] : value}
                </Typography.Text>
                {!isWaiting && note && (
                  <Typography.Text type="secondary">{note}</Typography.Text>
                )}
                {!isWaiting && action && <div>{action}</div>}
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
      {dialog === "account" && data.head && (
        <AccountModal head={data.head} onClose={close} />
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
