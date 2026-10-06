import { SearchOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { Button, Empty, Flex, Input, Skeleton, Space, Typography } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useIsAdmin } from "../auth/useCurrentUser";
import { EmployeeModal } from "./EmployeeModal";
import { MoveUnitModal } from "./MoveUnitModal";
import { type OrgUnit, orgUnitTreeQuery } from "./orgStructureApi";
import { ancestorIds, buildUnitTree } from "./orgTree";
import type { UnitAction } from "./UnitCard";
import { UnitFormModal } from "./UnitFormModal";
import { UnitTree } from "./UnitTree";
import { useUnitActivation } from "./useUnitActivation";
import { useUnitExpansion } from "./useUnitExpansion";

type PageDialog =
  | { kind: "addUnit"; parentId: string | null }
  | { kind: "addEmployee"; orgUnitId?: string }
  | { kind: "employee"; employeeId: string }
  | { kind: "edit" | "move"; unitId: string };

/**
 * The structure screen: a searchable hierarchy of unit and employee cards. The `unit` address parameter opens the
 * path to that unit and scrolls to it, so a shared link or a new unit or employee lands on the same card.
 * Employees open in a dialog from their cards; administrators change units and employees from the cards. The dialogs
 * and the activation prompt live here, once for all cards.
 */
export function OrgStructurePage() {
  const isAdmin = useIsAdmin();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: units, error, isPending, refetch } = useQuery(orgUnitTreeQuery);
  const roots = useMemo(() => buildUnitTree(units ?? []), [units]);
  const { text, search, openIds, setText, toggle, open } =
    useUnitExpansion(roots);
  const [dialog, setDialog] = useState<PageDialog | null>(null);
  const confirmActivation = useUnitActivation();
  const selectedId = searchParams.get("unit");

  useEffect(() => {
    if (units && selectedId !== null) {
      open([...ancestorIds(units, selectedId), selectedId]);
    }
  }, [units, selectedId, open]);

  const closeDialog = useCallback(() => setDialog(null), []);
  const handleAction = useCallback(
    (action: UnitAction, unit: OrgUnit) => {
      if (action === "toggleActive") confirmActivation(unit);
      else if (action === "addChild")
        setDialog({ kind: "addUnit", parentId: unit.id });
      else if (action === "addEmployee")
        setDialog({ kind: "addEmployee", orgUnitId: unit.id });
      else setDialog({ kind: action, unitId: unit.id });
    },
    [confirmActivation],
  );
  const openEmployee = useCallback(
    (employeeId: string) => setDialog({ kind: "employee", employeeId }),
    [],
  );

  const dialogUnit =
    dialog?.kind === "edit" || dialog?.kind === "move"
      ? units?.find((unit) => unit.id === dialog.unitId)
      : undefined;

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Оргструктура
        </Typography.Title>
        {isAdmin && (
          <Space wrap>
            <Button onClick={() => setDialog({ kind: "addEmployee" })}>
              Добавить сотрудника
            </Button>
            <Button
              type="primary"
              onClick={() => setDialog({ kind: "addUnit", parentId: null })}
            >
              Добавить корневое подразделение
            </Button>
          </Space>
        )}
      </Flex>
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить подразделения"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {units && (
        <>
          <Input
            allowClear
            prefix={<SearchOutlined />}
            aria-label="Поиск подразделения по названию"
            placeholder="Название подразделения"
            value={text}
            onChange={(event) => setText(event.target.value)}
          />
          {search && search.visibleIds.size === 0 ? (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Подразделения не найдены. Измените запрос."
            />
          ) : units.length === 0 ? (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description={
                "Подразделений пока нет." +
                (isAdmin ? " Добавьте корневое подразделение." : "")
              }
            />
          ) : (
            <UnitTree
              roots={roots}
              view={{
                openIds,
                search,
                selectedId,
                isAdmin,
                onToggle: toggle,
                onAction: handleAction,
                onOpenEmployee: openEmployee,
              }}
            />
          )}
        </>
      )}
      {dialog?.kind === "addUnit" && (
        <UnitFormModal
          parentId={dialog.parentId}
          onClose={closeDialog}
          onSaved={(created) => {
            closeDialog();
            setText("");
            setSearchParams({ unit: created.id });
          }}
        />
      )}
      {dialog?.kind === "addEmployee" && (
        <EmployeeModal
          orgUnitId={dialog.orgUnitId}
          onClose={closeDialog}
          onSaved={(created) => {
            closeDialog();
            setText("");
            setSearchParams({ unit: created.orgUnitId });
          }}
        />
      )}
      {dialog?.kind === "employee" && (
        <EmployeeModal
          employeeId={dialog.employeeId}
          onClose={closeDialog}
          onSaved={closeDialog}
        />
      )}
      {dialog?.kind === "edit" && dialogUnit && (
        <UnitFormModal
          unit={dialogUnit}
          onClose={closeDialog}
          onSaved={closeDialog}
        />
      )}
      {dialog?.kind === "move" && dialogUnit && (
        <MoveUnitModal unit={dialogUnit} onClose={closeDialog} />
      )}
    </Space>
  );
}
