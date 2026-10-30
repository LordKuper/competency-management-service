import { Progress, theme } from "antd";

interface ThresholdBarProps {
  /** The rating as the server gave it, in percent. */
  value: number;
  /** The rating the level asks for, in percent; omitted when the level sets none. */
  threshold: number | null;
  /** What the bar says to a screen reader. */
  label: string;
}

/** A bar filled to a rating with a mark at the threshold the level asks for. */
export function ThresholdBar({ value, threshold, label }: ThresholdBarProps) {
  const { token } = theme.useToken();
  return (
    <div role="img" aria-label={label} style={{ position: "relative" }}>
      <Progress percent={value} showInfo={false} size="small" />
      {threshold !== null && (
        <span
          aria-hidden
          style={{
            position: "absolute",
            top: "50%",
            left: `${threshold}%`,
            height: token.controlHeightXS,
            width: token.lineWidthBold,
            background: token.colorText,
            transform: "translateY(-50%)",
          }}
        />
      )}
    </div>
  );
}
