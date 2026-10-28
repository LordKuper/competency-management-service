import {
  Button,
  Flex,
  Form,
  Select,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useState } from "react";
import { useLeaveGuard } from "../../app/leaveGuard";
import { useFormSubmit } from "../../app/useFormSubmit";
import type {
  Commission,
  CommissionInput,
  CommissionMember,
  Versioned,
} from "./promotionApi";

/** An employee the search offers to add to a commission. */
export interface EmployeeOption {
  id: string;
  name: string;
  /** Unit and position, so namesakes can be told apart. */
  detail: string;
  /** Whether the employee has an account, which a rater without one is invited by e-mail instead. */
  hasAccount?: boolean;
}

interface CommissionFormValues {
  memberEmployeeIds: string[];
  chairEmployeeId?: string;
}

interface CommissionViewProps {
  commission: Versioned<Commission>;
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  isSearching: boolean;
  onSearch: (text: string) => void;
  /** Saves the commission; a refusal that names fields is shown under them, any other one is shown by the caller. */
  onSubmit: (input: CommissionInput) => Promise<unknown>;
}

/** The members of a unit's commission and its chair: those who sit by position cannot be removed, the others are added by search and removed. */
export function CommissionView({
  commission,
  candidates,
  isSearching,
  onSearch,
  onSubmit,
}: CommissionViewProps) {
  const [form] = Form.useForm<CommissionFormValues>();
  const [isDirty, setIsDirty] = useState(false);
  const [addedNames, setAddedNames] = useState<Record<string, string>>({});
  const { isSubmitting, submit } = useFormSubmit(form, async (values) => {
    await onSubmit({
      memberEmployeeIds: values.memberEmployeeIds,
      chairEmployeeId: values.chairEmployeeId ?? null,
    });
    setIsDirty(false);
  });
  useLeaveGuard({ isUnsaved: isDirty, isSaving: isSubmitting });

  const chosenIds = Form.useWatch("memberEmployeeIds", form) ?? [];
  const known = new Map(
    commission.members.map((member) => [member.employeeId, member]),
  );
  const members: CommissionMember[] = [
    ...commission.members.filter((member) => member.isRequired),
    ...chosenIds.map(
      (id) =>
        known.get(id) ?? {
          employeeId: id,
          fullName: addedNames[id] ?? "",
          isRequired: false,
          isResponsibleHr: false,
          isHead: false,
          isActing: true,
        },
    ),
  ];
  const chairOptions = members
    .filter((member) => member.isActing)
    .map((member) => ({ value: member.employeeId, label: member.fullName }));

  return (
    <Form
      form={form}
      name="promotion-commission"
      layout="vertical"
      disabled={isSubmitting}
      initialValues={{
        memberEmployeeIds: commission.members
          .filter((member) => !member.isRequired)
          .map((member) => member.employeeId),
        chairEmployeeId: commission.chairEmployeeId ?? undefined,
      }}
      onFinish={submit}
      onValuesChange={() => setIsDirty(form.isFieldsTouched())}
    >
      <Form.Item name="chairEmployeeId" label="Председатель">
        <Select
          allowClear
          placeholder="Выберите председателя"
          options={chairOptions}
        />
      </Form.Item>
      <Form.Item name="memberEmployeeIds" label="Члены комиссии">
        <MembersField
          members={members}
          candidates={candidates}
          isSearching={isSearching}
          isLocked={isSubmitting}
          onSearch={onSearch}
          onAdded={(option) =>
            setAddedNames((names) => ({ ...names, [option.id]: option.name }))
          }
          onRemoved={(id) => {
            if (form.getFieldValue("chairEmployeeId") === id) {
              form.setFieldValue("chairEmployeeId", undefined);
            }
          }}
        />
      </Form.Item>
      <Button type="primary" htmlType="submit" loading={isSubmitting}>
        Сохранить состав
      </Button>
    </Form>
  );
}

interface MembersFieldProps {
  /** Identifiers of the members that are not required; supplied by the surrounding form item. */
  value?: string[];
  onChange?: (value: string[]) => void;
  /** Everyone shown: the required members first. */
  members: readonly CommissionMember[];
  candidates: readonly EmployeeOption[];
  isSearching: boolean;
  isLocked: boolean;
  onSearch: (text: string) => void;
  onAdded: (option: EmployeeOption) => void;
  onRemoved: (id: string) => void;
}

function MembersField({
  value = [],
  onChange,
  members,
  candidates,
  isSearching,
  isLocked,
  onSearch,
  onAdded,
  onRemoved,
}: MembersFieldProps) {
  const options = candidates
    .filter(
      (candidate) =>
        !members.some((member) => member.employeeId === candidate.id),
    )
    .map((candidate) => ({ value: candidate.id, label: candidate.name }));

  const columns: TableColumnsType<CommissionMember> = [
    {
      title: "Сотрудник",
      key: "name",
      render: (_, member) => (
        <Flex wrap gap="small" align="center">
          <span>{member.fullName}</span>
          {member.isResponsibleHr && <Tag>Ответственный HR</Tag>}
          {member.isHead && <Tag>Руководитель</Tag>}
          {!member.isActing && <Tag color="warning">Не действует</Tag>}
        </Flex>
      ),
    },
    {
      title: "Действия",
      key: "actions",
      width: 1,
      render: (_, member) =>
        member.isRequired ? null : (
          <Button
            type="link"
            danger
            disabled={isLocked}
            aria-label={`Исключить ${member.fullName}`}
            onClick={() => {
              onChange?.(value.filter((id) => id !== member.employeeId));
              onRemoved(member.employeeId);
            }}
          >
            Исключить
          </Button>
        ),
    },
  ];

  return (
    <Flex vertical gap="middle">
      <Select<string | null>
        showSearch={{ filterOption: false, onSearch }}
        aria-label="Добавить члена комиссии"
        placeholder="Добавить: начните вводить ФИО"
        value={null}
        options={options}
        loading={isSearching}
        disabled={isLocked}
        optionRender={(option) => (
          <Flex vertical>
            <span>{option.label}</span>
            <Typography.Text type="secondary">
              {
                candidates.find((candidate) => candidate.id === option.value)
                  ?.detail
              }
            </Typography.Text>
          </Flex>
        )}
        notFoundContent="Сотрудники не найдены. Измените запрос."
        onSelect={(id) => {
          const added = candidates.find((candidate) => candidate.id === id);
          if (!added || id === null) return;
          onAdded(added);
          onChange?.([...value, id]);
        }}
      />
      <Table<CommissionMember>
        rowKey="employeeId"
        columns={columns}
        dataSource={[...members]}
        pagination={false}
        locale={{ emptyText: "В комиссии пока никого нет." }}
      />
    </Flex>
  );
}
