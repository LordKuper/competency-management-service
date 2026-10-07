import {
  CheckCircleOutlined,
  DownOutlined,
  EditOutlined,
  MoreOutlined,
  PlusOutlined,
  RightOutlined,
  StopOutlined,
  SwapOutlined,
  UserAddOutlined,
} from "@ant-design/icons";
import { theme as antdTheme, Button, Card, Dropdown, Flex } from "antd";
import { memo, useEffect, useRef } from "react";
import { Highlighted } from "./Highlighted";
import type { OrgUnit, OrgUnitTreeNode } from "./orgStructureApi";
import { UnitStatusTag } from "./StatusTags";

/**
 * Whether a unit lists its contents. "path" is a unit a search opened only to show the matches below it; "matches" is a
 * unit that lists, besides its child units, only the employees the search found in it.
 */
export type UnitOpenness = "closed" | "path" | "matches" | "open";

/** What an administrator can do with a unit from its card. */
export type UnitAction =
  | "addChild"
  | "addEmployee"
  | "edit"
  | "move"
  | "toggleActive";

interface UnitCardProps {
  unit: OrgUnitTreeNode;
  /** All child units, not only those a search lists, so the card reads the same open, closed or searched. */
  childCount: number;
  openness: UnitOpenness;
  /** Whether the address names this unit, so it is marked and scrolled into view. */
  isSelected: boolean;
  /** Lower-cased search text whose first occurrence in the name is highlighted; blank when not searching. */
  needle: string;
  isAdmin: boolean;
  onToggle: (unitId: string) => void;
  onAction: (action: UnitAction, unit: OrgUnit) => void;
}

/**
 * Card of a unit in the hierarchy: its name, status, counts of child units and employees and its head, all of which
 * read the same open or closed, the control that opens it, and for administrators the menu of actions.
 */
export const UnitCard = memo(function UnitCard({
  unit,
  childCount,
  openness,
  isSelected,
  needle,
  isAdmin,
  onToggle,
  onAction,
}: UnitCardProps) {
  const { token } = antdTheme.useToken();
  const inactiveNote = unit.isActive ? "" : " (подразделение неактивно)";
  const isOpen = openness !== "closed";
  const cardRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isSelected) cardRef.current?.scrollIntoView({ block: "center" });
  }, [isSelected]);

  return (
    <Card
      ref={cardRef}
      size="small"
      className="org-tree__item"
      aria-current={isSelected || undefined}
      style={
        isSelected
          ? {
              borderColor: token.colorPrimary,
              background: token.colorPrimaryBg,
            }
          : undefined
      }
    >
      <Flex align="center" gap={token.marginXS}>
        <Button
          type="text"
          className="org-card__name"
          aria-label={unit.name}
          aria-expanded={isOpen}
          icon={isOpen ? <DownOutlined /> : <RightOutlined />}
          onClick={() => onToggle(unit.id)}
        >
          <Highlighted text={unit.name} needle={needle} />
        </Button>
        {!unit.isActive && <UnitStatusTag />}
        {isAdmin && (
          <Dropdown
            trigger={["click"]}
            menu={{
              items: [
                {
                  key: "addChild",
                  icon: <PlusOutlined />,
                  label: `Добавить подразделение${inactiveNote}`,
                  disabled: !unit.isActive,
                  onClick: () => onAction("addChild", unit),
                },
                {
                  key: "addEmployee",
                  icon: <UserAddOutlined />,
                  label: `Добавить сотрудника${inactiveNote}`,
                  disabled: !unit.isActive,
                  onClick: () => onAction("addEmployee", unit),
                },
                {
                  key: "edit",
                  icon: <EditOutlined />,
                  label: "Править",
                  onClick: () => onAction("edit", unit),
                },
                {
                  key: "move",
                  icon: <SwapOutlined />,
                  label: "Перенести",
                  onClick: () => onAction("move", unit),
                },
                { type: "divider" },
                {
                  key: "toggleActive",
                  icon: unit.isActive ? (
                    <StopOutlined />
                  ) : (
                    <CheckCircleOutlined />
                  ),
                  danger: unit.isActive,
                  label: unit.isActive ? "Деактивировать" : "Активировать",
                  onClick: () => onAction("toggleActive", unit),
                },
              ],
            }}
          >
            <Button
              type="text"
              icon={<MoreOutlined />}
              aria-label={`Действия с подразделением ${unit.name}`}
            />
          </Dropdown>
        )}
      </Flex>
      <div className="org-card__meta">
        <span>Подразделений: {childCount}</span>
        <span>Сотрудников: {unit.employeeCount}</span>
        <span>Руководитель: {unit.headName ?? "не назначен"}</span>
      </div>
    </Card>
  );
});
