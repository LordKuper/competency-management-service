import { InfoCircleOutlined } from "@ant-design/icons";
import { Button, Popover, Table, Typography, theme } from "antd";
import { useEffect, useLayoutEffect, useRef, useState } from "react";
import type { Scale } from "./catalogApi";

const PANEL_WIDTH_IN_CONTROLS = 12;
const PANEL_MAX_HEIGHT = "60vh";
const PANEL_MAX_WIDTH_SHARE = 0.9;
const HOLDER = ".ant-select-dropdown, .ant-select";
const SCORE_WIDTH_IN_CONTROLS = 1.5;

interface Side {
  placement: "rightTop" | "leftTop";
  /** Horizontal shift that puts the panel beyond the edge of the field or list holding the button. */
  offset: number;
}

/** The side of the holder with room for the panel, so it covers neither the field nor the list; right when neither has. */
function sideOutside(
  holder: DOMRect,
  button: DOMRect,
  width: number,
  gap: number,
): Side {
  if (window.innerWidth - holder.right >= width + gap) {
    return { placement: "rightTop", offset: holder.right - button.right + gap };
  }
  if (holder.left >= width + gap) {
    return { placement: "leftTop", offset: holder.left - button.left - gap };
  }
  return { placement: "rightTop", offset: 0 };
}

interface ScalePointsHintProps {
  scale: Pick<Scale, "name" | "points">;
  /** Opens the panel while true, as when the arrow keys stand on the option the button sits in. */
  isActive?: boolean;
}

/**
 * The «ⓘ» button of a scale and the panel it opens with the scale's points: on hover and focus, on a tap or Enter and
 * Space. Escape closes only the panel; the dialog and the list around it stay. Pressing the button neither picks the
 * option nor opens the list it sits in. The button stands above the field's search input, which covers the selected value.
 */
export function ScalePointsHint({
  scale,
  isActive = false,
}: ScalePointsHintProps) {
  const { token } = theme.useToken();
  const [isOpen, setIsOpen] = useState(false);
  const button = useRef<HTMLButtonElement>(null);
  const [side, setSide] = useState<Side>({ placement: "rightTop", offset: 0 });
  const panelWidth = token.controlHeight * PANEL_WIDTH_IN_CONTROLS;

  useLayoutEffect(() => {
    const holder = button.current?.closest(HOLDER);
    if (!isOpen || !holder || !button.current) return;
    setSide(
      sideOutside(
        holder.getBoundingClientRect(),
        button.current.getBoundingClientRect(),
        Math.min(panelWidth, window.innerWidth * PANEL_MAX_WIDTH_SHARE),
        token.marginXXS,
      ),
    );
  }, [isOpen, panelWidth, token.marginXXS]);

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
      placement={side.placement}
      align={{ offset: [side.offset, 0] }}
      autoAdjustOverflow={side.offset === 0}
      title={scale.name}
      content={
        points.length === 0 ? (
          <Typography.Text type="secondary">Баллы не заданы</Typography.Text>
        ) : (
          <div
            style={{
              maxWidth: `min(${panelWidth}px, ${PANEL_MAX_WIDTH_SHARE * 100}vw)`,
              maxHeight: PANEL_MAX_HEIGHT,
              overflowY: "auto",
              paddingBottom: token.lineWidth,
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
        ref={button}
        style={{ position: "relative", zIndex: token.zIndexBase + 1 }}
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
