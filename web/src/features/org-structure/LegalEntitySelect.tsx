import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Select } from "antd";
import { useState } from "react";
import { describeApiError, describeNameFailure } from "../../app/apiErrors";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import {
  invalidateLegalEntities,
  type LegalEntity,
  legalEntitiesQuery,
  quickAddLegalEntity,
  SEARCH_TEXT_MAX_LENGTH,
} from "./orgStructureApi";

/** Value of the list entry that adds the typed name instead of choosing a legal entity; never an identifier. */
const ADD_VALUE = "add-legal-entity";

const NO_ENTITIES: readonly LegalEntity[] = [];

interface LegalEntityOption {
  value: string;
  label: string;
}

interface LegalEntitySelectProps {
  /** Element id that associates the form item label with the control; supplied by the surrounding form item. */
  id?: string;
  /** Identifiers of the chosen legal entities; supplied by the surrounding form item. */
  value?: string[];
  /** Receives the chosen identifiers; supplied by the surrounding form item. */
  onChange?: (value: string[]) => void;
  /** The legal entities the employee holds now, listed by name even where the search does not return them. */
  known?: readonly LegalEntity[];
}

/**
 * Searchable multiple choice of legal entities from the catalogue. When the typed name matches none of them, the list offers to
 * add it: the server finds or creates the legal entity and the form picks it, without leaving the form.
 */
export function LegalEntitySelect({
  id,
  value = [],
  onChange,
  known = NO_ENTITIES,
}: LegalEntitySelectProps) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const [search, setSearch] = useState("");
  const [remembered, setRemembered] = useState<ReadonlyMap<string, string>>(
    new Map(),
  );
  const name = search.trim();
  const settled = useDebouncedValue(name);
  const { data, error, isFetching } = useQuery(legalEntitiesQuery(settled));
  const quickAdd = useMutation({
    mutationFn: quickAddLegalEntity,
    onSuccess: (entity) => {
      setRemembered((previous) =>
        new Map(previous).set(entity.id, entity.name),
      );
      if (!value.includes(entity.id)) onChange?.([...value, entity.id]);
      setSearch("");
    },
    onError: (failure) => {
      message.error(describeNameFailure(failure));
    },
    onSettled: () => invalidateLegalEntities(queryClient),
  });

  const options = new Map<string, LegalEntityOption>();
  for (const entity of [...known, ...(data?.items ?? [])]) {
    options.set(entity.id, { value: entity.id, label: entity.name });
  }
  for (const [entityId, label] of remembered) {
    if (!options.has(entityId))
      options.set(entityId, { value: entityId, label });
  }
  const choices = [...options.values()];
  const isNew =
    name !== "" &&
    !choices.some(
      (option) => option.label.toLocaleLowerCase() === name.toLocaleLowerCase(),
    );
  if (isNew) {
    choices.push({
      value: ADD_VALUE,
      label: `Добавить юридическое лицо «${name}»`,
    });
  }

  function handleChange(next: string[]) {
    if (next.includes(ADD_VALUE)) quickAdd.mutate(name);
    else onChange?.(next);
  }

  return (
    <Select<string[], LegalEntityOption>
      id={id}
      mode="multiple"
      value={value}
      onChange={handleChange}
      onSelect={(selected, option) => {
        if (selected !== ADD_VALUE) {
          setRemembered((previous) =>
            new Map(previous).set(selected, option.label),
          );
        }
      }}
      options={choices}
      showSearch={{
        searchValue: search,
        onSearch: (text) => setSearch(text.slice(0, SEARCH_TEXT_MAX_LENGTH)),
        optionFilterProp: "label",
      }}
      onOpenChange={(isOpen) => {
        if (!isOpen) setSearch("");
      }}
      loading={isFetching || quickAdd.isPending}
      disabled={quickAdd.isPending || undefined}
      placeholder="Выберите или добавьте юридическое лицо"
      notFoundContent={error ? describeApiError(error) : undefined}
    />
  );
}
