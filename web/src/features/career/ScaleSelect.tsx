import { Flex, Select, type SelectProps, Tag } from "antd";
import { useRef, useState } from "react";
import { describeApiError } from "../../app/apiErrors";
import { CatalogStatusTag } from "./CatalogParts";
import { IN_WORK, type PickerOption, useCatalogSearch } from "./CatalogSelect";
import type { Scale } from "./catalogApi";
import { ScalePointsHint } from "./ScalePointsHint";

interface ScaleSelectProps
  extends Pick<
    SelectProps<string, PickerOption>,
    "id" | "value" | "onChange" | "disabled" | "style" | "aria-label"
  > {}

function ScaleRow({
  option,
  isActive = false,
}: {
  option: PickerOption;
  isActive?: boolean;
}) {
  const scale = option.entry as Scale | undefined;
  return (
    <Flex align="center" justify="space-between" gap="small">
      <Flex align="center" gap="small">
        {option.label}
        {scale?.status === "Draft" && <CatalogStatusTag status="Draft" />}
        {scale?.isDefault && <Tag>По умолчанию</Tag>}
      </Flex>
      {scale && <ScalePointsHint scale={scale} isActive={isActive} />}
    </Flex>
  );
}

/**
 * Choice of a scale with server search as the user types. The selected value and every option carry the «ⓘ» button
 * with the scale's points; the option the arrow keys stand on shows them too.
 */
export function ScaleSelect({
  value,
  disabled,
  style,
  ...props
}: ScaleSelectProps) {
  const { setSearch, options, isFetching, error } = useCatalogSearch(
    "scales",
    value ?? undefined,
    IN_WORK,
    !disabled,
  );
  const [activeValue, setActiveValue] = useState<string>();
  const isKeyboard = useRef(false);

  return (
    <div
      style={style}
      onKeyDownCapture={(event) => {
        isKeyboard.current =
          event.key === "ArrowDown" || event.key === "ArrowUp";
      }}
      onMouseMoveCapture={() => {
        isKeyboard.current = false;
      }}
    >
      <Select<string, PickerOption>
        {...props}
        value={value}
        disabled={disabled}
        style={{ width: "100%" }}
        showSearch={{ filterOption: false, onSearch: setSearch }}
        onOpenChange={(isOpen) => {
          if (isOpen) return;
          setSearch("");
          setActiveValue(undefined);
        }}
        onActive={(next) =>
          setActiveValue(isKeyboard.current ? next : undefined)
        }
        loading={isFetching}
        options={options}
        optionRender={({ data, value: optionValue }) => (
          <ScaleRow option={data} isActive={optionValue === activeValue} />
        )}
        labelRender={({ value: chosen }) => {
          const option = options.find((item) => item.value === chosen);
          return option ? <ScaleRow option={option} /> : chosen;
        }}
        notFoundContent={error ? describeApiError(error) : undefined}
      />
    </div>
  );
}
