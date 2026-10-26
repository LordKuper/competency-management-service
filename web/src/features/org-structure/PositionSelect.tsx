import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Select } from "antd";
import { useEffect, useState } from "react";
import { ApiError } from "../../api/ApiError";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import {
  invalidatePositions,
  unitPositionsQuery,
} from "../positions/positionsApi";

/** Value of the list entry that adds the typed name instead of choosing a position; never a position identifier. */
const ADD_VALUE = "add-position";

interface PositionOption {
  value: string;
  label: string;
}

/** A position added from the form, with the unit it was added to. */
interface AddedPosition extends PositionOption {
  orgUnitId: string;
}

interface PositionSelectProps {
  /** Element id that associates the form item label with the control; supplied by the surrounding form item. */
  id?: string;
  /** Identifier of the chosen position; supplied by the surrounding form item. */
  value?: string;
  /** Receives the chosen identifier; supplied by the surrounding form item. */
  onChange?: (value: string | undefined) => void;
  /** The unit chosen in the form, whose own and inherited lists the choice comes from. */
  orgUnitId?: string;
  /** The position the employee holds now, listed by name even when it is no longer allowed or active, so an unrelated edit keeps it; the caller passes it only while the unit is unchanged. */
  current?: { id: string; name: string };
  /** A position chosen before the form was opened, offered under any unit and never cleared by the unit's list. */
  preselected?: { id: string; name: string };
  /** Told when the form adds a position to the unit's list, so a scenario can report it. */
  onAdded?: (position: { id: string; name: string }) => void;
}

/** The server's reason a name was refused, which belongs to the name field the dialog does not show. */
function describeAddFailure(error: unknown): string {
  return error instanceof ApiError && error.errors?.name
    ? error.errors.name.join(" ")
    : describeApiError(error);
}

/**
 * Searchable choice of the active positions allowed in the chosen unit. When the typed name matches none of them, the list
 * offers to add it: the server finds or creates the position, allows it in the unit and the form picks it, without leaving the form.
 * The entry is an ordinary option, so it is reached with the arrow keys like any other. A choice the newly chosen unit does not
 * allow is cleared once its list has loaded. The server still decides whether the choice is allowed.
 */
export function PositionSelect({
  id,
  value,
  onChange,
  orgUnitId,
  current,
  preselected,
  onAdded,
}: PositionSelectProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const [search, setSearch] = useState("");
  const [added, setAdded] = useState<AddedPosition>();
  const { data, error, isFetching } = useQuery({
    ...unitPositionsQuery(orgUnitId ?? ""),
    enabled: orgUnitId !== undefined,
  });
  const quickAdd = useMutation({
    mutationFn: async ({ unitId, name }: { unitId: string; name: string }) =>
      unwrap(
        await api.POST("/api/v1/org-units/{id}/positions/quick-add", {
          params: { path: { id: unitId } },
          body: { name },
        }),
      ),
    onSuccess: (position, { unitId }) => {
      setAdded({ value: position.id, label: position.name, orgUnitId: unitId });
      onChange?.(position.id);
      onAdded?.({ id: position.id, name: position.name });
    },
    onError: (failure) => {
      message.error(describeAddFailure(failure));
    },
    onSettled: () => invalidatePositions(queryClient),
  });

  const options = new Map<string, PositionOption>();
  for (const position of data ?? []) {
    if (position.isActive) {
      options.set(position.positionId, {
        value: position.positionId,
        label: position.name,
      });
    }
  }
  for (const extra of [
    current && { value: current.id, label: current.name },
    preselected && { value: preselected.id, label: preselected.name },
    added?.orgUnitId === orgUnitId && added,
  ]) {
    if (extra && !options.has(extra.value)) options.set(extra.value, extra);
  }
  const name = search.trim();
  const isNew =
    name !== "" &&
    ![...options.values()].some(
      (option) => option.label.toLocaleLowerCase() === name.toLocaleLowerCase(),
    );
  const choices = [...options.values()];
  const isValueOffered = value === undefined || options.has(value);
  useEffect(() => {
    if (data && !isValueOffered) onChange?.(undefined);
  }, [data, isValueOffered, onChange]);
  if (orgUnitId !== undefined && isNew) {
    choices.push({ value: ADD_VALUE, label: `Добавить должность «${name}»` });
  }

  function handleChange(next: string | undefined) {
    if (next !== ADD_VALUE) onChange?.(next);
    else if (orgUnitId !== undefined)
      quickAdd.mutate({ unitId: orgUnitId, name });
  }

  return (
    <Select<string, PositionOption>
      id={id}
      value={value}
      onChange={handleChange}
      options={choices}
      showSearch={{
        searchValue: search,
        onSearch: setSearch,
        optionFilterProp: "label",
      }}
      onOpenChange={(isOpen) => {
        if (!isOpen) setSearch("");
      }}
      loading={isFetching || quickAdd.isPending}
      disabled={quickAdd.isPending || undefined}
      placeholder="Выберите должность"
      notFoundContent={error ? describeApiError(error) : undefined}
    />
  );
}
