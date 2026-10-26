import { MoreOutlined } from "@ant-design/icons";
import { Button, Dropdown, Flex } from "antd";
import { useNavigate } from "react-router";
import { useMatrixDeletion } from "./MatrixDeleteButton";
import type { Matrix, MatrixTransition, MatrixVersion } from "./matrixApi";

const MAX_VISIBLE = 2;

export const RETURN_LABEL = "Вернуть с правками";

/** Actions on the saved version that unsaved edits hold back; the others are links to desk scenarios. */
const HELD_ACTIONS: MatrixVersion["actions"] = [
  "Return",
  "AcceptAndRevise",
  "RollbackChanges",
];

/** Whether the bar offers at least one action that unsaved edits hold back. */
export const hasHeldActions = (
  version: MatrixVersion,
  isNextVersionOffered: boolean,
) =>
  isNextVersionOffered ||
  version.actions.some((action) => HELD_ACTIONS.includes(action));

/** An action of the version bar: a button, or a link to a desk scenario that looks like one. */
export interface MatrixBarAction {
  key: string;
  label: string;
  /** Route of a desk scenario; without it the action is a button. */
  to?: string;
  danger?: boolean;
  disabled?: boolean;
  onClick?: () => void;
}

interface MatrixActionsProps {
  matrix: Matrix;
  version: MatrixVersion;
  /** Whether the history of the version is readable by the user. */
  hasHistory: boolean;
  /** Whether the user may start the next version of a published matrix. */
  isNextVersionOffered: boolean;
  isBusy: boolean;
  /** Unsaved edits of the content hold back the actions on the saved version; the links to desk scenarios stay available. */
  hasUnsaved: boolean;
  onTransit: (transition: MatrixTransition) => void;
  onReturn: () => void;
  onNewVersion: () => void;
  onHistory: () => void;
  onDeleted: () => void;
}

/**
 * Actions of the version on the matrix page, in the order of the version's scenario: what leads on, then the
 * non-publishing decision. The first two are in view, as buttons or button-looking links, the rest in a «…» menu. The
 * server says which actions the user has; the page runs no publishing transition and links to the desk scenario that
 * does, «Согласовать версию» also when only the return is left to an approver with edits of his own.
 */
export function MatrixActions({
  matrix,
  version,
  hasHistory,
  isNextVersionOffered,
  isBusy,
  hasUnsaved,
  onTransit,
  onReturn,
  onNewVersion,
  onHistory,
  onDeleted,
}: MatrixActionsProps) {
  const navigate = useNavigate();
  const startDeletion = useMatrixDeletion({
    matrix,
    inWorkTag:
      version.id === matrix.inWorkVersion?.id ? version.version : undefined,
    onDeleted,
  });
  const can = (action: MatrixVersion["actions"][number]) =>
    version.actions.includes(action);
  const held = isBusy || hasUnsaved;
  const desk = (scenario: string) => `/desk/${scenario}/${version.id}`;
  const actions: MatrixBarAction[] = [
    ...(can("Return")
      ? [
          {
            key: "return",
            label: RETURN_LABEL,
            disabled: held,
            onClick: onReturn,
          },
        ]
      : []),
    ...(can("Approve") || can("Return")
      ? [
          {
            key: "review",
            label: "Согласовать версию",
            to: desk("review"),
          },
        ]
      : []),
    ...(can("AcceptAndRevise")
      ? [
          {
            key: "accept-and-revise",
            label: "Принять и доработать",
            disabled: held,
            onClick: () => onTransit("accept-and-revise"),
          },
        ]
      : []),
    ...(can("AcceptChanges")
      ? [
          {
            key: "rework",
            label: "Доработать возвращённую версию",
            to: desk("rework"),
          },
        ]
      : []),
    ...(can("Submit")
      ? [
          {
            key: "builder",
            label: "Перейти к отправке в мастере",
            to: desk("matrix-builder"),
          },
        ]
      : []),
    ...(isNextVersionOffered
      ? [
          {
            key: "new-version",
            label: "Создать новую версию",
            disabled: held,
            onClick: onNewVersion,
          },
        ]
      : []),
    ...(can("RollbackChanges")
      ? [
          {
            key: "rollback",
            label: "Откатить правки",
            disabled: held,
            onClick: () => onTransit("rollback-changes"),
          },
        ]
      : []),
    ...(hasHistory
      ? [{ key: "history", label: "История", onClick: onHistory }]
      : []),
    ...(startDeletion
      ? [
          {
            key: "delete",
            label: "Удалить",
            danger: true,
            onClick: startDeletion,
          },
        ]
      : []),
  ];

  const visible = actions.slice(0, MAX_VISIBLE);
  const hidden = actions.slice(MAX_VISIBLE);

  return (
    <Flex gap="small" align="center">
      {visible.map(({ key, label, to, disabled, onClick }) => (
        <Button
          key={key}
          href={to}
          disabled={disabled}
          onClick={(event) => {
            if (to) {
              event.preventDefault();
              void navigate(to);
            } else {
              onClick?.();
            }
          }}
        >
          {label}
        </Button>
      ))}
      {hidden.length > 0 && (
        <Dropdown
          trigger={["click"]}
          menu={{
            items: hidden.map(
              ({ key, label, to, danger, disabled, onClick }) => ({
                key,
                label,
                danger,
                disabled,
                onClick: () => (to ? void navigate(to) : onClick?.()),
              }),
            ),
          }}
        >
          <Button icon={<MoreOutlined />} aria-label="Действия с версией" />
        </Dropdown>
      )}
    </Flex>
  );
}
