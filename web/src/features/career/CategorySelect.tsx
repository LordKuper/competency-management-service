import { useMutation, useQueryClient } from "@tanstack/react-query";
import { App, Select } from "antd";
import { describeApiError } from "../../app/apiErrors";
import { IN_WORK, type PickerOption, useCatalogSearch } from "./CatalogSelect";
import { invalidateCatalogs, saveNamedEntry } from "./catalogApi";
import { useCatalogRights } from "./useCatalogRights";

const ADD_VALUE = "__add__";

interface CategorySelectProps {
  id?: string;
  value?: string;
  onChange?: (value: string | undefined) => void;
}

/**
 * Choice of a category with server search as the user types. When the typed name matches none found, the list offers to add it, and the new category is picked without leaving the form. The server still decides whether the addition is allowed.
 */
export function CategorySelect({ id, value, onChange }: CategorySelectProps) {
  const queryClient = useQueryClient();
  const rights = useCatalogRights();
  const { message } = App.useApp();
  const { search, setSearch, options, isFetching, error } = useCatalogSearch(
    "categories",
    value,
    IN_WORK,
  );
  const add = useMutation({
    mutationFn: (name: string) =>
      saveNamedEntry("categories", { name }, undefined),
    onSuccess: (category) => onChange?.(category.id),
    onError: (failure) => message.error(describeApiError(failure)),
    onSettled: () => invalidateCatalogs(queryClient),
  });

  const name = search.trim();
  const isNew =
    name !== "" &&
    !options.some(
      (option) => option.label.toLocaleLowerCase() === name.toLocaleLowerCase(),
    );
  const choices: PickerOption[] =
    isNew && rights.canCreate
      ? [
          ...options,
          { value: ADD_VALUE, label: `Добавить категорию «${name}»` },
        ]
      : options;

  return (
    <Select<string, PickerOption>
      id={id}
      value={value}
      onChange={(next) =>
        next === ADD_VALUE ? add.mutate(name) : onChange?.(next)
      }
      options={choices}
      showSearch={{
        filterOption: false,
        searchValue: search,
        onSearch: setSearch,
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
