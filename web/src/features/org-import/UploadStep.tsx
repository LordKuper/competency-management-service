import { UploadOutlined } from "@ant-design/icons";
import {
  Alert,
  Button,
  Flex,
  Table,
  type TableColumnsType,
  Typography,
  Upload,
} from "antd";
import { ApiError } from "../../api/ApiError";
import { ErrorAlert } from "../../app/ErrorAlert";
import { ActionBar, CompactAlert, FormColumn } from "../desk/ScenarioContext";
import type { ImportError } from "./orgImportApi";

const PAGE_SIZE = 50;
const TOO_LARGE_STATUS = 413;
const NOT_APPLICABLE = "—";

const COLUMNS: TableColumnsType<ImportError> = [
  {
    title: "Строка",
    dataIndex: "row",
    width: "12%",
    render: (row: number | null) => row ?? NOT_APPLICABLE,
  },
  {
    title: "Колонка",
    dataIndex: "column",
    width: "25%",
    render: (column: string | null) => column ?? NOT_APPLICABLE,
  },
  { title: "Причина", dataIndex: "message" },
];

interface UploadStepProps {
  /** Name of the file whose plan is held; absent until a usable file is loaded. */
  loadedName: string | undefined;
  /** Every problem of the last file read; empty when it was usable. */
  errors: readonly ImportError[];
  /** Why the last file could not be read at all. */
  failure: unknown;
  isLoading: boolean;
  onSelect: (file: File) => void;
  onNext: () => void;
}

/** Step 1: the choice of one `.xlsx` file; the file is read only to preview it, and a file with errors keeps the wizard here. */
export function UploadStep({
  loadedName,
  errors,
  failure,
  isLoading,
  onSelect,
  onNext,
}: UploadStepProps) {
  return (
    <FormColumn>
      <Typography.Paragraph>
        Загрузите выгрузку оргструктуры в формате .xlsx: на первом листе колонки
        «Подразделение 1-го уровня», «Подразделение 2-го уровня» и так далее,
        затем «ФИО», «Должность» и «Юридическое лицо». Изменения применяются
        только после вашего подтверждения.
      </Typography.Paragraph>
      <Flex align="center" gap="small" wrap>
        <Upload
          accept=".xlsx"
          maxCount={1}
          showUploadList={false}
          disabled={isLoading}
          beforeUpload={(file) => {
            onSelect(file);
            return false;
          }}
        >
          <Button icon={<UploadOutlined />} loading={isLoading}>
            {loadedName ? "Выбрать другой файл" : "Выбрать файл .xlsx"}
          </Button>
        </Upload>
        {isLoading && (
          <Typography.Text type="secondary">Файл разбирается…</Typography.Text>
        )}
        {loadedName && !isLoading && (
          <Typography.Text>{`Загружен файл «${loadedName}»`}</Typography.Text>
        )}
      </Flex>
      {failure != null && <LoadFailure failure={failure} />}
      {errors.length > 0 && (
        <Flex vertical gap="small">
          <Alert
            type="error"
            showIcon
            title="Файл не принят"
            description="Исправьте ошибки в файле и загрузите его снова."
          />
          <Table<ImportError>
            size="small"
            className="desk-table"
            rowKey={(_error, index) => index ?? 0}
            columns={COLUMNS}
            dataSource={[...errors]}
            pagination={{
              pageSize: PAGE_SIZE,
              hideOnSinglePage: true,
              showSizeChanger: false,
            }}
          />
        </Flex>
      )}
      {!loadedName && errors.length === 0 && failure == null && !isLoading && (
        <CompactAlert
          type="info"
          lead="Файл ещё не выбран."
          text="Выберите файл, чтобы увидеть, что изменится в оргструктуре."
        />
      )}
      <ActionBar>
        <Button
          type="primary"
          disabled={!loadedName || isLoading}
          onClick={onNext}
        >
          Далее
        </Button>
      </ActionBar>
    </FormColumn>
  );
}

function LoadFailure({ failure }: { failure: unknown }) {
  if (failure instanceof ApiError && failure.status === TOO_LARGE_STATUS) {
    return (
      <CompactAlert
        type="error"
        lead="Файл слишком большой."
        text="Сократите выгрузку или разделите её на части и загрузите снова."
      />
    );
  }
  return <ErrorAlert title="Не удалось прочитать файл" error={failure} />;
}
