import { useQuery } from "@tanstack/react-query";
import { Flex, Select, Typography } from "antd";
import { useState } from "react";
import { describeApiError } from "../../app/apiErrors";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { type MatrixPosition, matrixPositionsQuery } from "./matrixApi";

/** Why a position linked to a level of a track cannot have a position matrix, in words; a track the user cannot see is not named. */
export function linkedReason(position: MatrixPosition): string | undefined {
  if (!position.isLinked) return undefined;
  return position.linkedTrack
    ? `Связана с уровнем трека «${position.linkedTrack.name}»`
    : "Связана с уровнем карьерного трека";
}

interface MatrixPositionSelectProps {
  id?: string;
  value?: string;
  orgUnitId?: string;
  /** The name of the position `value` starts with, shown until the list holds it. */
  knownName?: string;
  onChange?: (value: string) => void;
}

/**
 * Choice of the position of a position matrix, searched on the server among the positions of the unit and of those above
 * it. A position linked to a level of a track stays in the list, off, with the reason under its name.
 */
export function MatrixPositionSelect({
  id,
  value,
  orgUnitId,
  knownName,
  onChange,
}: MatrixPositionSelectProps) {
  const [search, setSearch] = useState("");
  const q = useDebouncedValue(search.trim());
  const read = useQuery({
    ...matrixPositionsQuery(orgUnitId ?? "", q),
    enabled: orgUnitId !== undefined,
  });
  const [chosen, setChosen] = useState<MatrixPosition | undefined>(
    value && knownName
      ? { id: value, name: knownName, isLinked: false, linkedTrack: null }
      : undefined,
  );
  const listed = read.data?.items ?? [];
  const items =
    chosen && !listed.some((item) => item.id === chosen.id)
      ? [chosen, ...listed]
      : listed;
  return (
    <Select<string, { value: string; label: string; item: MatrixPosition }>
      id={id}
      value={value}
      disabled={orgUnitId === undefined || undefined}
      loading={read.isFetching}
      placeholder="Выберите должность"
      showSearch={{ filterOption: false, onSearch: setSearch }}
      onOpenChange={(isOpen) => {
        if (!isOpen) setSearch("");
      }}
      options={items.map((item) => ({
        value: item.id,
        label: item.name,
        item,
        disabled: item.isLinked,
      }))}
      optionRender={({ data }) => (
        <Flex vertical>
          <Typography.Text type={data.item.isLinked ? "secondary" : undefined}>
            {data.item.name}
          </Typography.Text>
          {data.item.isLinked && (
            <Typography.Text type="secondary">
              {linkedReason(data.item)}
            </Typography.Text>
          )}
        </Flex>
      )}
      notFoundContent={read.error ? describeApiError(read.error) : undefined}
      onChange={(next) => {
        setChosen(items.find((item) => item.id === next));
        onChange?.(next);
      }}
    />
  );
}
