import { Flex, Select } from "antd";
import type { RefSelectProps } from "antd/es/select";
import { useRef, useState } from "react";
import { describeApiError } from "../../app/apiErrors";
import { CatalogStatusTag } from "./CatalogParts";
import { IN_WORK, type PickerOption, useCatalogSearch } from "./CatalogSelect";
import { NameModal } from "./NameModal";
import { useCatalogRights } from "./useCatalogRights";

const CREATE_VALUE = "__create__";

interface DirectionSelectProps {
  id?: string;
  value?: string;
  onChange?: (value: string) => void;
}

function DirectionRow({ option }: { option: PickerOption }) {
  return (
    <Flex align="center" gap="small">
      {option.label}
      {option.entry?.status === "Draft" && <CatalogStatusTag status="Draft" />}
    </Flex>
  );
}

/**
 * Choice of a direction with server search as the user types. For users who may add directions, the last option opens
 * the «Новая область» dialog, and the direction it creates is chosen; the dialog and the form around it stay as they are.
 */
export function DirectionSelect({ id, value, onChange }: DirectionSelectProps) {
  const rights = useCatalogRights();
  const { setSearch, options, isFetching, error } = useCatalogSearch(
    "directions",
    value ?? undefined,
    IN_WORK,
  );
  const [isCreating, setIsCreating] = useState(false);
  const field = useRef<RefSelectProps>(null);
  const choices: PickerOption[] = rights.canCreate
    ? [...options, { value: CREATE_VALUE, label: "+ Создать область…" }]
    : options;

  return (
    <>
      <Select<string, PickerOption>
        id={id}
        ref={field}
        value={value}
        onChange={(next) => {
          if (next === CREATE_VALUE) setIsCreating(true);
          else onChange?.(next);
        }}
        showSearch={{ filterOption: false, onSearch: setSearch }}
        onOpenChange={(isOpen) => {
          if (!isOpen) setSearch("");
        }}
        loading={isFetching}
        options={choices}
        optionRender={({ data }) => <DirectionRow option={data} />}
        labelRender={({ value: chosen }) => {
          const option = choices.find((item) => item.value === chosen);
          return option ? <DirectionRow option={option} /> : chosen;
        }}
        notFoundContent={error ? describeApiError(error) : undefined}
      />
      {isCreating && (
        <NameModal
          segment="directions"
          title="Новая область"
          onSaved={(created) => onChange?.(created.id)}
          onClose={() => {
            setIsCreating(false);
            field.current?.focus();
          }}
        />
      )}
    </>
  );
}
