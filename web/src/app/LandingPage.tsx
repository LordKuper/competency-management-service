import { Empty, Typography } from "antd";
import { Navigate } from "react-router";
import { useCurrentUser } from "../features/auth/useCurrentUser";
import { navItemsFor } from "./features";

/** Opens the first section the user may see; a user with no section gets a short note on what to do. */
export function LandingPage() {
  const { data: user } = useCurrentUser();
  const first = user && navItemsFor(user.role)[0];
  if (first) return <Navigate to={first.path} replace />;
  return (
    <Empty
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={
        <>
          <Typography.Title level={3}>Добро пожаловать</Typography.Title>
          <Typography.Paragraph>
            Для вашей учётной записи пока нет доступных разделов. Обратитесь к
            администратору.
          </Typography.Paragraph>
        </>
      }
    />
  );
}
