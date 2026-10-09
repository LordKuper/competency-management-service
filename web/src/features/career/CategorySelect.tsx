import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Select } from "antd";
import { useState } from "react";
import { describeApiError } from "../../app/apiErrors";
import {
  categoriesQuery,
  invalidateCatalogs,
  saveNamedEntry,
  usableEntries,
} from "./catalogApi";
import { useCatalogRights } from "./useCatalogRights";

const ADD_VALUE = "__add__";

interface CategorySelectProps {
  id?: string;
  value?: string;
  onChange?: (value: string | undefined) => void;
  /** Unit that would keep a category added here; null for a shared one. */
  ownerOrgUnitId: string | null | undefined;
}

/**
 * Searchable choice of a category. When the typed name matches none, the list offers to add it for the competency's
 * unit, and the new category is picked without leaving the form. The server still decides whether the addition is allowed.
 */
export function CategorySelect({
  id,
  value,
  onChange,
  ownerOrgUnitId,
}: CategorySelectProps) {
  const queryClient = useQueryClient();
  const rights = useCatalogRights();
  const { message } = App.useApp();
  const [search, setSearch] = useState("");
  const { data, error, isFetching } = useQuery(categoriesQuery());
  const add = useMutation({
    mutationFn: (name: string) =>
      saveNamedEntry(
        "categories",
        { name, ownerOrgUnitId: ownerOrgUnitId ?? null },
        undefined,
      ),
    onSuccess: (category) => onChange?.(category.id),
    onError: (failure) => message.error(describeApiError(failure)),
    onSettled: () => invalidateCatalogs(queryClient),
  });

  const options = usableEntries(data?.items, value).map((category) => ({
    value: category.id,
    label: category.name,
  }));
  const name = search.trim();
  const isNew =
    name !== "" &&
    !options.some(
      (option) => option.label.toLocaleLowerCase() === name.toLocaleLowerCase(),
    );
  if (isNew && rights.canCreateFor(ownerOrgUnitId ?? null)) {
    options.push({ value: ADD_VALUE, label: `Добавить категорию «${name}»` });
  }

  return (
    <Select
      id={id}
      value={value}
      onChange={(next) =>
        next === ADD_VALUE ? add.mutate(name) : onChange?.(next)
      }
      options={options}
      showSearch={{
        searchValue: search,
        onSearch: setSearch,
        optionFilterProp: "label",
      }}
      onOpenChange={(isOpen) => {
        if (!isOpen) setSearch("");
      }}
      loading={isFetching || add.isPending}
      disabled={add.isPending}
      notFoundContent={error ? describeApiError(error) : undefined}
    />
  );
}
