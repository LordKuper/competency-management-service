import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Form } from "antd";
import { beforeEach, describe, expect, it } from "vitest";
import { queryClient } from "../../api/queryClient";
import { Providers } from "../../app/Providers";
import { fakeApi, problem } from "../../test/fakeApi";
import { LONG_FLOW_TIMEOUT } from "../../test/timeouts";
import { PositionSelect } from "./PositionSelect";

function entry(positionId: string, name: string, isActive = true) {
  return {
    positionId,
    name,
    isActive,
    orgUnitId: "sales",
    orgUnitName: "Продажи",
  };
}

function serve(
  list = [entry("engineer", "Инженер"), entry("cashier", "Кассир", false)],
) {
  fakeApi.on("GET /api/v1/org-units/sales/positions", () =>
    Response.json(list),
  );
}

function Field({ current }: { current?: { id: string; name: string } }) {
  const [form] = Form.useForm<{ positionId?: string }>();
  const value = Form.useWatch("positionId", form);
  return (
    <Form form={form} initialValues={{ positionId: current?.id }}>
      <Form.Item name="positionId" label="Должность">
        <PositionSelect orgUnitId="sales" current={current} />
      </Form.Item>
      <output data-testid="chosen">{value ?? ""}</output>
    </Form>
  );
}

async function openSelect(current?: { id: string; name: string }) {
  render(
    <Providers>
      <Field current={current} />
    </Providers>,
  );
  const user = userEvent.setup();
  await waitFor(() =>
    expect(fakeApi.calls("GET /api/v1/org-units/sales/positions")).toHaveLength(
      1,
    ),
  );
  return user;
}

beforeEach(() => {
  queryClient.clear();
});

describe("position choice in the employee form (AC-4)", {
  timeout: LONG_FLOW_TIMEOUT,
}, () => {
  it("offers the active positions of the chosen unit, with the placeholder «Выберите должность»", async () => {
    serve();
    const user = await openSelect();

    expect(screen.getByText("Выберите должность")).toBeInTheDocument();
    await user.click(screen.getByRole("combobox", { name: "Должность" }));

    expect(await screen.findByText("Инженер")).toBeInTheDocument();
    expect(screen.queryByText("Кассир")).not.toBeInTheDocument();
  });

  it("keeps listing the position the employee holds even when it is no longer active or allowed, so an unrelated edit keeps it", async () => {
    serve([]);
    const user = await openSelect({ id: "old", name: "Кладовщик" });

    expect(screen.getByText("Кладовщик")).toBeInTheDocument();
    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    expect(screen.getAllByText("Кладовщик").length).toBeGreaterThan(0);
    expect(screen.getByTestId("chosen")).toHaveTextContent("old");
  });

  it("offers «Добавить должность «…»» for a typed name that is not on the list, and picks the position the server returns", async () => {
    serve();
    fakeApi.on("POST /api/v1/org-units/sales/positions/quick-add", () =>
      Response.json({
        id: "new",
        name: "Аналитик",
        isActive: true,
        version: 0,
      }),
    );
    const user = await openSelect();

    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    await user.keyboard("Аналитик");
    await user.click(await screen.findByText("Добавить должность «Аналитик»"));

    await waitFor(() =>
      expect(
        fakeApi.calls("POST /api/v1/org-units/sales/positions/quick-add"),
      ).toHaveLength(1),
    );
    expect(
      await fakeApi
        .calls("POST /api/v1/org-units/sales/positions/quick-add")[0]
        ?.json(),
    ).toEqual({
      name: "Аналитик",
    });
    await waitFor(() =>
      expect(screen.getByTestId("chosen")).toHaveTextContent("new"),
    );
  });

  it("does not offer to add a name that equals a listed position regardless of letter case", async () => {
    serve();
    const user = await openSelect();

    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    await user.keyboard("инженер");

    expect(await screen.findByText("Инженер")).toBeInTheDocument();
    expect(screen.queryByText(/Добавить должность/)).not.toBeInTheDocument();
  });

  it("shows the server's reason when the position cannot be added and keeps the form's choice", async () => {
    serve();
    fakeApi.on("POST /api/v1/org-units/sales/positions/quick-add", () =>
      problem(400, { errors: { name: ["Не длиннее 200 символов"] } }),
    );
    const user = await openSelect();

    await user.click(screen.getByRole("combobox", { name: "Должность" }));
    await user.keyboard("Аналитик");
    await user.click(await screen.findByText("Добавить должность «Аналитик»"));

    expect(
      await screen.findByText("Не длиннее 200 символов"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("chosen")).toHaveTextContent("");
  });
});
