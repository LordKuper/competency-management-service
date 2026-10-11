import {
  CheckCircleOutlined,
  DownOutlined,
  EditOutlined,
  MoreOutlined,
  PlusOutlined,
  RightOutlined,
  StopOutlined,
  SwapOutlined,
  TeamOutlined,
  UnorderedListOutlined,
  UserAddOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { theme as antdTheme, Button, Card, Dropdown, Flex } from "antd";
import { memo, useEffect, useRef } from "react";
import { effectiveHr, unitHrQuery } from "../career/careerApi";
import { Highlighted } from "./Highlighted";
import type { OrgUnit, OrgUnitTreeNode } from "./orgStructureApi";
import type { UnitOpenness } from "./orgTree";
import { UnitStatusTag } from "./StatusTags";

/** What can be done with a unit from its card: "positions" and "hr" for everyone, the rest for administrators. */
export type UnitAction =
  | "addChild"
  | "addEmployee"
  | "edit"
  | "hr"
  | "move"
  | "positions"
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
  onToggle: (unitId: string, isOpen: boolean) => void;
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
  const { data: unitHr } = useQuery(unitHrQuery(unit.id));
  const hr = unitHr && effectiveHr(unitHr);

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
          onClick={() => onToggle(unit.id, !isOpen)}
        >
          <Highlighted text={unit.name} needle={needle} />
        </Button>
        {!unit.isActive && <UnitStatusTag />}
        <Dropdown
          trigger={["click"]}
          menu={{
            items: [
              ...(isAdmin
                ? [
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
                      label: "Изменить",
                      onClick: () => onAction("edit", unit),
                    },
                    {
                      key: "move",
                      icon: <SwapOutlined />,
                      label: "Перенести",
                      onClick: () => onAction("move", unit),
                    },
                  ]
                : []),
              {
                key: "positions",
                icon: <UnorderedListOutlined />,
                label: "Должности",
                onClick: () => onAction("positions", unit),
              },
              {
                key: "hr",
                icon: <TeamOutlined />,
                label: "HR",
                onClick: () => onAction("hr", unit),
              },
              ...(isAdmin
                ? [
                    { type: "divider" as const },
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
                  ]
                : []),
            ],
          }}
        >
          <Button
            type="text"
            icon={<MoreOutlined />}
            aria-label={`Действия с подразделением ${unit.name}`}
          />
        </Dropdown>
      </Flex>
      <div className="org-card__meta">
        <span>Подразделений: {childCount}</span>
        <span>Сотрудников: {unit.employeeCount}</span>
        <span>Руководитель: {unit.headName ?? "не назначен"}</span>
        {hr && <span>HR: {hr.name}</span>}
      </div>
    </Card>
  );
});
