import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Flex,
  Form,
  Select,
  Skeleton,
  Space,
  Typography,
} from "antd";
import { useState } from "react";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { AppModal } from "../../app/AppModal";
import { describeApiError } from "../../app/apiErrors";
import { destructiveConfirm } from "../../app/destructiveConfirm";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { useFormSubmit } from "../../app/useFormSubmit";
import { useIsAdmin } from "../auth/useCurrentUser";
import { PositionStatusTag } from "../positions/PositionStatusTag";
import {
  fetchUnitPositionImpact,
  invalidatePositions,
  positionListQuery,
  type UnitPosition,
  unitPositionsQuery,
} from "../positions/positionsApi";
import type { OrgUnit } from "./orgStructureApi";

const PICKER_PAGE_SIZE = 20;

interface UnitPositionsModalProps {
  unit: OrgUnit;
  onClose: () => void;
}

/**
 * Dialog of the positions allowed in a unit; read-only for others, while administrators change it: the unit's own list, where a position is taken off after
 * showing how many employees of the unit and the units below keep it, the positions inherited from the units above with
 * the unit each comes from, and the choice of an active catalogue position to add. Mount it only while it is shown.
 */
export function UnitPositionsModal({ unit, onClose }: UnitPositionsModalProps) {
  const isAdmin = useIsAdmin();
  const queryClient = useQueryClient();
  const { modal, message } = App.useApp();
  const { data, error, isPending, refetch } = useQuery(
    unitPositionsQuery(unit.id),
  );
  const reportFailure = (failure: unknown) => {
    message.error(describeApiError(failure));
  };
  const { mutateAsync: remove } = useMutation({
    mutationFn: async (positionId: string) =>
      unwrap(
        await api.DELETE("/api/v1/org-units/{id}/positions/{positionId}", {
          params: { path: { id: unit.id, positionId } },
        }),
      ),
    onSettled: () => invalidatePositions(queryClient),
  });
  const { mutateAsync: add } = useMutation({
    mutationFn: async (positionId: string) =>
      unwrap(
        await api.POST("/api/v1/org-units/{id}/positions", {
          params: { path: { id: unit.id } },
          body: { positionId },
        }),
      ),
    onError: (failure) => {
      if (!(failure instanceof ApiError && failure.errors)) {
        reportFailure(failure);
      }
    },
    onSettled: () => invalidatePositions(queryClient),
  });
  const own = data?.filter((position) => position.orgUnitId === unit.id) ?? [];
  const inherited =
    data?.filter((position) => position.orgUnitId !== unit.id) ?? [];

  async function confirmRemoval(position: UnitPosition) {
    let employeeCount: number;
    try {
      ({ employeeCount } = await fetchUnitPositionImpact(
        unit.id,
        position.positionId,
      ));
    } catch (failure) {
      reportFailure(failure);
      return;
    }
    modal.confirm({
      title: `Исключить «${position.name}» из перечня?`,
      content: `Сотрудников подразделения и дочерних с этой должностью: ${employeeCount}. Их должность не изменится.`,
      okText: "Исключить",
      ...destructiveConfirm(true),
      cancelText: "Отмена",
      onOk: () =>
        remove(position.positionId).then(() => undefined, reportFailure),
    });
  }

  return (
    <AppModal
      open
      title={`Должности подразделения «${unit.name}»`}
      onCancel={onClose}
      footer={null}
    >
      {isPending && <Skeleton active />}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить должности подразделения"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      {data && (
        <Space orientation="vertical" size="large" style={{ display: "flex" }}>
          <section>
            <Typography.Title level={3}>Собственные</Typography.Title>
            {own.length === 0 ? (
              <Typography.Text type="secondary">
                Собственных должностей нет
              </Typography.Text>
            ) : (
              own.map((position) => (
                <Flex
                  key={position.positionId}
                  justify="space-between"
                  align="center"
                  gap="small"
                >
                  <Flex align="center" gap="small" wrap>
                    <span>{position.name}</span>
                    {!position.isActive && (
                      <PositionStatusTag isActive={false} />
                    )}
                  </Flex>
                  {isAdmin && (
                    <Button
                      type="link"
                      size="small"
                      danger
                      aria-label={`Исключить «${position.name}» из перечня`}
                      onClick={() => void confirmRemoval(position)}
                    >
                      Исключить
                    </Button>
                  )}
                </Flex>
              ))
            )}
          </section>
          <section>
            <Typography.Title level={3}>Унаследованные</Typography.Title>
            {inherited.length === 0 ? (
              <Typography.Text type="secondary">
                Унаследованных должностей нет
              </Typography.Text>
            ) : (
              inherited.map((position) => (
                <Flex
                  key={`${position.orgUnitId}:${position.positionId}`}
                  align="center"
                  gap="small"
                  wrap
                >
                  <span>{position.name}</span>
                  {!position.isActive && <PositionStatusTag isActive={false} />}
                  <Typography.Text type="secondary">
                    из «{position.orgUnitName}»
                  </Typography.Text>
                </Flex>
              ))
            )}
          </section>
          {isAdmin && (
            <AddPositionForm
              ownIds={new Set(own.map((position) => position.positionId))}
              onAdd={add}
            />
          )}
        </Space>
      )}
    </AppModal>
  );
}

interface AddPositionValues {
  positionId: string;
}

interface AddPositionFormProps {
  /** Positions already on the unit's own list, which the choice leaves out. */
  ownIds: ReadonlySet<string>;
  /** Adds the position; a rejection is shown under the field it names. */
  onAdd: (positionId: string) => Promise<unknown>;
}

/** Remote-search choice of an active catalogue position and the button that adds it to the unit's own list. */
function AddPositionForm({ ownIds, onAdd }: AddPositionFormProps) {
  const [form] = Form.useForm<AddPositionValues>();
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onAdd(values.positionId);
    form.resetFields();
  });
  const [search, setSearch] = useState("");
  const text = useDebouncedValue(search.trim());
  const { data, error, isFetching } = useQuery(
    positionListQuery({
      q: text || undefined,
      isActive: true,
      page: 1,
      pageSize: PICKER_PAGE_SIZE,
    }),
  );
  const options = (data?.items ?? [])
    .filter((position) => !ownIds.has(position.id))
    .map((position) => ({ value: position.id, label: position.name }));

  return (
    <Form form={form} name="unit-position" layout="vertical" onFinish={submit}>
      <Form.Item
        name="positionId"
        label="Должность из справочника"
        rules={[{ required: true, message: "Выберите должность" }]}
      >
        <Select
          showSearch={{ filterOption: false, onSearch: setSearch }}
          options={options}
          loading={isFetching}
          placeholder="Выберите должность"
          notFoundContent={error ? describeApiError(error) : undefined}
        />
      </Form.Item>
      <Button htmlType="submit" loading={isSubmitting}>
        Добавить
      </Button>
    </Form>
  );
}
