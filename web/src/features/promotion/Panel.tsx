import { Card, Typography, theme } from "antd";
import type { ReactNode } from "react";

interface PanelProps {
  title: ReactNode;
  /** Heading level: 2 for the main panels of a screen, 3 for the ones beside them. */
  level?: 2 | 3;
  /** What stands at the right end of the title row, such as a count. */
  extra?: ReactNode;
  children: ReactNode;
}

/** A bordered panel with a heading, which the screens of a career session are built from. */
export function Panel({ title, level = 2, extra, children }: PanelProps) {
  const { token } = theme.useToken();
  const isMain = level === 2;
  return (
    <Card
      title={
        <Typography.Title
          level={level}
          style={{
            margin: 0,
            whiteSpace: "normal",
            fontSize: isMain ? token.fontSizeHeading3 : token.fontSizeHeading4,
            lineHeight: isMain
              ? token.lineHeightHeading3
              : token.lineHeightHeading4,
          }}
        >
          {title}
        </Typography.Title>
      }
      extra={extra}
    >
      {children}
    </Card>
  );
}
