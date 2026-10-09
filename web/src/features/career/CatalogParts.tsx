import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  App,
  Button,
  Form,
  Select,
  Space,
  type TableColumnsType,
  Tag,
} from "antd";
import type { ReactNode } from "react";
import { describeApiError } from "../../app/apiErrors";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import {
  type CatalogAction,
  type CatalogEntry,
  type CatalogSegment,
  type CatalogStatus,
  changeCatalogStatus,
  invalidateCatalogs,
} from "./catalogApi";
import { useCatalogRights } from "./useCatalogRights";

const SHARED_OWNER = "Общая";

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

/** The unit that keeps an entry, or the word for a shared one. */
export function ownerLabel(entry: Pick<CatalogEntry, "ownerOrgUnitName">) {
  return entry.ownerOrgUnitName
    ? `Подразделение: ${entry.ownerOrgUnitName}`
    : SHARED_OWNER;
}

/** The status and owner columns every catalogue table ends with before its actions. */
export function entryColumns<T extends CatalogEntry>(): TableColumnsType<T> {
  return [
    {
      title: "Статус",
      dataIndex: "status",
      render: (status: CatalogStatus) => <CatalogStatusTag status={status} />,
    },
    {
      title: "Владелец",
      key: "owner",
      render: (_, entry) => ownerLabel(entry),
    },
  ];
}

interface EntryActionsProps {
  segment: CatalogSegment;
  entry: CatalogEntry;
  /** Opens the edit dialog; omitted for an entry that cannot be edited here. */
  onEdit?: () => void;
  /** Further actions of the row, shown first. */
  children?: ReactNode;
}

/** The row actions of an entry the rights allow: edit, approve a draft, deactivate, and return to work. */
export function EntryActions({
  segment,
  entry,
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
  const canManage = rights.canManage(entry);

  return (
    <Space size={0} wrap>
      {children}
      {onEdit && rights.canEdit(entry) && (
        <Button type="link" size="small" onClick={onEdit}>
          Править
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
    </Space>
  );
}

/**
 * The unit that will keep a new entry. Administrators may leave it empty for a shared entry or pick any unit;
 * heads and HR choose among the units they work for.
 */
export function OwnerFormItem() {
  const rights = useCatalogRights();
  return (
    <Form.Item
      name="ownerOrgUnitId"
      label="Подразделение"
      rules={[{ required: !rights.isAdmin }]}
    >
      {rights.isAdmin ? (
        <OrgUnitSelect allowClear placeholder={SHARED_OWNER} />
      ) : (
        <Select
          options={rights.ownerUnits.map((unit) => ({
            value: unit.id,
            label: unit.name,
          }))}
        />
      )}
    </Form.Item>
  );
}
