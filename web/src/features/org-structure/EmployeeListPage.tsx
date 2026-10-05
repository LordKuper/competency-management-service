import { Breadcrumb, Button, Flex, Space, Typography } from "antd";
import { Link, useNavigate } from "react-router";
import { useIsAdmin } from "../auth/useCurrentUser";
import { EmployeeList } from "./EmployeeList";
import { paths } from "./paths";

/** Everyone in the structure with search and filters; administrators also start the creation of an employee. */
export function EmployeeListPage() {
  const isAdmin = useIsAdmin();
  const navigate = useNavigate();

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Breadcrumb
        items={[
          { title: <Link to={paths.tree}>Оргструктура</Link> },
          { title: "Сотрудники" },
        ]}
      />
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <Typography.Title level={1} style={{ margin: 0 }}>
          Сотрудники
        </Typography.Title>
        {isAdmin && (
          <Button type="primary" onClick={() => navigate(paths.newEmployee)}>
            Создать сотрудника
          </Button>
        )}
      </Flex>
      <EmployeeList />
    </Space>
  );
}
