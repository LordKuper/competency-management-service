import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Button, Space, type TableColumnsType, Tag } from "antd";
import type { ReactNode } from "react";
import { describeApiError } from "../../app/apiErrors";
import {
  type CatalogAction,
  type CatalogEntry,
  type CatalogSegment,
  type CatalogStatus,
  changeCatalogStatus,
  deleteEntry,
  invalidateCatalogs,
} from "./catalogApi";
import { choiceFilter } from "./columnFilters";
import { useCatalogRights } from "./useCatalogRights";
import { useConfirmDeletion } from "./useConfirmDeletion";

const STATUS_TAGS: Record<CatalogStatus, { label: string; color?: string }> = {
  Draft: { label: "Черновик", color: "warning" },
  Active: { label: "Действует", color: "success" },
  Inactive: { label: "Неактивна" },
};

/** Names the state of an entry in words, since colour alone must not carry it. */
export function CatalogStatusTag({ status }: { status: CatalogStatus }) {
  const { label, color } = STATUS_TAGS[status];
  return <Tag color={color}>{label}</Tag>;
}

const STATUS_OPTIONS = Object.entries(STATUS_TAGS).map(
  ([value, { label }]) => ({ text: label, value }),
);

/** The status column every catalogue table ends with before its actions; `status` is the status filter in the query params. */
export function entryColumns<T extends CatalogEntry>(
  status?: CatalogStatus,
): TableColumnsType<T> {
  return [
    {
      title: "Статус",
      dataIndex: "status",
      ...choiceFilter("status", status, STATUS_OPTIONS),
      render: (value: CatalogStatus) => <CatalogStatusTag status={value} />,
    },
  ];
}

/** What each catalogue's record is called in the deletion question, in the accusative. */
const KIND_ACCUSATIVE: Record<CatalogSegment, string> = {
  directions: "направление",
  tracks: "трек",
  levels: "уровень",
  categories: "категорию",
  competencies: "компетенцию",
  scales: "шкалу",
};

interface EntryActionsProps {
  segment: CatalogSegment;
  entry: CatalogEntry;
  /** Unit of the track the entry is or belongs to; omitted for an entry that belongs to no unit. */
  unitId?: string;
  /** Opens the edit dialog; omitted for an entry that cannot be edited here. */
  onEdit?: () => void;
  /** Further actions of the row, shown first. */
  children?: ReactNode;
}

/** The row actions of an entry the rights allow: edit, approve a draft, deactivate, return to work, and delete. */
export function EntryActions({
  segment,
  entry,
  unitId,
  onEdit,
  children,
}: EntryActionsProps) {
  const rights = useCatalogRights();
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  const { mutate, isPending } = useMutation({
    mutationFn: (action: CatalogAction) =>
      changeCatalogStatus(segment, entry, action),
    onError: (failure) => message.error(describeApiError(failure)),
    onSettled: () => invalidateCatalogs(queryClient),
  });
  const confirmDeletion = useConfirmDeletion();
  const canManage = rights.canManage(unitId);

  return (
    <Space size={0} wrap>
      {children}
      {onEdit && rights.canEdit(entry, unitId) && (
        <Button type="link" size="small" onClick={onEdit}>
          Изменить
        </Button>
      )}
      {canManage && entry.status === "Draft" && (
        <Button
          type="link"
          size="small"
          disabled={isPending}
          onClick={() => mutate("approve")}
        >
          Согласовать
        </Button>
      )}
      {canManage && entry.status !== "Inactive" && (
        <Button
          type="link"
          size="small"
          danger
          disabled={isPending}
          onClick={() => mutate("deactivate")}
        >
          Деактивировать
        </Button>
      )}
      {canManage && entry.status === "Inactive" && (
        <Button
          type="link"
          size="small"
          disabled={isPending}
          onClick={() => mutate("activate")}
        >
          Вернуть в работу
        </Button>
      )}
      {rights.canEdit(entry, unitId) && (
        <Button
          type="link"
          size="small"
          danger
          onClick={() =>
            confirmDeletion({
              kind: KIND_ACCUSATIVE[segment],
              name: entry.name,
              content:
                segment === "tracks" ? "Будут удалены и его уровни" : undefined,
              run: () => deleteEntry(segment, entry),
            })
          }
        >
          Удалить
        </Button>
      )}
    </Space>
  );
}
