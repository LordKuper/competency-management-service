import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Col, Empty, Flex, Row, Skeleton, Typography } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useLeaveGuard } from "../../app/leaveGuard";
import { contentMaxWidth } from "../../app/theme";
import { useEditBase } from "../../app/useEditBase";
import { CommissionList } from "./CommissionList";
import { CommissionView } from "./CommissionView";
import { Panel } from "./Panel";
import {
  type Commission,
  type CommissionInput,
  commissionQuery,
  commissionsQuery,
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

/** The units the viewer answers for, each with its commission's size, and beside them the commission of the chosen one to edit. */
export function CommissionsSection() {
  const { data, error, isPending, refetch } = useQuery(commissionsQuery);
  const { confirmLeave } = useLeaveGuard();
  const [chosenId, setChosenId] = useState<string>();
  const unitId = chosenId ?? data?.[0]?.unitId;
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
          title="Не удалось загрузить комиссии"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {isPending && <Skeleton active />}
      {data?.length === 0 && (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description="У вас нет подразделений, за которые вы отвечаете как HR. Администратор закрепляет HR за подразделением в разделе «Оргструктура»."
        />
      )}
      {data && data.length > 0 && (
        <Row gutter={[24, 24]}>
          <Col xs={24} lg={7}>
            <CommissionList
              commissions={data}
              selectedId={unitId}
              onSelect={(next) => confirmLeave(() => setChosenId(next))}
            />
          </Col>
          <Col xs={24} lg={17}>
            {unitId && <CommissionEditor key={unitId} unitId={unitId} />}
          </Col>
        </Row>
      )}
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
