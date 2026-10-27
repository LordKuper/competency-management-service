import { Flex, Table, type TableColumnsType } from "antd";
import { deskPaths } from "../desk/deskTexts";
import { LinkButton } from "../desk/LinkButton";
import { CompactAlert } from "../desk/ScenarioContext";
import { paths as orgPaths } from "../org-structure/paths";
import { CHANGE_GROUPS, DECISION_GROUPS } from "./importPlan";
import type { OrgImportResult } from "./orgImportApi";

interface Row {
  key: keyof OrgImportResult;
  title: string;
  count: number;
}

const COLUMNS: TableColumnsType<Row> = [
  { title: "Что изменилось", dataIndex: "title" },
  { title: "Число", dataIndex: "count", align: "right", width: "20%" },
];

interface ResultStepProps {
  fileName: string;
  result: OrgImportResult;
}

/** Step 4: what the applied import changed, by group. */
export function ResultStep({ fileName, result }: ResultStepProps) {
  const rows: Row[] = [...CHANGE_GROUPS, ...DECISION_GROUPS].map(
    ({ key, title }) => ({ key, title, count: result[key] }),
  );
  return (
    <Flex vertical gap="middle">
      <CompactAlert
        type="success"
        lead="Импорт применён."
        text={`Изменения из файла «${fileName}» записаны в оргструктуру.`}
      />
      <Table<Row>
        size="small"
        className="desk-table"
        columns={COLUMNS}
        dataSource={rows}
        pagination={false}
      />
      <Flex gap="small" wrap>
        <LinkButton type="primary" to={orgPaths.tree}>
          К оргструктуре
        </LinkButton>
        <LinkButton to={deskPaths.desk}>На рабочий стол</LinkButton>
      </Flex>
    </Flex>
  );
}
