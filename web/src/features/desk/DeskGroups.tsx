import {
  CloseCircleOutlined,
  DownOutlined,
  RightOutlined,
  WarningOutlined,
} from "@ant-design/icons";
import {
  theme as antdTheme,
  Button,
  Flex,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { type CSSProperties, type ReactNode, useState } from "react";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import {
  type DeskGroupView,
  type DeskItem,
  FIRST_ITEMS,
  MORE_ITEMS,
  useDeskGroupItems,
} from "./deskApi";
import {
  formatDate,
  GROUPS,
  itemAction,
  itemLink,
  objectContext,
  objectTitle,
  reasonLines,
  relativeTime,
  typeLabelOf,
} from "./deskTexts";
import type { InvitationResend } from "./useResendInvitation";
import "./scenario.css";

const NUMBER = new Intl.NumberFormat("ru-RU");
const AGE_WIDTH = "calc(var(--ant-control-height) * 4)";
const LINK_TERM_WIDTH = "calc(var(--ant-control-height) * 5)";
const ACTIONS_WIDTH = "calc(var(--ant-control-height) * 6)";
function usePanelStyle(): CSSProperties {
  const { token } = antdTheme.useToken();
  return {
    background: token.colorBgContainer,
    border: `${token.lineWidth}px solid ${token.colorBorderSecondary}`,
    borderRadius: token.borderRadiusLG,
    overflow: "hidden",
  };
}

/** Bordered white panel the desk draws a group, the reference group and the loading placeholder on. */
export function DeskPanel({ children }: { children: ReactNode }) {
  return <div style={usePanelStyle()}>{children}</div>;
}

/** One group of expected actions: its title with the full count, a queue link where there is one, and its rows. */
export function ActionGroup({
  group,
  resend,
}: {
  group: DeskGroupView;
  resend: InvitationResend;
}) {
  const { token } = antdTheme.useToken();
  const meta = GROUPS[group.group];
  return (
    <DeskPanel>
      <GroupHeading
        level={4}
        title={meta.title}
        total={group.total}
        queue={meta.queue}
        style={{
          padding: `${token.padding}px ${token.padding}px ${token.paddingSM}px`,
        }}
      />
      <GroupRows group={group} resend={resend} />
    </DeskPanel>
  );
}

/**
 * The group "К сведению": what concerns the user but asks nothing of them, outside the count. Collapsed by default so
 * long reference lists do not push the actions away; the full count stays visible.
 */
export function ReferenceGroup({ groups }: { groups: DeskGroupView[] }) {
  const { token } = antdTheme.useToken();
  const [isOpen, setOpen] = useState(false);
  const total = groups.reduce((sum, group) => sum + group.total, 0);
  const divider = `${token.lineWidth}px solid ${token.colorBorderSecondary}`;
  return (
    <DeskPanel>
      <Flex
        justify="space-between"
        align="center"
        gap={token.margin}
        wrap
        style={{ padding: token.padding }}
      >
        <Flex align="center" gap={token.marginXS}>
          <Typography.Title level={4} style={{ margin: 0 }}>
            <Button
              type="text"
              icon={isOpen ? <DownOutlined /> : <RightOutlined />}
              aria-expanded={isOpen}
              onClick={() => setOpen(!isOpen)}
              style={{ font: "inherit", paddingInline: 0 }}
            >
              К сведению
            </Button>
          </Typography.Title>
          <Tag>{NUMBER.format(total)}</Tag>
        </Flex>
        <Typography.Text
          type="secondary"
          style={{ fontSize: token.fontSizeSM }}
        >
          Не требует вашего действия и не входит в счётчик
        </Typography.Text>
      </Flex>
      {isOpen &&
        groups.map((group) => {
          const meta = GROUPS[group.group];
          return (
            <div key={group.group} style={{ borderTop: divider }}>
              <GroupHeading
                level={5}
                title={meta.title}
                total={group.total}
                queue={meta.queue}
                style={{ padding: `${token.paddingSM}px ${token.padding}px` }}
              />
              <GroupRows group={group} />
            </div>
          );
        })}
    </DeskPanel>
  );
}

function GroupHeading({
  level,
  title,
  total,
  queue,
  style,
}: {
  level: 4 | 5;
  title: string;
  total: number;
  queue: { title: string; path: string } | undefined;
  style: CSSProperties;
}) {
  const { token } = antdTheme.useToken();
  return (
    <Flex
      justify="space-between"
      align="center"
      gap={token.margin}
      wrap
      style={style}
    >
      <Flex align="center" gap={token.marginXS}>
        <Typography.Title level={level} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        <Tag>{NUMBER.format(total)}</Tag>
      </Flex>
      {queue && (
        <Link to={queue.path}>
          {level === 4 ? "Открыть очередь" : queue.title}
        </Link>
      )}
    </Flex>
  );
}

/**
 * The rows of a group: the first ten, then twenty more per "Показать ещё", read again from the start since done items
 * leave. A group longer than the server returns says so under the table instead of offering more.
 */
function GroupRows({
  group,
  resend,
}: {
  group: DeskGroupView;
  resend?: InvitationResend;
}) {
  const { token } = antdTheme.useToken();
  const [limit, setLimit] = useState(FIRST_ITEMS);
  const more = useDeskGroupItems(group.group, limit);
  const items = more.data?.items ?? group.items;
  const total = more.data?.total ?? group.total;
  const isLoadingMore =
    more.isPlaceholderData || (more.isPending && more.isFetching);
  const requested = more.data ? limit : FIRST_ITEMS;
  const isTruncated =
    !isLoadingMore && items.length < requested && items.length < total;
  const isSending = resend?.sendingAccountId != null;
  const footerStyle: CSSProperties = {
    padding: `${token.paddingSM}px ${token.padding}px`,
    borderTop: `${token.lineWidth}px solid ${token.colorBorderSecondary}`,
  };
  return (
    <>
      <Table<DeskItem>
        rowKey={(item) => `${item.object.type}:${item.object.id}`}
        columns={columnsOf(group, resend)}
        dataSource={items}
        pagination={false}
        tableLayout="fixed"
        className="desk-table"
      />
      {more.isError && !more.data && (
        <div style={footerStyle}>
          <ErrorAlert
            title="Не удалось загрузить пункты"
            error={more.error}
            onRetry={() => void more.refetch()}
          />
        </div>
      )}
      {isTruncated ? (
        <Flex style={footerStyle}>
          <Typography.Text type="secondary">
            {truncationNote(group, items.length, total)}
          </Typography.Text>
        </Flex>
      ) : (
        items.length < total && (
          <Flex align="center" gap={token.margin} style={footerStyle}>
            <Typography.Text type="secondary">
              Показано {NUMBER.format(items.length)} из {NUMBER.format(total)}
            </Typography.Text>
            <Button
              loading={isLoadingMore}
              onClick={() => setLimit(limit + MORE_ITEMS)}
            >
              Показать ещё{" "}
              {NUMBER.format(Math.min(MORE_ITEMS, total - items.length))}
            </Button>
          </Flex>
        )
      )}
      {isSending && items.some((item) => item.invitation) && (
        <Flex style={footerStyle}>
          <Typography.Text
            style={{
              color: token.colorTextTertiary,
              fontSize: token.fontSizeSM,
            }}
          >
            Пока идёт отправка письма, повторная отправка в других строках
            недоступна.
          </Typography.Text>
        </Flex>
      )}
    </>
  );
}

function truncationNote(
  group: DeskGroupView,
  shown: number,
  total: number,
): string {
  const head = `Показаны первые ${NUMBER.format(shown)} из ${NUMBER.format(total)} пунктов.`;
  if (!group.isReference) {
    return `${head} Остальные не показаны: выполните часть действий, и список обновится.`;
  }
  const meta = GROUPS[group.group];
  return meta.queue
    ? `${head} Остальные на столе не показываются: откройте очередь «${meta.queue.title}».`
    : `${head} Остальные на столе не показываются: найдите их в разделе «${meta.section}».`;
}

function columnsOf(
  group: DeskGroupView,
  resend: InvitationResend | undefined,
): TableColumnsType<DeskItem> {
  const columns: TableColumnsType<DeskItem> = [
    {
      key: "object",
      title: "Объект",
      render: (_, item) => <ObjectCell item={item} />,
    },
    {
      key: "reason",
      title: "Причина",
      render: (_, item) => <ReasonList item={item} />,
    },
  ];
  if (GROUPS[group.group].isDated) {
    columns.push({
      key: "age",
      title: "Давность",
      width: AGE_WIDTH,
      className: "desk-nowrap",
      render: (_, item) => <AgeCell item={item} />,
    });
  }
  if (group.group === "Invitations") {
    columns.push({
      key: "link",
      title: "Срок ссылки",
      width: LINK_TERM_WIDTH,
      className: "desk-nowrap",
      render: (_, item) => <LinkTermCell item={item} />,
    });
  }
  if (resend) {
    columns.push({
      key: "actions",
      title: "Действия",
      width: ACTIONS_WIDTH,
      className: "desk-nowrap",
      render: (_, item) => <ActionsCell item={item} resend={resend} />,
    });
  }
  return columns;
}

function SecondaryLine({ children }: { children: ReactNode }) {
  const { token } = antdTheme.useToken();
  return (
    <Typography.Text type="secondary" style={{ fontSize: token.fontSizeSM }}>
      {children}
    </Typography.Text>
  );
}

function MutedLine({ children }: { children: ReactNode }) {
  const { token } = antdTheme.useToken();
  return (
    <Typography.Text
      style={{ color: token.colorTextTertiary, fontSize: token.fontSizeSM }}
    >
      {children}
    </Typography.Text>
  );
}

function ObjectCell({ item }: { item: DeskItem }) {
  const typeLabel = typeLabelOf(item);
  const link = itemLink(item);
  const title = objectTitle(item.object);
  const context = objectContext(item);
  return (
    <Flex vertical>
      <span>
        {typeLabel && <Tag>{typeLabel}</Tag>}
        {link ? (
          <Link to={link}>{title}</Link>
        ) : (
          <Typography.Text strong>{title}</Typography.Text>
        )}
      </span>
      {context && <SecondaryLine>{context}</SecondaryLine>}
    </Flex>
  );
}

function ReasonList({ item }: { item: DeskItem }) {
  const { token } = antdTheme.useToken();
  return (
    <ul
      style={{
        margin: 0,
        padding: 0,
        listStyle: "none",
        display: "grid",
        gap: token.marginXXS,
      }}
    >
      {reasonLines(item).map((line, index) => (
        <li
          key={line.text}
          style={{
            color: index === 0 ? token.colorText : token.colorTextSecondary,
          }}
        >
          {line.isEarly && (
            <Tag color="warning" icon={<WarningOutlined />}>
              Утверждать рано
            </Tag>
          )}
          {line.text}
        </li>
      ))}
    </ul>
  );
}

function AgeCell({ item }: { item: DeskItem }) {
  const occurredAt = item.reasons[0]?.occurredAt;
  if (!occurredAt) return null;
  return (
    <Flex vertical>
      <span>{relativeTime(occurredAt)}</span>
      <MutedLine>{formatDate(occurredAt)}</MutedLine>
    </Flex>
  );
}

function LinkTermCell({ item }: { item: DeskItem }) {
  const reason = item.reasons.find(
    (candidate) => candidate.kind === "InvitationNotAccepted",
  );
  if (!reason) return null;
  const isSoon = reason.details?.linkState === "ExpiringSoon";
  const expiresAt = reason.occurredAt;
  return (
    <Flex vertical align="flex-start">
      {isSoon ? (
        <Tag color="warning" icon={<WarningOutlined />}>
          Скоро истечёт
        </Tag>
      ) : (
        <Tag color="error" icon={<CloseCircleOutlined />}>
          Истекла
        </Tag>
      )}
      {expiresAt && (
        <MutedLine>
          {isSoon
            ? `${formatDate(expiresAt)}, ${relativeTime(expiresAt)}`
            : formatDate(expiresAt)}
        </MutedLine>
      )}
    </Flex>
  );
}

function ActionsCell({
  item,
  resend,
}: {
  item: DeskItem;
  resend: InvitationResend;
}) {
  const action = itemAction(item);
  const { invitation } = item;
  const isSendingThis =
    invitation != null && resend.sendingAccountId === invitation.accountId;
  return (
    <Flex vertical align="flex-start">
      {action && <Link to={action.path}>{action.title}</Link>}
      {invitation && (
        <Button
          type="link"
          size="small"
          loading={isSendingThis}
          disabled={resend.sendingAccountId != null && !isSendingThis}
          onClick={() => resend.send(invitation)}
          style={{ paddingInline: 0 }}
        >
          {isSendingThis ? "Отправка…" : "Отправить приглашение повторно"}
        </Button>
      )}
    </Flex>
  );
}
