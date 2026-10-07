import { Button, Empty, Typography } from "antd";
import { useNavigate } from "react-router";

/** Shown for an address no route claims: says what happened, why, and offers the way back. */
export function NotFoundPage() {
  const navigate = useNavigate();
  return (
    <Empty
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={
        <>
          <Typography.Title level={3}>Страница не найдена</Typography.Title>
          <Typography.Paragraph>
            Адрес указан неверно, либо раздел недоступен для вашей учётной
            записи.
          </Typography.Paragraph>
        </>
      }
    >
      <Button type="primary" onClick={() => navigate("/")}>
        Перейти на главную
      </Button>
    </Empty>
  );
}
