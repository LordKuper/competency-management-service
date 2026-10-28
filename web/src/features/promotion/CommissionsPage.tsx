import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Col, Empty, Flex, Row, Skeleton, TreeSelect, Typography } from "antd";
import { useMemo, useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import { unitTreeItems, useAnswerableUnits } from "./answerableUnits";
import { CommissionView } from "./CommissionView";
import { Panel } from "./Panel";
import {
  type Commission,
  type CommissionInput,
  commissionQuery,
  promotionQueryKey,
  saveCommission,
  type Versioned,
} from "./promotionApi";
import { useEmployeeOptions } from "./useEmployeeOptions";

/** The commissions screen of HR and administrators. */
export function CommissionsPage() {
  return (
    <Flex
      vertical
      gap="large"
      style={{ maxWidth: contentMaxWidth, marginInline: "auto" }}
    >
      <Typography.Title level={1} style={{ margin: 0 }}>
        Комиссии
      </Typography.Title>
      <CommissionsSection />
    </Flex>
  );
}

/** Choose a unit the viewer answers for, then edit who sits in its commission. */
export function CommissionsSection() {
  const { units, error, refetch } = useAnswerableUnits();
  const { confirmLeave } = useLeaveGuard();
  const [unitId, setUnitId] = useState<string>();
  const items = useMemo(() => unitTreeItems(units ?? []), [units]);
  return (
    <Flex vertical gap="middle">
      <Flex justify="space-between" align="baseline" gap="middle" wrap>
        <Typography.Title level={2} style={{ margin: 0 }}>
          Комиссии подразделений
        </Typography.Title>
        <Typography.Text type="secondary">
          Руководитель и ответственный HR входят в комиссию автоматически
        </Typography.Text>
      </Flex>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить подразделения"
          error={error}
          onRetry={refetch}
        />
      )}
      {!units && !error && <Skeleton active />}
      {units?.length === 0 && (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="У вас нет подразделений, за которые вы отвечаете как HR. Администратор закрепляет HR за подразделением в разделе «Оргструктура»."
        />
      )}
      {units && units.length > 0 && (
        <Row>
          <Col xs={24} md={12}>
            <TreeSelect
              showSearch
              treeDefaultExpandAll
              treeNodeFilterProp="title"
              aria-label="Подразделение"
              placeholder="Выберите подразделение"
              style={{ width: "100%" }}
              treeData={items}
              value={unitId}
              onChange={(next: string) => confirmLeave(() => setUnitId(next))}
            />
          </Col>
        </Row>
      )}
      {unitId && <CommissionEditor key={unitId} unitId={unitId} />}
    </Flex>
  );
}

function CommissionEditor({ unitId }: { unitId: string }) {
  const { data, error, isPending, refetch } = useQuery(commissionQuery(unitId));
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить комиссию"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return <CommissionForm latest={data} />;
}

/** The form with the commission it was opened with; it takes the reread one only after a save is refused for a stale version. */
function CommissionForm({ latest }: { latest: Versioned<Commission> }) {
  const queryClient = useQueryClient();
  const { base = latest, noteSaveFailure, rebase } = useEditBase(latest);
  const employees = useEmployeeOptions("commission-picker");
  const save = useMutation({
    mutationFn: (input: CommissionInput) =>
      saveCommission(base.unitId, base.version, input),
    onSuccess: rebase,
    onError: noteSaveFailure,
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  return (
    <Panel title={base.unitName}>
      <Flex vertical gap="middle">
        {save.isError && (
          <ErrorAlert title="Комиссия не сохранена" error={save.error} />
        )}
        <CommissionView
          key={base.version}
          commission={base}
          candidates={employees.candidates}
          isSearching={employees.isSearching}
          onSearch={employees.onSearch}
          onSubmit={save.mutateAsync}
        />
      </Flex>
    </Panel>
  );
}
