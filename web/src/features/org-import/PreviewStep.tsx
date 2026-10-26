import { Button, Collapse, Flex, Table, Typography } from "antd";
import { ActionBar, CompactAlert } from "../desk/ScenarioContext";
import { CHANGE_GROUPS } from "./importPlan";
import type { OrgImportPlan } from "./orgImportApi";

const PAGE_SIZE = 50;

interface PreviewStepProps {
  fileName: string;
  plan: OrgImportPlan;
  onBack: () => void;
  onNext: () => void;
}

/** Step 2: what applying the file would change, by group, with the count of each and the lines behind it. */
export function PreviewStep({
  fileName,
  plan,
  onBack,
  onNext,
}: PreviewStepProps) {
  const groups = CHANGE_GROUPS.map(({ key, title }) => ({
    key,
    title,
    lines: plan[key],
  })).filter(({ lines }) => lines.length > 0);
  const total = groups.reduce((sum, { lines }) => sum + lines.length, 0);
  const missing = plan.missingUnits.length + plan.missingEmployees.length;

  return (
    <Flex vertical gap="middle">
      <Typography.Text>{`Файл «${fileName}». Изменений: ${total}.`}</Typography.Text>
      {total === 0 ? (
        <CompactAlert
          type="info"
          lead="Файл ничего не добавляет и не меняет."
          text="Новых подразделений, сотрудников, должностей и совмещений в нём нет."
        />
      ) : (
        <Collapse
          items={groups.map(({ key, title, lines }) => ({
            key,
            label: `${title}: ${lines.length}`,
            children: (
              <Table<{ line: string }>
                size="small"
                className="desk-table"
                showHeader={false}
                rowKey={(_row, index) => index ?? 0}
                columns={[{ dataIndex: "line" }]}
                dataSource={lines.map((line) => ({ line }))}
                pagination={{
                  pageSize: PAGE_SIZE,
                  hideOnSinglePage: true,
                  showSizeChanger: false,
                }}
              />
            ),
          }))}
        />
      )}
      {missing > 0 && (
        <CompactAlert
          type="warning"
          lead="В файле нет части того, что есть в системе."
          text={`Подразделений: ${plan.missingUnits.length}, сотрудников: ${plan.missingEmployees.length}. Решение по каждому — на следующем шаге.`}
        />
      )}
      <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
        <Button type="primary" onClick={onNext}>
          Далее
        </Button>
      </ActionBar>
    </Flex>
  );
}
