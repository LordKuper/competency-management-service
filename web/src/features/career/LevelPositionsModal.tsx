import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, Empty, Flex, List, Select, Space } from "antd";
import { useState } from "react";
import { ApiError } from "../../api/ApiError";
import { AppModal } from "../../app/AppModal";
import { describeApiError } from "../../app/apiErrors";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import {
  availablePositionsQuery,
  type CareerTrack,
  invalidateCatalogs,
  linkLevelPosition,
  type PublicationBlocker,
  trackPositionsQuery,
  unlinkLevelPosition,
} from "./catalogApi";
import { moreBlockersText } from "./useConfirmDeletion";

const PLURAL = new Intl.PluralRules("ru-RU");
const EMPLOYEE_FORMS: Record<string, string> = {
  one: "сотрудник",
  few: "сотрудника",
};

function employees(count: number) {
  return `${count} ${EMPLOYEE_FORMS[PLURAL.select(count)] ?? "сотрудников"}`;
}

/** The server's own explanation of a refusal that names levels, or the common one for any other failure. */
function PositionsError({ error }: { error: unknown }) {
  if (
    error instanceof ApiError &&
    error.status === 409 &&
    (error.blockers.length > 0 || error.moreCount > 0)
  ) {
    const blockers = error.blockers as unknown as PublicationBlocker[];
    return (
      <Alert
        type="error"
        showIcon
        title={error.detail ?? describeApiError(error)}
        description={
          <>
            <ul>
              {blockers.map((blocker) => (
                <li key={blocker.id}>{`Уровень «${blocker.name}»`}</li>
              ))}
            </ul>
            {error.moreCount > 0 && <p>{moreBlockersText(error.moreCount)}</p>}
          </>
        }
      />
    );
  }
  if (error instanceof ApiError && error.status === 412) {
    return <Alert type="warning" showIcon title={describeApiError(error)} />;
  }
  return <ErrorAlert title="Не удалось сохранить связь" error={error} />;
}

interface LevelPositionsModalProps {
  track: CareerTrack;
  levelId: string;
  onClose: () => void;
}

/**
 * Positions of one level. A link is saved at once under the level version the dialog shows; a refusal for a stale
 * version only rereads it, so the next link or unlink sends the new version. Buttons wait while a save runs.
 */
export function LevelPositionsModal({
  track,
  levelId,
  onClose,
}: LevelPositionsModalProps) {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState("");
  const [chosen, setChosen] = useState<string>();
  const q = useDebouncedValue(search);
  const read = useQuery(trackPositionsQuery(track.id));
  const available = useQuery(availablePositionsQuery(track.id, q));
  const level = read.data?.levels.find((item) => item.id === levelId);
  const change = useMutation({
    mutationFn: ({
      positionId,
      action,
    }: {
      positionId: string;
      action: "link" | "unlink";
    }) => {
      if (!level) throw new Error("The level is not loaded");
      return action === "link"
        ? linkLevelPosition(level, positionId)
        : unlinkLevelPosition(level, positionId);
    },
    onSuccess: () => setChosen(undefined),
    onSettled: () => invalidateCatalogs(queryClient),
  });
  const canEdit = read.data?.canEdit === true;
  const isLocked = change.isPending || read.isFetching;

  return (
    <AppModal
      open
      title={level ? `Должности уровня «${level.name}»` : "Должности уровня"}
      onCancel={onClose}
      footer={<Button onClick={onClose}>Закрыть</Button>}
    >
      <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
        {read.error && (
          <ErrorAlert
            title="Не удалось загрузить должности"
            error={read.error}
            onRetry={() => void read.refetch()}
          />
        )}
        {change.error && <PositionsError error={change.error} />}
        {level && level.positions.length === 0 && (
          <Empty
            image={Empty.PRESENTED_IMAGE_SIMPLE}
            description={`Связанных должностей нет. Уровню доступны действующие должности подразделения «${track.ownerOrgUnitName}» и дочерних.`}
          />
        )}
        {level && level.positions.length > 0 && (
          <List
            size="small"
            dataSource={level.positions}
            renderItem={(position) => (
              <List.Item
                actions={
                  canEdit && position.canUnlink
                    ? [
                        <Button
                          key="unlink"
                          type="link"
                          size="small"
                          disabled={isLocked}
                          aria-label={`Снять «${position.name}»`}
                          onClick={() =>
                            change.mutate({
                              positionId: position.id,
                              action: "unlink",
                            })
                          }
                        >
                          Снять
                        </Button>,
                      ]
                    : []
                }
              >
                {`${position.name} · ${employees(position.employeeCount)}${position.isActive ? "" : " · неактивна"}`}
              </List.Item>
            )}
          />
        )}
        {level && canEdit && (
          <Flex gap="small" align="center">
            <Select
              showSearch={{ filterOption: false, onSearch: setSearch }}
              aria-label="Должность"
              placeholder="Начните вводить название"
              style={{ flex: 1 }}
              value={chosen}
              loading={available.isFetching}
              disabled={isLocked}
              options={(available.data?.items ?? []).map((position) => ({
                value: position.id,
                label: `${position.name} · ${employees(position.employeeCount)}`,
              }))}
              onChange={setChosen}
            />
            <Button
              type="primary"
              loading={change.isPending}
              disabled={!chosen || isLocked}
              onClick={() =>
                chosen && change.mutate({ positionId: chosen, action: "link" })
              }
            >
              Связать
            </Button>
          </Flex>
        )}
      </Space>
    </AppModal>
  );
}
