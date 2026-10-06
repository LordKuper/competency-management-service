import {
  ApartmentOutlined,
  CheckCircleOutlined,
  DownOutlined,
  EditOutlined,
  InfoCircleOutlined,
  MoreOutlined,
  PlusOutlined,
  RightOutlined,
  StopOutlined,
  SwapOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Button,
  Card,
  Dropdown,
  Flex,
  Skeleton,
} from "antd";
import { memo, useEffect, useRef, useState } from "react";
import { Link } from "react-router";
import {
  employeeQuery,
  type OrgUnit,
  unitEmployeesQuery,
} from "./orgStructureApi";
import { paths } from "./paths";
import { UnitStatusTag } from "./StatusTags";
import { UnitDetails } from "./UnitDetails";

/** Whether a unit lists its contents; "path" is a unit a name search opened only to show the matches below it. */
export type UnitOpenness = "closed" | "path" | "open";

/** What an administrator can do with a unit from its card. */
export type UnitAction = "addChild" | "edit" | "move" | "toggleActive";

function Highlighted({ text, needle }: { text: string; needle: string }) {
  const start = needle ? text.toLocaleLowerCase("ru").indexOf(needle) : -1;
  if (start < 0) return text;
  const end = start + needle.length;
  return (
    <>
      {text.slice(0, start)}
      <mark>{text.slice(start, end)}</mark>
      {text.slice(end)}
    </>
  );
}

function UnitHead({ employeeId }: { employeeId: string }) {
  const { data, error } = useQuery(employeeQuery(employeeId));
  if (data) return <Link to={paths.employee(data.id)}>{data.fullName}</Link>;
  return error ? (
    "не удалось загрузить"
  ) : (
    <Skeleton.Input active size="small" />
  );
}

function OpenedFacts({ unit }: { unit: OrgUnit }) {
  const { data: employees } = useQuery(unitEmployeesQuery(unit.id));
  return (
    <>
      <span>Сотрудников: {employees?.length ?? "—"}</span>
      <span>
        Руководитель:{" "}
        {unit.headEmployeeId ? (
          <UnitHead employeeId={unit.headEmployeeId} />
        ) : (
          "не назначен"
        )}
      </span>
    </>
  );
}

interface UnitCardProps {
  unit: OrgUnit;
  /** Units listed under this one, which a name search may have narrowed. */
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
 * Card of a unit in the hierarchy: its name, status and counts, the control that opens it, a block with its path
 * and headcount, and for administrators the menu of actions. Opened, it also names the head and counts the employees.
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
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);
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
          <ApartmentOutlined />
          <Highlighted text={unit.name} needle={needle} />
        </Button>
        {!unit.isActive && <UnitStatusTag isActive={false} />}
        <Button
          type="text"
          icon={<InfoCircleOutlined />}
          aria-label={`Путь и сводка: ${unit.name}`}
          aria-expanded={isDetailsOpen}
          onClick={() => setIsDetailsOpen((previous) => !previous)}
        />
        {isAdmin && (
          <Dropdown
            trigger={["click"]}
            menu={{
              items: [
                {
                  key: "addChild",
                  icon: <PlusOutlined />,
                  label: unit.isActive
                    ? "Добавить подразделение"
                    : "Добавить подразделение (подразделение неактивно)",
                  disabled: !unit.isActive,
                  onClick: () => onAction("addChild", unit),
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
        {openness === "open" && <OpenedFacts unit={unit} />}
      </div>
      {isDetailsOpen && <UnitDetails unit={unit} />}
    </Card>
  );
});
