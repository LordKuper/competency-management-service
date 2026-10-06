import { SearchOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import { Button, Empty, Flex, Input, Skeleton, Space, Typography } from "antd";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useIsAdmin } from "../auth/useCurrentUser";
import { MoveUnitModal } from "./MoveUnitModal";
import { type OrgUnit, orgUnitTreeQuery } from "./orgStructureApi";
import { ancestorIds, buildUnitTree } from "./orgTree";
import { paths } from "./paths";
import type { UnitAction } from "./UnitCard";
import { UnitFormModal } from "./UnitFormModal";
import { UnitTree } from "./UnitTree";
import { useUnitActivation } from "./useUnitActivation";
import { useUnitExpansion } from "./useUnitExpansion";

type UnitDialog =
  | { kind: "create"; parentId: string | null }
  | { kind: "edit" | "move"; unitId: string };

/**
 * The structure screen: a searchable hierarchy of unit and employee cards. The `unit` address parameter opens the
 * path to that unit and scrolls to it, so a shared link or the unit link of an employee lands on the same card.
 * Administrators change units from the cards; the dialogs and the activation prompt live here, once for all cards.
 */
export function OrgStructurePage() {
  const isAdmin = useIsAdmin();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { data: units, error, isPending, refetch } = useQuery(orgUnitTreeQuery);
  const roots = useMemo(() => buildUnitTree(units ?? []), [units]);
  const { text, search, openIds, setText, toggle, open } =
    useUnitExpansion(roots);
  const [dialog, setDialog] = useState<UnitDialog | null>(null);
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
        setDialog({ kind: "create", parentId: unit.id });
      else setDialog({ kind: action, unitId: unit.id });
    },
    [confirmActivation],
  );

  const dialogUnit =
    dialog && dialog.kind !== "create"
      ? units?.find((unit) => unit.id === dialog.unitId)
      : undefined;

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Оргструктура
        </Typography.Title>
        <Space wrap>
          <Button onClick={() => navigate(paths.employees)}>Сотрудники</Button>
          {isAdmin && (
            <Button
              type="primary"
              onClick={() => setDialog({ kind: "create", parentId: null })}
            >
              Добавить корневое подразделение
            </Button>
          )}
        </Space>
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
              }}
            />
          )}
        </>
      )}
      {dialog?.kind === "create" && (
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
