import { DownOutlined } from "@ant-design/icons";
import { Alert, Button, Dropdown, Flex } from "antd";
import type { ReactNode } from "react";
import { useNavigate } from "react-router";
import { type DeskGroup, useDesk } from "./deskApi";
import { objectContext } from "./deskTexts";
import "./scenario.css";

/** The line under the breadcrumb: the object of the scenario, its tags, the choice of another object and the way back. */
export function ContextLine({ children }: { children: ReactNode }) {
  return (
    <Flex align="center" gap="small" wrap>
      {children}
    </Flex>
  );
}

interface OtherVersionProps {
  /** The desk group the other versions are taken from. */
  group: DeskGroup;
  currentId: string;
  pathOf: (versionId: string) => string;
  /** Off while an action on the open version is running. */
  isLocked?: boolean;
}

/** «Другая версия»: the versions of the desk group the open one belongs to; absent when the open one is the only version. */
export function OtherVersionMenu({
  group,
  currentId,
  pathOf,
  isLocked = false,
}: OtherVersionProps) {
  const navigate = useNavigate();
  const desk = useDesk();
  const items = (
    desk.data?.groups.find((candidate) => candidate.group === group)?.items ??
    []
  ).filter((item) => item.target);
  if (!items.some((item) => item.target?.id !== currentId)) return null;
  return (
    <Dropdown
      disabled={isLocked}
      trigger={["click"]}
      menu={{
        items: items.map((item) => {
          const versionId = item.target?.id ?? "";
          const isOpen = versionId === currentId;
          const unit = item.object.context?.at(-1);
          return {
            key: versionId,
            disabled: isOpen,
            label: [
              unit ? `${item.object.name} — ${unit}` : item.object.name,
              objectContext(item),
              isOpen ? "сейчас открыта" : null,
            ]
              .filter(Boolean)
              .join(" · "),
            onClick: () => void navigate(pathOf(versionId)),
          };
        }),
      }}
    >
      <Button size="small" disabled={isLocked}>
        Другая версия <DownOutlined />
      </Button>
    </Dropdown>
  );
}

/** The one column a form step stands in, centred under the full-width ruler; its action bar and tables stay inside it. */
export function FormColumn({ children }: { children: ReactNode }) {
  return <div className="form-column">{children}</div>;
}

interface ActionBarProps {
  /** Buttons at the start of the row, such as «Назад». */
  start?: ReactNode;
  /** The reason beside the buttons and the buttons themselves, the main one last. */
  children: ReactNode;
}

/** The row of buttons under a screen: the way back at the left, the decision at the right, no frame. */
export function ActionBar({ start, children }: ActionBarProps) {
  return (
    <Flex justify="space-between" align="center" gap="small" wrap>
      <Flex align="center" gap="small" wrap>
        {start}
      </Flex>
      <Flex
        align="center"
        gap="small"
        wrap
        style={{ marginInlineStart: "auto" }}
      >
        {children}
      </Flex>
    </Flex>
  );
}

interface CompactAlertProps {
  type: "info" | "success" | "warning" | "error";
  /** The bold first phrase. */
  lead: string;
  /** The one short explanation after it, in the same line. */
  text?: string;
  icon?: ReactNode;
}

/** An alert of a line or two: a bold phrase and, if needed, one short explanation, without a separate description. */
export function CompactAlert({ type, lead, text, icon }: CompactAlertProps) {
  return (
    <Alert
      type={type}
      showIcon
      icon={icon}
      title={
        <>
          <strong>{lead}</strong>
          {text && ` ${text}`}
        </>
      }
    />
  );
}
