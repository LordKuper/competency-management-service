import {
  Alert,
  Button,
  Checkbox,
  DatePicker,
  Flex,
  Form,
  Tag,
  Typography,
} from "antd";
import dayjs, { type Dayjs } from "dayjs";
import type { ReactNode } from "react";
import { useFormSubmit } from "../../app/useFormSubmit";
import { EDUCATION_LABELS } from "../career/matrixLabels";
import type {
  FormalInput,
  PromotionApplication,
  WaveApplication,
} from "./promotionApi";
import {
  FORMAL_CHECK_FAILED,
  FORMAL_CHECK_PASSED,
  FORMAL_FAILURE_TEXTS,
  formatDate,
  NO_VALUE,
} from "./promotionTexts";
import { RaterEmailsView } from "./RaterEmailsView";

const DATE_FORMAT = "DD.MM.YYYY";
const ISO_DATE = "YYYY-MM-DD";
const FORMAL_FORM_ID = "promotion-formal";

type Failure = PromotionApplication["warnings"][number];

interface FormalFormValues {
  hiredOrPromotedOn: Dayjs;
  ignoreInterval: boolean;
}

interface FormalViewProps {
  application: PromotionApplication;
  /** When the application was filed. */
  submittedAt?: WaveApplication["submittedAt"];
  /** Whether the viewer enters the date now. */
  canEnter: boolean;
  /** Months that must pass since the date; the interval warning shows while typing only when it is known. */
  minIntervalMonths?: number;
  /** The day the results of the session are published, to which the interval is counted; the criterion names it when known. */
  publishesOn?: string;
  /** The day the formal check is fixed, the end of its stage. */
  checkEndsOn?: string;
  /** Saves the date; a refusal that names the date is shown under it, any other one is shown by the caller. */
  onSubmit: (input: FormalInput) => Promise<unknown>;
  /** Enters and confirms the address of a rater without an account; a refusal is shown by the row. */
  onConfirmEmail: (employeeId: string, email: string) => Promise<unknown>;
  /** Opens the next application of the list; omitted when there is none. */
  onNext?: () => void;
}

function Criterion({ text, tag }: { text: string; tag?: ReactNode }) {
  return (
    <Flex
      justify="space-between"
      align="center"
      gap="middle"
      wrap
      style={{ paddingBlock: "var(--ant-padding-xs)" }}
    >
      <span>{text}</span>
      {tag}
    </Flex>
  );
}

function intervalTag(
  failures: readonly Failure[],
  formal: PromotionApplication["formal"],
) {
  if (failures.includes("MissingDate")) {
    return <Tag color="warning">Нет даты</Tag>;
  }
  if (failures.includes("IntervalNotPassed")) {
    return <Tag color="error">Не выполнено</Tag>;
  }
  return formal?.ignoreInterval ? (
    <Tag>Не учитывается</Tag>
  ) : (
    <Tag color="success">Выполнено</Tag>
  );
}

/** The formal check of one application: the candidate and target, what the check would find now, and for the responsible HR the date entry with the interval warning, the criteria and the addresses of the raters. */
export function FormalView({
  application,
  submittedAt,
  canEnter,
  minIntervalMonths,
  publishesOn,
  checkEndsOn,
  onSubmit,
  onConfirmEmail,
  onNext,
}: FormalViewProps) {
  const { target, formal, status, formalCheck, raters } = application;
  const failures = formalCheck?.failures ?? application.warnings;
  const fixedNote = checkEndsOn
    ? ` Итог пересчитывается при каждом сохранении и фиксируется ${formatDate(checkEndsOn)}.`
    : "";
  return (
    <Flex vertical gap="large">
      <Flex vertical>
        <Typography.Title level={2} style={{ margin: 0 }}>
          {application.candidateName}
        </Typography.Title>
        <Typography.Text type="secondary">
          {target.trackName} · {target.levelName}
          {submittedAt && ` · подана ${formatDate(submittedAt)}`}
        </Typography.Text>
      </Flex>
      {formalCheck ? (
        <Alert
          type={formalCheck.isPassed ? "success" : "warning"}
          showIcon
          title={
            formalCheck.isPassed ? FORMAL_CHECK_PASSED : FORMAL_CHECK_FAILED
          }
          description={
            formalCheck.isPassed
              ? fixedNote.trim() || undefined
              : `${formalCheck.failures
                  .map((failure) => FORMAL_FAILURE_TEXTS[failure])
                  .join("; ")}.${fixedNote}`
          }
        />
      ) : (
        application.warnings
          .filter((warning) => !(canEnter && warning === "IntervalNotPassed"))
          .map((warning) => (
            <Alert
              key={warning}
              type="warning"
              showIcon
              title={FORMAL_FAILURE_TEXTS[warning]}
            />
          ))
      )}
      {status === "FormalRejected" && (
        <Alert
          type="error"
          showIcon
          title="Отклонена по формальным признакам"
          description={application.formalFailures
            .map((failure) => FORMAL_FAILURE_TEXTS[failure])
            .join("; ")}
        />
      )}
      <Flex vertical gap="small">
        <Typography.Title level={3} style={{ margin: 0 }}>
          Данные кандидата
        </Typography.Title>
        {canEnter ? (
          <FormalForm
            key={`${formal?.hiredOrPromotedOn}|${formal?.ignoreInterval}`}
            formal={formal}
            minIntervalMonths={minIntervalMonths}
            onSubmit={onSubmit}
            onNext={onNext}
          />
        ) : (
          <>
            <Flex vertical>
              <Typography.Text type="secondary">
                Дата приёма на работу или последнего перехода
              </Typography.Text>
              <span>
                {formal?.hiredOrPromotedOn
                  ? formatDate(formal.hiredOrPromotedOn)
                  : "Не внесена"}
              </span>
            </Flex>
            {formal?.ignoreInterval && (
              <Typography.Text type="secondary">
                Интервал не учитывается
              </Typography.Text>
            )}
            {onNext && (
              <Flex>
                <Button onClick={onNext}>Следующая заявка</Button>
              </Flex>
            )}
          </>
        )}
      </Flex>
      <Flex vertical>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Критерии
        </Typography.Title>
        <Criterion
          text={`Интервал с последнего перехода${publishesOn ? ` на дату публикации решений ${formatDate(publishesOn)}` : ""}${minIntervalMonths === undefined ? "" : ` — не меньше ${minIntervalMonths} мес.`}`}
          tag={intervalTag(failures, formal)}
        />
        <Criterion
          text={`Образование: ${application.education ? EDUCATION_LABELS[application.education] : NO_VALUE}`}
          tag={
            failures.includes("EducationBelowRequired") ? (
              <Tag color="error">Не выполнено</Tag>
            ) : (
              <Tag color="success">Выполнено</Tag>
            )
          }
        />
        {raters && <Criterion text={`Оценивающих: ${raters.length}`} />}
      </Flex>
      {raters?.some((rater) => !rater.hasAccount) && (
        <RaterEmailsView
          key={raters.map((rater) => rater.employeeId).join("|")}
          raters={raters}
          canEdit={canEnter}
          onConfirm={onConfirmEmail}
        />
      )}
    </Flex>
  );
}

interface FormalFormProps {
  formal: PromotionApplication["formal"];
  minIntervalMonths?: number;
  onSubmit: (input: FormalInput) => Promise<unknown>;
  onNext?: () => void;
}

function FormalForm({
  formal,
  minIntervalMonths,
  onSubmit,
  onNext,
}: FormalFormProps) {
  const [form] = Form.useForm<FormalFormValues>();
  const { isSubmitting, submit } = useFormSubmit(form, (values) =>
    onSubmit({
      hiredOrPromotedOn: values.hiredOrPromotedOn.format(ISO_DATE),
      ignoreInterval: values.ignoreInterval,
    }),
  );
  const date = Form.useWatch("hiredOrPromotedOn", form);
  const isIgnored = Form.useWatch("ignoreInterval", form);
  const isIntervalShort =
    date?.add(minIntervalMonths ?? 0, "month").isAfter(dayjs(), "day") === true;

  return (
    <Form
      id={FORMAL_FORM_ID}
      form={form}
      name={FORMAL_FORM_ID}
      layout="vertical"
      disabled={isSubmitting}
      initialValues={{
        hiredOrPromotedOn: formal?.hiredOrPromotedOn
          ? dayjs(formal.hiredOrPromotedOn)
          : undefined,
        ignoreInterval: formal?.ignoreInterval ?? false,
      }}
      onFinish={submit}
    >
      <Form.Item
        name="hiredOrPromotedOn"
        label="Дата приёма на работу или последнего перехода"
        rules={[{ required: true, message: "Укажите дату" }]}
      >
        <DatePicker
          format={DATE_FORMAT}
          disabledDate={(day) => day.isAfter(dayjs(), "day")}
        />
      </Form.Item>
      {isIntervalShort && (
        <Form.Item>
          <Alert
            type="warning"
            showIcon
            title={FORMAL_FAILURE_TEXTS.IntervalNotPassed}
            description={
              isIgnored
                ? "Интервал не будет учтён при проверке."
                : "Чтобы допустить заявку, отметьте «Игнорировать проверку интервала»."
            }
          />
        </Form.Item>
      )}
      <Form.Item name="ignoreInterval" valuePropName="checked">
        <Checkbox>Игнорировать проверку интервала</Checkbox>
      </Form.Item>
      <Flex gap="small" wrap>
        <Button type="primary" htmlType="submit" loading={isSubmitting}>
          Сохранить
        </Button>
        {onNext && (
          <Button disabled={isSubmitting} onClick={onNext}>
            Следующая заявка
          </Button>
        )}
      </Flex>
    </Form>
  );
}
