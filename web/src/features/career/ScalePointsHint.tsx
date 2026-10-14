import { InfoCircleOutlined } from "@ant-design/icons";
import { Button, Popover, Table, Typography, theme } from "antd";
import { useEffect, useState } from "react";
import type { Scale } from "./catalogApi";

const PANEL_WIDTH_IN_CONTROLS = 9;
const PANEL_HEIGHT_IN_CONTROLS = 8;
const SCORE_WIDTH_IN_CONTROLS = 1.5;

interface ScalePointsHintProps {
  scale: Pick<Scale, "name" | "points">;
  /** Opens the panel while true, as when the arrow keys stand on the option the button sits in. */
  isActive?: boolean;
}

/**
 * The «ⓘ» button of a scale and the panel it opens with the scale's points: on hover and focus, on a tap or Enter and
 * Space. Escape closes only the panel; the dialog and the list around it stay. Pressing the button neither picks the
 * option nor opens the list it sits in.
 */
export function ScalePointsHint({
  scale,
  isActive = false,
}: ScalePointsHintProps) {
  const { token } = theme.useToken();
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    setIsOpen(isActive);
  }, [isActive]);
  useEffect(() => {
    if (!isOpen) return;
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      event.stopPropagation();
      setIsOpen(false);
    };
    document.addEventListener("keydown", closeOnEscape, true);
    return () => document.removeEventListener("keydown", closeOnEscape, true);
  }, [isOpen]);

  const points = [...scale.points].sort((a, b) => a.score - b.score);
  return (
    <Popover
      open={isOpen}
      onOpenChange={setIsOpen}
      trigger={["hover", "focus", "click"]}
      placement="right"
      title={scale.name}
      content={
        points.length === 0 ? (
          <Typography.Text type="secondary">Баллы не заданы</Typography.Text>
        ) : (
          <div
            style={{
              maxWidth: token.controlHeight * PANEL_WIDTH_IN_CONTROLS,
              maxHeight: token.controlHeight * PANEL_HEIGHT_IN_CONTROLS,
              overflowY: "auto",
            }}
          >
            <Table
              size="small"
              rowKey="score"
              pagination={false}
              dataSource={points}
              columns={[
                {
                  title: "Балл",
                  dataIndex: "score",
                  align: "right",
                  width: token.controlHeight * SCORE_WIDTH_IN_CONTROLS,
                },
                { title: "Значение", dataIndex: "label" },
              ]}
            />
          </div>
        )
      }
    >
      <Button
        type="text"
        size="small"
        shape="circle"
        icon={<InfoCircleOutlined />}
        aria-label={`Баллы шкалы «${scale.name}»`}
        onMouseDown={(event) => {
          event.preventDefault();
          event.stopPropagation();
        }}
        onClick={(event) => event.stopPropagation()}
      />
    </Popover>
  );
}
