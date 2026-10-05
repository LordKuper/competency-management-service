/** Addresses of the organization-structure screens, so links and routes cannot drift apart. */
export const paths = {
  tree: "/org-structure",
  unit: (unitId: string) => `/org-structure?unit=${unitId}`,
  employees: "/org-structure/employees",
  newEmployee: "/org-structure/employees/new",
  employee: (employeeId: string) => `/org-structure/employees/${employeeId}`,
} as const;
