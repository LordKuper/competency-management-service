import { useQuery } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Button,
  Col,
  Empty,
  Flex,
  Row,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useIsAdmin } from "../auth/useCurrentUser";
import { orgUnitTreeQuery } from "./orgStructureApi";
import { paths } from "./paths";
import { UnitCard } from "./UnitCard";
import { UnitFormModal } from "./UnitFormModal";
import { UnitTreePanel } from "./UnitTreePanel";

/**
 * The structure screen: the unit tree beside the card of the selected unit, which the `unit` address parameter names so
 * a reload or a shared link opens the same unit. On a tablet the card stacks under the tree.
 */
export function OrgStructurePage() {
  const { token } = antdTheme.useToken();
  const isAdmin = useIsAdmin();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [isRootFormOpen, setIsRootFormOpen] = useState(false);
  const { data: units, error, isPending, refetch } = useQuery(orgUnitTreeQuery);
  const selectedId = searchParams.get("unit");
  const selected = units?.find((unit) => unit.id === selectedId);
  const selectUnit = (unitId: string) => setSearchParams({ unit: unitId });

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Оргструктура
        </Typography.Title>
        <Space wrap>
          <Button onClick={() => navigate(paths.employees)}>Сотрудники</Button>
          {isAdmin && (
            <Button type="primary" onClick={() => setIsRootFormOpen(true)}>
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
        <Row gutter={[token.marginLG, token.marginLG]}>
          <Col xs={24} lg={9}>
            <UnitTreePanel
              units={units}
              selectedId={selectedId}
              onSelect={selectUnit}
            />
          </Col>
          <Col xs={24} lg={15}>
            {selected ? (
              <UnitCard unit={selected} onSelect={selectUnit} />
            ) : (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description={
                  units.length > 0
                    ? "Выберите подразделение в дереве, чтобы увидеть его карточку."
                    : "Подразделений пока нет." +
                      (isAdmin ? " Добавьте корневое подразделение." : "")
                }
              />
            )}
          </Col>
        </Row>
      )}
      {isRootFormOpen && (
        <UnitFormModal
          parentId={null}
          onClose={() => setIsRootFormOpen(false)}
          onSaved={(created) => {
            setIsRootFormOpen(false);
            selectUnit(created.id);
          }}
        />
      )}
    </Space>
  );
}
