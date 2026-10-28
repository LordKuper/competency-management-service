import {
  Alert,
  Button,
  Checkbox,
  DatePicker,
  Descriptions,
  Flex,
  Form,
  Tag,
} from "antd";
import dayjs, { type Dayjs } from "dayjs";
import { useFormSubmit } from "../../app/useFormSubmit";
import type { FormalInput, PromotionApplication } from "./promotionApi";
import {
  APPLICATION_STATUS_TAGS,
  FORMAL_CHECK_FAILED,
  FORMAL_CHECK_PASSED,
  FORMAL_FAILURE_TEXTS,
  formatDate,
} from "./promotionTexts";
import { RaterEmailsView } from "./RaterEmailsView";

const DATE_FORMAT = "DD.MM.YYYY";
const ISO_DATE = "YYYY-MM-DD";

interface FormalFormValues {
  hiredOrPromotedOn: Dayjs;
  ignoreInterval: boolean;
}

interface FormalViewProps {
  application: PromotionApplication;
  /** Whether the viewer enters the date now. */
  canEnter: boolean;
  /** Months that must pass since the date; the interval warning shows while typing only when it is known. */
  minIntervalMonths?: number;
  /** Saves the date; a refusal that names the date is shown under it, any other one is shown by the caller. */
  onSubmit: (input: FormalInput) => Promise<unknown>;
  /** Enters and confirms the address of a rater without an account; a refusal is shown by the row. */
  onConfirmEmail: (employeeId: string, email: string) => Promise<unknown>;
}

/** The formal check of one application: the candidate and target, what the check would find now, and for the responsible HR the date entry with the interval warning and the addresses of the raters. */
export function FormalView({
  application,
  canEnter,
  minIntervalMonths,
  onSubmit,
  onConfirmEmail,
}: FormalViewProps) {
  const { target, formal, status, formalCheck, raters } = application;
  return (
    <Flex vertical gap="middle">
      <Descriptions
        column={1}
        size="small"
        items={[
          { label: "Кандидат", children: application.candidateName },
          {
            label: "Цель",
            children: `${target.unitName}: ${target.trackName}, ${target.levelName}`,
          },
          {
            label: "Статус",
            children: (
              <Tag color={APPLICATION_STATUS_TAGS[status].color}>
                {APPLICATION_STATUS_TAGS[status].label}
              </Tag>
            ),
          },
        ]}
      />
      {formalCheck ? (
        <Alert
          type={formalCheck.isPassed ? "success" : "warning"}
          showIcon
          title={
            formalCheck.isPassed ? FORMAL_CHECK_PASSED : FORMAL_CHECK_FAILED
          }
          description={
            formalCheck.isPassed
              ? undefined
              : formalCheck.failures
                  .map((failure) => FORMAL_FAILURE_TEXTS[failure])
                  .join("; ")
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
      {canEnter ? (
        <FormalForm
          key={`${formal?.hiredOrPromotedOn}|${formal?.ignoreInterval}`}
          formal={formal}
          minIntervalMonths={minIntervalMonths}
          onSubmit={onSubmit}
        />
      ) : (
        <Descriptions
          column={1}
          size="small"
          items={[
            {
              label: "Дата приёма или повышения",
              children: formal?.hiredOrPromotedOn
                ? formatDate(formal.hiredOrPromotedOn)
                : "Не внесена",
            },
            ...(formal?.ignoreInterval
              ? [{ label: "Интервал", children: "Не учитывается" }]
              : []),
          ]}
        />
      )}
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
}

function FormalForm({ formal, minIntervalMonths, onSubmit }: FormalFormProps) {
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
      form={form}
      name="promotion-formal"
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
        label="Дата приёма на работу или последнего повышения"
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
      <Button type="primary" htmlType="submit" loading={isSubmitting}>
        Сохранить
      </Button>
    </Form>
  );
}
