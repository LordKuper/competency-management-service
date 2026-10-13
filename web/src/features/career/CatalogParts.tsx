import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, type TableColumnsType, Tag } from "antd";
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
import { type RowAction, RowActions } from "./RowActions";
import { useCatalogRights } from "./useCatalogRights";
import { useConfirmDeletion } from "./useConfirmDeletion";
import { KIND_ACCUSATIVE, usePublishEntry } from "./usePublishEntry";

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

interface EntryActionsProps {
  segment: CatalogSegment;
  entry: CatalogEntry;
  /** Unit of the track the entry is or belongs to, whose keepers manage its status; omitted for an entry that belongs to no unit. */
  unitId?: string;
  /** Opens the edit dialog; omitted for an entry that cannot be edited here. */
  onEdit?: () => void;
  /** Further actions of the row, placed first. */
  extra?: RowAction[];
  /** Further actions of the row, placed after «Изменить». */
  afterEdit?: RowAction[];
}

/**
 * The row actions of an entry the rights allow, beyond two behind a «…» menu: edit and delete as the server allows them for the entry, and approving a
 * draft, deactivating and returning to work for the keepers of its unit.
 */
export function EntryActions({
  segment,
  entry,
  unitId,
  onEdit,
  extra = [],
  afterEdit = [],
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
  const { publish, isBusy } = usePublishEntry();
  const canManage = rights.canManage(unitId);

  /** Approving and returning to work go through the publication preview, but a level publishes nothing with it. */
  const approveOrActivate = (action: "approve" | "activate") =>
    segment === "levels"
      ? mutate(action)
      : void publish({ segment, entry }, action);

  const actions: RowAction[] = [
    ...extra,
    ...(onEdit && entry.canEdit
      ? [{ key: "edit", label: "Изменить", onClick: onEdit }]
      : []),
    ...afterEdit,
    ...(canManage && entry.status === "Draft"
      ? [
          {
            key: "approve",
            label: "Согласовать",
            disabled: isPending || isBusy,
            onClick: () => approveOrActivate("approve"),
          },
        ]
      : []),
    ...(canManage && entry.status !== "Inactive"
      ? [
          {
            key: "deactivate",
            label: "Деактивировать",
            danger: true,
            disabled: isPending,
            onClick: () => mutate("deactivate"),
          },
        ]
      : []),
    ...(canManage && entry.status === "Inactive"
      ? [
          {
            key: "activate",
            label: "Вернуть в работу",
            disabled: isPending || isBusy,
            onClick: () => approveOrActivate("activate"),
          },
        ]
      : []),
    ...(entry.canDelete
      ? [
          {
            key: "delete",
            label: "Удалить",
            danger: true,
            onClick: () =>
              confirmDeletion({
                kind: KIND_ACCUSATIVE[segment],
                name: entry.name,
                content:
                  segment === "tracks"
                    ? "Будут удалены и его уровни"
                    : undefined,
                run: () => deleteEntry(segment, entry),
              }),
          },
        ]
      : []),
  ];

  return (
    <RowActions actions={actions} menuLabel={`Действия с «${entry.name}»`} />
  );
}
